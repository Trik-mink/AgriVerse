import type { Proposal, Scenario } from '../../types';
import type { PlayerProfile } from './player';

export function CertificateFinale({ scenario, proposal, player }: { scenario: Scenario; proposal: Proposal; player: PlayerProfile }) {
  const solution = scenario.interventions.filter((intervention) => proposal.intervention_ids.includes(intervention.id)).map((intervention) => intervention.label).join(', ');

  return <section className="immersive-certificate" aria-labelledby="certificate-heading">
    <p className="eyebrow">Field certificate</p>
    <h3 id="certificate-heading">Investigation complete</h3>
    <p>This recognizes {player.displayName}'s evidence-based policy submission for {scenario.title}.</p>
    <p className="immersive-certificate-solution">{solution || 'Evidence-based intervention'}</p>
  </section>;
}
