import { isDeepStrictEqual } from 'node:util';

import { z } from 'zod';

import type { DecisionContext, Scenario } from './scenario-loader.js';

export const CONTRACT_VERSION = '1.0';

export const StudentProposalSchema = z
  .object({
    intervention_ids: z.array(z.string().min(1).max(80)).min(1).max(8),
    parameters: z
      .record(z.unknown())
      .refine((value) => Object.keys(value).length <= 32, 'Too many proposal parameters.')
      .refine((value) => hasSafeParameterShape(value), 'Proposal parameters are too deeply nested or complex.')
      .refine((value) => Buffer.byteLength(JSON.stringify(value), 'utf8') <= 8_192, 'Proposal parameters are too large.')
      .default({}),
    support_measures: z.array(z.string().min(1).max(80)).max(16).default([]),
    rationale: z.string().trim().min(1).max(4000),
  })
  .strict();

function hasSafeParameterShape(value: unknown, depth = 0): boolean {
  if (value === null || typeof value === 'boolean') {
    return true;
  }
  if (typeof value === 'number') {
    return Number.isFinite(value);
  }
  if (typeof value === 'string') {
    return value.length <= 4_000;
  }
  if (depth >= 4) {
    return false;
  }
  if (Array.isArray(value)) {
    return value.length <= 32 && value.every((item) => hasSafeParameterShape(item, depth + 1));
  }
  if (typeof value === 'object') {
    const entries = Object.entries(value);
    return (
      entries.length <= 32 &&
      entries.every(
        ([key, item]) => key.length <= 80 && hasSafeParameterShape(item, depth + 1),
      )
    );
  }
  return false;
}

export type StudentProposal = z.infer<typeof StudentProposalSchema>;

const FitStatusSchema = z.enum(['fit', 'mismatch']);

export const DecisionContextSchema = z
  .object({
    salinity: z.object({ value: z.number().nonnegative(), unit: z.string().min(1) }).strict(),
    season: z
      .object({
        current: z.string().min(1),
        duration: z.string().min(1),
        salinity_pattern: z.string().min(1),
      })
      .strict(),
    freshwater_access: z.enum(['none', 'low', 'medium', 'high']),
    farmer_capital: z.enum(['low', 'medium', 'high']),
  })
  .strict();

export const FitAssessmentSchema = z
  .object({
    salinity: FitStatusSchema,
    seasonality: FitStatusSchema,
    freshwater: FitStatusSchema,
    farmer_capital: FitStatusSchema,
    overall: FitStatusSchema,
  })
  .strict()
  .superRefine((value, context) => {
    const expected =
      value.salinity === 'fit' &&
      value.seasonality === 'fit' &&
      value.freshwater === 'fit' &&
      value.farmer_capital === 'fit'
        ? 'fit'
        : 'mismatch';

    if (value.overall !== expected) {
      context.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'fit_assessment.overall must be the logical AND of the four factor statuses.',
        path: ['overall'],
      });
    }
  });

const SalinityMetricSchema = z.object({ value: z.number().nonnegative(), unit: z.string().min(1) }).strict();

const YieldItemSchema = z
  .object({
    commodity_id: z.string().min(1),
    value: z.number().nonnegative().nullable(),
    unit: z.string(),
  })
  .strict();

const OutcomeMetricsSchema = z
  .object({
    salinity: SalinityMetricSchema,
    yield: z.object({ items: z.array(YieldItemSchema).min(1) }).strict(),
    income: z
      .object({
        score: z.number().int().min(0).max(100),
        scale_min: z.literal(0),
        scale_max: z.literal(100),
        projected_value: z.number().nonnegative().nullable(),
        currency: z.string().min(1).nullable(),
        basis: z.string().min(1).nullable(),
      })
      .strict()
      .superRefine((income, context) => {
        const isAllNull = income.projected_value === null && income.currency === null && income.basis === null;
        const isAllPresent = income.projected_value !== null && income.currency !== null && income.basis !== null;

        if (!isAllNull && !isAllPresent) {
          context.addIssue({
            code: z.ZodIssueCode.custom,
            message: 'Income projection value, currency, and basis must be all populated or all null.',
          });
        }
      }),
    sustainability: z
      .object({ score: z.number().int().min(0).max(100), scale_min: z.literal(0), scale_max: z.literal(100) })
      .strict(),
  })
  .strict();

const TradeoffCategorySchema = z.enum([
  'yield',
  'income',
  'cost',
  'sustainability',
  'scale',
  'farmer_buy_in',
  'salinity',
  'seasonality',
  'freshwater',
  'farmer_capital',
]);

function scenarioSourceIds(scenario: Scenario): string[] {
  const sourceRecords = z.array(z.object({ id: z.string().min(1) }).passthrough()).parse(scenario.sources ?? []);
  return sourceRecords.map((source) => source.id);
}

function commodityUnits(scenario: Scenario): Map<string, string[]> {
  const commodities = z
    .array(z.object({ id: z.string().min(1), yield_units: z.array(z.string()) }).passthrough())
    .parse(scenario.commodities ?? []);

  return new Map(commodities.map((commodity) => [commodity.id, commodity.yield_units]));
}

function sourceIdsSchema(sourceIds: string[]) {
  return z.array(z.string().refine((id) => sourceIds.includes(id), 'Unknown source ID')).min(1);
}

export function createSimulatorResultSchema(scenario: Scenario, expectedContext: DecisionContext) {
  const allowedSourceIds = scenarioSourceIds(scenario);
  const allowedCommodityUnits = commodityUnits(scenario);
  const sourceIdList = sourceIdsSchema(allowedSourceIds);
  const outcomeSchema = OutcomeMetricsSchema.superRefine((outcomes, context) => {
    if (outcomes.salinity.unit !== expectedContext.salinity.unit) {
      context.addIssue({ code: z.ZodIssueCode.custom, message: 'Outcome salinity unit must match the scenario unit.', path: ['salinity', 'unit'] });
    }

    for (const [index, item] of outcomes.yield.items.entries()) {
      const allowedUnits = allowedCommodityUnits.get(item.commodity_id);
      if (!allowedUnits) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'Yield item commodity is not declared by the scenario.', path: ['yield', 'items', index, 'commodity_id'] });
      } else if (!allowedUnits.includes(item.unit)) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'Yield item unit is not declared for this commodity.', path: ['yield', 'items', index, 'unit'] });
      }
    }
  });

  return z
    .object({
      contract_version: z.literal(CONTRACT_VERSION),
      scenario_id: z.literal(scenario.id),
      intervention_summary: z.string().min(1),
      decision_context: DecisionContextSchema,
      fit_assessment: FitAssessmentSchema,
      years: z
        .array(
          z
            .object({
              year: z.number().int().min(1).max(5),
              outcomes: outcomeSchema,
              cost_level: z.enum(['low', 'medium', 'high', 'varies', 'not_quantified']),
              narrative: z.string().min(1),
              evidence_source_ids: sourceIdList,
            })
            .strict(),
        )
        .length(5),
      tradeoffs: z.array(z.object({ category: TradeoffCategorySchema, summary: z.string().min(1) }).strict()).min(1),
      headline: z.string().min(1),
    })
    .strict()
    .superRefine((result, context) => {
      if (!isDeepStrictEqual(result.decision_context, expectedContext)) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'decision_context must exactly match the selected test site.', path: ['decision_context'] });
      }

      result.years.forEach((year, index) => {
        if (year.year !== index + 1) {
          context.addIssue({ code: z.ZodIssueCode.custom, message: 'Simulation years must be ordered from 1 through 5.', path: ['years', index, 'year'] });
        }
      });
    });
}

export type SimulatorResult = z.infer<ReturnType<typeof createSimulatorResultSchema>>;

type SimulatorReference = {
  scenario_id: string;
  fit_assessment: z.infer<typeof FitAssessmentSchema>;
  years: Array<{ year: number; outcomes: z.infer<typeof OutcomeMetricsSchema> }>;
};

const RubricRatingSchema = z.enum(['meets', 'partly_meets', 'does_not_meet']);

function createEvidenceRefSchema(sourceIds: string[]) {
  return z
    .object({
      source_ids: z.array(z.string().refine((id) => sourceIds.includes(id), 'Unknown source ID')),
      simulation_years: z.array(z.number().int().min(1).max(5)),
    })
    .strict()
    .superRefine((evidence, context) => {
      if (evidence.source_ids.length === 0 && evidence.simulation_years.length === 0) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'Evidence must reference a source or simulation year.' });
      }
    });
}

function rubricIds(scenario: Scenario): string[] {
  const rubric = z
    .object({ criteria: z.array(z.object({ id: z.string().min(1) }).passthrough()).min(1) })
    .passthrough()
    .parse(scenario.rubric ?? {});

  return rubric.criteria.map((criterion) => criterion.id);
}

export function createGraderResultSchema(scenario: Scenario, simulation: SimulatorReference) {
  const allowedSourceIds = scenarioSourceIds(scenario);
  const evidenceSchema = createEvidenceRefSchema(allowedSourceIds);
  const expectedRubricIds = rubricIds(scenario);

  return z
    .object({
      contract_version: z.literal(CONTRACT_VERSION),
      scenario_id: z.literal(scenario.id),
      fit_assessment: FitAssessmentSchema,
      rubric_results: z
        .array(
          z
            .object({
              rubric_id: z.string().min(1),
              rating: RubricRatingSchema,
              feedback: z.string().min(1),
              evidence: evidenceSchema,
            })
            .strict(),
        )
        .length(expectedRubricIds.length),
      key_insight: z.object({ text: z.string().min(1), evidence: evidenceSchema }).strict(),
      revision_prompt: z.string().min(1),
      encouragement: z.string().min(1),
    })
    .strict()
    .superRefine((result, context) => {
      if (!isDeepStrictEqual(result.fit_assessment, simulation.fit_assessment)) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'Grader fit_assessment must exactly copy the simulator result.', path: ['fit_assessment'] });
      }

      result.rubric_results.forEach((rubric, index) => {
        if (rubric.rubric_id !== expectedRubricIds[index]) {
          context.addIssue({ code: z.ZodIssueCode.custom, message: 'Rubric results must follow the scenario rubric order.', path: ['rubric_results', index, 'rubric_id'] });
        }
      });

      const mismatchMap: Record<string, keyof z.infer<typeof FitAssessmentSchema>> = {
        salinity_fit: 'salinity',
        seasonality_fit: 'seasonality',
        freshwater_fit: 'freshwater',
        farmer_capital_fit: 'farmer_capital',
      };

      for (const [rubricId, factor] of Object.entries(mismatchMap)) {
        const rubric = result.rubric_results.find((candidate) => candidate.rubric_id === rubricId);
        if (result.fit_assessment[factor] === 'mismatch' && rubric?.rating !== 'does_not_meet') {
          context.addIssue({ code: z.ZodIssueCode.custom, message: 'A mismatched factor must receive does_not_meet feedback.', path: ['rubric_results'] });
        }
      }

      if (!result.rubric_results.some((rubric) => rubric.rating !== 'meets')) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'At least one rubric result must identify a revision need.', path: ['rubric_results'] });
      }
    });
}

const PolicyRiskCategorySchema = z.enum([
  'cost',
  'scale',
  'soil_water',
  'farmer_buy_in',
  'yield',
  'income',
  'salinity',
  'seasonality',
  'freshwater',
  'farmer_capital',
  'uncertainty',
]);

function stakeholderIds(scenario: Scenario): string[] {
  return scenario.stakeholders.map((stakeholder) => stakeholder.id);
}

export function createPolicyBriefResultSchema(scenario: Scenario, simulation: SimulatorReference) {
  const allowedSourceIds = scenarioSourceIds(scenario);
  const evidenceSchema = createEvidenceRefSchema(allowedSourceIds);
  const allowedStakeholderIds = stakeholderIds(scenario);

  return z
    .object({
      contract_version: z.literal(CONTRACT_VERSION),
      scenario_id: z.literal(scenario.id),
      title: z.string().min(1),
      problem_statement: z.object({ text: z.string().min(1), source_ids: sourceIdsSchema(allowedSourceIds) }).strict(),
      evidence: z
        .array(z.object({ claim: z.string().min(1), source_ids: sourceIdsSchema(allowedSourceIds) }).strict())
        .min(3)
        .max(4),
      recommended_solution: z
        .object({
          summary: z.string().min(1),
          fit_assessment: FitAssessmentSchema,
          factor_rationale: z
            .object({
              salinity: z.string().min(1),
              seasonality: z.string().min(1),
              freshwater: z.string().min(1),
              farmer_capital: z.string().min(1),
            })
            .strict(),
          evidence: evidenceSchema,
        })
        .strict(),
      projected_outcomes: z
        .object({
          year_1: OutcomeMetricsSchema,
          year_5: OutcomeMetricsSchema,
          summary: z.string().min(1),
        })
        .strict(),
      tradeoffs_and_risks: z
        .array(
          z
            .object({
              category: PolicyRiskCategorySchema,
              risk: z.string().min(1),
              mitigation: z.string().min(1),
              evidence: evidenceSchema,
            })
            .strict(),
        )
        .min(2),
      stakeholder_balance: z.array(
        z
          .object({
            stakeholder_id: z.string().refine((id) => allowedStakeholderIds.includes(id), 'Unknown stakeholder ID'),
            concern: z.string().min(1),
            response: z.string().min(1),
          })
          .strict(),
      ),
      next_steps: z
        .array(
          z
            .object({
              order: z.number().int().min(1).max(3),
              action: z.string().min(1),
              owner_stakeholder_id: z.string().refine((id) => allowedStakeholderIds.includes(id), 'Unknown stakeholder ID'),
            })
            .strict(),
        )
        .min(2)
        .max(3),
    })
    .strict()
    .superRefine((result, context) => {
      if (!isDeepStrictEqual(result.recommended_solution.fit_assessment, simulation.fit_assessment)) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'Policy brief fit_assessment must exactly copy the simulator result.', path: ['recommended_solution', 'fit_assessment'] });
      }

      if (!isDeepStrictEqual(result.projected_outcomes.year_1, simulation.years[0]?.outcomes)) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'Policy brief year_1 outcomes must exactly copy simulation year 1.', path: ['projected_outcomes', 'year_1'] });
      }

      if (!isDeepStrictEqual(result.projected_outcomes.year_5, simulation.years[4]?.outcomes)) {
        context.addIssue({ code: z.ZodIssueCode.custom, message: 'Policy brief year_5 outcomes must exactly copy simulation year 5.', path: ['projected_outcomes', 'year_5'] });
      }

      result.next_steps.forEach((step, index) => {
        if (step.order !== index + 1) {
          context.addIssue({ code: z.ZodIssueCode.custom, message: 'Policy brief next steps must be ordered sequentially.', path: ['next_steps', index, 'order'] });
        }
      });
    });
}

export type GraderResult = z.infer<ReturnType<typeof createGraderResultSchema>>;
export type PolicyBriefResult = z.infer<ReturnType<typeof createPolicyBriefResultSchema>>;
