import 'dotenv/config';

import express from 'express';
import rateLimit from 'express-rate-limit';
import helmet from 'helmet';
import { ZodError } from 'zod';

import { ApiError, toApiError } from './api-error.js';
import {
  generatePolicyBrief,
  grade,
  PolicyBriefInputSchema,
  respondAsStakeholder,
  GraderInputSchema,
  SimulationInputSchema,
  simulate,
  StakeholderMessageInputSchema,
} from './runtime.js';
import { getPublicScenario } from './scenario-loader.js';

const app = express();
const port = Number(process.env.PORT ?? 8787);

app.use(helmet());
app.use(express.json({ limit: '1mb' }));
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
    error: { code: apiError.code, message: apiError.message, ...(apiError.details ? { details: apiError.details } : {}) },
  });
});

app.listen(port, () => {
  console.info(`AgriVerse API listening on http://localhost:${port}`);
});
