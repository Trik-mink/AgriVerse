import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import {
  actualUsageMicroUsd,
  DurableBudgetLedger,
  estimateReservationMicroUsd,
} from '../server/budget-ledger.js';

let directory = '';
let ledgerPath = '';
const capMicroUsd = 1_000_000;
const now = () => new Date('2026-07-20T12:00:00.000Z');
const allowCreateUntil = new Date('2026-07-20T12:05:00.000Z');

beforeEach(async () => {
  directory = await mkdtemp(join(tmpdir(), 'agriverse-budget-'));
  ledgerPath = join(directory, 'ledger.json');
});

afterEach(async () => {
  await rm(directory, { recursive: true, force: true });
});

function createLedger(path = ledgerPath) {
  return new DurableBudgetLedger({ filePath: path, capMicroUsd, allowCreateUntil, now });
}

describe('durable model budget ledger', () => {
  it('atomically admits only reservations that fit within the cap', async () => {
    const ledger = createLedger();
    await ledger.initialize();

    const results = await Promise.allSettled([ledger.reserve(600_000), ledger.reserve(600_000)]);
    expect(results.filter((result) => result.status === 'fulfilled')).toHaveLength(1);
    expect(results.filter((result) => result.status === 'rejected')).toHaveLength(1);
    await expect(ledger.snapshot()).resolves.toMatchObject({
      chargedMicroUsd: 600_000,
      remainingMicroUsd: 400_000,
      activeReservations: 1,
    });
  });

  it('persists reconciled usage across process-like ledger recreation', async () => {
    const first = createLedger();
    const reservation = await first.reserve(700_000);
    await first.reconcile(reservation.id, 225_000);

    const restarted = createLedger();
    await expect(restarted.initialize()).resolves.toMatchObject({
      chargedMicroUsd: 225_000,
      remainingMicroUsd: 775_000,
      completedCalls: 1,
      activeReservations: 0,
    });
  });

  it('retains conservative charges for provider failures', async () => {
    const ledger = createLedger();
    const reservation = await ledger.reserve(300_000);
    await ledger.retainFailedReservation(reservation.id);

    await expect(createLedger().snapshot()).resolves.toMatchObject({
      chargedMicroUsd: 300_000,
      failedCalls: 1,
      activeReservations: 0,
    });
  });

  it('rejects calls before provider access when the cap is exhausted', async () => {
    const ledger = createLedger();
    await ledger.reserve(900_000);

    await expect(ledger.reserve(100_001)).rejects.toMatchObject({
      status: 503,
      code: 'BUDGET_EXHAUSTED',
    });
  });

  it('fails closed when the ledger is missing after its bootstrap window', async () => {
    const expiredBootstrapLedger = new DurableBudgetLedger({
      filePath: ledgerPath,
      capMicroUsd,
      allowCreateUntil,
      now: () => new Date('2026-07-20T12:05:00.001Z'),
    });

    await expect(expiredBootstrapLedger.initialize()).rejects.toMatchObject({
      status: 503,
      code: 'BUDGET_LEDGER_UNAVAILABLE',
    });
  });

  it('fails closed after on-disk corruption without replacing the ledger', async () => {
    const ledger = createLedger();
    await ledger.initialize();
    await writeFile(ledgerPath, '{"payload":{"version":1},"sha256":"bad"}\n', 'utf8');

    await expect(createLedger().reserve(1)).rejects.toMatchObject({
      status: 503,
      code: 'BUDGET_LEDGER_UNAVAILABLE',
    });
    expect(await readFile(ledgerPath, 'utf8')).toContain('"sha256":"bad"');
  });

  it('uses the conservative verified GPT-5.6 rates for reservation and reconciliation', () => {
    expect(estimateReservationMicroUsd({ text: 'abc' }, 100)).toBeGreaterThanOrEqual(3_000);
    expect(actualUsageMicroUsd({ input_tokens: 1_000_000, output_tokens: 1_000_000 })).toBe(36_250_000);
  });

  it('rejects inputs that could cross into unreserved long-context pricing', () => {
    expect(() =>
      estimateReservationMicroUsd(
        { text: 'x'.repeat(240_000) },
        100,
      ),
    ).toThrow('too large for the hosted judge AI service');
  });
});
