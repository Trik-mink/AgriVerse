export type StationId = 'field' | 'research' | 'office' | 'planning' | 'future' | 'reflection';

export type StationAnchor = {
  id: StationId;
  title: string;
  subtitle: string;
  position: [number, number, number];
};

export const STATIONS: readonly StationAnchor[] = [
  { id: 'field', title: 'Field station', subtitle: 'Water investigation', position: [0, 0, -8] },
  { id: 'research', title: 'Research post', subtitle: 'Evidence exchange', position: [11, 0, -5] },
  { id: 'office', title: 'District office', subtitle: 'Community decision', position: [17, 0, 3] },
  { id: 'planning', title: 'Planning dock', subtitle: 'Intervention design', position: [10, 0, 12] },
  { id: 'future', title: 'Future fields', subtitle: 'Five-year projection', position: [0, 0, 16] },
  { id: 'reflection', title: 'Reflection pavilion', subtitle: 'Review and brief', position: [-10, 0, 8] },
];
