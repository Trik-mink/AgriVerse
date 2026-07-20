import { randomUUID } from 'node:crypto';

import cors from 'cors';
import express from 'express';
import rateLimit from 'express-rate-limit';
import helmet from 'helmet';
import { ZodError, type ZodType } from 'zod';

import { toApiError, ApiError } from './api-error.js';
import { getJudgeAccessExpiration, ModelConcurrencyGate } from './model-access.js';
import {
  generatePolicyBrief,
  grade,
  GraderInputSchema,
  PolicyBriefInputSchema,
  respondAsStakeholder,
  simulate,
  SimulationInputSchema,
  StakeholderMessageInputSchema,
} from './runtime.js';
import {
  JUDGE_SESSION_HEADER,
  getRequestContext,
  runWithRequestContext,
  setRequestSession,
} from './request-context.js';
import { getPublicScenario } from './scenario-loader.js';

const DEFAULT_ALLOWED_ORIGINS = [
  'http://localhost:5173',
  'http://127.0.0.1:5173',
  'http://localhost:8000',
  'http://127.0.0.1:8000',
] as const;

const SIX_HOURS_MS = 6 * 60 * 60 * 1000;
const ONE_HOUR_MS = 60 * 60 * 1000;
const FIFTEEN_MINUTES_MS = 15 * 60 * 1000;
const JUDGE_SESSION_PATTERN = /^[A-Za-z0-9_-]{16,64}$/;

type RuntimeHandlers = {
  stakeholder: typeof respondAsStakeholder;
  simulation: typeof simulate;
  feedback: typeof grade;
  policyBrief: typeof generatePolicyBrief;
};

type RequestLimits = {
  generalPerFifteenMinutes: number;
  expensivePerIpHour: number;
  expensivePerSession: number;
  stakeholderPerSession: number;
  simulationPerSession: number;
  feedbackPerSession: number;
  policyBriefPerSession: number;
};

export type CreateAppOptions = {
  allowedOrigins?: readonly string[];
  now?: () => Date;
  judgeAccessExpiresAt?: Date;
  requestLimits?: Partial<RequestLimits>;
  runtime?: Partial<RuntimeHandlers>;
  trustProxy?: boolean | number;
};

const DEFAULT_REQUEST_LIMITS: RequestLimits = {
  generalPerFifteenMinutes: 80,
  expensivePerIpHour: 30,
  expensivePerSession: 14,
  stakeholderPerSession: 6,
  simulationPerSession: 3,
  feedbackPerSession: 3,
  policyBriefPerSession: 2,
};

export function getAllowedOrigins(configuredOrigins = process.env.CORS_ALLOWED_ORIGINS): string[] {
  const origins = configuredOrigins?.split(',') ?? [...DEFAULT_ALLOWED_ORIGINS];
  return [...new Set(origins.map((origin) => origin.trim()).filter(Boolean))];
}

export function createApp(options: CreateAppOptions = {}) {
  const allowedOrigins = new Set(options.allowedOrigins ?? getAllowedOrigins());
  const now = options.now ?? (() => new Date());
  const expiration = options.judgeAccessExpiresAt ?? getJudgeAccessExpiration();
  const limits = { ...DEFAULT_REQUEST_LIMITS, ...options.requestLimits };
  const runtime: RuntimeHandlers = {
    stakeholder: options.runtime?.stakeholder ?? respondAsStakeholder,
    simulation: options.runtime?.simulation ?? simulate,
    feedback: options.runtime?.feedback ?? grade,
    policyBrief: options.runtime?.policyBrief ?? generatePolicyBrief,
  };
  const httpGlobalConcurrency = parseBoundedLimit(
    process.env.HTTP_MODEL_GLOBAL_CONCURRENCY,
    2,
    1,
    4,
  );
  const requestConcurrency = new ModelConcurrencyGate(
    httpGlobalConcurrency,
    parseBoundedLimit(
      process.env.HTTP_MODEL_SESSION_CONCURRENCY,
      1,
      1,
      httpGlobalConcurrency,
    ),
  );
  const app = express();

  app.disable('x-powered-by');
  app.set('trust proxy', options.trustProxy ?? (process.env.RENDER === 'true' ? 1 : false));
  app.use(helmet());
  app.use((request, response, next) => {
    const requestId = randomUUID();
    const startedAt = performance.now();
    response.setHeader('X-Request-Id', requestId);
    response.on('finish', () => {
      const matchedRoute = request.route?.path;
      console.info(
        JSON.stringify({
          event: 'http_request',
          request_id: requestId,
          method: request.method,
          route: typeof matchedRoute === 'string' ? matchedRoute : safeRouteName(request.path),
          status: response.statusCode,
          latency_ms: Math.round(performance.now() - startedAt),
        }),
      );
    });
    runWithRequestContext({ requestId, route: safeRouteName(request.path) }, next);
  });
  app.use(
    cors({
      origin(origin, callback) {
        callback(null, origin === undefined || allowedOrigins.has(origin));
      },
      methods: ['GET', 'POST', 'OPTIONS'],
      allowedHeaders: ['Content-Type', 'X-AgriVerse-Session'],
      exposedHeaders: ['X-Request-Id', 'RateLimit', 'RateLimit-Policy', 'Retry-After'],
      maxAge: 600,
    }),
  );
  app.use(express.json({ limit: '128kb', strict: true }));
  app.use('/api', (_request, response, next) => {
    response.setHeader('Cache-Control', 'no-store');
    next();
  });
  app.use('/assets', express.static('public/assets', { fallthrough: false, maxAge: '1h' }));
  app.use(
    '/api',
    createLimiter({
      windowMs: FIFTEEN_MINUTES_MS,
      limit: limits.generalPerFifteenMinutes,
      keyPrefix: 'api',
    }),
  );

  app.get('/health', (_request, response) => {
    const hasRequiredProductionConfig =
      process.env.NODE_ENV !== 'production' ||
      Boolean(
        process.env.OPENAI_API_KEY &&
          process.env.BUDGET_LEDGER_PATH &&
          process.env.BUDGET_LEDGER_BOOTSTRAP_UNTIL &&
          expiration,
      );
    const expired = Boolean(expiration && now().getTime() >= expiration.getTime());
    response.status(hasRequiredProductionConfig ? 200 : 503).json({
      status: hasRequiredProductionConfig ? 'ok' : 'not_ready',
      service: 'agriverse-api',
      version:
        process.env.DEPLOYED_COMMIT?.trim() ||
        process.env.RENDER_GIT_COMMIT?.trim() ||
        'local',
      ai_access: !hasRequiredProductionConfig ? 'not_configured' : expired ? 'expired' : 'ready',
    });
  });

  app.get('/api/scenario', (_request, response) => {
    response.status(200).json(getPublicScenario());
  });

  const expensiveIpLimiter = createLimiter({
    windowMs: ONE_HOUR_MS,
    limit: limits.expensivePerIpHour,
    keyPrefix: 'expensive-ip',
  });
  const expensiveSessionLimiter = createLimiter({
    windowMs: SIX_HOURS_MS,
    limit: limits.expensivePerSession,
    keyPrefix: 'expensive-session',
    sessionKey: true,
  });
  const expensivePrelude = [
    requireJudgeSession,
    assertJudgeWindow(expiration, now),
    expensiveIpLimiter,
    expensiveSessionLimiter,
  ] as const;

  app.post(
    '/api/stakeholders/:stakeholderId/messages',
    ...expensivePrelude,
    createLimiter({
      windowMs: SIX_HOURS_MS,
      limit: limits.stakeholderPerSession,
      keyPrefix: 'stakeholder',
      sessionKey: true,
    }),
    expensiveHandler(requestConcurrency, StakeholderMessageInputSchema, async (request, input) => {
      const stakeholderId = request.params.stakeholderId;
      if (typeof stakeholderId !== 'string' || !/^[A-Za-z0-9_-]{1,80}$/.test(stakeholderId)) {
        throw new ApiError(422, 'VALIDATION_ERROR', 'The stakeholder identifier is invalid.');
      }
      return { message: await runtime.stakeholder(stakeholderId, input) };
    }),
  );

  app.post(
    '/api/simulations',
    ...expensivePrelude,
    createLimiter({
      windowMs: SIX_HOURS_MS,
      limit: limits.simulationPerSession,
      keyPrefix: 'simulation',
      sessionKey: true,
    }),
    expensiveHandler(requestConcurrency, SimulationInputSchema, (_request, input) => runtime.simulation(input)),
  );

  app.post(
    '/api/feedback',
    ...expensivePrelude,
    createLimiter({
      windowMs: SIX_HOURS_MS,
      limit: limits.feedbackPerSession,
      keyPrefix: 'feedback',
      sessionKey: true,
    }),
    expensiveHandler(requestConcurrency, GraderInputSchema, (_request, input) => runtime.feedback(input)),
  );

  app.post(
    '/api/policy-briefs',
    ...expensivePrelude,
    createLimiter({
      windowMs: SIX_HOURS_MS,
      limit: limits.policyBriefPerSession,
      keyPrefix: 'policy-brief',
      sessionKey: true,
    }),
    expensiveHandler(requestConcurrency, PolicyBriefInputSchema, (_request, input) => runtime.policyBrief(input)),
  );

  app.use((error: unknown, _request: express.Request, response: express.Response, _next: express.NextFunction) => {
    if (isEntityTooLarge(error)) {
      response.status(413).json({
        error: { code: 'PAYLOAD_TOO_LARGE', message: 'The request is larger than the service accepts.' },
      });
      return;
    }

    if (isMalformedJson(error)) {
      response.status(400).json({
        error: { code: 'MALFORMED_JSON', message: 'The request body must be valid JSON.' },
      });
      return;
    }

    if (error instanceof ZodError) {
      response.status(422).json({
        error: { code: 'VALIDATION_ERROR', message: 'The request data is invalid.' },
      });
      return;
    }

    const apiError = toApiError(error);
    response.status(apiError.status).json({
      error: {
        code: apiError.code,
        message: apiError.message,
      },
    });
  });

  return app;
}

function expensiveHandler<Schema extends ZodType, Result>(
  concurrency: ModelConcurrencyGate,
  schema: Schema,
  handler: (request: express.Request, input: ReturnType<Schema['parse']>) => Promise<Result>,
): express.RequestHandler {
  return async (request, response, next) => {
    let release: (() => void) | undefined;
    try {
      const sessionId = response.locals.judgeSession as string;
      release = concurrency.acquire(sessionId);
      const input = schema.parse(request.body) as ReturnType<Schema['parse']>;
      response.status(200).json(await handler(request, input));
    } catch (error) {
      next(error);
    } finally {
      release?.();
    }
  };
}

function requireJudgeSession(
  request: express.Request,
  response: express.Response,
  next: express.NextFunction,
): void {
  const raw = request.header(JUDGE_SESSION_HEADER)?.trim();
  if (!raw || !JUDGE_SESSION_PATTERN.test(raw)) {
    next(new ApiError(400, 'INVALID_SESSION', 'Start or resume a valid field mission session before using AI features.'));
    return;
  }
  response.locals.judgeSession = raw;
  setRequestSession(raw);
  next();
}

function assertJudgeWindow(
  expiration: Date | undefined,
  now: () => Date,
): express.RequestHandler {
  return (_request, _response, next) => {
    if (process.env.NODE_ENV === 'production' && !expiration) {
      next(new ApiError(503, 'SERVICE_NOT_READY', 'The hosted judge AI service is not configured.'));
      return;
    }
    if (expiration && now().getTime() >= expiration.getTime()) {
      next(new ApiError(503, 'JUDGE_ACCESS_EXPIRED', 'The hosted judging window has ended. No OpenAI request was made.'));
      return;
    }
    next();
  };
}

function createLimiter(options: {
  windowMs: number;
  limit: number;
  keyPrefix: string;
  sessionKey?: boolean;
}): express.RequestHandler {
  return rateLimit({
    windowMs: options.windowMs,
    limit: options.limit,
    standardHeaders: 'draft-8',
    legacyHeaders: false,
    passOnStoreError: false,
    keyGenerator: options.sessionKey
      ? (request) => `${options.keyPrefix}:${request.header(JUDGE_SESSION_HEADER) ?? 'missing'}`
      : undefined,
    handler: (request, response) => {
      const context = getRequestContext();
      console.warn(
        JSON.stringify({
          event: 'rate_limit',
          request_id: context?.requestId,
          route: safeRouteName(request.path),
          policy: options.keyPrefix,
        }),
      );
      response.status(429).json({
        error: {
          code: 'RATE_LIMITED',
          message: 'This field mission has reached a temporary request limit. Wait before retrying.',
        },
      });
    },
  });
}

function parseBoundedLimit(
  raw: string | undefined,
  fallback: number,
  minimum: number,
  maximum: number,
): number {
  if (!raw) {
    return fallback;
  }
  const parsed = Number(raw);
  if (!Number.isInteger(parsed) || parsed < minimum || parsed > maximum) {
    throw new Error(
      `HTTP concurrency must be an integer from ${minimum} through ${maximum}.`,
    );
  }
  return parsed;
}

function isEntityTooLarge(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'type' in error && error.type === 'entity.too.large';
}

function isMalformedJson(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'type' in error && error.type === 'entity.parse.failed';
}

function safeRouteName(path: string): string {
  if (path.startsWith('/api/stakeholders/')) {
    return '/api/stakeholders/:stakeholderId/messages';
  }
  if (
    path === '/health' ||
    path === '/api/scenario' ||
    path === '/api/simulations' ||
    path === '/api/feedback' ||
    path === '/api/policy-briefs'
  ) {
    return path;
  }
  if (path.startsWith('/assets/')) {
    return '/assets/*';
  }
  return 'unmatched';
}
