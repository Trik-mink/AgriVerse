import { STATIONS, type StationId } from './world/stations';

const DEFAULT_TRANSITION_DURATION_MS = 820;

export function getStationIndex(stationId: StationId) {
  return STATIONS.findIndex((station) => station.id === stationId);
}

export function getNextStationId(stationId: StationId) {
  return STATIONS[(getStationIndex(stationId) + 1) % STATIONS.length].id;
}

export function transitionDurationMs(reducedMotion: boolean) {
  return reducedMotion ? 0 : DEFAULT_TRANSITION_DURATION_MS;
}
