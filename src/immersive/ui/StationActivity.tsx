import { Consequences } from '../../components/Consequences';
import { FeedbackPanel } from '../../components/FeedbackPanel';
import { Interviews } from '../../components/Interviews';
import { PolicyBrief } from '../../components/PolicyBrief';
import { ProposalBuilder } from '../../components/ProposalBuilder';
import { WaterTesting } from '../../components/WaterTesting';
import type { GraderResult, PolicyBriefResult, Proposal, Scenario, SimulatorResult } from '../../types';
import type { StationId } from '../world/stations';

type InterviewRecord = { question: string; response: string };

type StationActivityProps = {
  stationId: StationId;
  scenario: Scenario;
  selectedSiteId?: string;
  testedSiteIds: string[];
  interviews: Record<string, InterviewRecord>;
  proposal: Proposal;
  simulation?: SimulatorResult;
  feedback?: GraderResult;
  brief?: PolicyBriefResult;
  busy?: string;
  canOpenProposal: boolean;
  canRunSimulation: boolean;
  onTest: (siteId: string) => void;
  onAsk: (stakeholderId: string, question: string) => void;
  onTargetSiteChange: (siteId: string) => void;
  onProposalChange: (proposal: Proposal) => void;
  onSimulate: () => void;
  onRequestFeedback: () => void;
  onRevise: () => void;
  onCreateBrief: () => void;
};

function LockedActivity({ children }: { children: string }) {
  return <section className="immersive-locked-activity"><p className="eyebrow">Station not ready</p><p>{children}</p></section>;
}

export function StationActivity(props: StationActivityProps) {
  const { stationId, scenario, selectedSiteId, testedSiteIds, interviews, proposal, simulation, feedback, brief, busy } = props;

  if (stationId === 'field') {
    return <WaterTesting sites={scenario.test_sites} unit={scenario.crisis.key_metric.unit} selectedSiteId={selectedSiteId} testedSiteIds={testedSiteIds} onTest={props.onTest} />;
  }

  if (stationId === 'research' || stationId === 'office') {
    if (!props.canOpenProposal) return <LockedActivity>Record every water sample before the stakeholder conversations open.</LockedActivity>;
    return <Interviews stakeholders={scenario.stakeholders} interviews={interviews} busyStakeholderId={busy?.replace('stakeholder:', '')} onAsk={props.onAsk} />;
  }

  if (stationId === 'planning') {
    if (!props.canOpenProposal) return <LockedActivity>Complete the field evidence and stakeholder conversations before designing an intervention.</LockedActivity>;
    return <ProposalBuilder scenario={scenario} proposal={proposal} targetSiteId={selectedSiteId} onTargetSiteChange={props.onTargetSiteChange} onProposalChange={props.onProposalChange} onSimulate={props.onSimulate} canSimulate={props.canRunSimulation} isSimulating={busy === 'simulation'} />;
  }

  if (stationId === 'future') {
    if (!simulation) return <LockedActivity>Run a five-year model at the planning dock to reveal this field.</LockedActivity>;
    return <Consequences simulation={simulation} onRequestFeedback={props.onRequestFeedback} isRequestingFeedback={busy === 'feedback'} />;
  }

  if (brief) return <PolicyBrief brief={brief} scenario={scenario} />;
  if (feedback) return <FeedbackPanel feedback={feedback} onRevise={props.onRevise} onCreateBrief={props.onCreateBrief} isCreatingBrief={busy === 'brief'} />;
  if (simulation) return <LockedActivity>Request grounded feedback from the five-year field before entering reflection.</LockedActivity>;
  return <LockedActivity>Complete the investigation, proposal, and five-year model before reflecting on the policy brief.</LockedActivity>;
}
