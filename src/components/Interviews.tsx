import { useMemo, useState } from 'react';

import type { Stakeholder } from '../types';

type InterviewRecord = { question: string; response: string };

type InterviewsProps = {
  stakeholders: Stakeholder[];
  interviews: Record<string, InterviewRecord>;
  busyStakeholderId?: string;
  onAsk: (stakeholderId: string, question: string) => void;
};

export function Interviews({ stakeholders, interviews, busyStakeholderId, onAsk }: InterviewsProps) {
  const [selectedId, setSelectedId] = useState(stakeholders[0]?.id ?? '');
  const [question, setQuestion] = useState('What would make this plan workable for your community?');
  const selected = useMemo(() => stakeholders.find((stakeholder) => stakeholder.id === selectedId), [selectedId, stakeholders]);

  if (!selected) return null;

  const record = interviews[selected.id];
  const isBusy = busyStakeholderId === selected.id;

  return (
    <section className="panel interview-panel" aria-labelledby="interview-heading">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Stakeholder room</p>
          <h2 id="interview-heading">Interview the people affected</h2>
        </div>
        <span className="counter">{Object.keys(interviews).length}/{stakeholders.length} interviewed</span>
      </div>
      <div className="stakeholder-tabs" role="tablist" aria-label="Stakeholders">
        {stakeholders.map((stakeholder) => (
          <button key={stakeholder.id} type="button" role="tab" aria-selected={stakeholder.id === selected.id} className={stakeholder.id === selected.id ? 'active' : ''} onClick={() => setSelectedId(stakeholder.id)}>
            <span>{stakeholder.name}</span><small>{stakeholder.role}</small>
          </button>
        ))}
      </div>
      <div className="interview-body">
        <p className="persona">{selected.persona}</p>
        {record ? <div className="dialogue"><p><strong>You asked</strong>{record.question}</p><p><strong>{selected.name}</strong>{record.response}</p></div> : <p className="empty-copy">Ask a focused question to uncover this stakeholder's concern.</p>}
        <label htmlFor="stakeholder-question">Your question</label>
        <textarea id="stakeholder-question" value={question} onChange={(event) => setQuestion(event.target.value)} maxLength={1400} rows={3} />
        <button type="button" className="command-button" disabled={isBusy || !question.trim()} onClick={() => onAsk(selected.id, question)}>{isBusy ? 'Listening...' : `Ask ${selected.name}`}</button>
      </div>
    </section>
  );
}
