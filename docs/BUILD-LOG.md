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

### 2026-07-17 - Immersive scope correction
- What Codex did: Updated the immersive presentation specification to remove Azure speech,
  visemes, VRM, custom character work, and the presentation speech-token endpoint.
- Human decision: Initially chose browser-only optional speech and Quaternius CC0 characters used
  as-is with code-only palette treatment; the later dialogue correction supersedes speech.
- GPT-5.6 technique used: Preserved the existing role-separated runtime while isolating all
  presentation decisions from scored systems.

### 2026-07-17 - Immersive dialogue correction
- What Codex did: Replaced the browser-speech plan with authoritative on-screen dialogue, an
  optional local CC0 UI cue, and generic character gestures only.
- Human decision: Removed all speech synthesis in favor of chat-based dialogue that works silently.
- GPT-5.6 technique used: Kept validated stakeholder text as the sole dialogue source while
  isolating optional presentation feedback from scored runtime behavior.

### 2026-07-17 - Immersive priority 1 foundation
- What Codex did: Added the landing choice, persistent full-screen canvas, six semantic station
  anchors, WebGL error boundary, and a state-preserving classic fallback.
- Human decision: Required the immersive and classic render paths to be confirmed before camera
  work begins.
- GPT-5.6 technique used: Preserved the scenario-driven runtime state while isolating immersive
  presentation as progressive enhancement.

### 2026-07-17 - Immersive priority 2 camera direction
- What Codex did: Added a six-stop guided camera rail with eased travel, station focus changes,
  and reduced-motion cuts while retaining the classic fallback.
- Human decision: Required guided camera crossfades and cuts before environment production.
- GPT-5.6 technique used: Kept the camera sequence data-driven from semantic station anchors.

### 2026-07-17 - Immersive priority 3 environment
- What Codex did: Replaced the placeholder terrain with a stylized channel, field plots, shelter,
  paths, and selected local CC0 vegetation, crop, boat, and bridge glTF assets.
- Human decision: Required a licensed local paddy-and-channel world before moving existing
  gameplay into physical stations.
- GPT-5.6 technique used: Isolated decorative asset failures from the scenario-driven runtime and
  recorded exact local asset provenance.

### 2026-07-17 - Immersive priority 4 station activities
- What Codex did: Mounted the existing water, interview, proposal, simulation, feedback, and
  policy-brief components at their physical stations without duplicating their runtime logic.
- Human decision: Required classic mode to remain a state-preserving fallback for all real gameplay.
- GPT-5.6 technique used: Reused validated role-agent and structured-output result surfaces through
  a presentation-only adapter.

### 2026-07-17 - Immersive priority 5 entry sequence
- What Codex did: Added local display-name entry, four code-only color presets, and a wake-up
  camera/focus sequence with a reduced-motion-safe cut.
- Human decision: Requested presets and presentation-only identity state, without custom character
  modeling or rigging work.
- GPT-5.6 technique used: Kept all player-selection data local and outside immutable scenario data.

### 2026-07-17 - Immersive priority 6 field guide
- What Codex did: Added a persistent local Quaternius CC0 guide, station-aware guide cues, and
  root-level idle/lead motion with a primitive fallback when the decorative model cannot load.
- Human decision: Required an as-is Quaternius character with code-only palette treatment and no
  speech, lip-sync, custom modeling, or rigging work.
- GPT-5.6 technique used: Kept guide cues generic and scenario-safe while isolating all character
  presentation from the scored runtime.

### 2026-07-16 - Immersive journey presentation plan
- What Codex did: Specified a six-station continuous first-person journey, persistent guide,
  camera and speech pipeline, licensed asset path, state-preserving fallback, and July 21 cut order.
- Human decision: Limited the redesign to presentation, kept every scored GPT-5.6 system and
  contract unchanged, and required an original stylized look grounded in real Mekong references.
- GPT-5.6 technique used: Preserved role-separated live dialogue, structured simulation output,
  grounded grading, and policy-brief generation behind a new presentation boundary.
