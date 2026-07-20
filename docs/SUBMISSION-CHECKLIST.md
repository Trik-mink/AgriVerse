# OpenAI Build Week submission checklist

Submission deadline: **July 21, 2026 at 5:00 pm Pacific Time**.

Official references:

- https://openai.devpost.com/rules
- https://openai.devpost.com/details/faqs

## Devpost fields

- [ ] Project name: **AgriVerse**
- [ ] Category: **Education**
- [x] Repository URL: https://github.com/Trik-mink/AgriVerse
- [x] macOS judge build URL:
  https://github.com/Trik-mink/AgriVerse/releases/download/v1.0.0-build-week/AgriVerse-macOS-Universal.zip
- [ ] Public YouTube demo URL: `<PUBLIC_YOUTUBE_URL>`
- [ ] Video is no more than three minutes.
- [ ] Video audio explains both Codex's build role and GPT-5.6's runtime role.
- [ ] Primary Builder `/feedback` Session ID:
  `019f71fd-f4b9-7991-bdda-1c1f42203916`
- [ ] README setup, testing, collaboration, limitations, and attribution links resolve.
- [x] The selected Apache-2.0 code license is present.
- [x] All third-party licensing and provenance rows are resolved.
- [ ] Submit before the deadline; no submission edits are possible afterward.

## Suggested Devpost description

AgriVerse is a first-person environmental decision game for high-school science students.
In the complete Vietnam episode, the player enters a 3D Mekong Delta field location, tests
water at three evidence sites, and interviews a farmer, researcher, and district official
whose GPT-5.6 agents have distinct private goals. The student then builds a proposal, watches
validated five-year consequences, receives retrieval-grounded rubric feedback, revises, and
generates a cited policy brief.

The AI is the mechanic rather than a chat add-on: three role-separated stakeholder systems,
a strict structured-output simulator, a grounded grader, and a structured policy-brief
generator all run through a server-side Express boundary. Unity never receives the API key,
hidden goals, or prompts. The scenario engine keeps Vietnam-specific facts in validated data,
so the same product architecture can support future cited episodes. Incoming India, Kenya,
Brazil, and Netherlands pins communicate that network without pretending those episodes are
already playable.

Codex accelerated the architecture, backend, Unity implementation, tests, licensed-asset
integration, and release recovery. The human set the learning design and visual direction,
required evidence before answers and revision as the core mechanic, rejected unsuccessful art
directions, and discovered key offline and build-contamination failures that became regression
tests.

## Video coverage

- [ ] Orbital Field Network and Vietnam mission selection.
- [ ] First-person canal arrival and one water sample.
- [ ] A live stakeholder response showing disagreement and grounding.
- [ ] Proposal submission and five-year Future Walk.
- [ ] Grounded feedback, revision, and original-versus-revised comparison.
- [ ] Policy brief or named certificate.
- [ ] Brief voiceover: scenario engine, four GPT-5.6 systems, grounding/citations, and Codex
  collaboration.
- [ ] No API key, `.env`, terminal secret, private note, or private session content is visible.

## Judge testing

- [ ] Download `AgriVerse-macOS-Universal.zip` and its `.sha256` file.
- [ ] Verify the checksum before extraction.
- [ ] Extract with Finder or `ditto`.
- [ ] Follow the README's Gatekeeper instructions if the ad-hoc signed app is quarantined.
- [ ] Use the approved judge-access endpoint/instructions; never send an API key to a judge.
- [ ] Launch the app, select Vietnam, enter a name, and begin the mission.
- [ ] Complete investigation → interviews → plan → simulation → feedback → revision → brief.
- [ ] Open Field Journal sources and confirm source IDs remain readable.
- [ ] Verify the certificate uses the entered name.

## Release evidence

- [x] `npm ci` on supported Node 24
- [x] Type checking and production web/server build
- [x] Backend and release tests, including hosted-service security boundaries: 45/45 passed
- [x] Dependency vulnerability audit: 0 findings
- [x] Unity EditMode: 134 passed, 0 failed, 1 intentionally skipped live-connectivity test
- [x] Focused non-GPT Unity PlayMode: field-network reliability, world-lab, and revised Future
  Walk suites passed
- [x] Focused offline/retry suite
- [x] Pre-hosting live GPT-backed full-loop regression: passed in 156.1 seconds
- [x] One final live GPT-backed journey against the hosted judge service: passed with exactly
  eight successful model routes and no paid retry
- [x] Fresh universal macOS build at
  `unity/Builds/Release/AgriVerse.app`
- [x] Correct executable, product name, and `org.agriverse.episode1` identifier
- [x] arm64 and x86_64 slices
- [x] Code signature validation
- [x] No SampleScene in release scene list
- [x] No development/profiler flags
- [x] No magenta, missing shader, runtime exception, or credential in the app
- [x] Release binaries were rebuilt from a neutral local path and contain no personal email
  or private home/Downloads path.
- [x] Metal player: 279.4 average FPS over the bounded five-second landing sample on Apple M4
  at 1280×720; the full 1920×1080 journey remained approximately 145–186 FPS
- [x] `git diff --check`
- [x] `git lfs fsck` and source-asset checksum integrity
- [x] Archive extraction and identity re-verification
- [x] Archive:
  `unity/Builds/Release/AgriVerse-macOS-Universal.zip`
- [x] SHA-256:
  `cb806af881c95e8a7facc082e78e275ed502bfdfbe7eaf0c48263cbe47a4d27d`
- [x] Uncompressed app size: 290,103,296 bytes
- [x] Archive size: 165,388,870 bytes

## Current publication state

- [x] Public repository created with exactly one branch named `main`.
- [x] Clean milestone tags and reachable Git LFS objects published.
- [x] Apache-2.0 source-code license and third-party notices published.
- [x] Publication secret, privacy, history, and LFS scan passed before first push.
- [x] Render billing added for the single authorized service within the US$8 cap.
- [x] Secured temporary judge backend deployed and healthy over HTTPS.
- [x] Unity rebuilt with the hosted HTTPS endpoint and no embedded API secret.
- [x] One final live hosted journey completed with source and citation IDs intact.
- [x] Create and verify `v1.0.0-build-week` and the GitHub Release.

## Remaining human submission work

- [ ] Record, edit, and publicly upload the under-three-minute YouTube demo.
- [ ] Obtain or confirm the Builder Session ID using `/feedback`.
- [ ] Replace the YouTube and Devpost Session-ID placeholders.
- [ ] Complete and submit Devpost.
- [ ] Perform a final incognito check of repository, release, video, and judge instructions.

The GitHub repository being public is not the Devpost submission. Do not mark the hackathon
submission complete until the remaining human tasks are done.
