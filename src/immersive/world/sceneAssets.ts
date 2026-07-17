import type { StationId } from './stations';

const SCENE_ROOT = '/assets/scenes/optimized';

const stationFiles: Record<StationId, string> = {
  field: 'paddy.webp',
  research: 'research-post.webp',
  office: 'district-office.webp',
  planning: 'planning-dock.webp',
  future: 'future-fields.webp',
  reflection: 'reflection-pavilion.webp',
};

export const heroSceneSrc = `${SCENE_ROOT}/hero.webp`;

export function stationSceneSrc(stationId: StationId) {
  return `${SCENE_ROOT}/${stationFiles[stationId]}`;
}
