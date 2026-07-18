using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AgriVerse.Client.Tests
{
    public sealed class InvestigationLiveScenarioTests
    {
        [UnitySetUp]
        public IEnumerator ResetPersistentSessions()
        {
            foreach (EvidenceNotebookSession session in Object.FindObjectsByType<EvidenceNotebookSession>(FindObjectsSortMode.None)) Object.Destroy(session.gameObject);
            foreach (InterviewNotebookSession session in Object.FindObjectsByType<InterviewNotebookSession>(FindObjectsSortMode.None)) Object.Destroy(session.gameObject);
            foreach (PlanSession session in Object.FindObjectsByType<PlanSession>(FindObjectsSortMode.None)) Object.Destroy(session.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LiveScenarioCreatesEveryMarkerAndUnlocksInterviewsAfterAllSamples()
        {
            var root = new GameObject("InvestigationLiveScenarioTest");
            InvestigationController controller = root.AddComponent<InvestigationController>();
            controller.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");

            yield return WaitForScenario(controller);

            Assert.That(controller.LoadState, Is.EqualTo(InvestigationLoadState.Ready));
            Assert.That(controller.Scenario, Is.Not.Null);
            Assert.That(controller.MarkerCount, Is.EqualTo(controller.Scenario.test_sites.Length));
            Assert.That(controller.InterviewsUnlocked, Is.False);

            foreach (TestSiteDto site in controller.Scenario.test_sites)
            {
                controller.SelectSite(site.id);
                Assert.That(controller.CollectSelectedSample(), Is.True);
            }

            Assert.That(controller.RecordedReadingCount, Is.EqualTo(controller.Scenario.test_sites.Length));
            Assert.That(controller.InterviewsUnlocked, Is.True);

            Object.Destroy(root);
            EvidenceNotebookSession session = Object.FindFirstObjectByType<EvidenceNotebookSession>();
            if (session != null)
            {
                Object.Destroy(session.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator LiveStakeholdersReplyAndUnlockPlanningAfterOneQuestionEach()
        {
            var root = new GameObject("InterviewLiveScenarioTest");
            InterviewController controller = root.AddComponent<InterviewController>();
            controller.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");

            yield return WaitForScenario(controller);

            Assert.That(controller.LoadState, Is.EqualTo(InvestigationLoadState.Ready));
            Assert.That(controller.MarkerCount, Is.EqualTo(controller.Scenario.stakeholders.Length));

            foreach (StakeholderDto stakeholder in controller.Scenario.stakeholders)
            {
                controller.AskForTesting(stakeholder.id, "What is the most important condition for a workable response?");
                yield return WaitForReply(controller);
                Assert.That(controller.IsBusy, Is.False);
                IReadOnlyList<ConversationTurnDto> conversation =
                    Object.FindFirstObjectByType<InterviewNotebookSession>().Notebook.ConversationFor(stakeholder.id);
                Assert.That(conversation, Has.Count.EqualTo(2));
                Assert.That(conversation[1].role, Is.EqualTo("stakeholder"));
                Assert.That(conversation[1].text, Is.Not.Empty);
                Debug.Log($"Live stakeholder reply ({stakeholder.id}): {conversation[1].text}");
            }

            Assert.That(controller.PlanUnlocked, Is.True);
            Object.Destroy(root);
            InterviewNotebookSession session = Object.FindFirstObjectByType<InterviewNotebookSession>();
            if (session != null) Object.Destroy(session.gameObject);
        }

        [UnityTest]
        public IEnumerator InvestigationCubesRemainClickableBeforeInterviewsAppearThenTheFullLoopUnlocksPlanning()
        {
            var investigationRoot = new GameObject("CombinedInvestigationTest");
            var interviewRoot = new GameObject("CombinedInterviewTest");
            InvestigationController investigation = investigationRoot.AddComponent<InvestigationController>();
            InterviewController interviews = interviewRoot.AddComponent<InterviewController>();
            investigation.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");
            interviews.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");

            yield return WaitForScenario(investigation);
            yield return WaitForScenario(interviews);
            Assert.That(interviews.InterviewsVisible, Is.False);

            foreach (TestSiteDto site in investigation.Scenario.test_sites)
            {
                investigation.SelectSite(site.id);
                Assert.That(investigation.CollectSelectedSample(), Is.True);
            }

            yield return null;
            Assert.That(interviews.InterviewsVisible, Is.True);

            foreach (StakeholderDto stakeholder in interviews.Scenario.stakeholders)
            {
                interviews.AskForTesting(stakeholder.id, "What condition must a workable response meet?");
                yield return WaitForReply(interviews);
            }

            Assert.That(interviews.PlanUnlocked, Is.True);
            Object.Destroy(investigationRoot);
            Object.Destroy(interviewRoot);
            EvidenceNotebookSession evidence = Object.FindFirstObjectByType<EvidenceNotebookSession>();
            if (evidence != null) Object.Destroy(evidence.gameObject);
            InterviewNotebookSession interviewSession = Object.FindFirstObjectByType<InterviewNotebookSession>();
            if (interviewSession != null) Object.Destroy(interviewSession.gameObject);
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator FullLoopReachesTheSimulationRoundTripAfterThePlanGate()
        {
            var investigationRoot = new GameObject("PlanLoopInvestigation");
            var interviewRoot = new GameObject("PlanLoopInterviews");
            var planRoot = new GameObject("PlanLoopPlan");
            var consequencesRoot = new GameObject("PlanLoopConsequences");
            var feedbackRoot = new GameObject("PlanLoopFeedback");
            var briefRoot = new GameObject("PlanLoopBrief");
            InvestigationController investigation = investigationRoot.AddComponent<InvestigationController>();
            InterviewController interviews = interviewRoot.AddComponent<InterviewController>();
            PlanController plan = planRoot.AddComponent<PlanController>();
            ConsequencesController consequences = consequencesRoot.AddComponent<ConsequencesController>();
            FeedbackController feedback = feedbackRoot.AddComponent<FeedbackController>();
            PolicyBriefController brief = briefRoot.AddComponent<PolicyBriefController>();
            investigation.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");
            interviews.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");
            plan.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");
            feedback.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");
            brief.ConfigureEndpointsForTesting("http://localhost:8787", "http://localhost:8787");
            yield return WaitForScenario(investigation); yield return WaitForScenario(interviews); yield return WaitForPlan(plan);
            foreach (TestSiteDto site in investigation.Scenario.test_sites) { investigation.SelectSite(site.id); Assert.That(investigation.CollectSelectedSample(), Is.True); }
            yield return null;
            foreach (StakeholderDto stakeholder in interviews.Scenario.stakeholders) { interviews.AskForTesting(stakeholder.id, "What condition must a workable response meet?"); yield return WaitForReply(interviews); }
            yield return null;
            Assert.That(plan.PlanVisible, Is.True);
            plan.ConfigureForTesting(plan.Session == null ? investigation.Scenario.test_sites[0].id : investigation.Scenario.test_sites[0].id, investigation.Scenario.interventions[0].id, "The selected intervention should fit the recorded site conditions and household capital.");
            plan.SubmitPlan();
            yield return WaitForSimulation(plan);
            Assert.That(plan.IsBusy, Is.False);
            Assert.That(plan.Session.SimulatorResult, Is.Not.Null);
            Assert.That(plan.Session.SimulatorResult.fit_assessment.overall, Is.Not.Empty);
            yield return WaitForConsequences(consequences);
            Assert.That(consequences.ConsequencesVisible, Is.True);
            Assert.That(consequences.DisplayedContentForTesting, Does.Contain("Year 1"));
            Assert.That(consequences.DisplayedContentForTesting, Does.Contain("Evidence source IDs:"));
            consequences.NextYear();
            Assert.That(consequences.CurrentYearIndex, Is.EqualTo(1));
            Assert.That(consequences.DisplayedContentForTesting, Does.Contain("Year 2"));
            consequences.UnlockFeedback();
            yield return WaitForFeedback(feedback);
            Assert.That(feedback.FeedbackVisible, Is.True);
            Assert.That(plan.Session.FeedbackResultJson, Is.Not.Empty);
            Assert.That(feedback.DisplayedContentForTesting, Does.Contain("Rubric results"));
            string originalTarget = plan.Session.TargetSiteId;
            string originalIntervention = plan.Session.InterventionIds[0];
            feedback.RevisePlan();
            yield return null;
            Assert.That(plan.PlanVisible, Is.True);
            Assert.That(plan.Session.TargetSiteId, Is.EqualTo(originalTarget));
            Assert.That(plan.Session.InterventionIds[0], Is.EqualTo(originalIntervention));
            plan.ConfigureForTesting(originalTarget, originalIntervention, "Revised rationale addresses the interview evidence and all four decision factors.");
            plan.SubmitPlan();
            yield return WaitForSimulation(plan);
            yield return WaitForConsequences(consequences);
            Assert.That(plan.Session.FeedbackResultJson, Is.Empty);
            consequences.UnlockFeedback();
            yield return WaitForFeedback(feedback);
            Assert.That(plan.Session.FeedbackResultJson, Is.Not.Empty);
            feedback.GenerateBrief();
            yield return WaitForBrief(brief);
            Assert.That(brief.BriefVisible, Is.True);
            Assert.That(plan.Session.PolicyBriefResultJson, Is.Not.Empty);
            Assert.That(brief.DisplayedContentForTesting, Does.Contain("Fit assessment"));
            Assert.That(brief.DisplayedContentForTesting, Does.Contain("Investigation complete"));
            Object.Destroy(investigationRoot); Object.Destroy(interviewRoot); Object.Destroy(planRoot); Object.Destroy(consequencesRoot); Object.Destroy(feedbackRoot); Object.Destroy(briefRoot);
        }

        [UnityTest]
        public IEnumerator ConsequencesPreserveCanonicalNumericTokensAndNullYields()
        {
            PlanSession session = PlanSession.GetOrCreate();
            session.StoreSimulatorResult(CanonicalSimulatorJson(), new SimulatorResultSummaryDto());
            var root = new GameObject("CanonicalConsequencesTest");
            ConsequencesController consequences = root.AddComponent<ConsequencesController>();

            yield return WaitForConsequences(consequences);

            Assert.That(consequences.ConsequencesVisible, Is.True);
            Assert.That(consequences.DisplayedContentForTesting, Does.Contain("Salinity: 1.2300 dS/m"));
            Assert.That(consequences.DisplayedContentForTesting, Does.Contain("rice: null t/ha"));
            Object.Destroy(root);
            Object.Destroy(session.gameObject);
        }

        private static IEnumerator WaitForScenario(InvestigationController controller)
        {
            const int maximumFrames = 600;
            for (int frame = 0; frame < maximumFrames &&
                                (controller.LoadState == InvestigationLoadState.NotStarted ||
                                 controller.LoadState == InvestigationLoadState.Loading); frame++)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitForScenario(InterviewController controller)
        {
            const int maximumFrames = 900;
            for (int frame = 0; frame < maximumFrames &&
                                (controller.LoadState == InvestigationLoadState.NotStarted ||
                                 controller.LoadState == InvestigationLoadState.Loading); frame++)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitForReply(InterviewController controller)
        {
            const int maximumIntervals = 600;
            for (int interval = 0; interval < maximumIntervals && controller.IsBusy; interval++)
            {
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        private static IEnumerator WaitForPlan(PlanController controller)
        {
            for (int interval = 0; interval < 200 && controller.LoadState != InvestigationLoadState.Ready; interval++) yield return null;
        }

        private static IEnumerator WaitForSimulation(PlanController controller)
        {
            for (int interval = 0; interval < 900 && controller.IsBusy; interval++) yield return new WaitForSecondsRealtime(0.1f);
        }

        private static IEnumerator WaitForConsequences(ConsequencesController controller)
        {
            for (int interval = 0; interval < 200 && !controller.ConsequencesVisible; interval++) yield return null;
        }

        private static IEnumerator WaitForFeedback(FeedbackController controller)
        {
            for (int interval = 0; interval < 1200 && (controller.IsBusy || string.IsNullOrWhiteSpace(PlanSession.GetOrCreate().FeedbackResultJson)); interval++) yield return new WaitForSecondsRealtime(0.1f);
        }

        private static IEnumerator WaitForBrief(PolicyBriefController controller)
        {
            for (int interval = 0; interval < 1200 && (controller.IsBusy || string.IsNullOrWhiteSpace(PlanSession.GetOrCreate().PolicyBriefResultJson)); interval++) yield return new WaitForSecondsRealtime(0.1f);
        }

        private static string CanonicalSimulatorJson()
        {
            var years = new StringBuilder();
            for (int year = 1; year <= 5; year++)
            {
                if (year > 1) years.Append(',');
                years.Append("{\"year\":").Append(year)
                    .Append(",\"outcomes\":{\"salinity\":{\"value\":1.2300,\"unit\":\"dS/m\"},\"yield\":{\"items\":[{\"commodity_id\":\"rice\",\"value\":null,\"unit\":\"t/ha\"}]},\"income\":{\"score\":57},\"sustainability\":{\"score\":63}},\"cost_level\":\"low\",\"narrative\":\"Narrative\",\"evidence_source_ids\":[\"S1\"]}");
            }
            return "{\"headline\":\"Headline\",\"fit_assessment\":{\"salinity\":\"fit\",\"seasonality\":\"fit\",\"freshwater\":\"fit\",\"farmer_capital\":\"fit\",\"overall\":\"fit\"},\"years\":[" + years + "],\"tradeoffs\":[{\"category\":\"cost\",\"summary\":\"Tradeoff\"}]}";
        }
    }
}
