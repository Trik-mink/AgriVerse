# AgriVerse - Codex / GPT-5.6 Playbook

How to use GPT-5.6 skillfully enough to win the Technological Implementation axis,
and how to run the three Codex sessions so the submitted thread tells a strong story.

---

## Part A - The core principle

There are TWO ways this hackathon rewards "GPT-5.6 use." Do both, on purpose.

1. **GPT-5.6 as build partner** (Codex writes the code). Documented in the README.
2. **GPT-5.6 as the product's runtime engine** (the model IS the gameplay).
   THIS is where the tech points live. Every core mechanic below is a structured GPT-5.6 call.

If a judge can open your repo and see *the prompts and the eval results*, you win this axis.
So: keep prompts in versioned files (e.g. `/prompts/*.md`), not buried in code strings.

---

## Part B - The four runtime GPT-5.6 systems (build these, in priority order)

### 1. Three stakeholder agents with PRIVATE goals (highest-value, most impressive)
- Farmer, researcher, official. Each is a separate system prompt with a hidden objective,
  a knowledge boundary, and a personality. They should *disagree with each other* on the
  same facts because their incentives differ - that emergent tension is the wow moment.
- Skillful technique to name in the README/demo: **role-separated multi-agent prompting
  with private state.** Each agent only knows what its character would know.
- Guardrail: give each a "stay in role / don't invent statistics beyond the provided data"
  instruction, and feed them the real figures from `data-sources.md` so they're grounded.

### 2. Consequence simulator = STRUCTURED OUTPUT (JSON), not free text
- Student submits an intervention. GPT-5.6 returns a strict JSON object:
  `{ year_1..year_5: { rice_yield, farmer_income, salinity_gL, sustainability_score,
  cost_to_date }, narrative_per_year, tradeoffs_triggered[] }`
- Why it scores: this is **structured output / function-calling**, and it makes the sim
  reproducible and testable. Judges trust JSON they can inspect over a chatbot's vibes.
- Ground the numbers in the real ranges (30-70% yield loss, $9,800-$40,000/ha rice-shrimp,
  4 g/L threshold) so outputs stay plausible.

### 3. Retrieval-grounded feedback grader (the anti-hallucination flex)
- After the student proposes, GPT-5.6 critiques "what your solution missed" - but grounded
  in the cited corpus (feed it the `data-sources.md` facts as context / lightweight RAG).
- Skillful technique to name: **retrieval-augmented grading with a rubric.** The feedback
  cites the salinity-level logic (e.g. "you chose rice-shrimp but your tested plot was 2 g/L
  - salt-tolerant rice would keep the farmer in higher-value rice").
- This directly showcases the non-obvious insight (solution must match measured salinity).

### 4. Policy-brief generator
- Final GPT-5.6 call turns the student's revised solution + sim results into a formatted
  one-page brief. Structured output again (headings, evidence, tradeoffs).

---

## Part C - The country-agnostic architecture (this is "every country lives in the pitch")

Build Vietnam as data, not as hardcode. One `scenario.json` schema drives everything:

```jsonc
{
  "id": "vietnam-mekong-salinity",
  "title": "Saving a Mekong Delta farming community",
  "location": { "region": "Mekong Delta", "env_asset": "mekong_delta_3d" },
  "crisis": {
    "type": "saltwater_intrusion",
    "driver_summary": "Reduced dry-season upstream flow + sea-level rise push the 4 g/L salinity line inland.",
    "key_metric": { "name": "salinity", "unit": "g/L", "danger_threshold": 4 }
  },
  // Decision depends on a VECTOR of 4 real factors, not salinity alone.
  "decision_factors": ["salinity_gL", "season", "freshwater_access", "farmer_capital"],
  "test_sites": [
    { "id": "coastal",  "salinity_gL": 12, "season": "dry", "freshwater_access": "low",  "note": "rice impossible; brackish year-round" },
    { "id": "mid",      "salinity_gL": 3,  "season": "dry", "freshwater_access": "med",  "note": "marginal - salt-tolerant rice viable if fresh water can dilute" },
    { "id": "upstream", "salinity_gL": 0.5,"season": "wet", "freshwater_access": "high", "note": "safe - normal rice" }
  ],
  // Salinity is SEASONAL: brackish in the dry season, fresh in the wet season.
  // This is why rice-shrimp works (shrimp in dry/brackish, rice in wet/fresh).
  "farmer_capital": "low",   // Mr. Ba can't easily fund capital-intensive conversion
  "datasets": [ /* the cited figures from data-sources.md, with source URLs */ ],
  "stakeholders": [
    { "id": "farmer",     "hidden_goal": "protect income & continuity", "knows": [...], "persona": "..." },
    { "id": "researcher", "hidden_goal": "long-term sustainability",     "knows": [...], "persona": "..." },
    { "id": "official",   "hidden_goal": "cost & scalability",           "knows": [...], "persona": "..." }
  ],
  "interventions": [
    // viability depends on the FACTOR VECTOR, not just salinity
    { "id": "salt_tolerant_rice", "viable_when": "salinity <=4 g/L AND some freshwater access", "cost": "low",  "income": "medium", "sustainability": "medium", "capital_need": "low" },
    { "id": "rice_shrimp",        "viable_when": "seasonal salinity (brackish dry / fresh wet) AND farmer has capital", "cost": "high", "income": "high", "sustainability": "contested", "capital_need": "high" },
    { "id": "crop_change",        "viable_when": "salinity >2 g/L, low freshwater", "cost": "med",  "income": "varies", "sustainability": "high", "capital_need": "med" }
  ],
  "rubric": { /* a good solution must address: (1) salinity match, (2) seasonality/duration,
                 (3) freshwater availability, (4) farmer capital/income, plus cost/scale and long-term soil */ }
}
```

**The pitch line:** "AgriVerse is a scenario engine. Vietnam is the reference implementation.
Adding Bangladesh flooding or California drought is authoring one JSON file - Codex can
generate a draft scenario from a country name - not rebuilding the product."

Optional flex for the demo: show Codex generating a *second* scenario's JSON stub live from
a prompt. Proves the architecture generalizes without you building a second full country.

---

## Part D - How to run the three Codex sessions

**Session 1 - Architecture/planning (NOT submitted).**
Use it to: pressure-test this schema, write the four system prompts, design the JSON contracts,
design the eval cases. Output = spec files + `/prompts/`. Decisions are YOURS here; capture them.

**Session 2 - Implementation (THIS IS THE ID YOU SUBMIT).**
Rules to keep the submitted thread strong, since judges may read it:
- Do the *majority of core functionality* here. Don't let big features leak into session 1 or 3.
- Keep prompts, ask Codex to explain tradeoffs, and make YOUR decisions visible in your replies
  ("no, ground the grader in the corpus instead of free-associating"). The thread should read
  like a builder directing a capable partner, not "write my whole app."
- Land the four runtime systems + the scenario engine here.

**Session 3 - Throwaway debugging (NOT submitted).** Environment errors, dependency hell,
unrelated bugs. Keeps the submitted thread clean and on-narrative.

---

## Part E - The one thing that most teams skip (do it, it's a differentiator)

**Write ~5 eval cases and run them.** Examples:
- Persona test: "Does the official ever advocate for the farmer's income over cost?" (should resist)
- Grounding test: submit a rice-shrimp plan for a 2 g/L plot -> grader must flag the salinity mismatch.
- Structured-output test: does the sim always return valid JSON with 5 years?
Screenshot the results. Mentioning "we evaluated our GPT-5.6 systems" in the demo is a rare,
credible signal of skillful use that most hackathon teams never bother with.
