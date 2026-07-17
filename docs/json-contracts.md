# AgriVerse JSON Contracts

Status: agreed planning contract, version `1.0`.

These contracts define the boundaries between the consequence simulator, feedback grader,
policy-brief generator, and UI. They are specifications only. The implementation session must
translate them into strict structured-output schemas and validate every model response at the
application boundary.

## Contract decisions

1. Intervention fit is a vector, not a salinity gate. Every decision uses measured salinity,
   season and duration pattern, freshwater access, and farmer capital.
2. The canonical `FitAssessment` has exactly five fields: `salinity`, `seasonality`,
   `freshwater`, `farmer_capital`, and `overall`. Every value is `fit` or `mismatch`.
3. `overall` is `fit` only when all four factor fields are `fit`. Any failed or omitted factor
   makes it `mismatch`; there is no `partial` state.
4. A proposal may resolve a capital or freshwater constraint only through an explicit support
   measure allowed by the scenario. The model must not assume financing, infrastructure, water,
   or a seasonal operating plan that the student did not provide.
5. All contract fields use `snake_case`. Every model output includes `contract_version` and
   `scenario_id`.
6. The canonical shared outcome object is `OutcomeMetrics`. Its four required keys are exactly
   `salinity`, `yield`, `income`, and `sustainability`.
7. The simulator owns `FitAssessment` and outcome projections. The grader copies the complete
   fit object and consumes simulator outcomes without renaming them. The brief copies the fit
   object and year 1/year 5 outcomes by deep equality; it never recalculates either.
8. Measurements and yields carry units as data. Commodity IDs, units, currencies, and source IDs
   come from `scenario.json`; contract field names never encode Vietnam, rice, dollars, or `g/L`.
9. A source fact and a model projection are different things. Simulator numbers are projections
   anchored to the supplied corpus, never observations or guarantees. The 0-100 income and
   sustainability scores are synthetic comparison indexes, not percentages or published facts.
10. The corpus has no numeric intervention-cost series. The contract uses qualitative
    `cost_level` and omits the draft `cumulative_cost_usd`/`cost_to_date` field.
11. Currency-denominated income is nullable. It may be populated only when the corpus supports a
    value or range for the selected intervention; otherwise `projected_value`, `currency`, and
    `basis` are null.
12. Missing grounded yield data is represented as `null`, not guessed. `yield.items` supports
    mixed systems with more than one commodity and unit.

## Shared types

### DecisionContext

The app builds this object from the selected test site and the scenario-level farmer profile. The
simulator returns it unchanged so downstream systems can show what was actually evaluated.

```jsonc
{
  "salinity": { "value": 0.0, "unit": "<scenario metric unit>" },
  "season": {
    "current": "<dry | wet>",
    "duration": "<seasonal | persistent>",
    "salinity_pattern": "<scenario pattern id>"
  },
  "freshwater_access": "<none | low | medium | high>",
  "farmer_capital": "<low | medium | high>"
}
```

The `season` object carries both the sampling season and duration/pattern needed to distinguish a
temporary dry-season reading from persistent salinity.

For `scenario.json`, map `test_site.salinity_gL` to `salinity.value`, `test_site.season` to
`season.current`, `test_site.salinity_duration` to `season.duration`,
`test_site.seasonal_pattern` to `season.salinity_pattern`, and copy
`test_site.freshwater_access` plus the scenario-level `farmer_capital` unchanged.

### FitAssessment

```json
{
  "salinity": "fit",
  "seasonality": "fit",
  "freshwater": "fit",
  "farmer_capital": "mismatch",
  "overall": "mismatch"
}
```

Field mapping from `scenario.json`:

| Fit field | Decision factor | What it tests |
|---|---|---|
| `salinity` | `salinity_gL` | Measured concentration versus the intervention's salinity condition. |
| `seasonality` | `season` | Current season, duration, wet/dry pattern, and required seasonal plan. |
| `freshwater` | `freshwater_access` | Flushing, dilution, or fresh-season water need. |
| `farmer_capital` | `farmer_capital` | Capital need versus available capital or an explicit allowed support measure. |
| `overall` | all four | Logical AND of the four factor statuses. |

If an intervention requires the student to explain a factor and the proposal omits it, that
factor is `mismatch` even when the physical context could otherwise be compatible.

### SalinityMetric

| Field | Type | Rules |
|---|---|---|
| `value` | number | Non-negative; measured values are copied unchanged and projected values retain the scenario unit. |
| `unit` | string | Must equal the scenario key metric unit, `g/L` in the reference scenario. |

### YieldItem

| Field | Type | Rules |
|---|---|---|
| `commodity_id` | string | Must match a commodity ID declared by the scenario or selected intervention. |
| `value` | number or null | Non-negative when present; null when the corpus supplies no defensible range. |
| `unit` | string | Must use the unit supplied for that commodity by the scenario/corpus. |

### IncomeMetric

| Field | Type | Rules |
|---|---|---|
| `score` | integer | Model-derived comparison index from 0 through 100; not a percentage. |
| `scale_min` | integer | Always `0`. |
| `scale_max` | integer | Always `100`. |
| `projected_value` | number or null | Non-negative and corpus-bounded when present. |
| `currency` | string or null | Scenario currency code; null exactly when `projected_value` is null. |
| `basis` | string or null | Corpus basis such as `per_ha`; null exactly when `projected_value` is null. |

`projected_value`, `currency`, and `basis` are either all populated or all null.

### SustainabilityMetric

| Field | Type | Rules |
|---|---|---|
| `score` | integer | Model-derived soil/water comparison index from 0 through 100; not a percentage. |
| `scale_min` | integer | Always `0`. |
| `scale_max` | integer | Always `100`. |

### OutcomeMetrics

This object is immutable across consumers: downstream systems may copy it but may not rename,
round, recompute, or change its shape.

```jsonc
{
  "salinity": { "value": 0.0, "unit": "<scenario metric unit>" },
  "yield": {
    "items": [
      {
        "commodity_id": "<scenario commodity id>",
        "value": null,
        "unit": "<scenario commodity unit>"
      }
    ]
  },
  "income": {
    "score": 0,
    "scale_min": 0,
    "scale_max": 100,
    "projected_value": null,
    "currency": null,
    "basis": null
  },
  "sustainability": {
    "score": 0,
    "scale_min": 0,
    "scale_max": 100
  }
}
```

The zeroes above show types and required fields, not expected scenario outcomes.

### EvidenceRef

```json
{
  "source_ids": ["S2"],
  "simulation_years": [1, 5]
}
```

`source_ids` may contain only IDs present in the scenario source registry.
`simulation_years` may contain only integers 1 through 5. At least one array must be non-empty.
A factual corpus claim requires a source ID; a projected claim requires a simulation year.

## Consequence simulator

### Input

```jsonc
{
  "scenario": "<validated scenario.json object>",
  "proposal": {
    "intervention_ids": ["<scenario intervention id>"],
    "parameters": {},
    "support_measures": ["<scenario-permitted mitigation id>"],
    "rationale": "<student text addressing all four factors>"
  },
  "target_site_id": "<scenario test-site id>",
  "decision_context": "<DecisionContext>",
  "grounding_corpus": ["<retrieved source records>"]
}
```

### Output: SimulatorResult

```jsonc
{
  "contract_version": "1.0",
  "scenario_id": "<scenario id>",
  "intervention_summary": "<one-line summary>",
  "decision_context": "<unchanged DecisionContext>",
  "fit_assessment": "<FitAssessment>",
  "years": [
    {
      "year": 1,
      "outcomes": "<OutcomeMetrics>",
      "cost_level": "low | medium | high | varies | not_quantified",
      "narrative": "<1-2 sentences>",
      "evidence_source_ids": ["<source id>"]
    }
  ],
  "tradeoffs": [
    {
      "category": "yield | income | cost | sustainability | scale | farmer_buy_in | salinity | seasonality | freshwater | farmer_capital",
      "summary": "<grounded tradeoff>"
    }
  ],
  "headline": "<one-sentence five-year summary>"
}
```

Validation invariants:

- `decision_context` is deep-equal to the input context.
- `fit_assessment` has exactly five keys with only `fit`/`mismatch` values; `overall` is the
  logical AND of the four factor statuses.
- `years` has exactly five entries, ordered 1 through 5, and every entry has all four canonical
  outcome keys.
- Fit is derived from scenario `viable_when` rules and explicit proposal support measures, not
  general model memory or salinity alone.
- Any mismatch must produce visibly worse outcomes than a factor-compatible comparator for the
  same site and farmer, using yield, income score, sustainability score, or a defensible
  combination of those outcomes.
- Each year cites at least one source that supports its projection logic.
- Conductivity units are preserved. Reference yield-loss evidence uses `4` and `6 dS/m`
  (approximately `2.56` and `3 g/L` in the corpus), not `4` and `6 g/L`.

## Feedback grader

### Input

```jsonc
{
  "scenario": "<validated scenario.json object>",
  "proposal": "<same proposal sent to simulator>",
  "target_site_id": "<site used by proposal>",
  "decision_context": "<DecisionContext>",
  "simulation": "<complete validated SimulatorResult>",
  "grounding_corpus": ["<retrieved source records>"]
}
```

The grader copies `simulation.fit_assessment` and reads
`simulation.years[].outcomes.salinity`, `yield`, `income`, and `sustainability` directly.

### Output: GraderResult

```jsonc
{
  "contract_version": "1.0",
  "scenario_id": "<scenario id>",
  "fit_assessment": "<exact copy of simulation.fit_assessment>",
  "rubric_results": [
    {
      "rubric_id": "salinity_fit | seasonality_fit | freshwater_fit | farmer_capital_fit | cost_scalability | long_term_sustainability",
      "rating": "meets | partly_meets | does_not_meet",
      "feedback": "<specific feedback>",
      "evidence": "<EvidenceRef>"
    }
  ],
  "key_insight": {
    "text": "<most important missed factor or tradeoff>",
    "evidence": "<EvidenceRef>"
  },
  "revision_prompt": "<one concrete question>",
  "encouragement": "<one supportive sentence>"
}
```

Validation invariants:

- `fit_assessment` is deep-equal to the simulator object.
- `rubric_results` contains all six rubric IDs exactly once in the documented order.
- Each mismatched factor forces its mapped rubric result to `does_not_meet` and explicit feedback.
- At least one result is `partly_meets` or `does_not_meet`.
- Every factual claim resolves to a supplied source ID. Model projections are labeled and tied to
  `simulation_years`; they are never restated as observed facts.

## Policy-brief generator

### Input

```jsonc
{
  "scenario": "<validated scenario.json object>",
  "final_proposal": "<student's revised proposal>",
  "target_site_id": "<site used by proposal>",
  "decision_context": "<DecisionContext>",
  "simulation": "<complete validated SimulatorResult for final proposal>",
  "stakeholder_concerns": [
    {
      "stakeholder_id": "<scenario stakeholder id>",
      "concern": "<concern raised during interview>"
    }
  ],
  "grounding_corpus": ["<retrieved source records>"]
}
```

### Output: PolicyBriefResult

```jsonc
{
  "contract_version": "1.0",
  "scenario_id": "<scenario id>",
  "title": "<brief title>",
  "problem_statement": {
    "text": "<2-3 grounded sentences>",
    "source_ids": ["<source id>"]
  },
  "evidence": [
    {
      "claim": "<corpus fact>",
      "source_ids": ["<source id>"]
    }
  ],
  "recommended_solution": {
    "summary": "<revised intervention>",
    "fit_assessment": "<exact copy of simulation.fit_assessment>",
    "factor_rationale": {
      "salinity": "<fit rationale>",
      "seasonality": "<fit rationale>",
      "freshwater": "<fit rationale>",
      "farmer_capital": "<fit rationale>"
    },
    "evidence": "<EvidenceRef>"
  },
  "projected_outcomes": {
    "year_1": "<deep-equal copy of simulation.years[0].outcomes>",
    "year_5": "<deep-equal copy of simulation.years[4].outcomes>",
    "summary": "<2-3 sentence comparison>"
  },
  "tradeoffs_and_risks": [
    {
      "category": "cost | scale | soil_water | farmer_buy_in | yield | income | salinity | seasonality | freshwater | farmer_capital | uncertainty",
      "risk": "<real downside, limitation, or uncertainty>",
      "mitigation": "<realistic response>",
      "evidence": "<EvidenceRef>"
    }
  ],
  "stakeholder_balance": [
    {
      "stakeholder_id": "<scenario stakeholder id>",
      "concern": "<interview concern>",
      "response": "<how recommendation responds or acknowledges it>"
    }
  ],
  "next_steps": [
    {
      "order": 1,
      "action": "<concrete step>",
      "owner_stakeholder_id": "<scenario stakeholder id>"
    }
  ]
}
```

Validation invariants:

- `evidence` has 3-4 entries; `tradeoffs_and_risks` has at least 2; `next_steps` has 2-3.
- `recommended_solution.fit_assessment` is deep-equal to the simulator object, and all four
  `factor_rationale` fields explicitly address the supplied decision context.
- `projected_outcomes.year_1` and `year_5` are deep-equal to the simulator's canonical outcome
  objects. Only `summary` may translate them into prose.
- Every mismatched factor is disclosed in the recommendation and risks; no mismatch is softened.
- Every corpus claim cites a source ID, every projected claim cites simulation years, and no model
  projection is presented as a guarantee.
- At least two genuine downsides, limitations, or uncertainties appear. Benefits rephrased as
  risks do not pass validation.

## Prompt edits applied

| Prompt | Contract correction |
|---|---|
| `prompts/consequence-simulator.md` | Replaced the draft salinity-only classification with five-key `fit_assessment`; added the copied four-factor decision context, omission/mitigation rules, and worse-outcome behavior for any factor mismatch. Retained canonical outcomes, qualitative cost, source IDs, five-year validation, and corrected conductivity units. |
| `prompts/grader-feedback.md` | Added the simulator's decision context and fit object as authoritative inputs; expanded the rubric to six rows so all four factors, scale, and sustainability are independently graded; mapped every factor mismatch to a required failing row. |
| `prompts/policy-brief-generator.md` | Added an exact simulator fit copy plus four factor-specific rationales; retained deep-equal year 1/year 5 outcome snapshots; expanded risk categories and required disclosure of every mismatch. |

No application validators or JSON Schema files were created in this planning session. The builder
session should encode these shapes with `additionalProperties: false` (or equivalent strict mode),
reject invalid model output, and retry rather than render partially valid JSON.
