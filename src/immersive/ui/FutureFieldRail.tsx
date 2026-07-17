import { useState } from 'react';

import type { SimulatorResult } from '../../types';

export function FutureFieldRail({ simulation }: { simulation: SimulatorResult }) {
  const [activeIndex, setActiveIndex] = useState(0);
  const year = simulation.years[activeIndex];

  return <section className="immersive-future-rail" aria-labelledby="future-rail-heading">
    <p className="eyebrow">Future field rail</p>
    <h3 id="future-rail-heading">Five years in view</h3>
    <div className="immersive-year-track" role="tablist" aria-label="Projected year">
      {simulation.years.map((item, index) => <button key={item.year} type="button" role="tab" aria-selected={index === activeIndex} onClick={() => setActiveIndex(index)}><span>Year</span><strong>{item.year}</strong></button>)}
    </div>
    <div className="immersive-year-focus" aria-live="polite">
      <p>{year.narrative}</p>
      <dl>
        <div><dt>Salinity</dt><dd>{year.outcomes.salinity.value} {year.outcomes.salinity.unit}</dd></div>
        <div><dt>Income</dt><dd>{year.outcomes.income.score}/100</dd></div>
        <div><dt>Sustainability</dt><dd>{year.outcomes.sustainability.score}/100</dd></div>
      </dl>
    </div>
  </section>;
}
