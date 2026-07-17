import { describe, expect, it } from 'vitest';

import { getNextStationId, getStationIndex, transitionDurationMs } from '../src/immersive/journeyNavigation.js';

describe('immersive station journey', () => {
  it('advances through the six semantic stations in order without exposing tab navigation', () => {
    expect(getStationIndex('field')).toBe(0);
    expect(getNextStationId('field')).toBe('research');
    expect(getNextStationId('reflection')).toBe('field');
  });

  it('uses an immediate cut for reduced motion and a short photo transition otherwise', () => {
    expect(transitionDurationMs(true)).toBe(0);
    expect(transitionDurationMs(false)).toBeGreaterThanOrEqual(700);
    expect(transitionDurationMs(false)).toBeLessThanOrEqual(1000);
  });
});
