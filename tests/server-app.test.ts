import type { AddressInfo } from 'node:net';
import { once } from 'node:events';

import { afterAll, beforeAll, describe, expect, it } from 'vitest';

import { createApp } from '../server/app.js';

const allowedOrigin = 'http://localhost:8000';
let baseUrl = '';
let server: ReturnType<ReturnType<typeof createApp>['listen']>;

beforeAll(async () => {
  server = createApp({ allowedOrigins: [allowedOrigin] }).listen(0, '127.0.0.1');
  await once(server, 'listening');

  const address = server.address() as AddressInfo;
  baseUrl = `http://127.0.0.1:${address.port}`;
});

afterAll(async () => {
  if (server) {
    await new Promise<void>((resolve, reject) => {
      server.close((error) => (error ? reject(error) : resolve()));
    });
  }
});

describe('Unity HTTP boundary', () => {
  it('serves the sanitized scenario to an Editor request without an Origin header', async () => {
    const response = await fetch(`${baseUrl}/api/scenario`);
    const scenario = (await response.json()) as {
      title?: string;
      stakeholders?: Array<Record<string, unknown>>;
    };

    expect(response.status).toBe(200);
    expect(scenario.title).toBeTypeOf('string');
    expect(scenario.title?.length).toBeGreaterThan(0);
    expect(scenario.stakeholders?.length).toBeGreaterThan(0);
    expect(scenario.stakeholders?.[0]).not.toHaveProperty('hidden_goal');
    expect(scenario.stakeholders?.[0]).not.toHaveProperty('prompt_file');
  });

  it('allows the configured Unity Web origin without reflecting other origins', async () => {
    const allowedResponse = await fetch(`${baseUrl}/api/scenario`, {
      headers: { Origin: allowedOrigin },
    });
    const blockedResponse = await fetch(`${baseUrl}/api/scenario`, {
      headers: { Origin: 'https://untrusted.example' },
    });

    expect(allowedResponse.headers.get('access-control-allow-origin')).toBe(allowedOrigin);
    expect(allowedResponse.headers.get('vary')).toContain('Origin');
    expect(blockedResponse.headers.get('access-control-allow-origin')).toBeNull();
  });

  it('answers an allowed JSON preflight for future Unity POST requests', async () => {
    const response = await fetch(`${baseUrl}/api/simulations`, {
      method: 'OPTIONS',
      headers: {
        Origin: allowedOrigin,
        'Access-Control-Request-Method': 'POST',
        'Access-Control-Request-Headers': 'content-type',
      },
    });

    expect(response.status).toBe(204);
    expect(response.headers.get('access-control-allow-origin')).toBe(allowedOrigin);
    expect(response.headers.get('access-control-allow-methods')).toContain('POST');
    expect(response.headers.get('access-control-allow-headers')?.toLowerCase()).toContain('content-type');
  });
});
