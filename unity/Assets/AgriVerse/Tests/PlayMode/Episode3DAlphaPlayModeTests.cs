using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AgriVerse.Client.Tests
{
    public sealed class Episode3DAlphaPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator ResetSessions()
        {
            DestroyAll<EvidenceNotebookSession>();
            DestroyAll<InterviewNotebookSession>();
            DestroyAll<EpisodeSession>();
            DestroyAll<PlanSession>();
            DestroyAll<RuntimePanelManager>();
            yield return null;
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator LiveAllConfiguredSitesReachTheNotebookAndUnlockInterviews()
        {
            var hotspotObjects = new GameObject[3];
            var hotspots = new WaterSampleHotspot[3];
            string[] siteIds = { "upstream", "mid", "coastal" };
            for (int index = 0; index < hotspots.Length; index++)
            {
                hotspotObjects[index] = new GameObject(
                    "AlphaTestHotspot_" + siteIds[index],
                    typeof(SphereCollider),
                    typeof(WaterSampleHotspot));
                hotspots[index] =
                    hotspotObjects[index].GetComponent<WaterSampleHotspot>();
            }

            GameObject root = new GameObject("AlphaTest");
            InvestigationController investigation =
                root.AddComponent<InvestigationController>();
            investigation.ConfigureEndpointsForTesting(
                "http://localhost:8787",
                "http://localhost:8787");
            investigation.ConfigurePresentation(
                createUi: false,
                createMarkers: false);
            Episode3DAlphaController alpha =
                root.AddComponent<Episode3DAlphaController>();
            alpha.Configure(
                investigation,
                null,
                hotspots,
                siteIds,
                null,
                null,
                null,
                Vector3.zero,
                0f,
                null,
                null,
                null);

            for (int interval = 0;
                 interval < 160 &&
                 alpha.State == Episode3DAlphaState.Loading;
                 interval++)
            {
                yield return new WaitForSecondsRealtime(.05f);
            }

            Assert.That(alpha.State, Is.EqualTo(Episode3DAlphaState.Intro));
            Assert.That(investigation.LoadState, Is.EqualTo(
                InvestigationLoadState.Ready));
            Assert.That(investigation.MarkerCount, Is.EqualTo(0));
            Assert.That(alpha.ConfiguredSiteCount, Is.EqualTo(3));

            alpha.AdvanceIntro();
            alpha.AdvanceIntro();
            Assert.That(alpha.State, Is.EqualTo(
                Episode3DAlphaState.Exploring));
            for (int siteIndex = 0;
                 siteIndex < siteIds.Length;
                 siteIndex++)
            {
                Assert.That(
                    alpha.SelectConfiguredSiteForTesting(
                        siteIds[siteIndex]),
                    Is.True);
                Assert.That(alpha.BeginSiteInteraction(), Is.True);
                Assert.That(alpha.State, Is.EqualTo(
                    Episode3DAlphaState.Predicting));
                Assert.That(alpha.ChoosePrediction(0), Is.True);
                Assert.That(alpha.State, Is.EqualTo(
                    Episode3DAlphaState.ReadyToCollect));
                Assert.That(alpha.BeginCollection(), Is.True);

                for (int interval = 0;
                     interval < 160 &&
                     alpha.State == Episode3DAlphaState.Sampling;
                     interval++)
                {
                    yield return new WaitForSecondsRealtime(.05f);
                }

                Assert.That(alpha.State, Is.EqualTo(
                    Episode3DAlphaState.Reading));
                Assert.That(
                    alpha.ReadingTextForTesting,
                    Does.Contain(alpha.ActiveSite.label));
                Assert.That(
                    alpha.ReadingTextForTesting,
                    Does.Contain("SOURCE IDs"));
                alpha.ContinueAfterReadingForTesting();
            }

            Assert.That(alpha.State, Is.EqualTo(
                Episode3DAlphaState.Complete));
            Assert.That(alpha.SampleRecorded, Is.True);
            Assert.That(investigation.RecordedReadingCount, Is.EqualTo(3));
            Assert.That(investigation.InterviewsUnlocked, Is.True);
            alpha.ToggleNotebook();
            Assert.That(alpha.NotebookOpenForTesting, Is.True);
            Assert.That(alpha.ReadingPanelVisibleForTesting, Is.False);
            Assert.That(alpha.DialogueVisibleForTesting, Is.False);
            Assert.That(
                alpha.ObjectiveTextForTesting,
                Does.Contain("Press N to close"));

            Object.Destroy(root);
            foreach (GameObject hotspotObject in hotspotObjects)
            {
                Object.Destroy(hotspotObject);
            }
            DestroyAll<EvidenceNotebookSession>();
            DestroyAll<EpisodeSession>();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RevisedFutureWalkCanCompareBothAuthoritativePlans()
        {
            PlanSession session = PlanSession.GetOrCreate();
            session.ConfigureScenario("scenario-1");
            session.StoreSimulatorResult(
                FutureJson("Original future", "1.2500"),
                new SimulatorResultSummaryDto());
            session.StoreSimulatorResult(
                FutureJson("Revised future", "0.7500"),
                new SimulatorResultSummaryDto());

            GameObject fieldObject =
                new GameObject("FutureField");
            InstancedVegetationField field =
                fieldObject.AddComponent<InstancedVegetationField>();
            GameObject root = new GameObject("FutureWalkTest");
            ConsequencesController consequences =
                root.AddComponent<ConsequencesController>();
            Episode3DFutureWalkController future =
                root.AddComponent<Episode3DFutureWalkController>();
            future.Configure(new[] { field });

            for (int frame = 0;
                 frame < 120 &&
                 !consequences.ConsequencesVisible;
                 frame++)
            {
                yield return null;
            }
            yield return null;

            Assert.That(future.ShowingRevised, Is.True);
            Assert.That(
                future.DisplayedText,
                Does.Contain("REVISED FUTURE · YEAR 1"));
            Assert.That(future.DisplayedText, Does.Contain("0.7500 g/L"));
            future.ShowOriginal();
            Assert.That(
                future.DisplayedText,
                Does.Contain("ORIGINAL FUTURE · YEAR 1"));
            Assert.That(future.DisplayedText, Does.Contain("1.2500 g/L"));
            consequences.NextYear();
            yield return null;
            Assert.That(
                future.DisplayedText,
                Does.Contain("ORIGINAL FUTURE · YEAR 2"));
            Assert.That(field.PresentationDensity, Is.InRange(.1f, 1f));

            Object.Destroy(root);
            Object.Destroy(fieldObject);
            DestroyAll<PlanSession>();
            DestroyAll<RuntimePanelManager>();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CinematicInterviewCanReturnToThe3DFieldBetweenStakeholders()
        {
            GameObject root =
                new GameObject("FieldInterviewTest");
            InterviewController interviews =
                root.AddComponent<InterviewController>();
            interviews.ConfigureEndpointsForTesting(
                "http://localhost:8787",
                "http://localhost:8787");
            interviews.ConfigurePresentation(
                createMarkers: false,
                activateAutomatically: false);

            for (int interval = 0;
                 interval < 160 &&
                 interviews.LoadState !=
                    InvestigationLoadState.Ready;
                 interval++)
            {
                yield return new WaitForSecondsRealtime(.05f);
            }
            Assert.That(
                interviews.LoadState,
                Is.EqualTo(InvestigationLoadState.Ready));
            EvidenceNotebookSession evidence =
                EvidenceNotebookSession.GetOrCreate();
            evidence.ConfigureScenario(interviews.Scenario.id);
            foreach (TestSiteDto site in
                     interviews.Scenario.test_sites)
            {
                Assert.That(
                    evidence.Notebook.Record(site),
                    Is.True);
            }

            Assert.That(interviews.BeginInterviews(), Is.True);
            interviews.SelectStakeholder(
                interviews.Scenario.stakeholders[0].id);
            yield return null;
            RuntimePanelManager manager =
                RuntimePanelManager.GetOrCreate();
            Assert.That(
                manager.ActiveStage,
                Is.EqualTo(RuntimeActivityStage.Interviews));

            interviews.ReturnToField();
            Assert.That(manager.ActiveStage, Is.Null);
            Assert.That(manager.ActivePanelCount, Is.EqualTo(0));
            Assert.That(
                interviews.SelectedStakeholderId,
                Is.EqualTo(
                    interviews.Scenario.stakeholders[0].id));

            Object.Destroy(root);
            DestroyAll<EvidenceNotebookSession>();
            DestroyAll<InterviewNotebookSession>();
            DestroyAll<RuntimePanelManager>();
            yield return null;
        }

        private static string FutureJson(
            string headline,
            string salinity)
        {
            var years = new StringBuilder();
            for (int year = 1; year <= 5; year++)
            {
                if (year > 1) years.Append(',');
                years.Append("{\"year\":")
                    .Append(year)
                    .Append(",\"outcomes\":{\"salinity\":{\"value\":")
                    .Append(salinity)
                    .Append(",\"unit\":\"g/L\"},\"yield\":{\"items\":[")
                    .Append("{\"commodity_id\":\"rice\",\"value\":5.5,\"unit\":\"t/ha\"}]},")
                    .Append("\"income\":{\"score\":61},\"sustainability\":{\"score\":72}},")
                    .Append("\"cost_level\":\"medium\",\"narrative\":\"Year ")
                    .Append(year)
                    .Append(" narrative\",\"evidence_source_ids\":[\"S1\"]}");
            }
            return "{\"headline\":\"" + headline +
                   "\",\"fit_assessment\":{\"salinity\":\"fit\"," +
                   "\"seasonality\":\"fit\",\"freshwater\":\"fit\"," +
                   "\"farmer_capital\":\"fit\",\"overall\":\"fit\"}," +
                   "\"years\":[" + years + "],\"tradeoffs\":[]}";
        }

        private static void DestroyAll<T>()
            where T : Component
        {
            foreach (T item in Object.FindObjectsByType<T>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(item.gameObject);
            }
        }
    }
}
