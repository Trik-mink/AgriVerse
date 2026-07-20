# Devpost draft

## Project

**AgriVerse**

**Category:** Education

**Tagline:** A first-person environmental decision game where students investigate,
negotiate, model consequences, and revise.

## Description

AgriVerse is a 3D learning simulation for high-school environmental science students. Its
complete first episode places the learner in Vietnam's Mekong Delta. The student tests water
at three scenario-defined sites, interviews a farmer, researcher, and district official,
builds an evidence-based proposal, watches five years of consequences, receives grounded
feedback, revises, compares the original and revised futures, and leaves with a cited policy
brief and named certificate.

The India, Kenya, Brazil, and Netherlands locations on the interactive globe are explicit
incoming previews. They demonstrate the global scenario-engine direction without pretending
that additional episodes are already playable.

## Problem

Environmental decisions are systems problems. Students must connect measurements, uncertain
future outcomes, livelihoods, infrastructure, and disagreement among people who value
different things. A quiz or unconstrained chatbot can flatten those tensions into a single
answer.

## Solution

AgriVerse makes evidence gathering and revision the mechanics. Students must investigate
before interviewing, listen to all three perspectives before planning, inspect the
consequences of their proposal, and revise after feedback. The game frames revision as
responsible professional practice, not failure.

## Technical implementation

- Unity 6.5 URP macOS client with a first-person 3D Mekong Delta field world
- Express/TypeScript server using the OpenAI Responses API
- Country-agnostic `scenario.json` contract
- Versioned server-side prompts and hidden stakeholder goals
- Strict Zod input and output validation
- Research corpus and source-ID verification
- Durable cost ledger, request/session/IP rate limits, bounded concurrency, judge expiration,
  exact CORS, safe logs, and a server-only API key

Unity is deliberately thin: it contains no OpenAI key, system prompt, hidden goal, or
simulation implementation.

## GPT-5.6 inside the product

1. Three role-separated stakeholder agents answer from distinct public roles and private
   objectives.
2. A strict structured-output simulator returns the canonical five-year consequence result.
3. A retrieval-grounded six-part grader cites evidence and asks for revision.
4. A structured policy-brief generator turns the revised journey into a validated cited
   report.

Model output is treated as untrusted data and must pass contract, domain, source-ID, and
citation validation before the Unity client displays it.

## Novelty and impact

AgriVerse does not reward chatting with AI. It requires evidence, exposes disagreement,
connects choices to multi-year tradeoffs, and makes revision visible through an
original-versus-revised Future Walk. The scenario-engine boundary makes the learning design
reusable for other cited agricultural-environmental decisions.

## How Codex was used

Codex accelerated the scenario and JSON-contract architecture, Express/OpenAI integration,
Unity client and 3D implementation, role-separated prompt wiring, validation, tests,
licensed-asset intake, release automation, and regression debugging.

The human chose the educational and product direction: evidence before answers, one complete
Vietnam episode before expansion, meaningful stakeholder disagreement, revision as the core
learning mechanic, rejection of unsuccessful world and UI directions, and the final Living
Field Atlas visual identity. Human testing also discovered the silent empty-globe and
contaminated-build failures; Codex corrected their root causes and added regressions.

## Links

- Repository: https://github.com/Trik-mink/AgriVerse
- macOS release:
  https://github.com/Trik-mink/AgriVerse/releases/tag/v1.0.0-build-week
- Public YouTube demo: `<PUBLIC_YOUTUBE_URL>`
- Builder `/feedback` Session ID: `<BUILDER_FEEDBACK_SESSION_ID>`

## Judge instructions

Download the universal macOS archive and checksum from the GitHub Release. Verify the
checksum, extract, then Control-click the ad-hoc-signed application and choose **Open** if
Gatekeeper asks. No API key, credits, Node.js, or Unity installation is required. Full
controls and troubleshooting are in `docs/JUDGE-ACCESS.md`.

## Known limitations

- macOS is the supported judge platform.
- The app is ad-hoc signed and not notarized.
- Live GPT stages depend on a temporary hosted judge service with rate, budget, and
  expiration controls.
- Incoming globe locations are previews rather than playable episodes.
- Audio is representative ambience, not a Vietnam field recording.
- Characters are text-first and have no voice or facial-animation system.

## Assets and licensing

Original AgriVerse source code is Apache-2.0. Third-party and generated models, textures,
photographs, audio, fonts, datasets, and other assets retain the licenses and terms recorded
in `THIRD_PARTY_NOTICES.md` and adjacent provenance records. Apache-2.0 does not override
those notices.
