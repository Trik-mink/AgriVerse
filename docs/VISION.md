# AgriVerse - Full Vision

## One-line pitch
AgriVerse is an immersive 3D learning world where a student travels the globe, drops into a
real agricultural crisis, investigates it with real data, talks to the people living it, proposes
an evidence-based solution, and watches the consequences unfold - learning climate-agriculture
decision-making by *doing*, not by reading a textbook.

## The feeling we're building
A student should feel like they *arrived* somewhere real. Wake-up into a living world, a friendly
guide beside them, characters who look them in the eye and speak, a place worth saving. Warm,
hopeful, cinematic - closer to an animated film or a narrative game than a quiz app.

---

## The complete experience (full vision)

### 1. Entry & identity
- A cinematic landing that sets tone (calm, farming, hopeful).
- Enter a name; choose/customize a character avatar.
- A "wake up, blur into focus" cinematic arrival into the world.

### 2. The globe
- An interactive 3D globe the student can rotate, zoom, and touch.
- Pins/markers for multiple countries, each with a crisis rooted in its real geography and culture
  (Vietnam salinity, Bangladesh flooding, Sahel drought, California water, etc.).
- Selecting a country flies the camera down into that place.

### 3. The country scenario (flagship: Vietnam - Mekong Delta saltwater intrusion)
The student moves through a continuous, guided journey - a persistent companion leads them from
place to place. Each stop is a real activity:
- **Investigate the land:** test water/soil at different sites; read real salinity, rainfall,
  crop, and economic data.
- **Meet the people:** interview embodied stakeholders who each want different things -
  - a rice farmer worried about income and risk,
  - an environmental researcher worried about long-term sustainability,
  - a local official worried about cost and scalability.
  They disagree with each other because their incentives differ.
- **Design a solution:** propose an intervention (salt-tolerant rice, rice-shrimp conversion,
  crop change, infrastructure, support measures) - judged on a real multi-factor model
  (salinity, seasonality, freshwater access, farmer capital), not a single number.
- **See the consequences:** watch a simulated multi-year outcome play out (yields, income,
  sustainability, cost).
- **Get feedback & revise:** research-grounded critique of what the solution missed; revise.
- **Capstone:** a final policy brief + a shareable certificate of completion.

### 4. Scale (the "every country" ambition)
AgriVerse is a **scenario engine**: every country is defined by data (a scenario file), not
hardcoded. Vietnam is the reference implementation; adding a country is authoring content, not
rebuilding the product. The globe is the visible promise of that scale.

---

## The AI core (the heart of the product)
Every core mechanic is powered by GPT-5.6 at runtime, grounded in real cited data:
1. **Three stakeholder agents** - separate personas, each with a private hidden goal and knowledge
   boundary, so their conflict is emergent and real.
2. **Consequence simulator** - returns a structured multi-year projection grounded in real ranges.
3. **Retrieval-grounded feedback grader** - critiques the student's plan against a cited corpus,
   with a rubric; never invents statistics.
4. **Policy-brief generator** - turns the revised solution into a formatted capstone artifact.
All AI outputs are structured, validated, and grounded. This is the "skillful GPT-5.6 use" pillar.

---

## Educational impact
Target: high-school environmental science / geography students who currently learn
climate-agriculture connections through static textbook content, not decision-based practice.
AgriVerse turns a passive topic into active, evidence-based decision-making with real tradeoffs -
the exact skill the subject is supposed to teach.

---

## Visual / world direction
- Warm, coherent, film-quality feel. Real Vietnamese Mekong Delta reference (rice paddies, water
  channels, sampan boats, stilt houses, palms) - authentic and respectful, never caricature.
- Characters are appealing and expressive; the world is alive (motion, light, ambient life).
- Where real-time 3D fidelity isn't feasible, the *cinematic* quality lives in pre-rendered
  sequences and the demo film.

---

## Build tiers (how to approximate this without over-scoping)
Codex should build in vertical slices, deepest-value first. Do NOT attempt everything at once.

- **Tier 0 (must ship):** ONE country (Vietnam) fully playable end-to-end - investigate → interview
  → propose → consequences → feedback → brief → certificate - with the four grounded GPT-5.6
  systems working. This is the flagship and the proof.
- **Tier 1 (immersion):** guided continuous journey, cinematic arrival, embodied characters,
  a beautiful realized environment.
- **Tier 2 (scale):** the interactive globe + one or two additional country scenarios, proving the
  scenario engine generalizes.
- **Tier 3 (polish):** avatar customization, richer animation, audio, accessibility, more countries.

Ship Tier 0 completely before adding any higher tier. Every tier must degrade gracefully - a
higher-tier feature failing must never break the core learning loop.

---

## Non-negotiables
- The four GPT-5.6 systems stay grounded in real cited data; never invent statistics.
- Never put an API key in the client; route model calls through a backend.
- The core learning loop must always work, even if immersive/visual layers fail.
- One country built deep beats many countries built shallow.
