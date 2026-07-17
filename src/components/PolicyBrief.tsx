import type { PolicyBriefResult, Scenario } from '../types';

export function PolicyBrief({ brief, scenario }: { brief: PolicyBriefResult; scenario: Scenario }) {
  const sourceTitle = (id: string) => scenario.sources.find((source) => source.id === id)?.title ?? id;
  return <article className="policy-brief" aria-labelledby="brief-heading">
    <p className="eyebrow">Final policy brief</p><h2 id="brief-heading">{brief.title}</h2>
    <section><h3>Problem</h3><p>{brief.problem_statement.text}</p><small>{brief.problem_statement.source_ids.map(sourceTitle).join(' · ')}</small></section>
    <section><h3>Evidence</h3><ul>{brief.evidence.map((item) => <li key={item.claim}>{item.claim} <small>{item.source_ids.join(', ')}</small></li>)}</ul></section>
    <section><h3>Recommended solution</h3><p>{brief.recommended_solution.summary}</p><dl className="rationale-list">{Object.entries(brief.recommended_solution.factor_rationale).map(([factor, text]) => <div key={factor}><dt>{factor.replaceAll('_', ' ')}</dt><dd>{text}</dd></div>)}</dl></section>
    <section><h3>Projected outcomes</h3><p>{brief.projected_outcomes.summary}</p><div className="outcome-compare"><p><strong>Year 1</strong>Income {brief.projected_outcomes.year_1.income.score}/100 · Sustainability {brief.projected_outcomes.year_1.sustainability.score}/100</p><p><strong>Year 5</strong>Income {brief.projected_outcomes.year_5.income.score}/100 · Sustainability {brief.projected_outcomes.year_5.sustainability.score}/100</p></div></section>
    <section><h3>Tradeoffs and risks</h3><ul>{brief.tradeoffs_and_risks.map((risk) => <li key={`${risk.category}-${risk.risk}`}><strong>{risk.category.replaceAll('_', ' ')}</strong>{risk.risk} <em>Response: {risk.mitigation}</em></li>)}</ul></section>
    <section><h3>Next steps</h3><ol>{brief.next_steps.map((step) => <li key={step.order}>{step.action}</li>)}</ol></section>
  </article>;
}
