# AgriVerse Unity art provenance

This record covers the external art sources imported for Episode 1. Source FBXs and
their companion texture folders are retained byte-for-byte under each asset's `Source/`
directory. Runtime-ready meshes, materials, texture atlases, LODs, prefabs, and animation
controllers are derivatives and must be stored outside those source directories.

## User-provided Tripo models

Creator and rights confirmation supplied by Tristan on 2026-07-18:

> I confirm that I generated the non-Poly Haven models through my own paid Tripo account using original text prompts and/or references I had permission to use. I authorize the AgriVerse project to modify, optimize, create derivatives from, and redistribute these models and their textures in the public source repository and compiled game builds, subject to Tripo’s applicable terms. Please preserve this confirmation in the project’s asset provenance records.

The creator identified these files and intended uses:

| Asset ID | Intended use | Source FBX SHA-256 |
|---|---|---|
| `CHAR-MAI-01` | Mai field guide | `088cd7a130670468d55d0a95d3826102cefa21fcd6841b03bbf8eb77de2fa242` |
| `ENV-RICE-01-A` | Rice clump source | `01d974a0df304562d44204b92167b20ef9b77a3d2f183641f770e68354f0cb4a` |
| `ENV-GRASS-01-A` | Wet/dry bank grass source | `11fd3a09f7427a114eee459830d43d0db099889c2bdc139055bacad9c8df6d4e` |
| `ENV-REED-01-A` | Wetland reed cluster | `787d4df87cedd3c12f6b532047801d5ce54c5a37df97356f9abf0bb13e601eaf` |
| `ENV-BANANA-01-A` | Banana plant | `4630f62849864e955b58fc756e0eb785f95fe38b799274f07a67f01e57385895` |
| `ENV-COCONUT-01-A` | Coconut palm | `4f4d443ed9d340475c256f7bc1fe924ecab29faad568456d6a8d93f2a0d75b04` |
| `ENV-PALMYRA-01-A` | Fan-shaped palm | `3c35fda328f4b009992e69a14357fbc5c0accf392f1d1807280a1902e8a13981` |
| `ENV-BROADLEAF-01-A` | Distant broadleaf tree | `8658ecdb0f7612c6d98a7407371cd1989fb24c5dca8520cdf1c125e9356c72bf` |
| `ENV-SHELTER-01-A` | Rural field shelter | `d42c22549e50824aa962174c979edd3c04923acae19ab06e86ff11ff3233bf3b` |
| `ENV-DOCK-01-A` | Wooden sampling dock | `5a3a0f1fabf4890cbade232f9fb745df7ead2cb72ed8b4298d10325db64ab1b1` |
| `ENV-BOAT-01-A` | Narrow wooden field boat | `24ae93c62f36eb24a1e480b2c3f955500256ef19af0c5058683b30f8d5414734` |

All source FBXs are Kaydara FBX 7.4 files exported by Tripo. Each contains an
unwanted default cube; runtime derivatives must omit that cube and must not overwrite
the source.

### Premium character and field-station intake

Tristan supplied the following additional authorization on 2026-07-19:

> I generated the listed Tripo assets through my account and authorize their modification and redistribution in the AgriVerse source repository, standalone builds, demo video and hackathon submission.

Each package was copied from the stated local delivery path into the listed `Source/`
directory without changing a byte. `PREMIUM-ASSET-SHA256SUMS.txt` records every supplied
FBX and texture file, not only the headline FBX hashes below.

| Asset ID | Use | Original delivery path | Source FBX SHA-256 |
|---|---|---|---|
| `CHAR-MRBA-01` | Mr. Ba stakeholder | `villager+farmer+3d+model` | `c582e01411301fdcbb097930160bb337f1b62f44c1a54eafc5ef8c3ae40cb4db` |
| `CHAR-DRLINH-01` | Dr. Linh stakeholder | `woman+explorer+3d+model` | `ad29e34b2389684593a5b34009d33759729acef05baed80be463b437d0f00930` |
| `CHAR-MSHOA-01` | Ms. Hoa stakeholder | `human+figure+3d+model` | `756dca2abe7cc39a3b6fb7f91309a74a2b4175ce0eb5fc6a4c852e9a6a7ff339` |
| `ENV-RESEARCH-POST-01` | Rural research post | `rural+hut+3d+model` | `c60301fd79f53f7f7587f6d735125b0d55d78d06d8dff6e0a6f887ac982e81c9` |
| `ENV-DISTRICT-OFFICE-01` | District agricultural office | `rural+service+office+3d+model` | `8c53c12d44bc44c49a27ea2ebb2629abd784e809b479e0f0ad1865d11d9a92a2` |
| `ENV-RESEARCH-WORKSTATION-01` | Research workstation | `laboratory+bench+3d+model` | `084074afd6e49a839e71673cdb23a56406a133088a18824b2e8d770ba1e745bc` |
| `ENV-REFLECTION-PAVILION-01` | Ending pavilion | `waterfront+pavilion+3d+model` | `62c5b65f88789655fb1777c5332415a3bdbd900f88cb5f11e41afcb39158ddee` |
| `ENV-SAMPLING-KIT-01` | Physical water station prop | `water+sampling+kit+3d+model` | `a093d25528c03d4b72d5e2177cd93abaf4e3d0cdb40c3fc84775f04b91f15812` |
| `ENV-PLANNING-TABLE-01` | Proposal planning station | `rural+planning+table+3d+model` | `126332c68e49c4e4df62a9897807245f1f8bf3c2d8fceac9df0e119ced9b1a37` |
| `ENV-WOVEN-BASKET-01` | Field dressing | `giỏ+đan+tre+3d+model` | `010fc900240a2918f4bf7cb2319c3867fe0885d51d162aea4071e137e39a0f0a` |
| `ENV-HOE-01` | Field dressing | `agricultural+hoe+3d+model` | `90edd6b179f0c6dd3fc1ac521041c2559bd819a2a93d756b5882582230724ce9` |
| `ENV-SHOVEL-01` | Field dressing | `shovel+3d+model` | `45d834f84e1b2aef9b6c3c50c184ce990b466fb9c213e42fe90f4b39b62202a8` |

All three stakeholder FBXs import as valid Unity Humanoid Avatars. Runtime prefabs use
the previously approved Mai Humanoid clips for retargeted idle, walk, talk, and gesture
motion. Unity strips source cameras, lights, visibility tracks, and unwanted default cubes
only from imported or derived representations.

## NASA globe package

The globe bundle was delivered at
`AgriVerse_Globe_Assets` and is preserved byte-for-byte at
`Globe/Source/`. It is not a Tripo or CC0 bundle. Its original `SOURCE_AND_LICENSE.md`,
`README.md`, and `SHA256SUMS.txt` remain intact.

- Earth color and clouds: NASA Earth Observatory / Blue Marble.
- Elevation and derived normal: NASA Earth Observatory using GEBCO 08 data.
- Star map: NASA/Goddard Space Flight Center Scientific Visualization Studio, with
  ESA/Gaia/DPAC and source-page catalogue acknowledgements retained.
- No NASA logos or insignia are included, and project credits must not imply NASA
  endorsement.
- Repository and release redistribution must retain the package acknowledgements and
  follow NASA's current media guidelines:
  `https://www.nasa.gov/nasa-brand-center/images-and-media/`.

## Poly Haven PBR materials

The `Environment/Materials/Source/` directory contains the 2K Clay, Muddy Tracks,
Grass Path 2, and Weathered Planks source maps supplied under CC0 1.0. Each material
directory contains its own `SOURCE.md` with creator, direct source URL, license URL,
download date, map inventory, and modification record. The imported files matched all
20 supplied SHA-256 checksums before Unity ingestion.

## CC0 audio

The processed Episode 1 audio is derived from seven Joseph Sardin / BigSoundBank
recordings whose saved source pages identify them as CC0/public domain. Full source
URLs, processing details, limitations, and the representative-not-Vietnam caveat are
retained in `Audio/SOURCE.md`. Only the seven final files are imported into Unity:

| Final file | SHA-256 |
|---|---|
| `Birds_Loop.ogg` | `432c3643a2c0e7e58ec959ae24f8cd8fac57ff7e33763fc4c1d63810aaf1d1a1` |
| `Canal_Loop.ogg` | `15940205ee6b5d59ba2dc10b254731e902035a328ba6b6367f4c447218b0388c` |
| `Insects_Loop.ogg` | `a0c8fbd9ff064358d2e4aeb583251d25c0dfe2d3dfd4ea676d449bfcc7095ecc` |
| `Wind_Loop.ogg` | `bef49581d2b21c87ce5653874f1d0cbe3e470c0e84007c766b3e9eedbfb2a978` |
| `VialCap_01.wav` | `329d675f0e424f597ad2e304186e8ebb7b419708800f6d883c2a68078ea85b77` |
| `VialHandle_01.wav` | `1f486f3281f7ac318ac4530152a1e32dd2ea7efeeb7f99b8366abedfc766f5bc` |
| `WaterScoop_01.wav` | `ede338bb2c96821bc5f046a0a34ff0616b2ad595fe26a4b7f94937bf685377ce` |

All final clips decode at 48 kHz. Ambience must be auditioned in the built scene because
it is representative sound design rather than a field recording made in Vietnam.
