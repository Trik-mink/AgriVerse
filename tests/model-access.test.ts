import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { DurableBudgetLedger } from '../server/budget-ledger.js';
import {
  getJudgeAccessExpiration,
  ModelAccessCoordinator,
} from '../server/model-access.js';

let directory = '';
let ledger: DurableBudgetLedger;

beforeEach(async () => {
  directory = await mkdtemp(join(tmpdir(), 'agriverse-model-access-'));
  ledger = new DurableBudgetLedger({
    filePath: join(directory, 'ledger.json'),
    capMicroUsd: 1_000_000,
    allowCreateUntil: new Date('2026-07-20T12:01:00.000Z'),
    now: () => new Date('2026-07-20T12:00:00.000Z'),
  });
  await ledger.initialize();
});

afterEach(async () => {
  vi.unstubAllEnvs();
  await rm(directory, { recursive: true, force: true });
});

describe('protected model access', () => {
  it('rejects concurrent calls from the same session before invoking the second provider call', async () => {
    const coordinator = new ModelAccessCoordinator({ ledger, globalConcurrency: 2, perSessionConcurrency: 1 });
    let markEntered: (() => void) | undefined;
    const entered = new Promise<void>((resolve) => {
      markEntered = resolve;
    });
    let releaseFirst: (() => void) | undefined;
    const first = coordinator.execute({
      sessionId: 'session-one',
      reservationInput: { request: 'first' },
      maxOutputTokens: 10,
      call: () =>
        new Promise((resolve) => {
          markEntered?.();
          releaseFirst = () => resolve({ value: 'done', usage: { input_tokens: 1, output_tokens: 1 } });
        }),
    });

    await entered;
    await expect(
      coordinator.execute({
        sessionId: 'session-one',
        reservationInput: { request: 'second' },
        maxOutputTokens: 10,
        call: async () => ({ value: 'should-not-run' }),
      }),
    ).rejects.toMatchObject({ status: 429, code: 'MODEL_BUSY' });

    releaseFirst?.();
    await expect(first).resolves.toBe('done');
  });

  it('rejects an expired judge request before reserving budget or calling the provider', async () => {
    let providerCalled = false;
    const coordinator = new ModelAccessCoordinator({
      ledger,
      expiresAt: new Date('2026-08-07T00:00:00.000Z'),
      now: () => new Date('2026-08-07T00:00:00.000Z'),
    });

    await expect(
      coordinator.execute({
        sessionId: 'expired-session',
        reservationInput: {},
        maxOutputTokens: 10,
        call: async () => {
          providerCalled = true;
          return { value: 'no' };
        },
      }),
    ).rejects.toMatchObject({ status: 503, code: 'JUDGE_ACCESS_EXPIRED' });
    expect(providerCalled).toBe(false);
    await expect(ledger.snapshot()).resolves.toMatchObject({ chargedMicroUsd: 0 });
  });

  it('keeps the full conservative reservation after provider failure', async () => {
    const coordinator = new ModelAccessCoordinator({ ledger });

    await expect(
      coordinator.execute({
        sessionId: 'failure-session',
        reservationInput: { student_text: 'not logged' },
        maxOutputTokens: 10,
        call: async () => {
          throw new Error('provider unavailable');
        },
      }),
    ).rejects.toThrow('provider unavailable');

    await expect(ledger.snapshot()).resolves.toMatchObject({
      failedCalls: 1,
      activeReservations: 0,
    });
    expect((await ledger.snapshot()).chargedMicroUsd).toBeGreaterThan(0);
  });

  it('rejects a configured judging window beyond the authorized end date', () => {
    vi.stubEnv(
      'JUDGE_ACCESS_EXPIRES_AT',
      '2026-08-07T00:00:00.001Z',
    );
    expect(() => getJudgeAccessExpiration()).toThrow(
      'cannot extend beyond August 6, 2026',
    );
  });
});
