# AgriVerse Episode 1 - Dream Game Execution

This file tracks the autonomous Unity desktop build from the verified Reality Spike 1
baseline through the release candidate. Update it at every checkpoint.

## Current phase

Phase 2 - Art-independent Episode 1 features.

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

## Current blockers

- The true-3D world and animated Mai require the consolidated external assets below.
- Art-independent implementation continues before Human Gate A.

## External assets required

All requested files must be CC0 or original work the user can redistribute in the public
repository. Unity scale is 1 unit = 1 meter, Y-up, forward +Z. Do not supply raw commercial
Asset Store content that cannot be redistributed.

### Characters

| Asset ID | Purpose and status | Delivery requirements | Suggested source | Destination |
|---|---|---|---|---|
| `CHAR-MAI-01` | Field guide; **required for Gate B** | `Mai.fbx`; humanoid rig, skinned mesh, 20k-60k triangles plus one lower LOD; neutral pose; PBR base color, normal, metallic/roughness, and AO maps at 2K; no facial rig required | Preferred: original/Meshy-to-Blender model with user-confirmed redistribution rights. Free fallback: Quaternius *Universal Base Characters* (CC0) | `unity/Assets/AgriVerse/Art/Characters/Mai/` |
| `CHAR-MRBA-01` | Farmer NPC; optional because portrait fallback ships | `MrBa.fbx`; same rig, maps, scale, orientation, and performance limits as Mai; culturally respectful practical field clothing | Same original workflow or Quaternius CC0 base | `unity/Assets/AgriVerse/Art/Characters/MrBa/` |
| `CHAR-DRLINH-01` | Researcher NPC; optional because portrait fallback ships | `DrLinh.fbx`; same rig/maps/limits; restrained field-research clothing | Same original workflow or Quaternius CC0 base | `unity/Assets/AgriVerse/Art/Characters/DrLinh/` |
| `CHAR-MSHOA-01` | District official NPC; optional because portrait fallback ships | `MsHoa.fbx`; same rig/maps/limits; practical district-field clothing | Same original workflow or Quaternius CC0 base | `unity/Assets/AgriVerse/Art/Characters/MsHoa/` |
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

## Checkpoint commits

- `87ae13b11266325ad9a608c9efdbec21ded65c9` - Reality Spike 1 technical baseline.
- `9eead89` - Cinematic stakeholder interview presentation.
- `1ea9ad5` - Dream Game execution tracker initialized.
