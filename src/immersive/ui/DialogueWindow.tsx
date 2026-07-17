import { useEffect, useMemo, useRef } from 'react';

import type { Stakeholder } from '../../types';
import { GUIDE_CUES } from '../content/journey-copy';
import type { StationId } from '../world/stations';

type InterviewRecord = { question: string; response: string };

type DialogueWindowProps = {
  stationId: StationId;
  stakeholders: Stakeholder[];
  interviews: Record<string, InterviewRecord>;
};

export function DialogueWindow({ stationId, stakeholders, interviews }: DialogueWindowProps) {
  const audio = useRef<HTMLAudioElement | undefined>(undefined);
  const latest = useMemo(() => Object.entries(interviews).at(-1), [interviews]);
  const stakeholder = latest ? stakeholders.find((candidate) => candidate.id === latest[0]) : undefined;
  const record = latest?.[1];
  const messageKey = record ? `${latest[0]}:${record.response}` : `guide:${stationId}`;

  useEffect(() => {
    if (!record) return;
    audio.current ??= new Audio('/assets/audio/dialogue-pop.ogg');
    audio.current.currentTime = 0;
    audio.current.volume = 0.32;
    void audio.current.play().catch(() => undefined);
  }, [messageKey, record]);

  if (!record || !stakeholder) {
    return <aside className="immersive-dialogue-window" aria-live="polite"><p className="eyebrow">Field guide</p><p key={messageKey} className="immersive-dialogue-message">{GUIDE_CUES[stationId]}</p></aside>;
  }

  return <aside className="immersive-dialogue-window" aria-live="polite">
    <p className="eyebrow">{stakeholder.name}</p>
    <small>{stakeholder.role}</small>
    <p className="immersive-dialogue-question">You: {record.question}</p>
    <p key={messageKey} className="immersive-dialogue-message">{record.response}</p>
  </aside>;
}
