import type { SimulatorResult } from '../types';

export function Consequences({ simulation, onRequestFeedback, isRequestingFeedback }: { simulation: SimulatorResult; onRequestFeedback: () => void; isRequestingFeedback: boolean }) {
  return <section className="panel consequences-panel" aria-labelledby="consequences-heading">
    <div className="section-heading"><div><p className="eyebrow">Five-year model</p><h2 id="consequences-heading">Watch the consequences</h2></div></div>
    <p className="headline">{simulation.headline}</p>
    <div className="fit-row">{Object.entries(simulation.fit_assessment).map(([factor, status]) => <span key={factor} className={`fit-chip ${status}`}>{factor.replaceAll('_', ' ')}: {status}</span>)}</div>
    <div className="timeline">{simulation.years.map((year) => <article key={year.year} className="year-entry"><p className="year-label">Year {year.year}</p><dl><div><dt>Salinity</dt><dd>{year.outcomes.salinity.value} {year.outcomes.salinity.unit}</dd></div><div><dt>Income index</dt><dd>{year.outcomes.income.score}/100</dd></div><div><dt>Sustainability</dt><dd>{year.outcomes.sustainability.score}/100</dd></div><div><dt>Cost</dt><dd>{year.cost_level}</dd></div></dl><p>{year.narrative}</p><small>Projection logic: {year.evidence_source_ids.join(', ')}</small></article>)}</div>
    <div className="tradeoff-strip">{simulation.tradeoffs.map((tradeoff) => <p key={`${tradeoff.category}-${tradeoff.summary}`}><strong>{tradeoff.category.replaceAll('_', ' ')}</strong>{tradeoff.summary}</p>)}</div>
    <button type="button" className="command-button" disabled={isRequestingFeedback} onClick={onRequestFeedback}>{isRequestingFeedback ? 'Reviewing...' : 'Get grounded feedback'}</button>
  </section>;
}
