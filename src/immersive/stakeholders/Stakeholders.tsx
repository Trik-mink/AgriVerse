import type { Stakeholder } from '../../types';
import { Character } from '../avatars/Character';

type InterviewRecord = { question: string; response: string };

const stakeholderPositions: [number, number, number][] = [
  [-1.8, 0, -7.6],
  [10.2, 0, -4.7],
  [16.4, 0, 3.2],
];

const stakeholderColors = ['#c36d58', '#4d8a9b', '#9173a8'];

export function Stakeholders({ stakeholders, interviews }: { stakeholders: Stakeholder[]; interviews: Record<string, InterviewRecord> }) {
  return <>{stakeholders.slice(0, stakeholderPositions.length).map((stakeholder, index) => <Character key={stakeholder.id} position={stakeholderPositions[index]} color={stakeholderColors[index]} gesture={interviews[stakeholder.id] ? 'react' : 'idle'} />)}</>;
}
