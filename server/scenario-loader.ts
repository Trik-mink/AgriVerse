import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

import { z } from 'zod';

const CapitalLevelSchema = z.enum(['low', 'medium', 'high']);
const FreshwaterAccessSchema = z.enum(['none', 'low', 'medium', 'high']);

const TestSiteSchema = z
  .object({
    id: z.string().min(1),
    label: z.string().min(1),
    salinity_gL: z.number().nonnegative(),
    season: z.string().min(1),
    salinity_duration: z.string().min(1),
    seasonal_pattern: z.string().min(1),
    freshwater_access: FreshwaterAccessSchema,
  })
  .passthrough();

const StakeholderSchema = z
  .object({
    id: z.string().min(1),
    name: z.string().min(1),
    role: z.string().min(1),
    persona: z.string().min(1),
    prompt_file: z.string().min(1),
    hidden_goal: z.string().min(1),
    knows: z.array(z.string()),
  })
  .passthrough();

const ScenarioSchema = z
  .object({
    id: z.string().min(1),
    title: z.string().min(1),
    crisis: z.object({ key_metric: z.object({ unit: z.string().min(1) }).passthrough() }).passthrough(),
    farmer_capital: CapitalLevelSchema,
    test_sites: z.array(TestSiteSchema).min(1),
    stakeholders: z.array(StakeholderSchema).min(1),
  })
  .passthrough();

export type Scenario = z.infer<typeof ScenarioSchema>;
export type DecisionContext = {
  salinity: { value: number; unit: string };
  season: { current: string; duration: string; salinity_pattern: string };
  freshwater_access: z.infer<typeof FreshwaterAccessSchema>;
  farmer_capital: z.infer<typeof CapitalLevelSchema>;
};

let cachedScenario: Scenario | undefined;

export function getScenario(): Scenario {
  if (!cachedScenario) {
    const scenarioPath = resolve(process.cwd(), 'scenario.json');
    const rawScenario: unknown = JSON.parse(readFileSync(scenarioPath, 'utf8'));
    cachedScenario = ScenarioSchema.parse(rawScenario);
  }

  return cachedScenario;
}

export function getPublicScenario() {
  const scenario = getScenario();

  return {
    ...scenario,
    stakeholders: scenario.stakeholders.map(({ hidden_goal: _hiddenGoal, prompt_file: _promptFile, ...stakeholder }) => stakeholder),
  };
}

export function getTestSite(siteId: string) {
  const site = getScenario().test_sites.find((candidate) => candidate.id === siteId);

  if (!site) {
    throw new Error(`Unknown test site: ${siteId}`);
  }

  return site;
}

export function buildDecisionContext(siteId: string): DecisionContext {
  const scenario = getScenario();
  const site = getTestSite(siteId);

  return {
    salinity: { value: site.salinity_gL, unit: scenario.crisis.key_metric.unit },
    season: {
      current: site.season,
      duration: site.salinity_duration,
      salinity_pattern: site.seasonal_pattern,
    },
    freshwater_access: site.freshwater_access,
    farmer_capital: scenario.farmer_capital,
  };
}
