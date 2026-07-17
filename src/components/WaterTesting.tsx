import type { TestSite } from '../types';

type WaterTestingProps = {
  sites: TestSite[];
  unit: string;
  selectedSiteId?: string;
  testedSiteIds: string[];
  onTest: (siteId: string) => void;
};

export function WaterTesting({ sites, unit, selectedSiteId, testedSiteIds, onTest }: WaterTestingProps) {
  const selectedSite = sites.find((site) => site.id === selectedSiteId);

  return (
    <section className="panel explorer-panel" aria-labelledby="water-testing-heading">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Field kit</p>
          <h2 id="water-testing-heading">Test the water</h2>
        </div>
        <span className="counter">{testedSiteIds.length}/{sites.length} tested</span>
      </div>
      <div className="site-list">
        {sites.map((site) => {
          const tested = testedSiteIds.includes(site.id);
          return (
            <button key={site.id} type="button" className={`site-row ${selectedSiteId === site.id ? 'selected' : ''}`} onClick={() => onTest(site.id)}>
              <span className="site-row-title">{site.label}</span>
              <span className="site-reading">{site.salinity_gL} {unit}</span>
              <span className="site-state">{tested ? 'Recorded' : 'Test'}</span>
            </button>
          );
        })}
      </div>
      {selectedSite ? (
        <div className="reading-detail" aria-live="polite">
          <p className="metric-value">{selectedSite.salinity_gL} <small>{unit}</small></p>
          <dl>
            <div><dt>Sample season</dt><dd>{selectedSite.season}</dd></div>
            <div><dt>Salt pattern</dt><dd>{selectedSite.seasonal_pattern.replaceAll('_', ' ')}</dd></div>
            <div><dt>Freshwater</dt><dd>{selectedSite.freshwater_access}</dd></div>
          </dl>
          <p>{selectedSite.note}</p>
          <p className="source-note">Field reading grounded in {selectedSite.measurement_grounding.source_ids.join(', ')}.</p>
        </div>
      ) : <p className="empty-copy">Select a marker or a site to record its reading.</p>}
    </section>
  );
}
