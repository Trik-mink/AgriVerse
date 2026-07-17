import { Canvas } from '@react-three/fiber';

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

import type { GraderResult, PolicyBriefResult, Proposal, Scenario, SimulatorResult } from '../types';
import { prefersReducedMotion } from './capabilities';
import { ImmersiveErrorBoundary } from './ImmersiveErrorBoundary';
import { getNextStationId, JourneyDirector } from './JourneyDirector';
import { STATIONS, type StationId } from './world/stations';
import { StationActivity } from './ui/StationActivity';
import type { PlayerProfile } from './ui/player';
import { WakeUpDirector } from './WakeUpDirector';
import { DialogueWindow } from './ui/DialogueWindow';
import { ContinuousWorld } from './world/ContinuousWorld';
import { WebGLContextLossHandler } from './WebGLContextLossHandler';
import { InitialFrame } from './InitialFrame';
import './immersive.css';

type InterviewRecord = { question: string; response: string };

type ImmersiveExperienceProps = {
  scenario: Scenario;
  player: PlayerProfile;
  view: string;
  selectedSiteId?: string;
  testedSiteIds: string[];
  interviews: Record<string, InterviewRecord>;
  proposal: Proposal;
  simulation?: SimulatorResult;
  feedback?: GraderResult;
  brief?: PolicyBriefResult;
  busy?: string;
  canInterview: boolean;
  canOpenProposal: boolean;
  canRunSimulation: boolean;
  onUseClassic: () => void;
  onTest: (siteId: string) => void;
  onAsk: (stakeholderId: string, question: string) => void;
  onTargetSiteChange: (siteId: string) => void;
  onProposalChange: (proposal: Proposal) => void;
  onSimulate: () => void;
  onRequestFeedback: () => void;
  onRevise: () => void;
  onCreateBrief: () => void;
};

function stationForView(view: string): StationId | undefined {
  if (view === 'explore') return 'field';
  if (view === 'interviews') return 'research';
  if (view === 'proposal') return 'planning';
  if (view === 'consequences') return 'future';
  if (view === 'feedback' || view === 'brief') return 'reflection';
  return undefined;
}

export function ImmersiveExperience(props: ImmersiveExperienceProps) {
  const { scenario, player, onUseClassic, view } = props;
  const [stationId, setStationId] = useState<StationId>('field');
  const [isTravelling, setIsTravelling] = useState(false);
  const reducedMotion = useMemo(prefersReducedMotion, []);
  const [isWaking, setIsWaking] = useState(!reducedMotion);
  const [contextLost, setContextLost] = useState(false);
  const stationHeading = useRef<HTMLHeadingElement>(null);
  const currentStation = STATIONS.find((station) => station.id === stationId)!;
  const nextStation = STATIONS.find((station) => station.id === getNextStationId(stationId))!;
  const announceArrival = useCallback(() => stationHeading.current?.focus(), []);

  useEffect(() => {
    const destination = stationForView(view);
    if (destination) setStationId(destination);
  }, [view]);

  return (
    <ImmersiveErrorBoundary contextLost={contextLost} onCanvasFailure={onUseClassic}>
      <main className={`immersive-shell ${isTravelling ? 'is-travelling' : ''} ${isWaking ? 'is-waking' : ''}`}>
        <Canvas className="immersive-canvas" frameloop="always" camera={{ position: STATIONS[0].camera, fov: 45 }} dpr={[1, 1]} gl={{ antialias: false, alpha: false, powerPreference: 'low-power', preserveDrawingBuffer: false, stencil: false }} aria-hidden="true">
          <ContinuousWorld stationId={stationId}>
            <InitialFrame />
            <JourneyDirector stationId={stationId} reducedMotion={reducedMotion} onTravelChange={setIsTravelling} onArrival={announceArrival} />
            <WakeUpDirector reducedMotion={reducedMotion} onComplete={() => setIsWaking(false)} />
            <WebGLContextLossHandler onContextLost={() => setContextLost(true)} />
          </ContinuousWorld>
        </Canvas>
        <header className="immersive-header">
          <div><p className="eyebrow">Guided field journey</p><h1>{scenario.title}</h1><p className="immersive-player-name">{player.displayName} · {player.presetId}</p></div>
        </header>
        <section className="immersive-station-directory" aria-labelledby="station-directory-heading">
          <p className="eyebrow">Journey map</p>
          <h2 id="station-directory-heading">Six stations are ready</h2>
          <ol>{STATIONS.map((station, index) => <li key={station.id} aria-current={station.id === stationId ? 'step' : undefined} className={station.id === stationId ? 'current' : ''}><button type="button" disabled={isWaking} onClick={() => setStationId(station.id)}><span>{index + 1}</span><span><strong>{station.title}</strong><small>{station.subtitle}</small></span></button></li>)}</ol>
        </section>
        <aside className="immersive-activity-panel" aria-live="polite">
          <p className="eyebrow">{isTravelling ? 'Moving along the channel' : currentStation.subtitle}</p>
          <h2 ref={stationHeading} tabIndex={-1}>{currentStation.title}</h2>
          <StationActivity {...props} stationId={stationId} />
          <button type="button" className="immersive-primary immersive-continue" disabled={isTravelling || isWaking} onClick={() => setStationId(nextStation.id)}>{isTravelling ? 'Travelling...' : `Continue to ${nextStation.title}`}</button>
        </aside>
        <DialogueWindow stationId={stationId} stakeholders={scenario.stakeholders} interviews={props.interviews} />
        {isWaking ? <div className="immersive-wake-overlay" aria-hidden="true" /> : null}
      </main>
    </ImmersiveErrorBoundary>
  );
}
