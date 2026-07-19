# AgriVerse Episode 1 - Dream Game Execution

This file tracks the autonomous Unity desktop build from the verified Reality Spike 1
baseline through the release candidate. Update it at every checkpoint.

## Current phase

Human Gate C - the complete premium standalone journey is built and verified from globe and
name entry through the revised future, policy brief, named certificate, and ending choice.
The universal fullscreen macOS release candidate, screenshots, performance evidence,
attribution audit, and final human-verification script are ready for approval.

## Completed tasks

- Reality Spike 1 photo presentation and procedural fallback are committed and retained.
- The complete investigation-to-policy-brief Unity learning loop is committed and verified.
- The approved cinematic stakeholder selection and interview presentation is committed.
- Phase 1 asset audit completed: Unity contains no production 3D character, animation,
  environment-mesh, vegetation, or ambient-audio assets. Existing usable fallbacks are the
  licensed An Giang photograph, four portraits, explicit URP materials, and a CC0 UI cue.
- Phase 2 slice 1 implemented: local name and portrait choice, authored Mai guidance,
  predict-before-reveal water testing, inline glossary, scenario presentation DTOs, and
  authoritative Future Walk data mappings.
- Phase 2 completed: original and revised simulation histories remain separately available;
  Judge View audits raw validated outputs and source IDs; the named certificate and both
  respectful ending choices are wired without entering scored request data.
- Human Gate A resolved: the user supplied Mai, rice, grass, reeds, tropical trees, shelter,
  dock, boat, four Poly Haven CC0 material sets, and seven CC0 audio deliverables, together
  with explicit rights confirmation for every Tripo-generated model and texture.
- Asset intake completed with byte-identical source preservation, source hashes, provenance,
  deterministic import settings, and Git LFS coverage.
- Mai CharacterLab completed with an explicit URP material, valid Humanoid avatar, restrained
  look-at behavior, and visually verified idle, walk, talk, wave, and hat-adjust motions.
- AnGiangWorldLab completed as an isolated 120-by-120-meter true-3D field location with a
  walkable canal/dike layout, dense instanced rice and bank vegetation, authored rural
  structures, coherent wind, layered CC0 ambience, matte PBR ground, and Metal-safe URP art.
- The Gate-B vertical slice is implemented in a separate Episode3DAlpha scene: Mai wakes the
  player beside the canal, first-person travel leads to a configuration-mapped upstream
  sample, prediction gates a physical vial interaction, and the authoritative reading enters
  a focused notebook without enabling legacy cubes or panels.
- Human Gate B approved: the user personally verified the standalone movement, world
  exploration, interaction, sampling, and notebook experience.
- Phase 5 integrated all three physical water stations, scenario-configured stakeholder
  meeting points, portrait-backed live interviews, proposal planning, simulation,
  grounded feedback, revision, policy brief, Judge View, certificate, and both endings.
- Phase 6 added an exact-token Future Walk with five-year field presentation and an
  original-versus-revised comparison driven only by the stored validated simulator JSON.
- Phase 7 added a scenario-driven globe and identity landing, a fade-to-canal arrival, and
  non-blocking transition into the first-person episode.
- Phase 8 completed a concentrated teal/amber presentation pass, bounded scrolling,
  first-person return between interviews, responsive 16:9 validation, and the macOS
  release-candidate build.
- Premium asset expansion intake completed: three user-authorized Tripo stakeholders, nine
  field-station structures/props, and the NASA globe package are preserved with complete
  checksums, LFS coverage, source/license records, explicit URP derivatives, and fallback-safe
  runtime assets.
- Player avatar selection was removed by human direction. Name entry remains authoritative
  for Mai's greeting and the certificate; the player is an unseen first-person observer.
- All three licensed stakeholder prefabs are integrated at scenario-configured meeting
  points with shared Humanoid idle, listening, talking, gesture, and look-at states.
- The exploration-to-interview-to-planning handoff is physical: interviews return to the
  walkable world and the licensed planning table opens the existing proposal workflow.
- The persistent notebook is now a keyboard-accessible Field Journal with Sites, People,
  Plan, and Sources sections, plus text scale, high-contrast, and reduced-motion controls.
- Premium structures and props establish research, district-office, planning, sampling, and
  reflection stations without intercepting learning raycasts.
- Consequences and original-versus-revised Future Walk presentation now preserve most of the
  3D landscape while exact canonical values remain scrollable and unchanged.
- The macOS bundle now identifies itself as `AgriVerse` with application identifier
  `org.agriverse.episode1`; the release helper restores developer project settings after
  applying fullscreen/native-resolution release metadata.

## Current blockers

- None. The existing portrait, procedural-world, and globe fallbacks remain available if an
  optional premium presentation asset fails at runtime.

## External asset manifest

The required bundle was supplied as CC0 material/audio sources and user-authorized original
Tripo work. Unity scale is 1 unit = 1 meter, Y-up, forward +Z. Optional stakeholder and
footstep rows remain documented below without blocking the current build.

### Characters

| Asset ID | Purpose and status | Delivery requirements | Suggested source | Destination |
|---|---|---|---|---|
| `CHAR-MAI-01` | Field guide; **required for Gate B** | `Mai.fbx`; humanoid rig, skinned mesh, 20k-60k triangles plus one lower LOD; neutral pose; PBR base color, normal, metallic/roughness, and AO maps at 2K; no facial rig required | Preferred: original/Meshy-to-Blender model with user-confirmed redistribution rights. Free fallback: Quaternius *Universal Base Characters* (CC0) | `unity/Assets/AgriVerse/Art/Characters/Mai/` |
| `CHAR-MRBA-01` | Farmer NPC; **supplied and Humanoid-validated** | Retargets approved idle/walk/talk/gesture clips; explicit URP material and portrait fallback retained | User-authorized original Tripo package | `unity/Assets/AgriVerse/Art/Characters/MrBa/` |
| `CHAR-DRLINH-01` | Researcher NPC; **supplied and Humanoid-validated** | Retargets approved idle/walk/talk/gesture clips; explicit URP material and portrait fallback retained | User-authorized original Tripo package | `unity/Assets/AgriVerse/Art/Characters/DrLinh/` |
| `CHAR-MSHOA-01` | District official NPC; **supplied and Humanoid-validated** | Retargets approved idle/walk/talk/gesture clips; explicit URP material and portrait fallback retained | User-authorized original Tripo package | `unity/Assets/AgriVerse/Art/Characters/MsHoa/` |
| `ANIM-HUMANOID-01` | Shared motion set; **required for Gate B** | Humanoid FBX clips: idle, walk, talk, point, wave, open-hand explain, and one calm listening gesture; in-place except walk; no root scale animation | Quaternius *Universal Animation Library* or *Library 2*, CC0 | `unity/Assets/AgriVerse/Art/Animations/Humanoid/` |

### Environment

| Asset ID | Purpose and status | Delivery requirements | Suggested source | Destination |
|---|---|---|---|---|
| `ENV-RICE-01` | Dense paddy rows; **required** | Three rice-clump variants, each under 1k triangles at LOD0 with LOD1/billboard; alpha-cutout 1K-2K base color and normal; GPU-instancing friendly | Original or CC0 vegetation asset | `unity/Assets/AgriVerse/Art/Environment/Vegetation/Rice/` |
| `ENV-BANK-VEG-01` | Grass, reeds, wet/dry variation; **required** | At least two grass and two reed variants, each under 2k triangles with LODs; shared atlas; wind-ready vertex colors optional | Original/CC0; Poly Haven grass may be used only after aggressive retopology | `unity/Assets/AgriVerse/Art/Environment/Vegetation/Banks/` |
| `ENV-TROPICAL-01` | Banana, palmyra, coconut, and local tree silhouettes; **required** | One banana, two fan-shaped palmyra variants, one restrained coconut, one broadleaf tree; each under 12k triangles with two LODs; 2K PBR maps | Original/CC0 vegetation | `unity/Assets/AgriVerse/Art/Environment/Vegetation/Trees/` |
| `ENV-SHELTER-01` | Modest field shelter; **required** | `FieldShelter.fbx`, under 30k triangles with simple collision; weathered timber/roof PBR maps; no tourist-resort decoration | Original/CC0 rural structure | `unity/Assets/AgriVerse/Art/Environment/Structures/Shelter/` |
| `ENV-DOCK-01` | Sampling and planning dock; **required** | `SamplingDock.fbx`, under 15k triangles with simple collision; 2K weathered-wood PBR maps | Original/CC0; Poly Haven *Modular Wooden Pier* is acceptable only if reduced to the small used modules | `unity/Assets/AgriVerse/Art/Environment/Structures/Dock/` |
| `ENV-BOAT-01` | Narrow local field boat; **required** | `LocalBoat.fbx`, 5k-20k triangles, no sails/engine/tourist styling, simple collision, 2K wood PBR maps | Original/CC0 model based on local reference | `unity/Assets/AgriVerse/Art/Environment/Props/Boat/` |
| `ENV-FARM-PROPS-01` | Sampling kit, baskets, tools, jars; optional | Combined FBX or separate prefabs, under 20k triangles total, 1K-2K PBR atlas | Original/CC0 | `unity/Assets/AgriVerse/Art/Environment/Props/Farm/` |
| `MAT-DELTA-PBR-01` | Clay banks, wet mud, sparse grass, timber; **required** | 2K PNG/TGA base color, normal OpenGL, roughness, and AO maps. Required named sources: Poly Haven *Muddy Tracks*, *Grass Path 2*, and *Weathered Planks*, all CC0 | Poly Haven CC0 | `unity/Assets/AgriVerse/Art/Environment/Materials/` |

### Audio

| Asset ID | Purpose and status | Delivery requirements | Suggested source | Destination |
|---|---|---|---|---|
| `AUD-DELTA-AMBIENCE-01` | Canal, insects, birds, and wind bed; **required for Gate B** | Separate seamless 45-120 second WAV or OGG loops, 48 kHz, no voices, music, engines, or identifiable copyrighted recordings | Original recording/generation with redistribution rights, or individually verified CC0 files | `unity/Assets/AgriVerse/Art/Audio/Ambience/` |
| `AUD-WATER-SAMPLE-01` | Physical sample interaction; **required for Gate B** | 2-4 short mono WAV/OGG water scoop, glass/plastic vial, and cap sounds, 48 kHz | Original or CC0 | `unity/Assets/AgriVerse/Art/Audio/SFX/Water/` |
| `AUD-FOOTSTEPS-01` | Dirt/wood walking feedback; optional for alpha | 6-10 short mono clips for dirt/mud and 4-6 for wood, 48 kHz, peak-normalized consistently | Original or CC0 | `unity/Assets/AgriVerse/Art/Audio/SFX/Footsteps/` |

## Verification results

### Licensed asset intake and Mai CharacterLab checkpoint

- Provenance: exact user authorization, source URLs/licenses, and SHA-256 hashes recorded;
  every copied source model remained byte-identical to the supplied file.
- Import tests: source cameras/lights/default cubes stripped only from Unity's imported
  representation; source FBX files unchanged; scenery animation disabled; PBR maps and
  streaming/decompressed audio import policies verified.
- EditMode: 41 passed, 0 failed, 1 existing connectivity test ignored.
- Mai Metal-player capture: all five semantic motions rendered at 1280x720 with a valid
  Humanoid avatar, stable deformation, explicit URP material, and no magenta or shader errors.
- CharacterLab macOS build: succeeded at `unity/Builds/macOS/CharacterLab.app`.
- SampleScene macOS checkpoint build: succeeded at
  `unity/Builds/macOS/AgriVerseCheckpoint.app`; startup log was clean on Apple M4 Metal.

### AnGiangWorldLab checkpoint

- Derived art: source FBXs stayed unchanged; rice and grass gained explicit LOD0/card/
  billboard derivatives, while the fan palm and boat gained bounded optimized meshes.
- EditMode: 51 passed, 0 failed, 1 existing connectivity test ignored; WorldLab-specific
  coverage includes triangle budgets, physical scale, 7,000-plus instances, matte terrain,
  authored-model orientation, three LODs, and scene isolation.
- PlayMode: first-person eye height, forward movement, CharacterController collision, and
  cursor release passed.
- Metal player: four 1280x720 views rendered without magenta/shader/runtime errors at roughly
  137-200 FPS after warm-up on Apple M4.
- macOS build: succeeded at `unity/Builds/macOS/AnGiangWorldLab.app`; SampleScene remained
  unchanged.

### Human Gate B approved alpha

- EditMode: 53 passed, 0 failed, 1 existing connectivity test ignored.
- PlayMode: isolated movement/CharacterController collision passed; the live upstream-site
  flow passed against `/api/scenario` from prediction through notebook recording.
- macOS Metal build: succeeded at `unity/Builds/macOS/AgriVerse3DAlpha.app`.
- Metal player: five 1280x720 evidence views rendered without runtime, shader, or magenta
  errors; reported roughly 190-258 FPS after warm-up on Apple M4.
- Geographic check: the An Giang field-base interaction maps through serialized world
  configuration to the upstream scenario site, never the coastal 12 g/L site.
- Human verification: approved after a standalone playthrough of movement, exploration,
  interaction, sampling, and the notebook.

### Complete 3D journey release candidate

- EditMode: 55 passed, 0 failed, 1 existing connectivity test ignored.
- Focused PlayMode: 3 passed, covering all three samples, world-return interviews, and the
  authoritative original-versus-revised Future Walk.
- Live full loop: passed in 137.9 seconds against the real backend, including all three
  stakeholder GPT replies, two simulator runs, two grounded feedback runs, revision
  preservation, and the policy brief.
- macOS release build: succeeded at `unity/Builds/macOS/AgriVerse.app`
  (208,740,705 bytes, universal Apple Silicon/Intel executable).
- Metal player: clean 1280x720 journey captures and a clean 1920x1080 identity capture;
  Player.log contained no runtime, shader, rendering, magenta-material, or missing-asset
  errors.
- Isolation: `Assets/Scenes/SampleScene.unity`, backend behavior, prompts, scenario data,
  API contracts, ProjectSettings, and tags remained unchanged.

### Premium 3D stakeholder journey checkpoint

- EditMode: 73 passed, 0 failed, 1 existing connectivity test ignored.
- Focused PlayMode: 3 passed, covering all configured water sites, return-to-field
  stakeholder flow, Field Journal sections, physical planning handoff, and exact Future Walk
  state preservation.
- Humanoid presentation: Mr. Ba, Dr. Linh, and Ms. Hoa use explicit URP materials, valid
  Humanoid avatars, retargeted approved body animation, eye-level focus, and portrait
  fallbacks.
- Metal player: the 1280x720 stakeholder capture rendered the licensed 3D character behind
  the cinematic interview interface with no magenta, shader, rendering, or runtime errors.
- Performance: the Apple M4 Metal player reported 185.8 average FPS over the instrumented
  five-second window.
- macOS build: non-development universal release build succeeded at
  `unity/Builds/macOS/AgriVerse.app` (approximately 277 MiB on disk).
- Isolation: `Assets/Scenes/SampleScene.unity`, backend behavior, prompts, scenario data,
  API contracts, ProjectSettings, and tags remained unchanged.

### Final premium macOS release candidate

- Unity EditMode: 73 passed, 0 failed, 1 existing connectivity test ignored.
- Focused 3D PlayMode: 3 passed, covering all three investigations, Field Journal/world
  return, stakeholder handoff, original/revised Future Walk, and exact result preservation.
- Live complete loop: passed in 160.4 seconds against the real Express/OpenAI service,
  including three role-separated stakeholder replies, initial simulation, grounded
  feedback, editable revision, replacement simulation, second feedback, and policy brief.
- Backend deterministic suite: 22 passed across sanitized scenario/CORS, validation,
  journey state, prompt loading, structured contracts, and invalid-input boundaries.
- Release build: succeeded at `unity/Builds/macOS/AgriVerse.app` (289,679,450 bytes,
  universal arm64/x86_64, non-development, fullscreen-native configuration).
- Bundle audit: product/executable name `AgriVerse`, identifier
  `org.agriverse.episode1`, version `0.1.0`, macOS minimum `12.0`, ad-hoc local signature.
- Metal visual checks: clean 1280x720 field/interview/Field Journal captures and clean
  1920x1080 globe/name-entry capture; no runtime, shader, rendering, missing-reference, or
  magenta-material errors.
- Performance: Apple M4 Metal reported 198.3 average FPS over the instrumented five-second
  1280x720 sample.
- Asset audit: premium source SHA-256 manifest passed, Git LFS fsck passed, source FBXs
  remained byte-identical, and Tripo, Poly Haven, CC0 audio, NASA, GEBCO, ESA/Gaia/DPAC,
  and photographic acknowledgements remain preserved.
- Fallback boundary: missing photo retains the procedural view; stakeholder presentation
  retains portraits/interaction rings; optional field dressing is non-blocking; missing
  globe assets retain the procedural globe. GPT-backed mechanics expose readable retry
  states but require the existing backend and are not replaced by fabricated offline scores.
- Isolation: `Assets/Scenes/SampleScene.unity`, backend behavior, prompts, scenario data,
  API contracts, ProjectSettings, and tags remained unchanged.

### Cinematic stakeholder interview checkpoint

- EditMode: 20 passed, 0 failed, 1 existing connectivity test ignored.
- Live PlayMode stakeholder regression: 1 passed; all three stakeholder replies recorded and
  the planning gate unlocked.
- macOS Metal player: non-development build succeeded at 1280x720.
- Player log: no runtime, shader, rendering, magenta-material, or crash errors during startup.
- Visual correction: human-approved before checkpoint finalization.

### Episode identity, narrative, and prediction checkpoint

- EditMode: 28 passed, 0 failed, 1 existing connectivity test ignored.
- Live PlayMode investigation regression: passed; every reading remained hidden until a
  prediction and all three predicted samples unlocked interviews.
- macOS Metal player: non-development build succeeded at 1280x720.
- Player log: Apple M4 Metal initialized with no runtime, shader, rendering, or crash errors.

### Episode revision, Judge View, and completion checkpoint

- EditMode: 34 passed, 0 failed, 1 existing connectivity test ignored.
- Live PlayMode full-loop regression: passed in 186.7 seconds against the real backend,
  including interviews, simulation, feedback, revision, resimulation, and policy brief.
- Revision assertion: the first simulator JSON remained byte-for-byte available after the
  second validated result replaced the active future.
- macOS Metal player: non-development 1280x720 checkpoint build succeeded at
  `unity/Builds/macOS/AgriVerseCheckpoint.app`.
- Player log: Apple M4 Metal initialized with no runtime, shader, rendering, or crash errors.

## Checkpoint commits

- `87ae13b11266325ad9a608c9efdbec21ded65c9` - Reality Spike 1 technical baseline.
- `9eead89` - Cinematic stakeholder interview presentation.
- `1ea9ad5` - Dream Game execution tracker initialized.
- `27b900b` - Repeatable macOS checkpoint build helper.
- `4894384` - Salt Line identity, authored guidance, glossary, and prediction flow.
- `adc011b` - Original/revised futures, Judge View, certificate, and ending choices.
- `c731041` - Licensed source-art intake and verified Mai CharacterLab.
- `ccf7daa` - Isolated true-3D AnGiangWorldLab, optimized vegetation, movement, and ambience.
- `6e90ca1` - Human-approved playable 3D investigation vertical slice.
- `814868d` - Complete 3D journey and macOS release candidate.
- `9d98317` - Licensed stakeholder, field-station, and NASA globe asset intake with
  name-only first-person identity.
- `87880e6` - Premium 3D stakeholder journey, physical planning handoff, Field Journal,
  accessibility controls, and compact Future Walk presentation.
- `998cb03` - Verified universal macOS release candidate with native AgriVerse bundle
  identity and complete release audit.
