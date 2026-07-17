import { describe, expect, it } from 'vitest';

import {
  createGraderResultSchema,
  createPolicyBriefResultSchema,
  createSimulatorResultSchema,
} from '../server/contracts.js';
import { buildDecisionContext, getScenario } from '../server/scenario-loader.js';

function validSimulation() {
  const scenario = getScenario();
  const decisionContext = buildDecisionContext('mid');
  const sourceId = (scenario.sources as Array<{ id: string }>)[0].id;

  return {
    contract_version: '1.0',
    scenario_id: scenario.id,
    intervention_summary: 'Test proposal summary.',
    decision_context: decisionContext,
    fit_assessment: {
      salinity: 'fit',
      seasonality: 'fit',
      freshwater: 'fit',
      farmer_capital: 'fit',
      overall: 'fit',
    },
    years: [1, 2, 3, 4, 5].map((year) => ({
      year,
      outcomes: {
        salinity: { value: decisionContext.salinity.value, unit: decisionContext.salinity.unit },
        yield: { items: [{ commodity_id: 'rice', value: null, unit: 't/ha' }] },
        income: {
          score: 50,
          scale_min: 0,
          scale_max: 100,
          projected_value: null,
          currency: null,
          basis: null,
        },
        sustainability: { score: 50, scale_min: 0, scale_max: 100 },
      },
      cost_level: 'low',
      narrative: `Projection for year ${year}.`,
      evidence_source_ids: [sourceId],
    })),
    tradeoffs: [{ category: 'cost', summary: 'Qualitative cost tradeoff.' }],
    headline: 'Five-year test projection.',
  };
}

describe('simulator result contract', () => {
  it('accepts a complete, canonical five-year result', () => {
    const scenario = getScenario();
    const result = validSimulation();

    expect(createSimulatorResultSchema(scenario, buildDecisionContext('mid')).parse(result)).toEqual(result);
  });

  it('rejects a fit assessment whose overall field is not the logical AND', () => {
    const scenario = getScenario();
    const result = validSimulation();
    result.fit_assessment.overall = 'mismatch';

    expect(() => createSimulatorResultSchema(scenario, buildDecisionContext('mid')).parse(result)).toThrow();
  });

  it('rejects a decision context that differs from the selected site', () => {
    const scenario = getScenario();
    const result = validSimulation();
    result.decision_context.salinity.value = 999;

    expect(() => createSimulatorResultSchema(scenario, buildDecisionContext('mid')).parse(result)).toThrow();
  });
});

describe('downstream structured contracts', () => {
  it('requires the grader to copy the simulator fit assessment and rubric order', () => {
    const scenario = getScenario();
    const simulation = validSimulation();
    const sourceId = (scenario.sources as Array<{ id: string }>)[0].id;
    const grader = {
      contract_version: '1.0',
      scenario_id: scenario.id,
      fit_assessment: simulation.fit_assessment,
      rubric_results: [
        'salinity_fit',
        'seasonality_fit',
        'freshwater_fit',
        'farmer_capital_fit',
        'cost_scalability',
        'long_term_sustainability',
      ].map((rubric_id, index) => ({
        rubric_id,
        rating: index === 4 ? 'partly_meets' : 'meets',
        feedback: `Feedback for ${rubric_id}.`,
        evidence: { source_ids: [sourceId], simulation_years: [] },
      })),
      key_insight: { text: 'Consider the qualitative cost tradeoff.', evidence: { source_ids: [sourceId], simulation_years: [] } },
      revision_prompt: 'How will the plan manage the cost tradeoff?',
      encouragement: 'Your evidence chain is clear.',
    };

    expect(createGraderResultSchema(scenario, simulation).parse(grader)).toEqual(grader);
  });

  it('requires the policy brief to preserve canonical year one and year five outcomes', () => {
    const scenario = getScenario();
    const simulation = validSimulation();
    const sourceId = (scenario.sources as Array<{ id: string }>)[0].id;
    const stakeholderId = scenario.stakeholders[0].id;
    const brief = {
      contract_version: '1.0',
      scenario_id: scenario.id,
      title: 'Test policy brief',
      problem_statement: { text: 'Grounded problem statement.', source_ids: [sourceId] },
      evidence: [1, 2, 3].map((index) => ({ claim: `Grounded claim ${index}.`, source_ids: [sourceId] })),
      recommended_solution: {
        summary: 'Test recommendation.',
        fit_assessment: simulation.fit_assessment,
        factor_rationale: {
          salinity: 'Addresses salinity.',
          seasonality: 'Addresses seasonal pattern.',
          freshwater: 'Addresses freshwater access.',
          farmer_capital: 'Addresses capital constraints.',
        },
        evidence: { source_ids: [sourceId], simulation_years: [] },
      },
      projected_outcomes: {
        year_1: simulation.years[0].outcomes,
        year_5: simulation.years[4].outcomes,
        summary: 'Projection comparison.',
      },
      tradeoffs_and_risks: [
        { category: 'cost', risk: 'Cost is a real constraint.', mitigation: 'Use staged support.', evidence: { source_ids: [sourceId], simulation_years: [] } },
        { category: 'uncertainty', risk: 'Future conditions are uncertain.', mitigation: 'Monitor field conditions.', evidence: { source_ids: [], simulation_years: [1, 5] } },
      ],
      stakeholder_balance: [{ stakeholder_id: stakeholderId, concern: 'Household risk.', response: 'Staged support responds to the concern.' }],
      next_steps: [
        { order: 1, action: 'Confirm seasonal conditions.', owner_stakeholder_id: stakeholderId },
        { order: 2, action: 'Review the support plan.', owner_stakeholder_id: stakeholderId },
      ],
    };

    expect(createPolicyBriefResultSchema(scenario, simulation).parse(brief)).toEqual(brief);
  });
});
