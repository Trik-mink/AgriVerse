import { createHash, randomUUID } from 'node:crypto';
import { mkdir, open, readFile, rename, rm } from 'node:fs/promises';
import { dirname } from 'node:path';

import { z } from 'zod';

import { ApiError } from './api-error.js';

export const MICRO_USD_PER_USD = 1_000_000;
export const GPT_5_6_INPUT_RESERVATION_MICRO_USD_PER_MILLION = 6_250_000;
export const GPT_5_6_OUTPUT_MICRO_USD_PER_MILLION = 30_000_000;
export const MAX_SHORT_CONTEXT_INPUT_TOKEN_UPPER_BOUND = 240_000;

const ReservationSchema = z
  .object({
    amount_micro_usd: z.number().int().positive(),
    created_at: z.string().datetime(),
  })
  .strict();

const LedgerPayloadSchema = z
  .object({
    version: z.literal(1),
    cap_micro_usd: z.number().int().positive(),
    charged_micro_usd: z.number().int().nonnegative(),
    completed_calls: z.number().int().nonnegative(),
    failed_calls: z.number().int().nonnegative(),
    active_reservations: z.record(ReservationSchema),
    updated_at: z.string().datetime(),
  })
  .strict();

const LedgerFileSchema = z
  .object({
    payload: LedgerPayloadSchema,
    sha256: z.string().regex(/^[a-f0-9]{64}$/),
  })
  .strict();

type LedgerPayload = z.infer<typeof LedgerPayloadSchema>;

export type BudgetReservation = {
  id: string;
  reservedMicroUsd: number;
};

export type BudgetSnapshot = {
  capMicroUsd: number;
  chargedMicroUsd: number;
  remainingMicroUsd: number;
  activeReservations: number;
  completedCalls: number;
  failedCalls: number;
};

type DurableBudgetLedgerOptions = {
  filePath: string;
  capMicroUsd: number;
  allowCreateUntil?: Date;
  now?: () => Date;
};

function checksum(payload: LedgerPayload): string {
  return createHash('sha256').update(JSON.stringify(payload)).digest('hex');
}

function unavailable(message = 'The judge budget ledger is unavailable.'): ApiError {
  return new ApiError(503, 'BUDGET_LEDGER_UNAVAILABLE', message);
}

export class DurableBudgetLedger {
  private operationQueue: Promise<void> = Promise.resolve();
  private readonly now: () => Date;

  public constructor(private readonly options: DurableBudgetLedgerOptions) {
    this.now = options.now ?? (() => new Date());
    if (!Number.isSafeInteger(options.capMicroUsd) || options.capMicroUsd <= 0) {
      throw new Error('The budget cap must be a positive integer number of micro-US dollars.');
    }
  }

  public initialize(): Promise<BudgetSnapshot> {
    return this.withLock(async () => this.toSnapshot(await this.loadOrCreate()));
  }

  public reserve(amountMicroUsd: number): Promise<BudgetReservation> {
    if (!Number.isSafeInteger(amountMicroUsd) || amountMicroUsd <= 0) {
      return Promise.reject(new Error('A budget reservation must be a positive integer number of micro-US dollars.'));
    }

    return this.withLock(async () => {
      const payload = await this.loadOrCreate();
      if (payload.charged_micro_usd + amountMicroUsd > payload.cap_micro_usd) {
        throw new ApiError(
          503,
          'BUDGET_EXHAUSTED',
          'The hosted judge AI budget is exhausted. No OpenAI request was made.',
        );
      }

      const id = randomUUID();
      payload.charged_micro_usd += amountMicroUsd;
      payload.active_reservations[id] = {
        amount_micro_usd: amountMicroUsd,
        created_at: this.now().toISOString(),
      };
      payload.updated_at = this.now().toISOString();
      await this.write(payload);

      return { id, reservedMicroUsd: amountMicroUsd };
    });
  }

  public reconcile(reservationId: string, actualMicroUsd: number): Promise<BudgetSnapshot> {
    if (!Number.isSafeInteger(actualMicroUsd) || actualMicroUsd < 0) {
      return Promise.reject(new Error('Actual usage must be a non-negative integer number of micro-US dollars.'));
    }

    return this.withLock(async () => {
      const payload = await this.loadOrCreate();
      const reservation = payload.active_reservations[reservationId];
      if (!reservation) {
        throw unavailable('The budget reservation could not be reconciled safely.');
      }

      payload.charged_micro_usd += actualMicroUsd - reservation.amount_micro_usd;
      delete payload.active_reservations[reservationId];
      payload.completed_calls += 1;
      payload.updated_at = this.now().toISOString();
      await this.write(payload);

      if (payload.charged_micro_usd > payload.cap_micro_usd) {
        throw unavailable('Actual model usage exceeded its conservative reservation; further AI access is disabled.');
      }

      return this.toSnapshot(payload);
    });
  }

  public retainFailedReservation(reservationId: string): Promise<BudgetSnapshot> {
    return this.withLock(async () => {
      const payload = await this.loadOrCreate();
      if (!payload.active_reservations[reservationId]) {
        throw unavailable('The failed budget reservation could not be finalized safely.');
      }

      delete payload.active_reservations[reservationId];
      payload.failed_calls += 1;
      payload.updated_at = this.now().toISOString();
      await this.write(payload);
      return this.toSnapshot(payload);
    });
  }

  public snapshot(): Promise<BudgetSnapshot> {
    return this.withLock(async () => this.toSnapshot(await this.loadOrCreate()));
  }

  private withLock<Result>(operation: () => Promise<Result>): Promise<Result> {
    const result = this.operationQueue.then(operation, operation);
    this.operationQueue = result.then(
      () => undefined,
      () => undefined,
    );
    return result;
  }

  private async loadOrCreate(): Promise<LedgerPayload> {
    let raw: string;
    try {
      raw = await readFile(this.options.filePath, 'utf8');
    } catch (error) {
      if (!isMissingFile(error)) {
        throw unavailable();
      }

      const allowCreateUntil = this.options.allowCreateUntil;
      if (!allowCreateUntil || this.now().getTime() > allowCreateUntil.getTime()) {
        throw unavailable('The durable budget ledger is missing; AI access is disabled.');
      }

      const payload: LedgerPayload = {
        version: 1,
        cap_micro_usd: this.options.capMicroUsd,
        charged_micro_usd: 0,
        completed_calls: 0,
        failed_calls: 0,
        active_reservations: {},
        updated_at: this.now().toISOString(),
      };
      await this.write(payload);
      return payload;
    }

    try {
      const ledgerFile = LedgerFileSchema.parse(JSON.parse(raw));
      if (ledgerFile.sha256 !== checksum(ledgerFile.payload)) {
        throw new Error('Checksum mismatch.');
      }
      if (ledgerFile.payload.cap_micro_usd !== this.options.capMicroUsd) {
        throw new Error('Configured cap does not match the durable ledger.');
      }
      return ledgerFile.payload;
    } catch {
      throw unavailable('The durable budget ledger failed its integrity check; AI access is disabled.');
    }
  }

  private async write(payload: LedgerPayload): Promise<void> {
    const directory = dirname(this.options.filePath);
    const temporaryPath = `${this.options.filePath}.tmp-${process.pid}-${randomUUID()}`;
    const serialized = `${JSON.stringify({ payload, sha256: checksum(payload) })}\n`;

    await mkdir(directory, { recursive: true });
    try {
      const handle = await open(temporaryPath, 'wx', 0o600);
      try {
        await handle.writeFile(serialized, 'utf8');
        await handle.sync();
      } finally {
        await handle.close();
      }
      await rename(temporaryPath, this.options.filePath);
      const directoryHandle = await open(directory, 'r');
      try {
        await directoryHandle.sync();
      } finally {
        await directoryHandle.close();
      }
    } catch (error) {
      await rm(temporaryPath, { force: true }).catch(() => undefined);
      if (error instanceof ApiError) {
        throw error;
      }
      throw unavailable();
    }
  }

  private toSnapshot(payload: LedgerPayload): BudgetSnapshot {
    return {
      capMicroUsd: payload.cap_micro_usd,
      chargedMicroUsd: payload.charged_micro_usd,
      remainingMicroUsd: Math.max(0, payload.cap_micro_usd - payload.charged_micro_usd),
      activeReservations: Object.keys(payload.active_reservations).length,
      completedCalls: payload.completed_calls,
      failedCalls: payload.failed_calls,
    };
  }
}

function isMissingFile(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'code' in error && error.code === 'ENOENT';
}

export function estimateReservationMicroUsd(input: unknown, maxOutputTokens: number): number {
  if (!Number.isSafeInteger(maxOutputTokens) || maxOutputTokens <= 0) {
    throw new Error('maxOutputTokens must be a positive integer.');
  }

  const serializedInput = JSON.stringify(input);
  const inputTokenUpperBound = Buffer.byteLength(serializedInput, 'utf8') + 512;
  if (inputTokenUpperBound > MAX_SHORT_CONTEXT_INPUT_TOKEN_UPPER_BOUND) {
    throw new ApiError(
      422,
      'MODEL_INPUT_TOO_LARGE',
      'The request is too large for the hosted judge AI service.',
    );
  }
  const inputCost = Math.ceil(
    (inputTokenUpperBound * GPT_5_6_INPUT_RESERVATION_MICRO_USD_PER_MILLION) / 1_000_000,
  );
  const outputCost = Math.ceil(
    (maxOutputTokens * GPT_5_6_OUTPUT_MICRO_USD_PER_MILLION) / 1_000_000,
  );
  return inputCost + outputCost;
}

export function actualUsageMicroUsd(usage: { input_tokens: number; output_tokens: number }): number {
  const inputCost = Math.ceil(
    (usage.input_tokens * GPT_5_6_INPUT_RESERVATION_MICRO_USD_PER_MILLION) / 1_000_000,
  );
  const outputCost = Math.ceil(
    (usage.output_tokens * GPT_5_6_OUTPUT_MICRO_USD_PER_MILLION) / 1_000_000,
  );
  return inputCost + outputCost;
}
