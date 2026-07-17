import { Canvas } from '@react-three/fiber';

import { useCallback, useMemo, useRef, useState } from 'react';

import type { Scenario } from '../types';
import { prefersReducedMotion } from './capabilities';
import { ImmersiveErrorBoundary } from './ImmersiveErrorBoundary';
import { getNextStationId, JourneyDirector } from './JourneyDirector';
import { STATIONS, type StationId } from './world/stations';
import { ContinuousWorld } from './world/ContinuousWorld';
import './immersive.css';

function CanvasUnavailable({ onUseClassic }: { onUseClassic: () => void }) {
  return <div className="immersive-canvas-unavailable"><p>3D view is unavailable on this device.</p><button type="button" onClick={onUseClassic}>Open classic investigation</button></div>;
}

export function ImmersiveExperience({ scenario, onUseClassic }: { scenario: Scenario; onUseClassic: () => void }) {
  const [stationId, setStationId] = useState<StationId>('field');
  const [isTravelling, setIsTravelling] = useState(false);
  const reducedMotion = useMemo(prefersReducedMotion, []);
  const stationHeading = useRef<HTMLHeadingElement>(null);
  const currentStation = STATIONS.find((station) => station.id === stationId)!;
  const nextStation = STATIONS.find((station) => station.id === getNextStationId(stationId))!;
  const announceArrival = useCallback(() => stationHeading.current?.focus(), []);

  return (
    <ImmersiveErrorBoundary onCanvasFailure={onUseClassic}>
      <main className={`immersive-shell ${isTravelling ? 'is-travelling' : ''}`}>
        <Canvas className="immersive-canvas" camera={{ position: STATIONS[0].camera, fov: 45 }} dpr={[1, 1.5]} fallback={<CanvasUnavailable onUseClassic={onUseClassic} />} aria-hidden="true">
          <ContinuousWorld>
            <JourneyDirector stationId={stationId} reducedMotion={reducedMotion} onTravelChange={setIsTravelling} onArrival={announceArrival} />
          </ContinuousWorld>
        </Canvas>
        <header className="immersive-header">
          <div><p className="eyebrow">Guided field journey</p><h1>{scenario.title}</h1></div>
          <button type="button" className="immersive-secondary" onClick={onUseClassic}>Use classic view</button>
        </header>
        <section className="immersive-station-directory" aria-labelledby="station-directory-heading">
          <p className="eyebrow">Journey map</p>
          <h2 id="station-directory-heading">Six stations are ready</h2>
          <ol>{STATIONS.map((station, index) => <li key={station.id} aria-current={station.id === stationId ? 'step' : undefined} className={station.id === stationId ? 'current' : ''}><span>{index + 1}</span><div><strong>{station.title}</strong><small>{station.subtitle}</small></div></li>)}</ol>
        </section>
        <aside className="immersive-placeholder" aria-live="polite">
          <p className="eyebrow">{isTravelling ? 'Moving along the channel' : currentStation.subtitle}</p>
          <h2 ref={stationHeading} tabIndex={-1}>{currentStation.title}</h2>
          <p>Station activities remain in the classic investigation for this slice. Your existing work and all AI-powered activities are preserved in classic view.</p>
          <button type="button" className="immersive-primary" disabled={isTravelling} onClick={() => setStationId(nextStation.id)}>{isTravelling ? 'Travelling...' : `Continue to ${nextStation.title}`}</button>
        </aside>
      </main>
    </ImmersiveErrorBoundary>
  );
}
