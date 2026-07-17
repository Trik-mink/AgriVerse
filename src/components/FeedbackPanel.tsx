import type { GraderResult } from '../types';

export function FeedbackPanel({ feedback, onRevise, onCreateBrief, isCreatingBrief }: { feedback: GraderResult; onRevise: () => void; onCreateBrief: () => void; isCreatingBrief: boolean }) {
  return <section className="panel feedback-panel" aria-labelledby="feedback-heading">
    <div className="section-heading"><div><p className="eyebrow">Evidence review</p><h2 id="feedback-heading">Revise your plan</h2></div></div>
    <div className="insight"><strong>Key insight</strong><p>{feedback.key_insight.text}</p></div>
    <div className="rubric-list">{feedback.rubric_results.map((item) => <article key={item.rubric_id}><span className={`rating ${item.rating}`}>{item.rating.replaceAll('_', ' ')}</span><div><strong>{item.rubric_id.replaceAll('_', ' ')}</strong><p>{item.feedback}</p><small>{[...item.evidence.source_ids, ...item.evidence.simulation_years.map((year) => `model year ${year}`)].join(' · ')}</small></div></article>)}</div>
    <p className="revision-question">{feedback.revision_prompt}</p><p className="encouragement">{feedback.encouragement}</p>
    <div className="button-row"><button type="button" className="command-button secondary" onClick={onRevise}>Revise and resimulate</button><button type="button" className="command-button" disabled={isCreatingBrief} onClick={onCreateBrief}>{isCreatingBrief ? 'Writing brief...' : 'Generate policy brief'}</button></div>
  </section>;
}
