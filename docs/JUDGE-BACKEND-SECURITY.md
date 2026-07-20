# Judge Backend Security and Budget Controls

Last reviewed: July 20, 2026

The public judge path is intentionally narrow:

`Unity desktop -> HTTPS Express service -> OpenAI Responses API`

The OpenAI key remains in Render's secret environment. Unity receives only validated
scenario, stakeholder, simulator, grader, and policy-brief responses.

## Threat model

| Risk | Control |
| --- | --- |
| Public unauthenticated traffic | IP, opaque-session, route, and concurrency limits; an absolute lifetime budget gate |
| Automated cost exhaustion | Durable US$9 internal cap, conservative pre-call reservation, one service instance, judge-window expiration |
| Oversized or malformed input | 256 KiB HTTP body ceiling, strict Zod schemas, bounded strings and arrays, bounded proposal parameters |
| Prompt injection in student questions | Student text remains a bounded user message; versioned system prompts, private stakeholder roles, grounding corpus, strict structured outputs, and citation validation remain authoritative |
| Concurrent calls and retry storms | Two model calls globally, one per session, no SDK retries, bounded structured-validation retries |
| Replay or repeated-session abuse | Six-hour per-session route limits plus a separate IP limit and durable lifetime cap |
| Secret or private-text leakage | No prompt, hidden goal, free text, model response, IP address, authorization header, or credential is logged |
| Browser CORS misuse | No wildcard origin; only exact configured development origins; origin-free Unity desktop traffic remains supported |
| Invalid model output | Model output is untrusted and passes existing Zod, domain, evidence-ID, and citation checks before use |
| Restart or deploy resets spending | A checksummed ledger is stored on the single service's persistent disk and reloaded at startup |
| Missing or corrupt ledger | Expensive routes fail closed; the application never silently creates a replacement after the one-time bootstrap deadline |
| Judging-window overrun | Expensive routes reject before OpenAI access at the configured expiration |

## Normal journey allowance

A complete original-and-revised journey normally uses eight GPT calls:

- Three stakeholder replies
- Two consequence simulations
- Two grounded feedback results
- One policy brief

The session route limits allow that journey plus modest retry room: 6 stakeholder calls,
3 simulations, 3 feedback calls, and 2 policy briefs, with 14 expensive calls total.
The OpenAI SDK performs no automatic retries. Structured-output validation retains its
existing maximum of three explicit attempts; every attempt independently reserves budget.

## Durable spending invariant

The internal application cap is US$9, below the human-authorized absolute US$10 maximum.
Before each Responses API call, the ledger atomically charges a conservative reservation.
The call is rejected before provider access if the reservation would exceed the cap.

After a successful response, returned usage reconciles the reservation. A provider failure
retains the full reservation. A crash after reservation also leaves that charge in place.
These choices may end judge access early, but cannot reset the ledger or spend optimistically.

The estimator was verified against the official OpenAI pricing page on July 20, 2026:
[OpenAI API pricing](https://developers.openai.com/api/docs/pricing). For the configured
`gpt-5.6` alias, it conservatively prices every input token at the cache-write rate
(US$6.25 per million) and output at US$30 per million. It does not assume cache discounts.
Input reservation uses UTF-8 bytes as a token-count upper bound plus framing allowance
and rejects an upper bound above 240,000 tokens, keeping the service below the model's
long-context pricing threshold.
Text responses are capped at 700 output tokens and structured responses at 3,200.

The ledger is checksummed and written by atomic replacement with restrictive file
permissions. It lives at `BUDGET_LEDGER_PATH` on a 1 GB Render persistent disk. Only a
single Node process and one Render instance may use it. A short
`BUDGET_LEDGER_BOOTSTRAP_UNTIL` window permits the first empty disk to initialize; after
that deadline a missing ledger disables AI access.

## Hosted configuration

- One Render Starter web service
- One 1 GB persistent disk
- One instance; no Postgres, Redis, worker, cron, preview service, or custom domain
- Health check: `/health` (never calls OpenAI)
- Node pinned by `.node-version`
- Deterministic `npm ci` and compiled production server
- Judge expiration: `2026-08-07T00:00:00Z`

Render's published price on July 20, 2026 was US$7/month for Starter plus
US$0.25/GB/month for the disk. Render states compute is prorated by the second. Even a
full month of this authorized configuration is US$7.25, below the US$8 ceiling.
[Render pricing](https://render.com/pricing) and
[persistent disk behavior](https://render.com/docs/disks) are the operational sources.

## Safe operations

The health endpoint exposes only service state, the deployed commit, and whether AI access
is ready, expired, or not configured. Logs contain request IDs, normalized routes, HTTP
status, latency, rate-limit events, conservative cost metadata, and remaining internal
budget. They never contain secret values or student/model content.

Owner operations:

```sh
render whoami
render services
render logs --help
```

Use the Render service dashboard to inspect sanitized logs, redeploy a known commit,
roll back to a previous deploy, or suspend the service. After judging:

1. Suspend the Render service.
2. Revoke the AgriVerse judge project API key in the OpenAI platform.
3. Retain or archive the ledger only as a private cost record.

Never download, print, or copy the environment group's secret values during these
operations.
