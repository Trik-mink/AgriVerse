import type { Intervention, Proposal, Scenario } from '../types';

type ProposalBuilderProps = {
  scenario: Scenario;
  proposal: Proposal;
  targetSiteId?: string;
  onTargetSiteChange: (siteId: string) => void;
  onProposalChange: (proposal: Proposal) => void;
  onSimulate: () => void;
  canSimulate: boolean;
  isSimulating: boolean;
};

function toggle(value: string, values: string[]) {
  return values.includes(value) ? values.filter((entry) => entry !== value) : [...values, value];
}

export function ProposalBuilder({ scenario, proposal, targetSiteId, onTargetSiteChange, onProposalChange, onSimulate, canSimulate, isSimulating }: ProposalBuilderProps) {
  const updateIntervention = (intervention: Intervention) => onProposalChange({ ...proposal, intervention_ids: toggle(intervention.id, proposal.intervention_ids) });

  return (
    <section className="panel proposal-panel" aria-labelledby="proposal-heading">
      <div className="section-heading"><div><p className="eyebrow">Design desk</p><h2 id="proposal-heading">Propose an intervention</h2></div></div>
      <div className="form-grid">
        <label>Target plot
          <select value={targetSiteId ?? ''} onChange={(event) => onTargetSiteChange(event.target.value)}>
            <option value="" disabled>Select a tested plot</option>
            {scenario.test_sites.map((site) => <option key={site.id} value={site.id}>{site.label}</option>)}
          </select>
        </label>
        <p className="context-line">Farmer capital profile: <strong>{scenario.farmer_capital}</strong></p>
      </div>
      <fieldset><legend>Intervention</legend><div className="intervention-list">
        {scenario.interventions.map((intervention) => (
          <label key={intervention.id} className="intervention-option">
            <input type="checkbox" checked={proposal.intervention_ids.includes(intervention.id)} onChange={() => updateIntervention(intervention)} />
            <span><strong>{intervention.label}</strong><small>{intervention.description}</small><em>Cost: {intervention.cost} · Income: {intervention.income} · Sustainability: {intervention.sustainability}</em></span>
          </label>
        ))}
      </div></fieldset>
      <fieldset><legend>Support measures</legend><div className="check-row">
        {scenario.support_measure_options.map((measure) => <label key={measure.id}><input type="checkbox" checked={proposal.support_measures.includes(measure.id)} onChange={() => onProposalChange({ ...proposal, support_measures: toggle(measure.id, proposal.support_measures) })} />{measure.description}</label>)}
      </div></fieldset>
      <label htmlFor="proposal-rationale">Evidence-based rationale
        <textarea id="proposal-rationale" rows={5} value={proposal.rationale} maxLength={4000} onChange={(event) => onProposalChange({ ...proposal, rationale: event.target.value })} placeholder="Explain the salinity, season, freshwater, and capital fit of your plan." />
      </label>
      <button type="button" className="command-button" disabled={!canSimulate || isSimulating} onClick={onSimulate}>{isSimulating ? 'Running model...' : 'Simulate five years'}</button>
    </section>
  );
}
