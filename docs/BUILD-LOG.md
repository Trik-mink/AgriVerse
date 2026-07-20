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
- **Web Builder session (submit this IF the web app ships):** `019f6d16-cb05-7582-8c12-26b61f7b292a`
- **Uni-Builder session (submit this IF the Unity client ships):** `019f71fd-f4b9-7991-bdda-1c1f42203916`
  - gpt-5.6-terra, reasoning high. Started 2026-07-17 17:31. Unity phase core functionality.
- Architect session (planning, not submitted): `019f6d16-ffa3-7d42-afa6-c53f0490fa4e`
- DECISION AT SUBMISSION TIME: pick the ID matching the product actually submitted.
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

### 2026-07-17 - Immersive priority 7 stakeholder dialogue
- What Codex did: Added three local stakeholder character instances, an authoritative dialogue
  window driven by existing live agent replies, text reveal, and an optional CC0 local cue.
- Human decision: Required chat-only dialogue with no speech synthesis, browser voice, lip-sync,
  or mouth animation; audio failure must leave dialogue fully usable.
- GPT-5.6 technique used: Rendered only the existing validated stakeholder response as dialogue and
  kept the cue outside runtime scoring and contracts.

### 2026-07-17 - Immersive priority 8 future field rail
- What Codex did: Added a selectable five-year field rail that visualizes the existing simulator
  years, measures, scores, and narratives at the Future Fields station.
- Human decision: Required the exact structured simulation values to remain the authoritative
  consequence presentation.
- GPT-5.6 technique used: Bound the visual rail directly to validated simulator JSON rather than
  deriving or inventing projections.

### 2026-07-17 - Immersive priority 9 reflection finale
- What Codex did: Added a Reflection Pavilion certificate that appears with the existing policy
  brief and resolves the current player name and intervention labels through live state and scenario data.
- Human decision: Required the certificate to be additive while the full generated policy brief
  remains readable and authoritative.
- GPT-5.6 technique used: Kept generated brief JSON untouched and used scenario IDs for the
  presentation-only certificate label.

### 2026-07-17 - Immersive WebGL context-loss recovery
- What Codex did: Added a direct `webglcontextlost` listener, stopped the renderer loop, routed
  loss through the immersive boundary to classic mode, and reduced the world to a single low-power
  canvas with capped DPR, primitive active-station scenery, and no mounted glTF characters.
- Human decision: Prioritized runtime stability over character fidelity; deferred all 3D characters
  and required browser verification of a six-station walkthrough plus forced fallback.
- GPT-5.6 technique used: Preserved all scored systems and student state while isolating a GPU
  presentation failure behind progressive enhancement.

### 2026-07-17 - Immersive portraits and low-poly landscape
- What Codex did: Added optimized portrait dialogue with name-badge fallback, restored a primitive
  low-poly paddy/channel scene using instanced plants and lazy active-station props, and fixed the
  interview gate to open after water testing.
- Human decision: Kept characters 2D, deferred all 3D people, and required a single-canvas,
  low-power landscape verified through a real-browser six-station walkthrough.
- GPT-5.6 technique used: Reused live validated stakeholder text while binding portraits by
  scenario stakeholder order and keeping all world content presentation-only.

### 2026-07-16 - Immersive journey presentation plan
- What Codex did: Specified a six-station continuous first-person journey, persistent guide,
  camera and speech pipeline, licensed asset path, state-preserving fallback, and July 21 cut order.
- Human decision: Limited the redesign to presentation, kept every scored GPT-5.6 system and
  contract unchanged, and required an original stylized look grounded in real Mekong references.
- GPT-5.6 technique used: Preserved role-separated live dialogue, structured simulation output,
  grounded grading, and policy-brief generation behind a new presentation boundary.

### 2026-07-17 - Immersive first-frame and contrast polish
- What Codex did: Forced an initial WebGL compile and invalidated first frames, strengthened station-panel text contrast, and warmed the low-poly paddy lighting with softer palm silhouettes.
- Human decision: Required an immediate visible landscape, readable water results, and browser-verified stability without increasing GPU cost.
- GPT-5.6 technique used: Kept the presentation layer separate from the validated scenario-driven runtime systems.

### 2026-07-17 - Immersive automatic classic fallback
- What Codex did: Removed the manual classic-view control from the immersive interface while retaining automatic capability, canvas-failure, and context-loss handoff to the classic investigation.
- Human decision: Kept the safety fallback mandatory but removed the visible manual escape control from the field journey.
- GPT-5.6 technique used: Preserved the stateful scenario journey while applying progressive enhancement only at the presentation boundary.

### 2026-07-17 - Render-confirmed immersive arrival
- What Codex did: Replaced the timed wake veil with a warm arrival overlay that releases only after two completed WebGL frames, then fades into the existing landscape.
- Human decision: Required the student to see a warm arrival state rather than a blank render surface while the scene initializes.
- GPT-5.6 technique used: Kept render-readiness state inside the presentation layer without changing scenario data or scored GPT-5.6 systems.

### 2026-07-17 - Real-photo 2.5D station backdrops
- What Codex did: Replaced the immersive WebGL world with decoded, optimized Commons-photo layers that cross-fade without unmounting the outgoing backdrop; converted the classic map to DOM as well.
- Human decision: Chose real photos and no immersive WebGL to eliminate context-loss and blank-transition risk, while retaining automatic classic fallback and all existing journey mechanics.
- GPT-5.6 technique used: Preserved scenario-driven station state and the scored runtime systems behind a presentation-only media layer.

### 2026-07-17 - Video-capable station backdrops
- What Codex did: Added active-station-only muted video layers that fade in over decoded poster images, unload on station exit, and fall back to the poster on media failure; removed the remaining manual classic-view control.
- Human decision: Required no WebGL, no blank media states, reduced-motion posters, and automatic classic fallback only.
- GPT-5.6 technique used: Kept the presentation layer separate from the scenario-driven, structured-output runtime systems.

### 2026-07-17 - Unity scenario connectivity checkpoint
- What Codex did: Added Unity-safe Git/LFS rules, a tested scenario DTO and title presenter, exact-origin Express CORS, Editor/Web connectivity checks, and a successful Unity Web build.
- Human decision: Required reuse of the existing server and assets, server-only OpenAI credentials, a primitives-first checkpoint, and the locked investigation-to-brief implementation order.
- GPT-5.6 technique used: Preserved the scenario-driven sanitized data boundary while leaving all runtime GPT systems server-side and unchanged.

### 2026-07-17 - Unity Investigation water testing
- What Codex did: Added scenario-driven gray test-site markers, explicit sample collection, a persistent Evidence Notebook, and an all-sites-recorded interview gate; verified the live three-site flow in Unity Play Mode.
- Human decision: Required Investigation only, existing sanitized backend data, no polish, and manual Unity setup instructions before Interviews begins.
- GPT-5.6 technique used: Kept evidence structured and copied only sanitized scenario readings and source IDs for later citation.

### 2026-07-17 - Unity stakeholder interviews
- What Codex did: Added scenario-driven stakeholder markers, session-persistent Q&A histories, live GPT-backed chat with retry handling, served portrait assets with a name-badge fallback, and the all-stakeholders-replied plan gate.
- Human decision: Required the existing stakeholder endpoint and assets, primitives only, and Play Mode verification with one real question per stakeholder before the plan stage.
- GPT-5.6 technique used: Preserved role-separated stakeholder prompting and recorded only the returned public dialogue for later-stage evidence.

### 2026-07-17 - Unity Investigation/Interviews input handoff
- What Codex did: Hid and deactivated the entire Interviews stage until all water readings are recorded, disabled raycast targets on passive UI graphics, and added a combined full-loop Play Mode regression test.
- Human decision: Required water-test cubes to remain clickable with both bootstraps present and prohibited overlapping stage panels.
- GPT-5.6 technique used: Kept the stage gate deterministic from the session notebook before allowing live stakeholder dialogue.

### 2026-07-17 - Unity readable layout and Plan Builder
- What Codex did: Organized the runtime into stable top, left, right, and bottom regions; added a scenario-driven plan form, persisted the complete simulator response, and displayed the overall fit confirmation.
- Human decision: Required usability before polish, one active activity panel at a time, no consequence display yet, and a real simulation round trip in Play Mode.
- GPT-5.6 technique used: Sent the existing structured simulation contract unchanged and retained its validated response for later stages.

### 2026-07-17 - Unity Consequences display
- What Codex did: Added a read-only, navigable five-year simulator-result display with all fit fields, outcome fields, tradeoffs, source IDs, and a local feedback-stage unlock.
- Human decision: Required the stored simulator JSON to remain authoritative, primitive readable regions, and no feedback request at this stage.
- GPT-5.6 technique used: Preserved the validated structured simulator response as canonical data and rendered numeric JSON tokens without recomputation or rounding.

### 2026-07-17 - Unity feedback, revision, and policy brief
- What Codex did: Added live feedback and policy-brief requests using the existing contracts, scrollable read-only result panels, retry handling, and a revision path that preserves the plan while replacing downstream results after resimulation.
- Human decision: Required the full primitive Unity loop, authoritative structured outputs, retained evidence/interviews, and a final `Investigation complete` state before the Sunday gate.
- GPT-5.6 technique used: Used the retrieval-grounded grader and policy-brief generator as separate validated runtime systems, carrying the complete simulator JSON and recorded stakeholder dialogue forward.

### 2026-07-17 - Unity chat-to-plan layout handoff
- What Codex did: Replaced implicit plan activation with an explicit Continue to planning handoff, prevented interview-stage reactivation after the handoff, and added a live Play Mode regression for the chat-open gate transition.
- Human decision: Required exactly one active left activity panel and a single top instruction line when the plan gate unlocks.
- GPT-5.6 technique used: Preserved the recorded live stakeholder dialogue until the learner explicitly advances to the plan stage.

### 2026-07-17 - Unity structural stage-panel manager
- What Codex did: Centralized every Unity stage transition in one panel manager, replaced duplicated status labels with one shared instruction slot, added Feedback-to-Consequences return navigation, and hid portraits until a stakeholder is selected.
- Human decision: Required a structural all-stage fix for stacked panels, no duplicate top or button text, and regression coverage across the full learning-loop transition sequence.
- GPT-5.6 technique used: Kept each validated runtime result authoritative while separating presentation-state transitions from the GPT-backed learning systems.

### 2026-07-17 - Unity reachable long-form content
- What Codex did: Added one shared scroll-view contract for the Evidence Notebook, interview history, Consequences, Feedback, and policy brief, with wheel/trackpad support and visible-on-overflow scrollbars.
- Human decision: Required every clipped content region to be reachable, including the policy brief's final `Investigation complete.` line, and required the transition regression to enforce it.
- GPT-5.6 technique used: Kept the complete grounded outputs intact and navigable rather than truncating or rewriting them for presentation.

### 2026-07-18 - Unity scroll input and scene markers
- What Codex did: Replaced ScrollRect UI hit surfaces with bounded Input System wheel/drag handling so scroll cards remain usable without intercepting 3D marker clicks; extended the click/transition regressions accordingly.
- Human decision: Required stakeholder cylinders to remain clickable while all visible content regions retain scrolling.
- GPT-5.6 technique used: Kept the interaction layer deterministic and separate from the live GPT-backed stakeholder and assessment calls.

### 2026-07-18 - Unity scroll-card text inset
- What Codex did: Corrected the shared scroll content's vertical anchor and added a stable top-left gutter so placeholder and long-form text render inside, rather than clipped by, the card mask.
- Human decision: Flagged the chat-history placeholder as visibly clipped in live play.
- GPT-5.6 technique used: Reused one runtime UI primitive so the correction applies consistently across every long-form result view.

### 2026-07-18 - macOS URP runtime primitive material
- What Codex did: Added a serialized Resources-backed URP Unlit material and applied clones to the runtime ground, cubes, and stakeholder cylinders so the standalone retains its required surface shader while preserving gray marker colors and colliders.
- Human decision: Required a narrow material-reference fix for macOS magenta primitives, without changing URP, runtime data, API behavior, or learning flow.
- GPT-5.6 technique used: Kept the presentation fix isolated from the scenario-driven and GPT-backed learning systems.

### 2026-07-18 - Unity stylized Mekong environment
- What Codex did: Added a procedural URP-lit Mekong field with paddy rows, water channel, paths, dock, shelter, palms, horizon, and warm lighting; guarded marker raycasts and channel-ripple bounds with Unity tests.
- Human decision: Approved Environment Pass 1 after visual review, while requiring gray markers, existing learning flow, and data systems to remain unchanged.
- GPT-5.6 technique used: Kept the presentation layer data-neutral and isolated from the scenario-driven, GPT-backed learning loop.

### 2026-07-18 - Unity Reality Spike 1
- What Codex did: Added a licensed An Giang canal photo-led arrival view, compact at-rest Investigation UI, subtle camera/water motion, automatic procedural fallback, and raycast/fallback tests.
- Human decision: Approved the spike as a technical baseline, preserving the licensed-photo attribution and fallback while deferring final embedded landscape art to Reality Spike 2.
- GPT-5.6 technique used: Kept the geographic presentation distinct from scenario evidence and runtime GPT-backed learning behavior.

### 2026-07-18 - Unity cinematic stakeholder interviews
- What Codex did: Added a photo-first stakeholder selection and interview shell with compact progress HUD, portrait cards, bounded dialogue, retry states, and a focused Evidence Notebook drawer.
- Human decision: Approved the corrected cinematic direction after requiring one readable response region, a distraction-free evidence overlay, hidden world-marker remnants, and simplified identity cards.
- GPT-5.6 technique used: Rendered validated role-separated stakeholder replies verbatim while keeping live requests, conversation state, evidence, and progression in the existing Unity controllers.

### 2026-07-18 - Unity Salt Line identity and investigation framing
- What Codex did: Wired local player naming, four portrait presets, authored Mai guidance, predict-before-reveal water testing, the inline glossary, scenario presentation fields, and exact five-year visual mappings.
- Human decision: Declared the Salt Line script final, required local identity to stay outside scored requests, and required predictions to carry no penalty before authoritative readings appear.
- GPT-5.6 technique used: Kept the four live GPT systems and validated JSON untouched while preparing presentation directly from sanitized scenario data and canonical simulator tokens.

### 2026-07-18 - Unity revision comparison and mission completion
- What Codex did: Preserved original and revised simulator results, added authoritative Future Walk comparison mappings, a citation-audited Judge View, named field-service certificate, and respectful ending choices.
- Human decision: Required revision to represent responsible professional practice, retained exact backend values, and kept player identity outside scored requests.
- GPT-5.6 technique used: Exposed role-separated agent state and raw validated structured outputs without changing them, then audited every result document against the sanitized source registry.

### 2026-07-18 - Licensed art intake and Mai CharacterLab
- What Codex did: Preserved and provenance-recorded the supplied Tripo, Poly Haven CC0, and CC0 audio sources; added deterministic Unity import rules plus an isolated URP CharacterLab validating Mai's Humanoid rig, materials, look-at behavior, and five named motions.
- Human decision: Confirmed redistribution and derivative rights for the Tripo models, supplied the complete art/audio bundle, and required the original rice and grass FBX files to remain unchanged while optimized derivatives are built separately.
- GPT-5.6 technique used: Kept the character and asset pipeline presentation-only, isolated from prompts, scenario facts, validated outputs, and all four runtime GPT systems.

### 2026-07-18 - Unity An Giang 3D WorldLab
- What Codex did: Built an isolated walkable canal-and-paddy lab with matte CC0 PBR terrain, GPU-instanced three-LOD rice/grass, correctly oriented authored vegetation and structures, coherent wind, water, atmosphere, collision, and layered CC0 ambience.
- Human decision: Supplied the licensed art/audio set, required source rice and grass FBXs to remain unchanged, and prioritized a compact grounded first-person world over another procedural-primitive presentation.
- GPT-5.6 technique used: Kept the quality lab and asset derivatives separate from scenario facts, prompts, backend contracts, deterministic outputs, and the four live GPT systems.

### 2026-07-19 - Unity playable 3D investigation vertical slice
- What Codex did: Added a separate first-person Episode3DAlpha with Mai's canal arrival, a configuration-mapped upstream prediction and physical sample interaction, exact sanitized reading, focused notebook, ambience, and a Metal build.
- Human decision: Approved Human Gate B after personally verifying movement, exploration, sampling, interaction, and notebook feel; required moving directly to complete full-loop integration.
- GPT-5.6 technique used: Reused the scenario-backed investigation and evidence contracts unchanged while keeping all prompts and GPT systems server-side.

### 2026-07-19 - Complete Unity 3D learning journey
- What Codex did: Expanded the approved field slice into a globe-to-ending desktop journey with three physical sample stations, world-return stakeholder interviews, planning, five-year Future Walk, feedback/revision comparison, brief, Judge View, certificate, endings, and a responsive presentation pass.
- Human decision: Approved Human Gate B and required autonomous integration through a single concentrated beauty pass and macOS release candidate without intermediate visual gates.
- GPT-5.6 technique used: Preserved the four live role-separated and structured-output systems unchanged, kept exact validated simulator tokens authoritative for original/revised visuals, and completed one approved live end-to-end verification.

### 2026-07-19 - Premium Unity asset intake and identity correction
- What Codex did: Preserved twelve new Tripo packages and the NASA globe bundle byte-for-byte,
  recorded all hashes and rights, built URP/LOD/collider derivatives, and validated three
  retargetable Humanoid stakeholder prefabs.
- Human decision: Authorized redistribution, removed avatar selection/upload, retained name
  entry and an unseen first-person participant, and kept dialogue text-first without voice.
- GPT-5.6 technique used: Preserved the existing grounded stakeholder agents and all scored
  contracts while changing only local presentation identity and licensed art.

### 2026-07-19 - Premium 3D stakeholder journey
- What Codex did: Integrated three licensed Humanoid stakeholders, physical field stations and planning table, cinematic focus/gesture states, a tabbed Field Journal, accessibility controls, and a compact environment-first Future Walk into the complete 3D journey.
- Human decision: Required a first-person unseen player, text-first conversations with body gestures, a premium field-journal interface, and completion of the full loop before final polish.
- GPT-5.6 technique used: Kept live role-separated dialogue and every validated structured output authoritative while changing only presentation, navigation, and scenario-configured world placement.

### 2026-07-19 - AgriVerse macOS release candidate
- What Codex did: Completed the live arrival-to-brief regression, universal fullscreen Metal build, 1280x720 and 1920x1080 visual checks, performance/error scan, asset-hash/LFS/attribution audit, and native AgriVerse bundle identity.
- Human decision: Required one complete release candidate after the full loop, with no avatar body or voice pipeline and no intermediate visual gates.
- GPT-5.6 technique used: Verified all four real runtime systems live once for completion while using deterministic tests for repeated contract, citation, fallback, and presentation checks.

### 2026-07-19 - Interactive Field Network and Living Field Atlas reliability
- What Codex did: Added the data-driven five-location orbital network, offline packaged-scenario
  recovery, complete keyboard pin navigation, clean landing/world handoff, Living Field Atlas
  presentation, and stable derived wrappers for characters and station props.
- Human decision: Approved the globe, field network, world layout, upright assets, and full
  learning loop; required the atlas identity, then discovered the silent-offline globe and
  contaminated mixed-executable build that became release regressions.
- GPT-5.6 technique used: Preserved every live prompt, structured contract, citation, and
  validated outcome while keeping offline behavior limited to scenario discovery and explicit
  connection-required states.

### 2026-07-19 - OpenAI Build Week release preparation
- What Codex did: Audited current files, history, LFS, secrets, privacy, dependencies, assets,
  licenses, and source hashes; hardened a fresh universal macOS release pipeline; created public
  documentation and notices; and verified the signed archive after extraction.
- Human decision: Required a publication gate with no push, tag, visibility change, release
  upload, backend deployment, code-license assumption, or secret exposure before explicit
  approval.
- GPT-5.6 technique used: Ran one live completion regression across all four runtime systems and
  used contract-valid recorded responses for repeated visual capture to conserve API budget.

### 2026-07-20 - Judge View and return-to-network release UX
- What Codex did: Replaced raw-JSON-first Judge View output with a human-readable validated record plus optional technical disclosure, and added a clean certificate-to-globe reset that supports a fresh name and mission.
- Human decision: Limited the final release UX correction to Judge View readability and an unambiguous Return to Field Network action without redesigning approved systems.
- GPT-5.6 technique used: Preserved the exact simulator, grader, policy-brief, evidence, and citation documents while changing only their presentation and reset lifecycle.

### 2026-07-20 - Public judge backend safety controls
- What Codex did: Added strict HTTP validation, bounded sessions/rates/concurrency, sanitized observability, judge-window expiration, and an atomic persistent US$9 model-budget ledger with conservative pre-call reservations.
- Human decision: Authorized one secure temporary judge backend with an absolute US$10 OpenAI ceiling, a US$9 application cap, and no client-side secret or fabricated AI fallback.
- GPT-5.6 technique used: Kept Responses API calls, versioned prompts, grounded inputs, strict structured outputs, and citation validation intact while bounding every paid request.

### 2026-07-20 - Main branch and public judge documentation
- What Codex did: Consolidated the verified release onto `main`, published the privacy-audited source and LFS history with preserved milestone tags, and prepared judge, video, Devpost, and troubleshooting documentation.
- Human decision: Authorized the first public GitHub publication while reserving YouTube, Devpost, `/feedback`, and any cloud billing confirmation for the human.
- GPT-5.6 technique used: Documented the four live runtime systems and distinguished the one authoritative live journey from recorded presentation replays used to conserve the judge budget.

### 2026-07-20 - Render production-build dependency correction
- What Codex did: Diagnosed the first Blueprint deploy failure, made the production build install compile-time TypeScript dependencies before pruning them, and added a release-configuration regression test.
- Human decision: Added the authorized Render billing method and applied the single-service Blueprint without expanding the approved cloud scope.
- GPT-5.6 technique used: Preserved the secured Responses API runtime unchanged while repairing only its deterministic production compilation path.

### 2026-07-20 - Secure hosted judge service
- What Codex did: Deployed one Starter Render service with a 1 GB persistent budget ledger, verified secret-free health and scenario delivery, and centralized the macOS release client on its HTTPS origin while retaining localhost Editor overrides.
- Human decision: Authorized a temporary non-sleeping judge service under the US$8 Render cap and a durable US$9 OpenAI application ceiling.
- GPT-5.6 technique used: Kept the API key in the linked server-side secret group and preserved the four validated Responses API systems without client-side prompts, secrets, or fabricated fallbacks.
