import { z } from 'zod';

import {
  createGraderResultSchema,
  createPolicyBriefResultSchema,
  createSimulatorResultSchema,
  StudentProposalSchema,
} from './contracts.js';
import { ApiError } from './api-error.js';
import { runStructuredResponse, runTextResponse } from './openai.js';
import { getGroundingCorpus, loadPrompt } from './prompt-loader.js';
import { buildDecisionContext, getScenario, getTestSite } from './scenario-loader.js';

const ConversationTurnSchema = z.object({ role: z.enum(['student', 'stakeholder']), text: z.string().trim().min(1).max(2000) }).strict();

export const StakeholderMessageInputSchema = z
  .object({
    message: z.string().trim().min(1).max(1400),
    conversation: z.array(ConversationTurnSchema).max(6).default([]),
  })
  .strict();

export const SimulationInputSchema = z
  .object({
    target_site_id: z.string().min(1).max(80),
    proposal: StudentProposalSchema,
  })
  .strict();

export const GraderInputSchema = SimulationInputSchema.extend({ simulation: z.unknown() }).strict();

export const PolicyBriefInputSchema = SimulationInputSchema.extend({
  simulation: z.unknown(),
  stakeholder_concerns: z
    .array(z.object({ stakeholder_id: z.string().min(1).max(80), concern: z.string().trim().min(1).max(1200) }).strict())
    .max(12),
}).strict();

function commonGrounding() {
  const scenario = getScenario();
  return { scenario, grounding_corpus: getGroundingCorpus() };
}

function decisionContextFor(siteId: string) {
  try {
    return buildDecisionContext(siteId);
  } catch {
    throw new ApiError(404, 'UNKNOWN_TEST_SITE', 'The selected test site does not exist in this scenario.');
  }
}

export async function respondAsStakeholder(stakeholderId: string, input: z.infer<typeof StakeholderMessageInputSchema>) {
  const scenario = getScenario();
  const stakeholder = scenario.stakeholders.find((candidate) => candidate.id === stakeholderId);
  if (!stakeholder) {
    throw new ApiError(404, 'UNKNOWN_STAKEHOLDER', 'The selected stakeholder does not exist in this scenario.');
  }

  const knownFactIds = new Set(stakeholder.knows);
  const datasets = z.array(z.object({ id: z.string().min(1) }).passthrough()).parse(scenario.datasets ?? []);
  const relevantDatasets = datasets.filter((dataset) => knownFactIds.has(dataset.id));
  const systemPrompt = loadPrompt(stakeholder.prompt_file);

  return runTextResponse(systemPrompt, {
    scenario_id: scenario.id,
    stakeholder: {
      id: stakeholder.id,
      name: stakeholder.name,
      role: stakeholder.role,
      persona: stakeholder.persona,
      hidden_goal: stakeholder.hidden_goal,
    },
    relevant_datasets: relevantDatasets,
    grounding_corpus: getGroundingCorpus(),
    conversation: input.conversation,
    student_message: input.message,
  });
}

export async function simulate(input: z.infer<typeof SimulationInputSchema>) {
  const { scenario, grounding_corpus } = commonGrounding();
  const decisionContext = decisionContextFor(input.target_site_id);
  const schema = createSimulatorResultSchema(scenario, decisionContext);

  return runStructuredResponse({
    systemPrompt: loadPrompt('prompts/consequence-simulator.md'),
    schema,
    schemaName: 'simulator_result',
    input: { scenario, proposal: input.proposal, target_site_id: input.target_site_id, decision_context: decisionContext, grounding_corpus },
  });
}

export async function grade(input: z.infer<typeof GraderInputSchema>) {
  const { scenario, grounding_corpus } = commonGrounding();
  const decisionContext = decisionContextFor(input.target_site_id);
  const simulation = createSimulatorResultSchema(scenario, decisionContext).parse(input.simulation);
  const schema = createGraderResultSchema(scenario, simulation);

  return runStructuredResponse({
    systemPrompt: loadPrompt('prompts/grader-feedback.md'),
    schema,
    schemaName: 'grader_result',
    input: { scenario, proposal: input.proposal, target_site_id: input.target_site_id, decision_context: decisionContext, simulation, grounding_corpus },
  });
}

export async function generatePolicyBrief(input: z.infer<typeof PolicyBriefInputSchema>) {
  const { scenario, grounding_corpus } = commonGrounding();
  const decisionContext = decisionContextFor(input.target_site_id);
  const simulation = createSimulatorResultSchema(scenario, decisionContext).parse(input.simulation);
  const schema = createPolicyBriefResultSchema(scenario, simulation);
  const stakeholderIds = new Set(scenario.stakeholders.map((stakeholder) => stakeholder.id));

  for (const concern of input.stakeholder_concerns) {
    if (!stakeholderIds.has(concern.stakeholder_id)) {
      throw new ApiError(422, 'UNKNOWN_STAKEHOLDER', 'A supplied stakeholder concern does not exist in this scenario.');
    }
  }

  return runStructuredResponse({
    systemPrompt: loadPrompt('prompts/policy-brief-generator.md'),
    schema,
    schemaName: 'policy_brief_result',
    input: {
      scenario,
      final_proposal: input.proposal,
      target_site_id: input.target_site_id,
      decision_context: decisionContext,
      simulation,
      stakeholder_concerns: input.stakeholder_concerns,
      grounding_corpus,
    },
  });
}

export function assertKnownTestSite(siteId: string) {
  return getTestSite(siteId);
}
