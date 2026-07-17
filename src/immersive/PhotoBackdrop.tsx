import { useCallback, useEffect, useRef, useState } from 'react';

import { stationSceneSrc } from './world/sceneAssets';
import { STATIONS, type StationId } from './world/stations';

type SceneLayer = { id: StationId; src?: string; title: string };

type PhotoBackdropProps = {
  stationId: StationId;
  reducedMotion: boolean;
  onFirstFramesReady: () => void;
  onTransitionComplete: () => void;
};

function layerFor(stationId: StationId): SceneLayer {
  const station = STATIONS.find((candidate) => candidate.id === stationId)!;
  return { id: stationId, src: stationSceneSrc(stationId), title: station.title };
}

function SceneLayerView({ layer, visible, onTransitionEnd }: { layer: SceneLayer; visible: boolean; onTransitionEnd?: () => void }) {
  const className = `immersive-photo-layer ${visible ? 'is-visible' : ''}`;

  if (!layer.src) {
    return <div className={`${className} is-fallback`} onTransitionEnd={onTransitionEnd}><span>{layer.title}</span></div>;
  }

  return <div className={className} onTransitionEnd={(event) => { if (event.target === event.currentTarget && event.propertyName === 'opacity') onTransitionEnd?.(); }}><img src={layer.src} alt="" /></div>;
}

export function PhotoBackdrop({ stationId, reducedMotion, onFirstFramesReady, onTransitionComplete }: PhotoBackdropProps) {
  const [active, setActive] = useState<SceneLayer | undefined>();
  const [outgoing, setOutgoing] = useState<SceneLayer | undefined>();
  const [pending, setPending] = useState<SceneLayer | undefined>(() => layerFor(stationId));
  const [activeVisible, setActiveVisible] = useState(false);
  const requestedStationId = useRef(stationId);
  const didRevealInitialScene = useRef(false);
  const transitionInProgress = useRef(false);
  const frameIds = useRef<number[]>([]);

  requestedStationId.current = stationId;

  useEffect(() => {
    if (active?.id === stationId || pending?.id === stationId) return;
    setPending(layerFor(stationId));
  }, [active?.id, pending?.id, stationId]);

  useEffect(() => () => frameIds.current.forEach(cancelAnimationFrame), []);

  const finishTransition = useCallback(() => {
    setOutgoing(undefined);
    if (!transitionInProgress.current) return;
    transitionInProgress.current = false;
    onTransitionComplete();
  }, [onTransitionComplete]);

  const activate = useCallback((next: SceneLayer) => {
    if (next.id !== requestedStationId.current) return;

    const isInitialScene = !didRevealInitialScene.current;
    didRevealInitialScene.current = true;
    transitionInProgress.current = Boolean(active);
    setPending(undefined);
    setOutgoing(active);
    setActive(next);
    setActiveVisible(false);

    const revealFrame = requestAnimationFrame(() => {
      setActiveVisible(true);
      if (reducedMotion) finishTransition();

      if (isInitialScene) {
        const firstRenderedFrame = requestAnimationFrame(() => {
          const secondRenderedFrame = requestAnimationFrame(onFirstFramesReady);
          frameIds.current.push(secondRenderedFrame);
        });
        frameIds.current.push(firstRenderedFrame);
      }
    });
    frameIds.current.push(revealFrame);
  }, [active, finishTransition, onFirstFramesReady, reducedMotion]);

  useEffect(() => {
    if (!pending?.src) return;

    let cancelled = false;
    const preloader = new Image();
    const settle = (scene: SceneLayer) => {
      if (!cancelled) activate(scene);
    };
    const handleLoad = () => {
      void preloader.decode().catch(() => undefined).finally(() => settle(pending));
    };

    preloader.onload = handleLoad;
    preloader.onerror = () => settle({ ...pending, src: undefined });
    preloader.src = pending.src;
    if (preloader.complete) {
      if (preloader.naturalWidth > 0) handleLoad();
      else preloader.onerror(new Event('error'));
    }

    return () => { cancelled = true; };
  }, [activate, pending]);

  return (
    <div className="immersive-photo-backdrop" aria-hidden="true">
      <div className="immersive-photo-base" />
      {outgoing ? <SceneLayerView key={`outgoing-${outgoing.id}`} layer={outgoing} visible /> : null}
      {active ? <SceneLayerView key={`active-${active.id}`} layer={active} visible={activeVisible} onTransitionEnd={finishTransition} /> : null}
      <div className="immersive-photo-grade" />
    </div>
  );
}
