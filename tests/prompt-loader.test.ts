import { describe, expect, it } from 'vitest';

import { getGroundingCorpus, loadPrompt } from '../server/prompt-loader.js';

describe('prompt loader', () => {
  it('loads versioned prompts from the prompts directory', () => {
    expect(loadPrompt('prompts/consequence-simulator.md')).toContain('Consequence Simulator');
  });

  it('rejects paths outside the prompts directory', () => {
    expect(() => loadPrompt('../AGENTS.md')).toThrow('Prompt path must remain within');
  });

  it('includes the source document in the model grounding corpus', () => {
    expect(getGroundingCorpus()).toContain('Mekong Delta');
  });
});
