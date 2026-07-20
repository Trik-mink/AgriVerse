import { once } from 'node:events';
import type { AddressInfo } from 'node:net';

import { describe, expect, it } from 'vitest';

import { createApp, type CreateAppOptions } from '../server/app.js';

const sessionId = 'test-session-0001';
const validSimulationRequest = {
  target_site_id: 'mid',
  proposal: {
    intervention_ids: ['salt_tolerant_rice'],
    parameters: {},
    support_measures: [],
    rationale: 'Use the field evidence and stakeholder concerns.',
  },
};

async function withServer<Result>(
  options: CreateAppOptions,
  operation: (baseUrl: string) => Promise<Result>,
): Promise<Result> {
  const server = createApp(options).listen(0, '127.0.0.1');
  await once(server, 'listening');
  const address = server.address() as AddressInfo;

  try {
    return await operation(`http://127.0.0.1:${address.port}`);
  } finally {
    await new Promise<void>((resolve, reject) => {
      server.close((error) => (error ? reject(error) : resolve()));
    });
  }
}

function postJson(baseUrl: string, path: string, body: unknown, session = sessionId) {
  return fetch(`${baseUrl}${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'X-AgriVerse-Session': session,
    },
    body: JSON.stringify(body),
  });
}

describe('public judge HTTP security boundary', () => {
  it('rejects malformed and unknown request fields before runtime execution', async () => {
    let runtimeCalled = false;
    await withServer(
      {
        runtime: {
          simulation: async () => {
            runtimeCalled = true;
            return {} as never;
          },
        },
      },
      async (baseUrl) => {
        const response = await postJson(baseUrl, '/api/simulations', {
          ...validSimulationRequest,
          unexpected: 'must be rejected',
        });

        expect(response.status).toBe(422);
        expect(await response.json()).toEqual({
          error: { code: 'VALIDATION_ERROR', message: 'The request data is invalid.' },
        });
      },
    );
    expect(runtimeCalled).toBe(false);
  });

  it('rejects oversized bodies with a generic error and no reflected student text', async () => {
    await withServer({}, async (baseUrl) => {
      const privateStudentText = 'sensitive-student-text';
      const response = await postJson(baseUrl, '/api/simulations', {
        ...validSimulationRequest,
        proposal: {
          ...validSimulationRequest.proposal,
          rationale: `${privateStudentText}${'x'.repeat(270_000)}`,
        },
      });
      const body = await response.text();

      expect(response.status).toBe(413);
      expect(body).toContain('PAYLOAD_TOO_LARGE');
      expect(body).not.toContain(privateStudentText);
    });
  });

  it('rejects deeply nested free-form proposal parameters before runtime execution', async () => {
    let runtimeCalled = false;
    await withServer(
      {
        runtime: {
          simulation: async () => {
            runtimeCalled = true;
            return {} as never;
          },
        },
      },
      async (baseUrl) => {
        const response = await postJson(baseUrl, '/api/simulations', {
          ...validSimulationRequest,
          proposal: {
            ...validSimulationRequest.proposal,
            parameters: {
              level1: { level2: { level3: { level4: { level5: 'too deep' } } } },
            },
          },
        });
        expect(response.status).toBe(422);
      },
    );
    expect(runtimeCalled).toBe(false);
  });

  it('requires an opaque bounded session identifier for every expensive route', async () => {
    await withServer({}, async (baseUrl) => {
      const response = await postJson(baseUrl, '/api/simulations', validSimulationRequest, 'short');
      expect(response.status).toBe(400);
      expect(await response.json()).toMatchObject({ error: { code: 'INVALID_SESSION' } });
    });
  });

  it('rejects expired access before runtime or OpenAI execution', async () => {
    let runtimeCalled = false;
    await withServer(
      {
        judgeAccessExpiresAt: new Date('2026-08-07T00:00:00.000Z'),
        now: () => new Date('2026-08-07T00:00:00.000Z'),
        runtime: {
          simulation: async () => {
            runtimeCalled = true;
            return {} as never;
          },
        },
      },
      async (baseUrl) => {
        const response = await postJson(baseUrl, '/api/simulations', validSimulationRequest);
        expect(response.status).toBe(503);
        expect(await response.json()).toMatchObject({ error: { code: 'JUDGE_ACCESS_EXPIRED' } });
      },
    );
    expect(runtimeCalled).toBe(false);
  });

  it('enforces per-session rate limits while allowing a normal bounded sequence', async () => {
    await withServer(
      {
        requestLimits: {
          expensivePerSession: 2,
          simulationPerSession: 10,
        },
        runtime: { simulation: async () => ({ accepted: true }) as never },
      },
      async (baseUrl) => {
        expect((await postJson(baseUrl, '/api/simulations', validSimulationRequest)).status).toBe(200);
        expect((await postJson(baseUrl, '/api/simulations', validSimulationRequest)).status).toBe(200);
        const limited = await postJson(baseUrl, '/api/simulations', validSimulationRequest);
        expect(limited.status).toBe(429);
        expect(await limited.json()).toMatchObject({ error: { code: 'RATE_LIMITED' } });
      },
    );
  });

  it('rejects concurrent expensive calls from one session instead of queueing a retry storm', async () => {
    let markEntered: (() => void) | undefined;
    const entered = new Promise<void>((resolve) => {
      markEntered = resolve;
    });
    let releaseFirst: (() => void) | undefined;
    const waitForRelease = new Promise<void>((resolve) => {
      releaseFirst = resolve;
    });

    await withServer(
      {
        runtime: {
          simulation: async () => {
            markEntered?.();
            await waitForRelease;
            return { accepted: true } as never;
          },
        },
      },
      async (baseUrl) => {
        const first = postJson(baseUrl, '/api/simulations', validSimulationRequest);
        await entered;
        const second = await postJson(baseUrl, '/api/simulations', validSimulationRequest);
        expect(second.status).toBe(429);
        expect(await second.json()).toMatchObject({ error: { code: 'MODEL_BUSY' } });

        releaseFirst?.();
        expect((await first).status).toBe(200);
      },
    );
  });

  it('never returns provider details, credentials, or student text in an internal error', async () => {
    const credentialFragment =
      'provider-credential-fragment-never-return-this';
    const studentText = 'private student question';
    await withServer(
      {
        runtime: {
          stakeholder: async () => {
            throw new Error(`provider failed ${credentialFragment} ${studentText}`);
          },
        },
      },
      async (baseUrl) => {
        const response = await postJson(
          baseUrl,
          '/api/stakeholders/mr-ba/messages',
          { message: studentText, conversation: [] },
        );
        const body = await response.text();
        expect(response.status).toBe(500);
        expect(body).toContain('INTERNAL_ERROR');
        expect(body).not.toContain(credentialFragment);
        expect(body).not.toContain(studentText);
      },
    );
  });

  it('returns security headers and a secret-free readiness document', async () => {
    await withServer({}, async (baseUrl) => {
      const response = await fetch(`${baseUrl}/health`);
      const body = await response.text();
      expect(response.status).toBe(200);
      expect(response.headers.get('x-content-type-options')).toBe('nosniff');
      expect(response.headers.get('x-powered-by')).toBeNull();
      expect(body).toContain('"service":"agriverse-api"');
      expect(body).not.toContain('OPENAI_API_KEY');
    });
  });
});
