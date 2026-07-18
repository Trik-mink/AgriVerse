# AgriVerse - Episode 1: The Salt Line
## Narrative script, guide lines, glossary, and certificate text

All text is final copy, ready to wire in. Rules for implementation: this is authored
presentation content (not GPT-generated at runtime); interpolate [PLAYER] with the entered
name; never alter numbers - every figure here matches scenario.json / data-sources.md.
Plain ASCII throughout.

---

## 1. Landing screen

TITLE: AgriVerse
EPISODE: Episode 1: The Salt Line
TAGLINE: Test the water. Hear the people. Revise the future.

INTRO PARAGRAPH:
A farming community in Vietnam's Mekong Delta is losing its rice to something it cannot
see. You are the field advisor sent to find out why - and to leave behind a plan that
works. The district council meets after tomorrow's tide. What you learn before then is
up to you.

NAME PROMPT: What should the community call you?
START BUTTON: Begin the field mission

## 2. The guide

Name: Mai (young local field coordinator; warm, practical, encouraging; she guides,
she never gives answers or invents facts).

ARRIVAL (wake-up):
"[PLAYER]? Good - you're here. I'm Mai, your field coordinator. Something is wrong
with the water, and nobody agrees on what to do about it. The district council meets
after tomorrow's tide, and they expect a plan grounded in evidence. Let's start where
every good plan starts: with the water itself."

## 3. Investigation - with predict-before-reveal

STATION INTRO:
"Three plots, three stories. Test each one and your notebook will keep the record.
Before each reading - tell me what you expect. Good advisors predict, then measure."

PREDICT PROMPTS (show before revealing each reading; two choices each, no penalty):
- Coastal plot: "This plot sits nearest the sea. What do you expect - water rice can
  live with, or water that has already turned against it?"
- Mid-delta plot: "Halfway between river and sea. Safe, or on the edge?"
- Upstream plot: "Far from the coast, fed by the river. Your prediction?"

AFTER EACH READING (generic): "Now the numbers are yours. The notebook remembers."

AFTER ALL THREE:
"Three plots, three different worlds - and the same delta. Numbers alone will not
finish this plan. Time to hear the people who live with this water."

## 4. Interviews

STATION INTRO:
"Meet all three - the farmer, the researcher, the official. They disagree, and every
one of them is right about something. Ask real questions. Your plan will have to
answer to each of them."

AFTER ALL THREE HAVE REPLIED:
"You have heard the field, the science, and the district. Ready to put a plan on the
table?"

## 5. Planning

STATION INTRO:
"Build your proposal. Match the intervention to what you measured - and to what you
heard. If your plan needs money the farmer does not have, say who will carry that
cost. The council will ask."

## 6. Consequences (Future Walk)

INTRO: "The model will now play your plan forward five years. Walk through them
slowly. The delta answers honestly."

## 7. Feedback

INTRO: "The review is grounded in the evidence - every claim is cited. Read what
your plan does well, and what it missed. No advisor gets it perfect the first time."

## 8. Revision

INTRO: "This is the real work of an advisor: take what you learned and make the
plan stronger. The community is counting on the second draft."

AFTER IMPROVED RESULT: "Look at the difference. That is what revision does - not
an admission of failure, but the way good plans are made."

## 9. Policy brief and ending

BRIEF INTRO: "Your findings, your interviews, your plan - written up for the
council. This is what you leave behind."

ENDING (after "Investigation complete"):
"The council has your brief, [PLAYER]. Your mission here is done - you have earned
the journey home. Or stay another season, if the delta has more to teach you.
Either way: this community plans its future with your work in its hands."

CHOICES: [ Return home ] [ Stay another season (free exploration) ]
(If "stay" is not implemented, show only the certificate.)

## 10. Certificate

CERTIFICATE OF FIELD SERVICE
[PLAYER]
completed the AgriVerse field investigation
"Episode 1: The Salt Line" - Mekong Delta, Vietnam
Recommended intervention: [chosen intervention label(s)]
Evidence gathered. Voices heard. Plan revised.
[date]

## 11. Glossary (plain language, shown inline or on tap next to terms)

- Salinity: how much salt is in the water. Rice starts to suffer around 4 g/L.
- g/L: grams of salt in each liter of water. Seawater is about 35; healthy rice
  water stays under 4.
- Brackish: saltier than a river, fresher than the sea.
- Salt pattern - persistent brackish: the water stays salty all year.
- Salt pattern - brackish dry, fresh wet: salty in the dry season, fresh again
  when the rains return. Some plans depend on exactly this swing.
- Freshwater access: how easily clean water can reach this plot to dilute or
  flush away salt.
- Dry season / wet season: the delta's two worlds. Less river flow in the dry
  season lets the sea push in; the wet season pushes it back.
- Yield (t/ha): how much rice a field produces, in tonnes per hectare. A healthy
  paddy here is about 6; salt-hit fields can fall to 3 or less.
- Income score / sustainability score: 0-100 comparison indexes from the model -
  higher is better. They compare plans; they are not real-world statistics.
- Fit assessment: whether your plan matches this place on four things - the salt
  level, the seasons, the freshwater, and what the farmer can afford.
- Source IDs (S1, S2...): the real published sources behind each fact. Nothing
  here is invented.

## 12. Judge View labels (for the toggleable panel)

PANEL TITLE: Judge View - under the hood
- Responding agent: [stakeholder id] (role-separated GPT-5.6 persona)
- Grounding evidence: [source IDs]
- Raw structured output: [JSON]
- Rubric result: [grader output]
- Citation validation: [pass/fail]
- Fallback used: [yes/no]
FOOTNOTE: Every number and claim above is traceable to the cited corpus. The AI
systems are the game mechanics.
