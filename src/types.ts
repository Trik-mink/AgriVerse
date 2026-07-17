export type TestSite = {
  id: string;
  label: string;
  salinity_gL: number;
  season: string;
  salinity_duration: string;
  seasonal_pattern: string;
  freshwater_access: 'none' | 'low' | 'medium' | 'high';
  note: string;
  measurement_grounding: { source_ids: string[] };
};

export type Stakeholder = { id: string; name: string; role: string; persona: string; knows: string[] };

export type Intervention = {
  id: string;
  label: string;
  description: string;
  cost: string;
  income: string;
  sustainability: string;
  capital_need: string;
};

export type Scenario = {
  id: string;
  title: string;
  location: { country: string; region: string; env_asset: string };
  crisis: { key_metric: { label: string; unit: string; danger_threshold: { operator: string; value: number } } };
  farmer_capital: string;
  test_sites: TestSite[];
  stakeholders: Stakeholder[];
  interventions: Intervention[];
  support_measure_options: Array<{ id: string; description: string }>;
  sources: Array<{ id: string; title: string; publisher: string; url: string }>;
};

export type Proposal = {
  intervention_ids: string[];
  parameters: Record<string, unknown>;
  support_measures: string[];
  rationale: string;
};

export type OutcomeMetrics = {
  salinity: { value: number; unit: string };
  yield: { items: Array<{ commodity_id: string; value: number | null; unit: string }> };
  income: { score: number; projected_value: number | null; currency: string | null; basis: string | null };
  sustainability: { score: number };
};

export type SimulatorResult = {
  intervention_summary: string;
  fit_assessment: Record<string, 'fit' | 'mismatch'>;
  years: Array<{
    year: number;
    outcomes: OutcomeMetrics;
    cost_level: string;
    narrative: string;
    evidence_source_ids: string[];
  }>;
  tradeoffs: Array<{ category: string; summary: string }>;
  headline: string;
};

export type GraderResult = {
  fit_assessment: Record<string, 'fit' | 'mismatch'>;
  rubric_results: Array<{ rubric_id: string; rating: string; feedback: string; evidence: { source_ids: string[]; simulation_years: number[] } }>;
  key_insight: { text: string; evidence: { source_ids: string[]; simulation_years: number[] } };
  revision_prompt: string;
  encouragement: string;
};

export type PolicyBriefResult = {
  title: string;
  problem_statement: { text: string; source_ids: string[] };
  evidence: Array<{ claim: string; source_ids: string[] }>;
  recommended_solution: { summary: string; factor_rationale: Record<string, string> };
  projected_outcomes: { year_1: OutcomeMetrics; year_5: OutcomeMetrics; summary: string };
  tradeoffs_and_risks: Array<{ category: string; risk: string; mitigation: string }>;
  stakeholder_balance: Array<{ stakeholder_id: string; concern: string; response: string }>;
  next_steps: Array<{ order: number; action: string; owner_stakeholder_id: string }>;
};
