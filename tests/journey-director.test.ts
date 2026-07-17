import { describe, expect, it } from 'vitest';

import { getNextStationId, getStationIndex, travelDurationMs } from '../src/immersive/JourneyDirector.js';

describe('immersive camera journey', () => {
  it('advances through the six semantic stations in order without exposing tab navigation', () => {
    expect(getStationIndex('field')).toBe(0);
    expect(getNextStationId('field')).toBe('research');
    expect(getNextStationId('reflection')).toBe('field');
  });

  it('uses an immediate cut for reduced motion and a bounded rail duration otherwise', () => {
    expect(travelDurationMs(true)).toBe(0);
    expect(travelDurationMs(false)).toBeGreaterThanOrEqual(2000);
    expect(travelDurationMs(false)).toBeLessThanOrEqual(3500);
  });
});
