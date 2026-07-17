import { useEffect, useMemo, useState } from 'react';

import { api } from './api/client';
import { Consequences } from './components/Consequences';
import { FeedbackPanel } from './components/FeedbackPanel';
import { Interviews } from './components/Interviews';
import { MekongScene } from './components/MekongScene';
import { PolicyBrief } from './components/PolicyBrief';
import { ProposalBuilder } from './components/ProposalBuilder';
import { WaterTesting } from './components/WaterTesting';
import { canOpenProposal, canRunSimulation } from './flow/progress';
import { supportsWebGL, type PresentationMode } from './immersive/capabilities';
import { ImmersiveExperience } from './immersive/ImmersiveExperience';
import { ImmersiveLanding } from './immersive/ui/ImmersiveLanding';
import type { GraderResult, PolicyBriefResult, Proposal, Scenario, SimulatorResult } from './types';

type View = 'explore' | 'interviews' | 'proposal' | 'consequences' | 'feedback' | 'brief';
type InterviewRecord = { question: string; response: string };

const emptyProposal: Proposal = { intervention_ids: [], parameters: {}, support_measures: [], rationale: '' };

export function App() {
  const [scenario, setScenario] = useState<Scenario>();
  const [error, setError] = useState<string>();
  const [view, setView] = useState<View>('explore');
  const [selectedSiteId, setSelectedSiteId] = useState<string>();
  const [testedSiteIds, setTestedSiteIds] = useState<string[]>([]);
  const [interviews, setInterviews] = useState<Record<string, InterviewRecord>>({});
  const [proposal, setProposal] = useState<Proposal>(emptyProposal);
  const [simulation, setSimulation] = useState<SimulatorResult>();
  const [feedback, setFeedback] = useState<GraderResult>();
  const [brief, setBrief] = useState<PolicyBriefResult>();
  const [busy, setBusy] = useState<string>();
  const [presentationMode, setPresentationMode] = useState<PresentationMode>('landing');

  useEffect(() => { api.getScenario().then(setScenario).catch((requestError: Error) => setError(requestError.message)); }, []);

  const stakeholderIds = scenario?.stakeholders.map((stakeholder) => stakeholder.id) ?? [];
  const siteIds = scenario?.test_sites.map((site) => site.id) ?? [];
  const repliedStakeholderIds = Object.keys(interviews);
  const proposalUnlocked = canOpenProposal(repliedStakeholderIds, stakeholderIds);
  const simulationUnlocked = canRunSimulation({ testedSiteIds, siteIds, repliedStakeholderIds, stakeholderIds, interventionIds: proposal.intervention_ids, rationale: proposal.rationale });
  const currentSiteId = selectedSiteId ?? testedSiteIds.at(-1);
  const currentSite = scenario?.test_sites.find((site) => site.id === currentSiteId);
  const isBusy = (name: string) => busy === name;

  const navigation = useMemo(() => [
    ['explore', '1. Test water', true],
    ['interviews', '2. Interview', testedSiteIds.length === siteIds.length && siteIds.length > 0],
    ['proposal', '3. Propose', proposalUnlocked],
    ['consequences', '4. Consequences', Boolean(simulation)],
    ['feedback', '5. Feedback', Boolean(feedback)],
    ['brief', '6. Policy brief', Boolean(brief)],
  ] as const, [brief, feedback, proposalUnlocked, simulation, siteIds.length, testedSiteIds.length]);

  if (error) return <main className="app-shell"><p className="eyebrow">Connection problem</p><h1>AgriVerse</h1><p className="status-copy">{error}</p></main>;
  if (!scenario) return <main className="app-shell"><p className="eyebrow">Environmental science simulation</p><h1>AgriVerse</h1><p className="status-copy">Loading field investigation...</p></main>;

  const startImmersive = () => setPresentationMode(supportsWebGL() ? 'immersive' : 'classic');
  const useClassic = () => setPresentationMode('classic');
  const selectAndTest = (siteId: string) => { setSelectedSiteId(siteId); setTestedSiteIds((tested) => tested.includes(siteId) ? tested : [...tested, siteId]); };
  const ask = async (stakeholderId: string, question: string) => {
    setBusy(`stakeholder:${stakeholderId}`); setError(undefined);
    try {
      const existing = interviews[stakeholderId];
      const conversation = existing ? [{ role: 'student' as const, text: existing.question }, { role: 'stakeholder' as const, text: existing.response }] : [];
      const result = await api.askStakeholder(stakeholderId, { message: question, conversation });
      setInterviews((records) => ({ ...records, [stakeholderId]: { question, response: result.message } }));
    } catch (requestError) { setError((requestError as Error).message); } finally { setBusy(undefined); }
  };
  const runSimulation = async () => {
    if (!currentSiteId) return;
    setBusy('simulation'); setError(undefined);
    try { const result = await api.simulate({ target_site_id: currentSiteId, proposal }); setSimulation(result); setFeedback(undefined); setBrief(undefined); setView('consequences'); }
    catch (requestError) { setError((requestError as Error).message); } finally { setBusy(undefined); }
  };
  const requestFeedback = async () => {
    if (!currentSiteId || !simulation) return;
    setBusy('feedback'); setError(undefined);
    try { setFeedback(await api.grade({ target_site_id: currentSiteId, proposal, simulation })); setView('feedback'); }
    catch (requestError) { setError((requestError as Error).message); } finally { setBusy(undefined); }
  };
  const createBrief = async () => {
    if (!currentSiteId || !simulation) return;
    setBusy('brief'); setError(undefined);
    try { const stakeholder_concerns = Object.entries(interviews).map(([stakeholder_id, record]) => ({ stakeholder_id, concern: record.response })); setBrief(await api.createBrief({ target_site_id: currentSiteId, proposal, simulation, stakeholder_concerns })); setView('brief'); }
    catch (requestError) { setError((requestError as Error).message); } finally { setBusy(undefined); }
  };

  if (presentationMode === 'landing') {
    return <ImmersiveLanding scenario={scenario} onStartImmersive={startImmersive} onUseClassic={useClassic} />;
  }

  if (presentationMode === 'immersive') {
    return <ImmersiveExperience scenario={scenario} view={view} selectedSiteId={selectedSiteId} testedSiteIds={testedSiteIds} interviews={interviews} proposal={proposal} simulation={simulation} feedback={feedback} brief={brief} busy={busy} canOpenProposal={proposalUnlocked} canRunSimulation={simulationUnlocked} onUseClassic={useClassic} onTest={selectAndTest} onAsk={ask} onTargetSiteChange={setSelectedSiteId} onProposalChange={setProposal} onSimulate={runSimulation} onRequestFeedback={requestFeedback} onRevise={() => setView('proposal')} onCreateBrief={createBrief} />;
  }

  return <main className="product-shell">
    <header className="topbar"><div><p className="eyebrow">Environmental science simulation</p><h1>AgriVerse</h1></div><div className="crisis-badge"><span>{scenario.location.region}</span><strong>{scenario.crisis.key_metric.label}</strong></div></header>
    <nav className="flow-nav" aria-label="Investigation progress">{navigation.map(([id, label, unlocked]) => <button key={id} type="button" disabled={!unlocked} className={view === id ? 'active' : ''} onClick={() => setView(id)}>{label}</button>)}</nav>
    {error ? <p className="request-error" role="alert">{error}</p> : null}
    {view === 'explore' ? <section className="field-layout"><MekongScene sites={scenario.test_sites} selectedSiteId={selectedSiteId} onSelectSite={selectAndTest} /><WaterTesting sites={scenario.test_sites} unit={scenario.crisis.key_metric.unit} selectedSiteId={selectedSiteId} testedSiteIds={testedSiteIds} onTest={selectAndTest} /></section> : null}
    {view === 'interviews' ? <Interviews stakeholders={scenario.stakeholders} interviews={interviews} busyStakeholderId={busy?.replace('stakeholder:', '')} onAsk={ask} /> : null}
    {view === 'proposal' ? <ProposalBuilder scenario={scenario} proposal={proposal} targetSiteId={currentSiteId} onTargetSiteChange={setSelectedSiteId} onProposalChange={setProposal} onSimulate={runSimulation} canSimulate={simulationUnlocked} isSimulating={isBusy('simulation')} /> : null}
    {view === 'consequences' && simulation ? <Consequences simulation={simulation} onRequestFeedback={requestFeedback} isRequestingFeedback={isBusy('feedback')} /> : null}
    {view === 'feedback' && feedback ? <FeedbackPanel feedback={feedback} onRevise={() => setView('proposal')} onCreateBrief={createBrief} isCreatingBrief={isBusy('brief')} /> : null}
    {view === 'brief' && brief ? <PolicyBrief brief={brief} scenario={scenario} /> : null}
    <footer><span>Scenario: {scenario.id}</span>{currentSite ? <span>Current focus: {currentSite.label}</span> : null}</footer>
  </main>;
}
