import OpenAI from 'openai';
import { zodTextFormat } from 'openai/helpers/zod';
import { z } from 'zod';

import { ApiError } from './api-error.js';

const MAX_STRUCTURED_ATTEMPTS = 3;

function getOpenAIClient() {
  const apiKey = process.env.OPENAI_API_KEY;

  if (!apiKey) {
    throw new ApiError(503, 'OPENAI_NOT_CONFIGURED', 'Add OPENAI_API_KEY to .env before using AI features.');
  }

  return new OpenAI({ apiKey });
}

function getModelName() {
  return process.env.OPENAI_MODEL?.trim() || 'gpt-5.6';
}

function isModelConfigurationError(error: unknown): boolean {
  if (typeof error !== 'object' || error === null || !('status' in error)) {
    return false;
  }

  if (error.status === 404) {
    return true;
  }

  const message = 'message' in error && typeof error.message === 'string' ? error.message : '';
  return error.status === 400 && /model/i.test(message) && /(not found|does not exist|unsupported|invalid|access)/i.test(message);
}

function isProviderError(error: unknown): boolean {
  return typeof error === 'object' && error !== null && 'status' in error && typeof error.status === 'number';
}

export async function runTextResponse(systemPrompt: string, input: unknown): Promise<string> {
  try {
    const response = await getOpenAIClient().responses.create({
      model: getModelName(),
      input: [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: JSON.stringify(input) },
      ],
      max_output_tokens: 700,
    });

    const text = response.output_text.trim();
    if (!text) {
      throw new ApiError(502, 'EMPTY_MODEL_RESPONSE', 'The AI system returned no usable response.');
    }

    return text;
  } catch (error) {
    if (error instanceof ApiError) {
      throw error;
    }
    if (isModelConfigurationError(error)) {
      throw new ApiError(502, 'MODEL_CONFIGURATION_ERROR', 'The configured OPENAI_MODEL was not accepted by the API. Update .env and retry.');
    }
    throw new ApiError(502, 'AI_REQUEST_FAILED', 'The AI system could not complete this request.');
  }
}

export async function runStructuredResponse<Schema extends z.ZodTypeAny>(options: {
  systemPrompt: string;
  input: unknown;
  schema: Schema;
  schemaName: string;
}): Promise<z.infer<Schema>> {
  const client = getOpenAIClient();
  const model = getModelName();
  let lastError: unknown;

  for (let attempt = 1; attempt <= MAX_STRUCTURED_ATTEMPTS; attempt += 1) {
    try {
      const response = await client.responses.parse({
        model,
        input: [
          { role: 'system', content: options.systemPrompt },
          {
            role: 'user',
            content: JSON.stringify({
              request: options.input,
              validation_retry: attempt === 1 ? undefined : `Attempt ${attempt}: return the complete response matching the supplied schema exactly.`,
            }),
          },
        ],
        max_output_tokens: 3200,
        text: { format: zodTextFormat(options.schema, options.schemaName) },
      });

      if (response.output_parsed === null) {
        lastError = new Error('The structured response was absent or refused.');
        continue;
      }

      return options.schema.parse(response.output_parsed);
    } catch (error) {
      if (isModelConfigurationError(error)) {
        throw new ApiError(502, 'MODEL_CONFIGURATION_ERROR', 'The configured OPENAI_MODEL was not accepted by the API. Update .env and retry.');
      }
      if (isProviderError(error)) {
        throw new ApiError(502, 'AI_REQUEST_FAILED', 'The AI system could not complete this request.');
      }
      lastError = error;
    }
  }

  void lastError;
  throw new ApiError(502, 'INVALID_STRUCTURED_RESPONSE', 'The AI system could not produce a complete valid result after retrying.');
}
