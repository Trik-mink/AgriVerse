# AgriVerse Build Log

Running record of how the project was built with Codex / GPT-5.6.
Codex appends an entry after each feature (see the build-log rule in `AGENTS.md`).
This is the source material for the required README collaboration section.

Format per entry:
- **What Codex did** - what was implemented/generated
- **Human decision** - any instruction, correction, or override the user gave
- **GPT-5.6 technique used** - e.g. structured output, role-separated agents, grounded grading

---

### 2026-07-16 - Project setup (pre-code, done outside Codex)
- What Codex did: n/a - architecture package prepared (data sources, prompts, scenario-engine plan)
- Human decision: Scoped to ONE country (Vietnam) built deep, with country-agnostic architecture
  so "every country" lives in the schema + pitch, not in code. Designed three stakeholder personas
  with conflicting hidden goals. Chose to ground all AI in cited real data.
- GPT-5.6 technique used: (planned) role-separated stakeholder agents, structured-output sim,
  retrieval-grounded grader.

## Codex session IDs (verify/refresh before submitting)
- **Builder session (SUBMIT THIS - majority of core functionality):** `019f6d16-cb05-7582-8c12-26b61f7b292a`
- Architect session (planning, not submitted): `019f6d16-ffa3-7d42-afa6-c53f0490fa4e`
- Codex v0.144.5. AGENTS.md loaded in both sessions.
- NOTE: restarting a session assigns a NEW id. Re-grab the builder id with `/status` after the
  main build is complete, and confirm it's the thread where most core functionality was built.

## Session configuration (deliberate model/reasoning choice per role)
| Session | Model | Reasoning | Rationale |
|---|---|---|---|
| Architect (design/spec) | gpt-5.6 **Sol** | **Max** | One-off deep design: JSON contracts, scenario schema, eval cases. Detail-focused model + top single-problem reasoning, run in short bursts. |
| Builder (implementation) | gpt-5.6 **Terra** | **High** | Coding workhorse at standard build level for high-volume iteration; escalate individual hard bugs to Extra High/Max on demand. |
- Reasoning ladder (Codex): Low < Medium < High < Extra High < Max < Ultra.
- Credit discipline: keep Builder at High; escalate only when stuck. Credits (not weekly limit)
  are the real constraint - weekly reset is 23 Jul, after the 21 Jul deadline.

<!-- Codex: append new entries below this line -->

### 2026-07-16 - Multi-factor decision architecture specs
- What Codex did: Unified the simulator, grader, and policy-brief JSON contracts around a
  four-factor fit assessment; drafted the grounded scenario and five-case runtime eval suite.
- Human decision: Replaced salinity-only matching with salinity, seasonality/duration, freshwater
  access, and farmer capital; required every mismatch to be graded and to worsen simulated outcomes.
- GPT-5.6 technique used: Structured output, retrieval-grounded multi-factor grading, and
  role-separated stakeholder evals.

### 2026-07-17 - Project scaffold and health check
- What Codex did: Created the React, Vite, TypeScript, Express, and Three.js-ready scaffold with a
  Vite API proxy and a verified `GET /health` route; added secret-safe environment configuration.
- Human decision: Approved network dependency installation and selected `OPENAI_MODEL=gpt-5.6` as
  the default configurable runtime model.
- GPT-5.6 technique used: Native structured-output SDK integration is prepared through Zod-backed
  contracts for the next runtime feature.

### 2026-07-17 - Scenario engine boundary
- What Codex did: Added a validated scenario loader, derived four-factor decision contexts from
  test-site data, and served a sanitized scenario endpoint that excludes stakeholder hidden goals.
- GPT-5.6 technique used: Scenario-driven data retrieval and role-separated private context.

### 2026-07-17 - Grounded GPT-5.6 runtime boundary
- What Codex did: Added external prompt and source loaders plus validated stakeholder, simulation,
  feedback, and policy-brief API services with native Zod-derived structured outputs and retries.
- Human decision: Required the OpenAI native structured-output response format while retaining
  Zod validation as a reject-and-retry safety net.
- GPT-5.6 technique used: Native structured output, retrieval grounding, role-separated agents,
  and cross-artifact contract validation.

### 2026-07-17 - Student investigation flow
- What Codex did: Built the responsive 3D field scene, water testing, stakeholder interviews,
  intervention proposal, five-year model view, feedback/revision loop, and final policy brief UI.
- GPT-5.6 technique used: Scenario-driven interaction flow, stakeholder dialogue, simulated
  consequences, grounded grading, and policy-brief synthesis.
