import { Character } from '../avatars/Character';
import type { PlayerPresetId } from '../ui/player';
import type { StationId } from '../world/stations';

const guidePositions: Record<StationId, [number, number, number]> = {
  field: [0.35, 0, -7.2],
  research: [9.8, 0, -5.2],
  office: [16.1, 0, 4.1],
  planning: [8.8, 0, 11.2],
  future: [1.2, 0, 14.8],
  reflection: [-8.7, 0, 7.2],
};

const guideColors: Record<PlayerPresetId, string> = {
  channel: '#5ba99e',
  clay: '#d28a68',
  sun: '#d4ad4d',
  leaf: '#7d9f61',
};

export function Guide({ stationId, presetId }: { stationId: StationId; presetId: PlayerPresetId }) {
  return <Character position={guidePositions[stationId]} color={guideColors[presetId]} gesture={stationId === 'field' ? 'lead' : 'idle'} />;
}
