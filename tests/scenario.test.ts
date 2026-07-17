import { describe, expect, it } from 'vitest';

import {
  buildDecisionContext,
  getPublicScenario,
  getScenario,
} from '../server/scenario-loader.js';

describe('scenario loader', () => {
  it('builds the canonical decision context from a selected test site', () => {
    const scenario = getScenario();
    const site = scenario.test_sites.find((candidate) => candidate.id === 'mid');

    expect(site).toBeDefined();
    expect(buildDecisionContext('mid')).toEqual({
      salinity: { value: site?.salinity_gL, unit: scenario.crisis.key_metric.unit },
      season: {
        current: site?.season,
        duration: site?.salinity_duration,
        salinity_pattern: site?.seasonal_pattern,
      },
      freshwater_access: site?.freshwater_access,
      farmer_capital: scenario.farmer_capital,
    });
  });

  it('does not expose hidden stakeholder goals or server-side prompt paths', () => {
    const publicScenario = getPublicScenario();

    expect(publicScenario.stakeholders).toHaveLength(3);
    expect(publicScenario.stakeholders[0]).not.toHaveProperty('hidden_goal');
    expect(publicScenario.stakeholders[0]).not.toHaveProperty('prompt_file');
  });

  it('rejects an unknown test site', () => {
    expect(() => buildDecisionContext('missing-site')).toThrow('Unknown test site');
  });
});
