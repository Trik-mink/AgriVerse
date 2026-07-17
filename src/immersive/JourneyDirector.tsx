import { useEffect, useRef } from 'react';
import { useFrame, useThree } from '@react-three/fiber';
import { CatmullRomCurve3, Vector3 } from 'three';

import { STATIONS, type StationId } from './world/stations';

const DEFAULT_TRAVEL_DURATION_MS = 2600;

export function getStationIndex(stationId: StationId) {
  return STATIONS.findIndex((station) => station.id === stationId);
}

export function getNextStationId(stationId: StationId): StationId {
  return STATIONS[(getStationIndex(stationId) + 1) % STATIONS.length].id;
}

export function travelDurationMs(reducedMotion: boolean) {
  return reducedMotion ? 0 : DEFAULT_TRAVEL_DURATION_MS;
}

type JourneyDirectorProps = {
  stationId: StationId;
  reducedMotion: boolean;
  onTravelChange: (isTravelling: boolean) => void;
  onArrival: () => void;
};

type ActiveRail = { startedAt: number; durationSeconds: number; curve: CatmullRomCurve3; lookFrom: Vector3; lookTo: Vector3 };

export function JourneyDirector({ stationId, reducedMotion, onTravelChange, onArrival }: JourneyDirectorProps) {
  const { camera, clock } = useThree();
  const rail = useRef<ActiveRail | undefined>(undefined);
  const didMount = useRef(false);

  useEffect(() => {
    const station = STATIONS[getStationIndex(stationId)];
    const destination = new Vector3(...station.camera);
    const lookTarget = new Vector3(...station.lookAt);

    if (!didMount.current || reducedMotion) {
      camera.position.copy(destination);
      camera.lookAt(lookTarget);
      didMount.current = true;
      rail.current = undefined;
      onTravelChange(false);
      onArrival();
      return;
    }

    const start = camera.position.clone();
    const midpoint = start.clone().lerp(destination, 0.5).add(new Vector3(0, 2.2, 0));
    rail.current = {
      startedAt: clock.getElapsedTime(),
      durationSeconds: travelDurationMs(false) / 1000,
      curve: new CatmullRomCurve3([start, midpoint, destination]),
      lookFrom: camera.position.clone().add(camera.getWorldDirection(new Vector3())),
      lookTo: lookTarget,
    };
    onTravelChange(true);
  }, [camera, clock, onArrival, onTravelChange, reducedMotion, stationId]);

  useFrame(({ clock: frameClock }) => {
    const activeRail = rail.current;
    if (!activeRail) return;

    const rawProgress = Math.min(1, (frameClock.getElapsedTime() - activeRail.startedAt) / activeRail.durationSeconds);
    const easedProgress = rawProgress * rawProgress * (3 - 2 * rawProgress);
    camera.position.copy(activeRail.curve.getPoint(easedProgress));
    camera.lookAt(activeRail.lookFrom.clone().lerp(activeRail.lookTo, easedProgress));

    if (rawProgress === 1) {
      rail.current = undefined;
      onTravelChange(false);
      onArrival();
    }
  });

  return null;
}
