export type StationId = 'field' | 'research' | 'office' | 'planning' | 'future' | 'reflection';

export type StationAnchor = {
  id: StationId;
  title: string;
  subtitle: string;
  position: [number, number, number];
  camera: [number, number, number];
  lookAt: [number, number, number];
};

export const STATIONS: readonly StationAnchor[] = [
  { id: 'field', title: 'Field station', subtitle: 'Water investigation', position: [0, 0, -8], camera: [0, 5.5, 1], lookAt: [0, 0.8, -8] },
  { id: 'research', title: 'Research post', subtitle: 'Evidence exchange', position: [11, 0, -5], camera: [5.5, 5.2, -0.5], lookAt: [11, 0.8, -5] },
  { id: 'office', title: 'District office', subtitle: 'Community decision', position: [17, 0, 3], camera: [11.5, 5.4, 2], lookAt: [17, 0.8, 3] },
  { id: 'planning', title: 'Planning dock', subtitle: 'Intervention design', position: [10, 0, 12], camera: [8.5, 5.3, 6], lookAt: [10, 0.8, 12] },
  { id: 'future', title: 'Future fields', subtitle: 'Five-year projection', position: [0, 0, 16], camera: [2.8, 5.5, 10], lookAt: [0, 0.8, 16] },
  { id: 'reflection', title: 'Reflection pavilion', subtitle: 'Review and brief', position: [-10, 0, 8], camera: [-5.8, 5.4, 5], lookAt: [-10, 0.8, 8] },
];
