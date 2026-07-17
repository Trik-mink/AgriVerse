import { describe, expect, it } from 'vitest';

import { shouldUseClassic } from '../src/immersive/capabilities.js';
import { stationSceneSrc, stationVideoSrc } from '../src/immersive/world/sceneAssets.js';
import { STATIONS } from '../src/immersive/world/stations.js';

describe('immersive presentation foundation', () => {
  it('defines six ordered semantic station anchors without scenario-specific content', () => {
    expect(STATIONS.map((station) => station.id)).toEqual([
      'field',
      'research',
      'office',
      'planning',
      'future',
      'reflection',
    ]);
    expect(STATIONS.every((station) => station.position.length === 3)).toBe(true);
  });

  it('keeps the classic experience available for explicit preference or unavailable WebGL', () => {
    expect(shouldUseClassic('classic', true)).toBe(true);
    expect(shouldUseClassic('immersive', false)).toBe(true);
    expect(shouldUseClassic('immersive', true)).toBe(false);
  });

  it('pairs each station poster with its matching optional video path', () => {
    for (const station of STATIONS) {
      expect(stationSceneSrc(station.id)).toMatch(/^\/assets\/scenes\/optimized\/.+\.webp$/);
      expect(stationVideoSrc(station.id)).toMatch(/^\/assets\/scenes\/.+\.mp4$/);
    }
  });
});
