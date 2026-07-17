import { describe, expect, it } from 'vitest';

import { canOpenProposal, canRunSimulation } from '../src/flow/progress.js';

describe('student flow gates', () => {
  it('unlocks proposal work only after all scenario stakeholders have replied', () => {
    expect(canOpenProposal(['farmer', 'researcher'], ['farmer', 'researcher', 'official'])).toBe(false);
    expect(canOpenProposal(['farmer', 'researcher', 'official'], ['farmer', 'researcher', 'official'])).toBe(true);
  });

  it('requires each water site, each interview, and a rationale before simulation', () => {
    expect(
      canRunSimulation({
        testedSiteIds: ['coastal', 'mid', 'upstream'],
        siteIds: ['coastal', 'mid', 'upstream'],
        repliedStakeholderIds: ['farmer', 'researcher', 'official'],
        stakeholderIds: ['farmer', 'researcher', 'official'],
        interventionIds: ['salt_tolerant_rice'],
        rationale: 'Use the seasonal conditions and support plan.',
      }),
    ).toBe(true);

    expect(
      canRunSimulation({
        testedSiteIds: ['coastal', 'mid'],
        siteIds: ['coastal', 'mid', 'upstream'],
        repliedStakeholderIds: ['farmer', 'researcher', 'official'],
        stakeholderIds: ['farmer', 'researcher', 'official'],
        interventionIds: ['salt_tolerant_rice'],
        rationale: 'Use the seasonal conditions and support plan.',
      }),
    ).toBe(false);
  });
});
