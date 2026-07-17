import type { Scenario } from '../../types';

export function ImmersiveLanding({ scenario, onStartImmersive, onUseClassic }: { scenario: Scenario; onStartImmersive: () => void; onUseClassic: () => void }) {
  return (
    <main className="immersive-landing">
      <div className="immersive-landing-art" aria-hidden="true"><span /><span /><span /></div>
      <section className="immersive-landing-copy" aria-labelledby="immersive-title">
        <p className="eyebrow">Field investigation</p>
        <h1 id="immersive-title">{scenario.title}</h1>
        <p>Trace one guided journey from field observation to an evidence-based policy brief.</p>
        <div className="immersive-landing-actions">
          <button type="button" className="immersive-primary" onClick={onStartImmersive}>Enter the field journey</button>
          <button type="button" className="immersive-secondary" onClick={onUseClassic}>Use classic view</button>
        </div>
      </section>
    </main>
  );
}
