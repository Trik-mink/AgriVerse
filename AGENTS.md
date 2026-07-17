# AGENTS.md - AgriVerse

Context for AI coding agents (Codex / GPT-5.6) working in this repo.

## What this project is
AgriVerse is a 3D educational simulation for high-school environmental science students.
A student investigates a real agricultural crisis - **saltwater intrusion in Vietnam's
Mekong Delta** - tests water, interviews three AI-driven stakeholders, proposes an
evidence-based solution, watches multi-year consequences, gets research-grounded feedback,
revises, and receives a final policy brief.

Built for the OpenAI Build Week hackathon (Education track). GPT-5.6 is both the build
partner AND the product's runtime engine - the AI systems ARE the core mechanics.

## Architecture principles (do not violate)
- **Scenario-engine, not hardcoded country.** Everything about Vietnam lives in data
  (`scenario.json`). Never hardcode Vietnam-specific values in components/logic - drive them
  from the scenario schema so new countries are one JSON file. This is a scored design goal.
- **Prompts are versioned files, not inline strings.** All system prompts live in `/prompts/`.
  Load them from there; never paste prompt text directly into code.
- **The four runtime GPT-5.6 systems are the core - never stub them out:**
  1. Three stakeholder agents with private hidden goals (`/prompts/stakeholder-*.md`)
  2. Consequence simulator - returns strict JSON (5-year yields/income/salinity)
  3. Retrieval-grounded feedback grader (`/prompts/grader-feedback.md`)
  4. Policy-brief generator

## Where things live
- `/prompts/` - all GPT-5.6 system prompts (source of truth for AI behavior)
- `docs/data-sources.md` - the real, cited Mekong Delta figures. All AI factual claims must
  stay grounded in these. Never invent statistics.
- `docs/codex-playbook.md` - the build strategy and JSON contracts.
- `scenario.json` - the country-agnostic scenario definition (drives the whole app).

## Conventions
- Keep AI outputs grounded in `docs/data-sources.md`. Plausibility > invention.
- Structured outputs (JSON) for the simulator, grader, and policy brief - inspectable and testable.
- Prefer clarity and simplicity; this is a hackathon build, but the core AI systems must be real.

## Build-log rule (do this after every feature - it feeds the required README)
After you build or meaningfully change a feature, append one entry to `docs/BUILD-LOG.md`:

```
### <date> - <feature name>
- What Codex did: <1-2 lines on what you implemented/generated>
- Human decision: <any instruction, correction, or override the user gave - "user chose X over Y", "user rejected Z". Omit if none this step.>
- GPT-5.6 technique used: <e.g. structured output, role-separated agents, grounded grading - if applicable>
```

Keep entries short and factual. This log is the source material for the hackathon's required
"how I collaborated with Codex (what it accelerated vs. what I decided)" README section. Never
delete past entries.
