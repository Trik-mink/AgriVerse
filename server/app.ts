import cors from 'cors';
import express from 'express';
import rateLimit from 'express-rate-limit';
import helmet from 'helmet';
import { ZodError } from 'zod';

import { toApiError } from './api-error.js';
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
import { getPublicScenario } from './scenario-loader.js';

const DEFAULT_ALLOWED_ORIGINS = [
  'http://localhost:5173',
  'http://127.0.0.1:5173',
  'http://localhost:8000',
  'http://127.0.0.1:8000',
] as const;

type CreateAppOptions = {
  allowedOrigins?: readonly string[];
};

export function getAllowedOrigins(configuredOrigins = process.env.CORS_ALLOWED_ORIGINS): string[] {
  const origins = configuredOrigins?.split(',') ?? [...DEFAULT_ALLOWED_ORIGINS];

  return [...new Set(origins.map((origin) => origin.trim()).filter(Boolean))];
}

export function createApp(options: CreateAppOptions = {}) {
  const allowedOrigins = new Set(options.allowedOrigins ?? getAllowedOrigins());
  const app = express();

  app.use(helmet());
  app.use(
    cors({
      origin(origin, callback) {
        callback(null, origin === undefined || allowedOrigins.has(origin));
      },
      methods: ['GET', 'POST', 'OPTIONS'],
      allowedHeaders: ['Content-Type'],
      maxAge: 600,
    }),
  );
  app.use(express.json({ limit: '1mb' }));
  app.use('/assets', express.static('public/assets', { fallthrough: false, maxAge: '1h' }));
  app.use(
    '/api',
    rateLimit({
      windowMs: 15 * 60 * 1000,
      limit: 40,
      standardHeaders: 'draft-8',
      legacyHeaders: false,
    }),
  );

  app.get('/health', (_request, response) => {
    response.status(200).json({ status: 'ok', service: 'agriverse-api' });
  });

  app.get('/api/scenario', (_request, response) => {
    response.status(200).json(getPublicScenario());
  });

  app.post('/api/stakeholders/:stakeholderId/messages', async (request, response, next) => {
    try {
      const input = StakeholderMessageInputSchema.parse(request.body);
      const message = await respondAsStakeholder(request.params.stakeholderId, input);
      response.status(200).json({ message });
    } catch (error) {
      next(error);
    }
  });

  app.post('/api/simulations', async (request, response, next) => {
    try {
      const input = SimulationInputSchema.parse(request.body);
      response.status(200).json(await simulate(input));
    } catch (error) {
      next(error);
    }
  });

  app.post('/api/feedback', async (request, response, next) => {
    try {
      const input = GraderInputSchema.parse(request.body);
      response.status(200).json(await grade(input));
    } catch (error) {
      next(error);
    }
  });

  app.post('/api/policy-briefs', async (request, response, next) => {
    try {
      const input = PolicyBriefInputSchema.parse(request.body);
      response.status(200).json(await generatePolicyBrief(input));
    } catch (error) {
      next(error);
    }
  });

  app.use((error: unknown, _request: express.Request, response: express.Response, _next: express.NextFunction) => {
    if (error instanceof ZodError) {
      response.status(422).json({
        error: { code: 'VALIDATION_ERROR', message: 'The request data is invalid.', details: error.flatten() },
      });
      return;
    }

    const apiError = toApiError(error);
    response.status(apiError.status).json({
      error: {
        code: apiError.code,
        message: apiError.message,
        ...(apiError.details ? { details: apiError.details } : {}),
      },
    });
  });

  return app;
}
