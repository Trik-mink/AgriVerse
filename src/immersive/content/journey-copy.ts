import type { StationId } from '../world/stations';

export const GUIDE_CUES: Record<StationId, string> = {
  field: 'Start with the field kit. Record each sample before drawing conclusions.',
  research: 'Compare what the evidence shows with the concerns you hear here.',
  office: 'Keep the community decision connected to the field evidence.',
  planning: 'Build one practical intervention and explain its fit.',
  future: 'Read each year as a tradeoff, not as a guaranteed outcome.',
  reflection: 'Use the feedback to make the policy brief more defensible.',
};
