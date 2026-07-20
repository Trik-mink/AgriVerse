import type { DurableBudgetLedger, BudgetSnapshot } from './budget-ledger.js';
import {
  actualUsageMicroUsd,
  DurableBudgetLedger as FileBudgetLedger,
  estimateReservationMicroUsd,
  MICRO_USD_PER_USD,
} from './budget-ledger.js';
import { ApiError } from './api-error.js';
import { getModelSessionId, getRequestContext } from './request-context.js';

export const ABSOLUTE_AUTHORIZED_OPENAI_USD = 10;
export const DEFAULT_INTERNAL_OPENAI_USD = 9;
export const LATEST_ALLOWED_JUDGE_EXPIRATION =
  new Date('2026-08-07T00:00:00.000Z');

type ModelUsage = {
  input_tokens: number;
  output_tokens: number;
};

type ModelCallResult<Result> = {
  value: Result;
  usage?: ModelUsage | null;
};

type ModelAccessCoordinatorOptions = {
  ledger?: DurableBudgetLedger;
  expiresAt?: Date;
  now?: () => Date;
  globalConcurrency?: number;
  perSessionConcurrency?: number;
};

export class ModelConcurrencyGate {
  private globalInFlight = 0;
  private readonly sessionInFlight = new Map<string, number>();

  public constructor(
    private readonly globalLimit = 2,
    private readonly perSessionLimit = 1,
  ) {
    if (globalLimit < 1 || perSessionLimit < 1 || perSessionLimit > globalLimit) {
      throw new Error('Model concurrency limits are invalid.');
    }
  }

  public acquire(sessionId: string): () => void {
    const sessionCount = this.sessionInFlight.get(sessionId) ?? 0;
    if (this.globalInFlight >= this.globalLimit || sessionCount >= this.perSessionLimit) {
      throw new ApiError(
        429,
        'MODEL_BUSY',
        'The field network is handling another AI request. Wait for it to finish before retrying.',
      );
    }

    this.globalInFlight += 1;
    this.sessionInFlight.set(sessionId, sessionCount + 1);
    let released = false;

    return () => {
      if (released) {
        return;
      }
      released = true;
      this.globalInFlight -= 1;
      const remaining = (this.sessionInFlight.get(sessionId) ?? 1) - 1;
      if (remaining <= 0) {
        this.sessionInFlight.delete(sessionId);
      } else {
        this.sessionInFlight.set(sessionId, remaining);
      }
    };
  }
}

export class ModelAccessCoordinator {
  private readonly now: () => Date;
  private readonly concurrency: ModelConcurrencyGate;

  public constructor(private readonly options: ModelAccessCoordinatorOptions = {}) {
    this.now = options.now ?? (() => new Date());
    this.concurrency = new ModelConcurrencyGate(options.globalConcurrency, options.perSessionConcurrency);
  }

  public async initialize(): Promise<BudgetSnapshot | undefined> {
    return this.options.ledger?.initialize();
  }

  public async execute<Result>(options: {
    sessionId: string;
    reservationInput: unknown;
    maxOutputTokens: number;
    call: () => Promise<ModelCallResult<Result>>;
  }): Promise<Result> {
    this.assertWithinJudgeWindow();
    const release = this.concurrency.acquire(options.sessionId);
    let reservation: Awaited<ReturnType<DurableBudgetLedger['reserve']>> | undefined;

    try {
      if (this.options.ledger) {
        const amount = estimateReservationMicroUsd(options.reservationInput, options.maxOutputTokens);
        reservation = await this.options.ledger.reserve(amount);
        const snapshot = await this.options.ledger.snapshot();
        logBudgetEvent('model_budget_reserved', {
          reserved_micro_usd: amount,
          remaining_micro_usd: snapshot.remainingMicroUsd,
        });
      }

      let result: ModelCallResult<Result>;
      try {
        result = await options.call();
      } catch (error) {
        if (reservation && this.options.ledger) {
          const snapshot = await this.options.ledger.retainFailedReservation(reservation.id);
          logBudgetEvent('model_budget_failure_retained', {
            reserved_micro_usd: reservation.reservedMicroUsd,
            remaining_micro_usd: snapshot.remainingMicroUsd,
          });
        }
        throw error;
      }

      if (reservation && this.options.ledger) {
        const actual = result.usage
          ? actualUsageMicroUsd(result.usage)
          : reservation.reservedMicroUsd;
        const snapshot = await this.options.ledger.reconcile(reservation.id, actual);
        logBudgetEvent('model_budget_reconciled', {
          actual_micro_usd: actual,
          remaining_micro_usd: snapshot.remainingMicroUsd,
        });
      }

      return result.value;
    } finally {
      release();
    }
  }

  public snapshot(): Promise<BudgetSnapshot | undefined> {
    return this.options.ledger?.snapshot() ?? Promise.resolve(undefined);
  }

  private assertWithinJudgeWindow(): void {
    const expiresAt = this.options.expiresAt;
    if (expiresAt && this.now().getTime() >= expiresAt.getTime()) {
      throw new ApiError(
        503,
        'JUDGE_ACCESS_EXPIRED',
        'The hosted judging window has ended. No OpenAI request was made.',
      );
    }
  }
}

let defaultCoordinator: ModelAccessCoordinator | undefined;

export function getJudgeAccessExpiration(): Date | undefined {
  const configured = process.env.JUDGE_ACCESS_EXPIRES_AT?.trim();
  if (!configured) {
    return undefined;
  }

  const parsed = new Date(configured);
  if (!Number.isFinite(parsed.getTime())) {
    throw new Error('JUDGE_ACCESS_EXPIRES_AT must be an ISO-8601 timestamp.');
  }
  if (parsed.getTime() > LATEST_ALLOWED_JUDGE_EXPIRATION.getTime()) {
    throw new Error(
      'JUDGE_ACCESS_EXPIRES_AT cannot extend beyond August 6, 2026.',
    );
  }
  return parsed;
}

export function assertJudgeAccessOpen(now = new Date()): void {
  const expiration = getJudgeAccessExpiration();
  if (process.env.NODE_ENV === 'production' && !expiration) {
    throw new ApiError(503, 'SERVICE_NOT_READY', 'The hosted judge AI service is not configured.');
  }
  if (expiration && now.getTime() >= expiration.getTime()) {
    throw new ApiError(503, 'JUDGE_ACCESS_EXPIRED', 'The hosted judging window has ended. No OpenAI request was made.');
  }
}

export function getDefaultModelAccess(): ModelAccessCoordinator {
  if (!defaultCoordinator) {
    defaultCoordinator = createCoordinatorFromEnvironment();
  }
  return defaultCoordinator;
}

export async function initializeModelAccess(): Promise<BudgetSnapshot | undefined> {
  return getDefaultModelAccess().initialize();
}

export async function runProtectedModelCall<Result>(options: {
  reservationInput: unknown;
  maxOutputTokens: number;
  call: () => Promise<ModelCallResult<Result>>;
}): Promise<Result> {
  return getDefaultModelAccess().execute({
    ...options,
    sessionId: getModelSessionId(),
  });
}

function createCoordinatorFromEnvironment(): ModelAccessCoordinator {
  const expiresAt = getJudgeAccessExpiration();
  const globalConcurrency = parseBoundedInteger(process.env.MODEL_GLOBAL_CONCURRENCY, 2, 1, 4);
  const perSessionConcurrency = parseBoundedInteger(process.env.MODEL_SESSION_CONCURRENCY, 1, 1, globalConcurrency);

  if (process.env.NODE_ENV !== 'production') {
    return new ModelAccessCoordinator({ expiresAt, globalConcurrency, perSessionConcurrency });
  }

  if (!expiresAt) {
    throw new Error('JUDGE_ACCESS_EXPIRES_AT is required in production.');
  }
  if (!process.env.OPENAI_API_KEY?.trim()) {
    throw new Error('OPENAI_API_KEY is required in production.');
  }
  if ((process.env.OPENAI_MODEL?.trim() || 'gpt-5.6') !== 'gpt-5.6') {
    throw new Error('The production judge service must use the approved gpt-5.6 model.');
  }

  const filePath = process.env.BUDGET_LEDGER_PATH?.trim();
  const bootstrapUntilRaw = process.env.BUDGET_LEDGER_BOOTSTRAP_UNTIL?.trim();
  if (!filePath || !bootstrapUntilRaw) {
    throw new Error('Durable budget ledger configuration is required in production.');
  }

  const bootstrapUntil = new Date(bootstrapUntilRaw);
  if (!Number.isFinite(bootstrapUntil.getTime())) {
    throw new Error('BUDGET_LEDGER_BOOTSTRAP_UNTIL must be an ISO-8601 timestamp.');
  }
  if (bootstrapUntil.getTime() >= expiresAt.getTime()) {
    throw new Error(
      'BUDGET_LEDGER_BOOTSTRAP_UNTIL must precede the judge-access expiration.',
    );
  }

  const configuredUsd = Number(process.env.OPENAI_INTERNAL_BUDGET_USD ?? DEFAULT_INTERNAL_OPENAI_USD);
  if (!Number.isFinite(configuredUsd) || configuredUsd <= 0 || configuredUsd > DEFAULT_INTERNAL_OPENAI_USD) {
    throw new Error(`OPENAI_INTERNAL_BUDGET_USD must be greater than zero and no more than ${DEFAULT_INTERNAL_OPENAI_USD}.`);
  }

  const capMicroUsd = Math.floor(configuredUsd * MICRO_USD_PER_USD);
  const ledger = new FileBudgetLedger({
    filePath,
    capMicroUsd,
    allowCreateUntil: bootstrapUntil,
  });

  return new ModelAccessCoordinator({
    ledger,
    expiresAt,
    globalConcurrency,
    perSessionConcurrency,
  });
}

function parseBoundedInteger(raw: string | undefined, fallback: number, minimum: number, maximum: number): number {
  if (!raw) {
    return fallback;
  }
  const parsed = Number(raw);
  if (!Number.isInteger(parsed) || parsed < minimum || parsed > maximum) {
    throw new Error(`Concurrency value must be an integer from ${minimum} through ${maximum}.`);
  }
  return parsed;
}

function logBudgetEvent(event: string, values: Record<string, number>): void {
  const context = getRequestContext();
  console.info(
    JSON.stringify({
      event,
      request_id: context?.requestId,
      route: context?.route,
      ...values,
    }),
  );
}
