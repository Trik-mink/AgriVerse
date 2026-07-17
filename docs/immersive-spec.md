# AgriVerse Immersive Presentation Specification

**Status:** Active presentation specification for the `immersive` branch  
**Specification date:** 2026-07-16  
**Delivery deadline:** 2026-07-21  
**Scope:** Presentation only. No scored GPT-5.6 behavior, prompt, scenario, endpoint
contract, or structured-output contract may change.

## 0. Accepted Scope Overrides (2026-07-17)

The following decisions supersede any conflicting instruction in this document:

- Removed from scope: Azure Speech, the presentation speech-token endpoint, timed visemes,
  viseme mapping, authored mouth targets, VRM / `three-vrm`, and custom 3D character modeling.
- Dialogue is chat-based only. The visible dialogue window and caption are authoritative; no
  browser or cloud speech synthesis is used.
- A new message may reveal briefly and play one local CC0 UI cue. Audio failure leaves the text in
  place and never blocks progress. Characters may use a generic talking gesture with no mouth
  animation.
- Characters use the Quaternius Universal Base Character CC0 family as-is. The guide,
  stakeholders, and four player presets are differentiated with in-code palette and simple
  clothing-color treatment only; no asset authoring or rigging work is required.

## 1. Decision Summary

AgriVerse becomes one guided, first-person journey through a single persistent
Three.js scene. The existing six workflow states still own progression and all
runtime calls. The immersive presentation maps those states onto six physical
stations, moves the camera between them, and renders the existing controls and
validated GPT-5.6 outputs as accessible DOM overlays.

The implementation must preserve the current UI as a live fallback. Immersion is
progressive enhancement, not a second application:

- One `App` state tree continues to own the investigation.
- No immersive component calls a scored endpoint directly.
- A failed model, audio request, post-processing pass, or canvas must not erase
  state or force a GPT request to run again.
- Every factual number shown in the immersive view comes from the unchanged
  scenario or a validated endpoint response.
- The original policy brief remains the final substantive artifact. The
  certificate is an additional presentation layer, not a replacement.
- Immersive TypeScript never branches on a scenario ID or hardcodes a
  stakeholder name, site label, measurement, or intervention. The current
  Mekong scenario fills reusable station roles.

### Non-goals

- No free-roaming open world, pointer-lock first-person controller, combat, or
  physics simulation.
- No new agricultural rules, computed outcomes, thresholds, or explanatory
  claims.
- No changes to `scenario.json`, `docs/json-contracts.md`, or `prompts/`.
- No imitation of a specific film, character, logo, costume, or other protected
  Disney or Pixar property.
- No dependence on a hosted avatar creator or asset CDN during the demo.

## 2. Experience And State Mapping

The apparent journey has six physical stations. The existing `View` values in
`src/App.tsx` remain the source of truth even though interviews span three
locations.

| Physical station | World treatment | Existing state | Existing activity |
|---|---|---|---|
| 1. The Paddy | Rice plots beside a water channel and field shelter | `explore`, then the first part of `interviews` | Test all three labeled water samples; meet and interview Mr. Ba |
| 2. Research Post | Open-sided field lab with maps and sample jars | `interviews` | Interview Dr. Linh |
| 3. District Office | Small civic office beside the channel | `interviews` | Interview Ms. Hoa |
| 4. Planning Dock | Shaded table overlooking the fields | `proposal` | Select intervention, parameters, support measures, target site, and rationale |
| 5. Future Fields | Five adjacent field slices or one field with a five-step time rail | `consequences` | Present the unchanged five-year simulation and its fit assessment |
| 6. Reflection Pavilion | Quiet waterside review area that becomes the finale | `feedback`, then `brief` | Review, revise, create the policy brief, and reveal the certificate |

This mapping resolves the mismatch between six workflow states and three
stakeholder locations:

- `explore` starts at Station 1.
- `interviews` is one unchanged workflow state with three ordered location
  anchors: Station 1, Station 2, and Station 3.
- `proposal` through `brief` retain their current order.
- `brief` does not create a seventh station. It transforms Station 6 from a
  review setting into the finale after the unchanged brief response arrives.

The world is a compressed educational diorama, not a geographic scale model.
The three test sites remain individually labeled with their scenario-provided
names. The scene must never imply that the coastal, mid-delta, and upstream plots
are literally adjacent.

### Progression Rules

Existing gates in `src/flow/progress.ts` remain authoritative:

1. The guide does not lead away from Station 1 until every test site is recorded.
2. The Planning Dock stays unavailable until all three stakeholder IDs have a
   response.
3. The simulation remains unavailable until the existing target, testing,
   interview, intervention, and rationale requirements pass.
4. Feedback can send the player back to Station 4 using the existing
   `onRevise` behavior.
5. A completed state may be revisited without reissuing its GPT request.

Locked progression is represented in-world by the guide waiting at the next
path marker and by a concise DOM status. It is not represented as tab
navigation.

## 3. Continuous World

### Layout

The scene follows a clockwise loop around one broad water channel:

```text
                    [5 Future Fields]
                           |
        [6 Reflection] -- channel -- [4 Planning Dock]
               |                         |
          wake-up point             [3 District Office]
               |                         |
          [1 The Paddy] -------- [2 Research Post]
```

Exact coordinates belong in a presentation-only station configuration. They are
semantic anchors, not scenario data. A recommended starting scale is:

| Anchor | Relative position | Camera arrival |
|---|---:|---|
| Wake-up | `(0, 1.6, 0)` | Seated/low view that rises to normal eye height |
| Paddy | `(0, 0, -8)` | Across the channel toward the sampling area |
| Research Post | `(11, 0, -5)` | Three-quarter view with Dr. Linh in front of the lab |
| District Office | `(17, 0, 3)` | Shaded entry with Ms. Hoa near the desk |
| Planning Dock | `(10, 0, 12)` | Facing the planning table and fields |
| Future Fields | `(0, 0, 16)` | Centered on the five-year display |
| Reflection Pavilion | `(-10, 0, 8)` | Facing the review surface, then turning toward the finale |

The coordinates may be tuned for framing, but their order and semantic roles
must not change.

### Navigation Model

- Keep one full-viewport R3F `Canvas` mounted for the whole journey.
- Use a directed camera rail rather than WASD movement. This avoids students
  getting lost and keeps station UI reliably framed.
- At a station, allow limited pointer/touch look within a comfortable yaw and
  pitch range. Do not require pointer lock.
- The primary command is contextual: test, ask, continue, simulate, review,
  revise, or finish. The guide walks toward the next anchor when that command
  completes the current requirement.
- Camera travel follows a pre-authored spline for 2 to 3.5 seconds with
  ease-in/ease-out. Disable station actions during travel.
- During revision, travel backward from Station 6 to Station 4. Existing
  simulation and feedback remain in memory until the current code clears them.
- A small six-stop progress compass may show current/completed/locked status,
  but it is not clickable tab navigation. Backtracking is offered through
  contextual in-world commands only.
- DOM controls remain outside the canvas so camera blur never makes required
  text unreadable.

### Persistent Scene Behavior

- Load the terrain, water, path, Station 1, and guide first.
- Stream the next station before travel begins.
- A missing decorative asset gets a primitive or hidden replacement; it never
  blocks the station.
- Stakeholder and guide roots stay mounted. They move, hide, or switch animation
  clips without recreating the canvas.
- Use one sunlight rig, baked ambient occlusion where practical, instanced crop
  rows, and conservative shadow casting.

## 4. Cinematic Intro

### Screen Flow

1. **Landing:** Existing product identity, literal start command, and a visible
   "Use classic view" option.
2. **Name entry:** One text field, 1 to 40 visible characters. Trim surrounding
   whitespace. Store locally for the session and certificate only.
3. **Character selection:** Four curated, locally hosted stylized presets from
   the same art family. Show a rotating bust or full character, name-neutral
   color swatches, and an accessible static thumbnail.
4. **Enter world:** The confirmation click initializes audio capability and
   starts the wake-up sequence. No audio starts before this user gesture.
5. **Wake-up:** The first-person view opens at the field shelter beside the
   paddy. The guide comes into focus, greets the player by display name, and
   points toward the field kit.

The selected player avatar is visible during selection, in a small journey
portrait, and on the certificate. First-person hands or a reflection are
optional and must not be required to make the selection feel consequential.

### Wake-up Camera And Focus Rack

| Time | Camera | Image treatment | Audio/UI |
|---:|---|---|---|
| 0 to 0.3 s | Camera fixed at the wake-up anchor | Black eyelid overlay | Silence |
| 0.3 to 0.9 s | Very small breathing sway | Eyelid opens; scene remains heavily defocused | Ambient channel audio fades in |
| 0.9 to 2.2 s | Camera focus target moves from near field to the guide | Depth of field resolves from strong bokeh to a focused guide | Guide wave begins |
| 2.2 to 3.0 s | Camera rises smoothly to standing eye height and settles | Bokeh returns to normal gameplay level | Guide greeting and first action appear |

Use `@react-three/postprocessing` with `EffectComposer` and `DepthOfField`.
Its current R3F wrapper documents normalized focus distance, focal length, and
bokeh controls; `Autofocus` also supports a target and manual updates
([DepthOfField docs](https://react-postprocessing.docs.pmnd.rs/effects/depth-of-field),
[Autofocus docs](https://react-postprocessing.docs.pmnd.rs/effects/autofocus)).

Implementation constraints:

- Animate the focus target and bokeh strength, not the DOM overlay.
- Do not combine depth of field with bloom or motion blur for the initial
  release. The single focus rack supplies the intended effect at lower cost.
- Keep the guide's final face and pointing hand inside the camera safe area on
  desktop and mobile aspect ratios.
- For `prefers-reduced-motion`, skip sway, rise, and spline travel. Use a brief
  opacity crossfade into an already focused station view.
- If post-processing fails, use the same eyelid overlay plus a CSS blur-to-clear
  transition on the canvas element.
- If the canvas fails, skip the cinematic and open the classic `explore` view
  with the entered name and selected portrait still retained.

## 5. Persistent Guide

The guide is one original, stylized human field companion with rounded
proportions, expressive eyebrows, broad readable gestures, and a practical
field outfit. The guide must not resemble a known animated character or use
studio branding.

### Responsibilities

- Remain visible from wake-up through the certificate.
- Walk a few meters ahead on each camera rail and stop at the arrival marker.
- Point toward the current interactive object or stakeholder.
- Give short, presentation-only narration before and after an activity.
- Step aside and use a quiet idle animation while a stakeholder speaks.
- Celebrate completion without claiming that the selected policy is objectively
  successful.

### Narration Boundary

Guide narration is not another GPT system. It may:

- Name a station or stakeholder using values already in the scenario response.
- Restate progress such as "three samples recorded."
- Introduce an action such as "compare the five projected years."

It may not:

- Judge whether an intervention fits.
- Add a salinity threshold, outcome, causal claim, or statistic.
- Paraphrase a stakeholder response in a way that changes its meaning.
- Reveal a stakeholder's hidden goal.

All factual instruction remains in existing scenario fields or validated GPT
outputs. Generic guide copy lives in presentation-only content and interpolates
scenario labels at runtime.

### Guide State Machine

`idle -> cue -> lead -> arrive -> point -> wait -> react`

- State changes are driven by existing workflow state and completion flags.
- Animation failure falls back to a static guide pose.
- Model failure falls back to a fixed 2D guide portrait and DOM cue in the same
  screen position.
- Narration failure falls back to the always-visible caption.

## 6. Stakeholders And Chat-Based Dialogue

### Placement And Behavior

| Stakeholder | Required location | Introduction | During dialogue |
|---|---|---|---|
| Mr. Ba | The Paddy | Introduces himself after water testing unlocks interviews | Faces the camera near the field shelter; guide stands to one side |
| Dr. Linh | Research Post | Introduces herself when the camera arrives | Stands beside samples/maps; uses restrained explanatory gestures |
| Ms. Hoa | District Office | Introduces herself at the office entrance | Stands near the planning desk; uses open, diplomatic gestures |

Names, roles, persona text, and dialogue come from the unchanged scenario and
stakeholder endpoint. Clothing and props communicate occupation without
caricature. Do not infer visual traits from hidden goals.

The question form remains a DOM control. The response text appears immediately
when the existing endpoint resolves. Audio is enhancement and may arrive later.
Progress counts the existing text response, never successful speech playback.

### Required Live Pipeline

1. The student submits through the unchanged
   `POST /api/stakeholders/:stakeholderId/messages`.
2. The validated `message` is rendered verbatim in an on-screen dialogue bubble
   and caption region.
3. When a new message arrives, reveal the text with a brief optional animation
   and play one local CC0 UI pop/chime cue when browser audio permits it.
4. While the message appears, the presentation-only character may perform a
   generic talking gesture. No mouth, viseme, blend-shape, or timing pipeline
   exists.
5. On completion, cancellation, station change, or audio error, the gesture
   returns to idle. Captions remain visible throughout.

No dialogue enhancement sends prompts, hidden goals, conversation history,
student data, or a new network request. The validated text is never rewritten.

### Speech Fallback Ladder

| Failure | Fallback |
|---|---|
| Browser audio unavailable or blocked | Silent character with the full dialogue bubble |
| Local sound request fails | Keep the caption visible and complete the gesture silently |
| Avatar model missing | 2D stakeholder portrait, role label, and dialogue bubble |
| Entire canvas missing | Existing `Interviews` component |

The local sound cue is optional enhancement only; visible text is the complete
interaction. Its source and CC0 license must be recorded in
`public/assets/ATTRIBUTIONS.md`.

### Avatar Technology Evaluation

| Option | Current browser/React fit | Art and license fit | Decision |
|---|---|---|---|
| Ready Player Me | Its archived docs describe a React iframe creator, browser renderer, GLB output, and Oculus viseme morph targets | Hosted creator and model delivery would be a demo dependency; default avatar licensing also requires care | **Reject.** Ready Player Me's own site states its services were discontinued on 2026-01-31. Do not add it, even though the old [React guide](https://docs.readyplayer.me/ready-player-me/integration-guides/react/quickstart) remains online. |
| `@pixiv/three-vrm` | Not required for the presentation | Adds a runtime and expression pipeline that this build does not use | **Reject.** VRM and `three-vrm` are out of scope. |
| Local stylized glTF pack | Native fit with the repo's existing Three/R3F stack; no hosted service | CC0 pack avoids hosted demo dependency | **Recommended.** Use the Quaternius base family as-is with code-only palette treatment. |

Recommended local base: [Quaternius Universal Base Characters](https://quaternius.com/packs/universalbasecharacters.html).
The pack provides glTF/FBX/Blend files, a humanoid rig, game-ready stylized
characters, and a CC0 license. Its public page does not document speech morph
targets, so facial target authoring must be treated as required asset work, not
assumed pack functionality.

Recommended avatar production path:

1. Select one base family for the guide, three stakeholders, and four player
   presets.
2. Customize hair, clothing colors, props, face proportions, and silhouettes in
   Blender while retaining a shared material/shader language.
3. Apply code-only color and clothing palette variants to distinguish roles.
4. Validate local models or primitive presentation fallbacks in the actual R3F
   canvas. Do not alter rigs or author facial targets.

## 7. Station Presentation

### Station 1: The Paddy And Water Testing

- Place the three existing scenario test sites on a clearly labeled sample rack
  or non-scale field transect.
- Selecting a marker moves the camera a short distance or turns it toward the
  sampling object; it invokes the current `selectAndTest` behavior.
- The existing reading detail remains authoritative and displays salinity,
  season, seasonal pattern, freshwater access, note, and source IDs.
- After all samples are recorded, Mr. Ba approaches or becomes active at the
  field shelter. This begins the existing `interviews` state.

### Stations 2 And 3: Research And District Interviews

- Each station presents the same question input and response behavior as the
  existing `Interviews` component.
- The guide cannot lead onward until that stakeholder ID has a response.
- Returning to a completed stakeholder shows the stored question and response;
  it does not silently make a new call.

### Station 4: Propose

- The Planning Dock opens the current proposal controls as a focused DOM panel
  aligned over the planning table.
- All intervention names, target sites, support measures, parameters, and
  rationale fields come from existing state/scenario data.
- A four-part compass around the table may visually remind the player to
  consider salinity, seasonality, freshwater, and capital. It must not declare
  fit before the simulator does.
- "Simulate" uses the unchanged `runSimulation` action.

### Station 5: Consequences

- Present five labeled years as adjacent field slices or a single field with a
  scrubber. The exact five `years` entries remain the source of truth.
- Each year shows the existing salinity, yield data, income score,
  sustainability score, cost level, narrative, and evidence source IDs.
- Use water tint, crop density, and activity only as qualitative reinforcement
  of returned values. Never generate a sixth year or interpolate a new number.
- Show all five factor statuses from the unchanged `fit_assessment`, including
  `overall`.
- Keep the existing tradeoffs visible.
- Provide pause, previous year, next year, and skip-animation controls.
- If 3D year visualization fails, render the current `Consequences` component
  without changing state.

### Station 6: Feedback, Revision, Brief, And Certificate

- Feedback appears on a review surface with the current key insight, rubric
  results, evidence IDs/model years, revision prompt, and encouragement.
- "Revise and resimulate" runs the current callback and glides back to Station 4.
- "Generate policy brief" makes the unchanged brief request.
- When the brief arrives, keep the full current `PolicyBrief` content available
  in a readable scroll/panel.
- Then transition the environment to the finale: warmer key light, raised
  banners, guide and stakeholders in celebratory idle poses, and the
  certificate.

Certificate fields:

- Player display name from local presentation state.
- "Completed the AgriVerse field investigation."
- Chosen solution label or labels resolved from the unchanged scenario using
  `proposal.intervention_ids`.
- Scenario title and region from the unchanged scenario.
- Selected player portrait.

The certificate must not say that the player "solved," "saved," or "fixed" the
crisis. A mismatch may still reach the finale; learning and evidence-based
revision are what completion recognizes.

## 8. Visual And Asset Direction

### Style Language

Use an original family-feature-animation aesthetic:

- Warm, rounded silhouettes and softened edges.
- Expressive faces and hands with readable poses at medium distance.
- Slightly enlarged heads and eyes, but culturally respectful proportions.
- Hand-painted or simple gradient textures rather than photoreal materials.
- Cohesive stylized treatment across people, plants, water, boats, buildings,
  and UI portrait renders.
- A balanced palette of rice green, channel green-blue, clay red, sun yellow,
  white plaster, dark wood, and restrained civic blue. Do not let one hue
  dominate the entire scene.

Avoid:

- Actual studio logos, character likenesses, costume replicas, typography, or
  musical cues.
- Generic fantasy cottages standing in for Vietnamese rural architecture
  without adaptation.
- Photoreal characters in a low-poly environment.
- Decorative blur, bloom, or floating effects that obstruct evidence or
  controls.

### Real Mekong Reference Board

These references establish composition and material cues. They are not factual
sources for agricultural claims:

- [Rice paddy near Chau Doc](https://commons.wikimedia.org/wiki/File:Ride_Paddy_-_Chau_Doc_-_Vietnam.JPG):
  field scale, hut, path, and rice-row reference. Photo by Adam Jones,
  CC BY-SA 3.0.
- [Canal in the Mekong Delta at My Tho](https://commons.wikimedia.org/wiki/File:Canal_in_Mekong_Delta_-_My_Tho_-_Vietnam_(15912635245).jpg):
  narrow channel, boats, dense bank vegetation, and water color. Photo by Esin
  Ustun, CC BY 2.0.
- [Southern Vietnamese stilt house](https://commons.wikimedia.org/wiki/File:Nh%C3%A0_s%C3%A0n_Nam_B%E1%BB%99.jpg):
  roof pitch, raised floor, wall material, and waterside structure. Photo by
  Thuydaonguyen, CC BY-SA 3.0.
- [Mekong Delta rowboats](https://unsplash.com/photos/people-riding-on-boat-during-daytime-Tt4P7v7z_qo):
  boat proportions, seating, oars, and palm-lined channel. Photo by zibik,
  Unsplash License.

The initial release should use these as private production references and not
display them in the product. If a real photo is later displayed, add its exact
author, source URL, license link, and modification note to an in-product credits
view and `public/assets/ATTRIBUTIONS.md`.

### Recommended 3D Asset Sources

1. **Primary vegetation and terrain:** [Quaternius Ultimate Stylized Nature Pack](https://quaternius.com/packs/ultimatestylizednature.html).
   It supplies stylized nature assets in glTF/FBX/OBJ/Blend under CC0. Use it as
   the main visual family and build instanced rice tufts/field edges from
   adapted CC0 geometry.
2. **Secondary and low-spec fallback:** [Kenney Nature Kit](https://kenney.nl/assets/nature-kit).
   It supplies a broad 3D nature set under CC0. Its simpler assets are suitable
   for the lightweight scene tier and missing-model fallbacks.
3. **Mekong-specific props:** use [CC0 low-poly coconut trees](https://opengameart.org/content/low-poly-strange-coconut-trees)
   by FabinhoSC for palms, and the [CC0 Quaternius Boat](https://poly.pizza/m/5UEl54KsuC)
   as a base mesh for a locally reworked, photo-referenced river boat. Do not
   label the unmodified generic boat a sampan.

All three source groups permit inclusion in a public repository under their
listed CC0 terms. Still preserve a source ledger for provenance.

### Asset Intake Rule

Every committed visual asset needs an entry with:

- Local file path.
- Original title and creator.
- Source page URL.
- License name and license URL.
- Whether the file was modified.
- Optimization performed.

Prefer CC0. Do not commit raw commercial asset packs whose license prohibits
redistribution. Do not scrape or embed a model from a page that lacks an
explicit download license.

## 9. Graceful Degradation

### Capability Tiers

| Tier | Conditions | Presentation |
|---|---|---|
| A: Full immersive | WebGL2 works, core assets load, motion allowed, stable frame rate | Full scene, camera rails, post-processing, animated avatars, enhanced speech if configured |
| B: Lightweight immersive | WebGL works but post-processing, high-detail assets, or speech are unavailable | Same stations and canvas; lower-detail assets, no DOF, crossfades, text bubbles, reduced particles/shadows |
| C: Classic | No WebGL, canvas/context error, core world load failure, user preference, or explicit switch | Existing working UI and components with the same current state |

### Failure Isolation

| Failure boundary | Required behavior |
|---|---|
| Decorative environment GLB | Hide it or show a simple primitive; continue |
| One station GLB | Show a lightweight station marker and the same DOM activity |
| Guide GLB/animation | Show 2D guide portrait and cue |
| Stakeholder GLB | Show 2D portrait/name/role and live text |
| TTS/token/viseme | Follow the speech fallback ladder |
| Post-processing | CSS fade/blur transition; never fail the canvas |
| Canvas or WebGL context | Switch to classic presentation without resetting state |
| Scored API request | Keep the current error behavior and allow retry; do not fabricate output |

R3F's current `Canvas` API includes a no-WebGL fallback and recommends an error
boundary for context crashes
([Canvas documentation](https://r3f.docs.pmnd.rs/api/canvas)). Use both.

### State Preservation

- Immersive and classic presentations consume the same state and callbacks from
  `App`.
- Presentation mode changes do not clear `testedSiteIds`, `interviews`,
  `proposal`, `simulation`, `feedback`, `brief`, or the current target.
- The player can choose "Classic view" at any time.
- A canvas crash automatically selects classic mode for the remainder of the
  session and explains that the investigation is still intact.
- Returning to immersive mode is optional and never automatic after a crash.

## 10. Accessibility And Performance

### Accessibility

- Keep all required inputs, buttons, output text, captions, and errors in DOM,
  not canvas-only UI.
- Make captions on by default and provide mute, replay, and stop controls.
- Respect `prefers-reduced-motion` from the first frame.
- Provide keyboard operation without pointer lock.
- Move DOM focus to the station heading after each camera arrival.
- Mark decorative canvas content hidden from screen readers; the current
  semantic components remain the accessible representation.
- Never encode fit, mismatch, completion, or score with color alone.
- Keep a visible "Use classic view" command on landing and in settings.

### Performance Budgets

These are delivery targets, not scenario facts:

- Initial interactive payload: terrain, Station 1, guide, and UI first; defer
  later stations.
- Target 60 fps on a current laptop and a stable 30 fps lightweight tier.
- Limit device pixel ratio to a tested range rather than unbounded native DPR.
- Use instancing for rice, grass, and repeated props.
- Prefer compressed GLB textures and geometry after visual validation.
- Only nearby characters animate at full update frequency.
- One shadow-casting key light; bake or fake other shading.
- Stop speech, animation mixers, and listeners when no longer active.

Performance degradation should lower quality before selecting classic mode.
Correct input/output presentation takes priority over visual fidelity.

## 11. Integration Boundary

### Existing Files And Endpoints Reused Unchanged

The following are immutable for this branch:

- `scenario.json`
- `docs/json-contracts.md`
- Every file in `prompts/`
- `server/contracts.ts`
- `server/runtime.ts`
- `server/openai.ts`
- `server/prompt-loader.ts`
- `server/scenario-loader.ts`
- Existing request/response types in `src/types.ts`
- Existing progress rules in `src/flow/progress.ts`
- Existing API methods and scored payloads in `src/api/client.ts`
- `GET /health`
- `GET /api/scenario`
- `POST /api/stakeholders/:stakeholderId/messages`
- `POST /api/simulations`
- `POST /api/feedback`
- `POST /api/policy-briefs`

The route paths, request bodies, response bodies, validation, retries, prompt
loading, grounding, and model calls above remain exactly as they are.

The following current components remain intact as the classic and accessible
fallback:

- `src/components/MekongScene.tsx`
- `src/components/WaterTesting.tsx`
- `src/components/Interviews.tsx`
- `src/components/ProposalBuilder.tsx`
- `src/components/Consequences.tsx`
- `src/components/FeedbackPanel.tsx`
- `src/components/PolicyBrief.tsx`

### Existing Files With Presentation-Only Changes

| File | Allowed change |
|---|---|
| `src/App.tsx` | Retain current state, gates, API actions, and clearing semantics; add player/session presentation state and choose immersive or classic render tree |
| `server/index.ts` | Additive mount for the optional presentation speech-token route only; do not edit existing route handlers |
| `package.json` / lockfile | Add only verified presentation dependencies |

Keep legacy styling in `src/styles.css`; immersive styling belongs in a new
stylesheet so classic fallback does not drift.

### New Presentation Files

The implementation should use this boundary:

| Proposed path | Responsibility |
|---|---|
| `src/immersive/ImmersiveExperience.tsx` | Top-level persistent canvas plus DOM station layer |
| `src/immersive/ImmersiveErrorBoundary.tsx` | Canvas/context recovery to classic mode |
| `src/immersive/capabilities.ts` | Tier selection, reduced motion, and session crash flag |
| `src/immersive/JourneyDirector.tsx` | Maps existing state/completion to station, guide, and camera commands |
| `src/immersive/world/ContinuousWorld.tsx` | Terrain, channel, lighting, streamed station roots |
| `src/immersive/world/stations.ts` | Country-agnostic semantic anchors and camera rails |
| `src/immersive/guide/Guide.tsx` | Persistent guide model and animation state |
| `src/immersive/avatars/Character.tsx` | Shared local character loader and animation mixer |
| `src/immersive/audio/dialogue-cue.ts` | Local CC0 dialogue cue playback and generic talking gesture state |
| `src/immersive/ui/IntroFlow.tsx` | Landing, name entry, avatar selection, audio unlock |
| `src/immersive/ui/StationOverlay.tsx` | Selects and positions the current station's DOM activity; never renders flow tabs |
| `src/immersive/ui/StakeholderDialogue.tsx` | Single-location question/response UI without stakeholder tabs |
| `src/immersive/ui/CertificateFinale.tsx` | Local-name and selected-solution certificate |
| `src/immersive/content/journey-copy.ts` | Generic, non-factual guide cues with scenario-label interpolation |
| `src/immersive/immersive.css` | Immersive-only DOM presentation |
| `src/immersive/assets/manifest.ts` | Semantic asset paths, licenses, and fallback assets |
| `public/assets/ATTRIBUTIONS.md` | Asset and any displayed-photo provenance |

New presentation-only types must live under `src/immersive/`; do not extend the
three scored JSON result types.

### Scenario-Engine Compliance

- Station components consume the existing `Scenario`, `TestSite`,
  `Stakeholder`, `Proposal`, and validated result types through props.
- Test markers are generated from `scenario.test_sites`.
- The three interview anchors resolve the first, second, and third stakeholder
  records from scenario data; rendered names and roles are never literals.
- Proposal and certificate labels resolve IDs through scenario data.
- Camera anchors use semantic roles such as `field`, `research`, `office`,
  `planning`, `future`, and `reflection`; they contain no country facts.
- The initial licensed asset manifest is a default delta-agriculture theme, not
  a logic dependency. Another scenario remains functionally usable with the
  same semantic world or the classic fallback even before a new art theme is
  supplied.

There is no presentation speech endpoint or browser-speech feature. Dialogue
enhancement is a local sound cue only and requires no server credentials.

## 12. Privacy And Content Safety

- Player name and avatar choice stay in session/local browser state.
- Neither is added to stakeholder, simulation, grading, or policy-brief
  payloads.
- The certificate escapes the display name as ordinary React text.
- The local UI cue receives no dialogue data.
- Do not record microphone input. Stakeholder questions remain typed for this
  deadline.
- Do not use a student's selfie for avatar creation.
- Do not load third-party trackers or avatar iframes.

## 13. Acceptance Criteria

### Immutable Runtime

- Git diff shows no changes to `prompts/`, `scenario.json`, or
  `docs/json-contracts.md`.
- Existing endpoint contract and runtime tests pass without fixture updates.
- Network inspection shows the same request bodies and validated responses for
  all five existing API calls.
- No presentation module imports prompt files or OpenAI runtime code.

### Journey

- From landing through certificate, the canvas does not reload and the browser
  does not navigate to another page.
- No tab bar is used for activity navigation.
- All six physical stations are visible in one scene and visited in the
  specified order.
- Mr. Ba, Dr. Linh, and Ms. Hoa appear at their required locations.
- Revision returns to the Planning Dock and preserves prior evidence.
- The policy brief remains readable in full before/with the certificate.

### Dialogue

- Endpoint text appears in the dialogue window independently of audio.
- A new message may activate only a generic talking gesture and local CC0 cue.
- Interrupting, revising, or moving stations stops the cue and returns the
  character to idle.
- Audio and avatar failures leave the visible caption and progression intact.

### Fallback

- A no-WebGL test opens the classic UI.
- A forced canvas exception returns to classic UI with all completed work.
- A missing guide, stakeholder, station, and post-processing asset each degrades
  locally.
- Reduced-motion mode contains no camera glide, sway, or focus rack.
- The classic experience remains fully usable by keyboard and screen reader.

### Visual QA

- Desktop and mobile screenshots show no overlap between captions, station
  controls, guide, stakeholder, and progress compass.
- Every fixed-format control has stable dimensions.
- Character and environment materials read as one coherent stylized family.
- Reference-informed rice rows, channel edges, boats, palms, and structures are
  recognizable without using a real photo as an uncredited texture.

## 14. Wow-Per-Hour And Cut Order

Ordered from highest to lowest expected demo impact per implementation hour:

| Rank | Feature | Estimate | Priority | Deadline decision |
|---:|---|---:|---|---|
| 1 | Persistent full-screen canvas with six station anchors and classic fallback | 6-8 h | **Must** | Foundation; implement first |
| 2 | Guided camera rails, station framing, crossfades, and reduced-motion cuts | 4-6 h | **Must** | Creates the continuous-journey effect |
| 3 | Stylized paddy/channel world using licensed local assets | 5-7 h | **Must** | One coherent hero environment is enough |
| 4 | Existing controls/results presented at physical stations | 5-7 h | **Must** | Protects all real gameplay |
| 5 | Name entry, four preset characters, and wake-up focus rack | 4-6 h | **Must** | High opening impact; presets only |
| 6 | Persistent guide with idle, lead, point, and react animations | 5-7 h | **Must** | Use one reliable local character |
| 7 | Three embodied stakeholder avatars with text bubbles | 5-7 h | **Must** | Static face fallback is acceptable |
| 8 | Five-year consequence field/time-rail presentation | 4-6 h | **Must** | Exact DOM values remain authoritative |
| 9 | Reflection pavilion, policy brief reveal, and certificate | 3-5 h | **Must** | Certificate is additive |
| 10 | Chat dialogue with local CC0 UI cue and generic talking gesture | 3-5 h | **Must** | Audio is enhancement; the caption is complete on its own |
| 11 | Ambient water, crop movement, birds/insects audio, and station reactions | 3-6 h | **Nice** | Keep subtle; cut decorative density first |
| 12 | Azure timed-viseme speech with authored mouth targets | Removed | — | Superseded by browser-only optional speech |
| 13 | VRM conversion and `three-vrm` expression pipeline | Removed | — | Superseded by the Quaternius CC0 base-character decision |
| 14 | First-person hands, reflection, or full visible player body | 5-10 h | **Cut if short** | Portrait and certificate already validate selection |
| 15 | Free locomotion, collision, day/night cycle, weather, or cinematic crowd scenes | 12 h+ each | **Cut if short** | Excluded from July 21 build |

### Delivery Cut Line

- By end of July 18: one persistent canvas, Stations 1-6, camera rail, and
  state-preserving classic fallback must work with primitives.
- By end of July 19: final environment pass, guide, stakeholder placement, and
  all existing UI overlays must work.
- July 20: intro, consequence animation, finale, basic audio, failure injection,
  and accessibility.
- July 21: verification and demo rehearsal only. Do not begin a new avatar or
  speech pipeline that day.

### Biggest Technical Risk

The largest remaining presentation risk is preserving the working investigation
while a full-screen WebGL canvas and station overlays enhance it. The local
dialogue cue is optional and must never block visible dialogue or progression.

Mitigation:

1. Render text first and make it sufficient for progression.
2. Keep the local dialogue cue and avatar gestures independently optional.
3. Ship the caption when audio or character presentation is unavailable.

## 15. Verified Library Notes

Verified on 2026-07-16 against current official documentation:

- Existing repo versions are React 19.1.1, R3F 9.3.0, Drei 10.7.4, and Three
  0.180.0.
- [`@react-three/postprocessing`](https://react-postprocessing.docs.pmnd.rs/)
  is the R3F wrapper for the documented effect chain. Its current Depth of Field
  API supports the planned focus rack.
- [R3F Canvas](https://r3f.docs.pmnd.rs/api/canvas) supports a fallback when GL
  is unavailable and documents guarding context failures with an error boundary.
- A local CC0 UI sound is the only optional dialogue-audio path. No cloud or
  browser speech, timed visemes, or facial-expression runtime is used.
- [Ready Player Me](https://readyplayer.me/?welcome=true) states that its
  services were discontinued on 2026-01-31. Its remaining integration docs are
  historical evidence, not a viable production dependency.
