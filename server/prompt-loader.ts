import { readFileSync } from 'node:fs';
import { resolve, sep } from 'node:path';

let cachedGroundingCorpus: string | undefined;

export function loadPrompt(promptPath: string): string {
  const promptsDirectory = resolve(process.cwd(), 'prompts');
  const resolvedPromptPath = resolve(process.cwd(), promptPath);

  if (!resolvedPromptPath.startsWith(`${promptsDirectory}${sep}`)) {
    throw new Error('Prompt path must remain within the prompts directory.');
  }

  return readFileSync(resolvedPromptPath, 'utf8');
}

export function getGroundingCorpus(): string {
  if (!cachedGroundingCorpus) {
    cachedGroundingCorpus = readFileSync(resolve(process.cwd(), 'docs/data-sources.md'), 'utf8');
  }

  return cachedGroundingCorpus;
}
