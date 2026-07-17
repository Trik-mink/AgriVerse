import { Canvas } from '@react-three/fiber';

import type { Scenario } from '../types';
import { ImmersiveErrorBoundary } from './ImmersiveErrorBoundary';
import { STATIONS } from './world/stations';
import { ContinuousWorld } from './world/ContinuousWorld';
import './immersive.css';

function CanvasUnavailable({ onUseClassic }: { onUseClassic: () => void }) {
  return <div className="immersive-canvas-unavailable"><p>3D view is unavailable on this device.</p><button type="button" onClick={onUseClassic}>Open classic investigation</button></div>;
}

export function ImmersiveExperience({ scenario, onUseClassic }: { scenario: Scenario; onUseClassic: () => void }) {
  return (
    <ImmersiveErrorBoundary onCanvasFailure={onUseClassic}>
      <main className="immersive-shell">
        <Canvas className="immersive-canvas" camera={{ position: [27, 25, 35], fov: 45 }} dpr={[1, 1.5]} fallback={<CanvasUnavailable onUseClassic={onUseClassic} />} aria-hidden="true">
          <ContinuousWorld />
        </Canvas>
        <header className="immersive-header">
          <div><p className="eyebrow">Guided field journey</p><h1>{scenario.title}</h1></div>
          <button type="button" className="immersive-secondary" onClick={onUseClassic}>Use classic view</button>
        </header>
        <section className="immersive-station-directory" aria-labelledby="station-directory-heading">
          <p className="eyebrow">Journey map</p>
          <h2 id="station-directory-heading">Six stations are ready</h2>
          <ol>{STATIONS.map((station, index) => <li key={station.id}><span>{index + 1}</span><div><strong>{station.title}</strong><small>{station.subtitle}</small></div></li>)}</ol>
        </section>
        <aside className="immersive-placeholder" aria-live="polite">
          <p className="eyebrow">Foundation complete</p>
          <h2>Station activities remain in the classic investigation for this first slice.</h2>
          <p>The canvas stays mounted while the next presentation layers are added. Your existing work and all AI-powered activities are preserved in classic view.</p>
        </aside>
      </main>
    </ImmersiveErrorBoundary>
  );
}
