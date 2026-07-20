using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client.Tests
{
    public sealed class EpisodePresentationControllerTests
    {
        [TearDown]
        public void RemovePersistentSession()
        {
            foreach (EpisodeSession session in
                     Object.FindObjectsByType<EpisodeSession>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(session.gameObject);
            }
            foreach (PlanSession session in
                     Object.FindObjectsByType<PlanSession>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(session.gameObject);
            }
            foreach (RuntimePanelManager manager in
                     Object.FindObjectsByType<RuntimePanelManager>(
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(manager.gameObject);
            }
            foreach (EvidenceNotebookSession session in
                     Object.FindObjectsByType<EvidenceNotebookSession>(
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(session.gameObject);
            }
            foreach (InterviewNotebookSession session in
                     Object.FindObjectsByType<InterviewNotebookSession>(
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void LandingCapturesLocalIdentityThenShowsMaiAndAnAccessibleGlossary()
        {
            GameObject root = new GameObject("EpisodePresentationTest");
            EpisodePresentationController controller =
                root.AddComponent<EpisodePresentationController>();
            controller.BuildForTesting(new ScenarioDto
            {
                id = "scenario-1",
                title = "Scenario title",
                location = new LocationDto { region = "Delta" }
            });

            Assert.That(controller.LandingVisible, Is.True);
            Assert.That(controller.SelectedFieldLocationIdForTesting, Is.Empty);
            Assert.That(controller.NameEntryVisibleForTesting, Is.False);
            Assert.That(controller.MissionStartVisibleForTesting, Is.False);
            GlobeLandingRenderer globe =
                root.GetComponentInChildren<GlobeLandingRenderer>(true);
            Assert.That(globe, Is.Not.Null);
            Assert.That(globe.HasDirectScreenCamera, Is.True);
            Assert.That(globe.UsesRectangularRenderTarget, Is.False);
            Assert.That(globe.PinCount, Is.EqualTo(5));
            Assert.That(
                root.GetComponentsInChildren<RawImage>(true),
                Is.Empty,
                "The orbital globe must not render inside a framed UI texture.");
            Assert.That(
                root.GetComponentsInChildren<Transform>(true),
                Has.None.Matches<Transform>(
                    item => item.name == "LandingCard"));
            Assert.That(
                root.GetComponentsInChildren<Button>(true),
                Has.None.Matches<Button>(button =>
                    button.name.StartsWith("Avatar_")));
            Assert.That(
                controller.SelectFieldLocationForTesting("scenario-1"),
                Is.True);
            Assert.That(controller.NameEntryVisibleForTesting, Is.True);
            Assert.That(controller.MissionStartVisibleForTesting, Is.True);
            Assert.That(controller.BeginMissionForTesting("Lan", "sun-amber"), Is.True);
            Assert.That(controller.LandingVisible, Is.False);
            Assert.That(controller.GuideVisible, Is.True);
            Assert.That(controller.GuideTextForTesting, Does.Contain("Lan? Good - you're here."));
            Assert.That(EpisodeSession.GetOrCreate().Progress.PlayerName, Is.EqualTo("Lan"));

            controller.ToggleGlossary();
            Assert.That(controller.GlossaryVisible, Is.True);
            Assert.That(controller.GlossaryTextForTesting, Does.Contain("Salinity:"));
            Assert.That(controller.GlossaryTextForTesting, Does.Contain("Source IDs"));
            controller.ToggleGlossary();
            Assert.That(controller.GlossaryVisible, Is.False);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void OrbitalLandingTemporarilyRevealsTheWorldCameraThenRestoresWakeFade()
        {
            GameObject fadeObject =
                new GameObject(
                    "WakeFade",
                    typeof(CanvasGroup));
            CanvasGroup wakeFade =
                fadeObject.GetComponent<CanvasGroup>();
            wakeFade.alpha = 1f;
            wakeFade.blocksRaycasts = true;
            GameObject root =
                new GameObject("EpisodePresentationTest");
            try
            {
                EpisodePresentationController controller =
                    root.AddComponent<
                        EpisodePresentationController>();
                controller.BuildForTesting(new ScenarioDto
                {
                    id = "scenario-1",
                    title = "Scenario title",
                    location = new LocationDto
                    {
                        country = "Vietnam",
                        region = "Delta"
                    }
                });

                Assert.That(wakeFade.alpha, Is.Zero);
                Assert.That(wakeFade.blocksRaycasts, Is.False);
                controller.SelectFieldLocationForTesting("scenario-1");
                Assert.That(
                    controller.BeginMissionForTesting(
                        "Lan",
                        string.Empty),
                    Is.True);
                Assert.That(wakeFade.alpha, Is.EqualTo(1f));
                Assert.That(wakeFade.blocksRaycasts, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(fadeObject);
            }
        }

        [TestCase(RuntimeActivityStage.Investigation)]
        [TestCase(RuntimeActivityStage.Interviews)]
        [TestCase(RuntimeActivityStage.Plan)]
        [TestCase(RuntimeActivityStage.Consequences)]
        [TestCase(RuntimeActivityStage.Feedback)]
        [TestCase(RuntimeActivityStage.Brief)]
        public void MaiGuideSuspendsWhileAnyActivityStageOwnsTheScreen(
            RuntimeActivityStage stage)
        {
            GameObject root =
                new GameObject("EpisodePresentationTest");
            EpisodePresentationController controller =
                root.AddComponent<EpisodePresentationController>();
            controller.BuildForTesting(new ScenarioDto
            {
                id = "scenario-1",
                title = "Scenario title",
                location = new LocationDto
                {
                    country = "Vietnam",
                    region = "Delta"
                },
                test_sites = new[]
                {
                    new TestSiteDto { id = "site-1" }
                },
                stakeholders = new[]
                {
                    new StakeholderDto { id = "person-1" }
                }
            });
            controller.SelectFieldLocationForTesting("scenario-1");
            Assert.That(
                controller.BeginMissionForTesting(
                    "Lan",
                    string.Empty),
                Is.True);
            Assert.That(controller.GuideVisible, Is.True);

            RuntimePanelManager manager =
                RuntimePanelManager.GetOrCreate();
            manager.Show(stage);
            controller.RefreshGuideVisibilityForTesting();
            Assert.That(
                controller.GuideVisible,
                Is.False,
                "No stage panel may stack with Mai guidance.");

            manager.Clear();
            controller.RefreshGuideVisibilityForTesting();
            Assert.That(
                controller.GuideVisible,
                Is.True,
                "The suspended guidance should resume in free exploration.");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void CompletedBriefOffersNamedCertificateAndBothRespectfulEndingChoices()
        {
            GameObject root = new GameObject("EpisodePresentationTest");
            EpisodePresentationController controller =
                root.AddComponent<EpisodePresentationController>();
            ScenarioDto scenario = new ScenarioDto
            {
                id = "scenario-1",
                title = "Scenario title",
                location = new LocationDto
                {
                    country = "Vietnam",
                    region = "Mekong Delta"
                },
                interventions = new[]
                {
                    new InterventionDto
                    {
                        id = "rice",
                        label = "Salt-tolerant rice"
                    }
                }
            };
            controller.BuildForTesting(scenario);
            Assert.That(
                controller.SelectFieldLocationForTesting(scenario.id),
                Is.True);
            Assert.That(
                controller.BeginMissionForTesting("Lan", "rice-green"),
                Is.True);
            PlanSession plan = PlanSession.GetOrCreate();
            plan.ConfigureScenario(scenario.id);
            plan.InterventionIds = new[] { "rice" };
            plan.StorePolicyBriefResult("{\"title\":\"Brief\"}");

            controller.RefreshForTesting();
            Assert.That(controller.CertificateAvailable, Is.True);
            controller.OpenCertificate();
            Assert.That(controller.CertificateVisible, Is.True);
            Transform certificateCard =
                root.transform.Find(
                    "EpisodePresentationView/" +
                    "EpisodePresentationCanvas/" +
                    "CertificateBlocker/CertificateCard");
            Assert.That(certificateCard, Is.Not.Null);
            Assert.That(
                certificateCard.GetComponent<AtlasSurfaceGraphic>()
                    ?.SurfaceKind,
                Is.EqualTo(AtlasSurfaceKind.FieldPaper));
            Assert.That(
                certificateCard.Find("ExpeditionSeal"),
                Is.Not.Null);
            Assert.That(
                certificateCard.Find("CertificateRoute"),
                Is.Not.Null);
            Assert.That(controller.CertificateTextForTesting, Does.Contain("Lan"));
            Assert.That(
                controller.CertificateTextForTesting,
                Does.Contain("Salt-tolerant rice"));
            Assert.That(
                controller.CertificateTextForTesting,
                Does.Contain("Mekong Delta, Vietnam"));

            controller.ChooseEndingForTesting(EpisodeEndingChoice.StayAnotherSeason);
            Assert.That(controller.CertificateVisible, Is.False);
            Assert.That(
                EpisodeSession.GetOrCreate().Progress.EndingChoice,
                Is.EqualTo(EpisodeEndingChoice.StayAnotherSeason));
            Object.DestroyImmediate(root);
        }

        [Test]
        public void ReturnToFieldNetworkClearsJourneySurfacesAndAllowsANewName()
        {
            GameObject root = new GameObject("EpisodePresentationTest");
            EpisodePresentationController controller =
                root.AddComponent<EpisodePresentationController>();
            ScenarioDto scenario = new ScenarioDto
            {
                id = "scenario-1",
                title = "Scenario title",
                location = new LocationDto
                {
                    country = "Vietnam",
                    region = "Mekong Delta"
                },
                test_sites = new[]
                {
                    new TestSiteDto
                    {
                        id = "site-1",
                        label = "Site one",
                        measurement_grounding =
                            new MeasurementGroundingDto
                            {
                                source_ids = new[] { "S1" }
                            }
                    }
                },
                stakeholders = new[]
                {
                    new StakeholderDto
                    {
                        id = "farmer",
                        name = "Farmer"
                    }
                }
            };
            controller.BuildForTesting(scenario);
            Assert.That(
                controller.SelectFieldLocationForTesting(scenario.id),
                Is.True);
            Assert.That(
                controller.BeginMissionForTesting("Lan", string.Empty),
                Is.True);

            EvidenceNotebookSession.GetOrCreate()
                .ConfigureScenario(scenario.id);
            EvidenceNotebookSession.GetOrCreate()
                .Notebook.Record(scenario.test_sites[0]);
            InterviewNotebookSession.GetOrCreate()
                .ConfigureScenario(scenario.id);
            InterviewNotebookSession.GetOrCreate()
                .Notebook.AddReply("farmer", "Recorded reply");
            PlanSession plan = PlanSession.GetOrCreate();
            plan.ConfigureScenario(scenario.id);
            plan.StorePolicyBriefResult("{\"title\":\"Brief\"}");
            controller.RefreshForTesting();
            controller.OpenCertificate();
            Assert.That(controller.CertificateVisible, Is.True);

            RuntimePanelManager manager =
                RuntimePanelManager.GetOrCreate();
            GameObject briefPanel = new GameObject("BriefPanel");
            manager.Register(RuntimeActivityStage.Brief, briefPanel);
            manager.Show(RuntimeActivityStage.Brief);

            controller.ReturnToFieldNetworkForTesting();

            Assert.That(controller.LandingVisible, Is.True);
            Assert.That(controller.CertificateVisible, Is.False);
            Assert.That(controller.GuideVisible, Is.False);
            Assert.That(controller.GlossaryVisible, Is.False);
            Assert.That(manager.ActivePanelCount, Is.Zero);
            Assert.That(
                EpisodeSession.GetOrCreate().Progress.HasIdentity,
                Is.False);
            Assert.That(
                EvidenceNotebookSession.GetOrCreate()
                    .Notebook.RecordedReadings,
                Is.Empty);
            Assert.That(
                InterviewNotebookSession.GetOrCreate()
                    .Notebook.Conversations,
                Is.Empty);
            Assert.That(
                PlanSession.GetOrCreate().PolicyBriefResultJson,
                Is.Empty);
            Assert.That(
                controller.SelectFieldLocationForTesting(scenario.id),
                Is.True);
            Assert.That(
                controller.BeginMissionForTesting("Minh", string.Empty),
                Is.True);
            Assert.That(
                EpisodeSession.GetOrCreate().Progress.PlayerName,
                Is.EqualTo("Minh"));

            Object.DestroyImmediate(briefPanel);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void LandingRejectsInvalidIdentityWithoutDismissing()
        {
            GameObject root = new GameObject("EpisodePresentationTest");
            EpisodePresentationController controller =
                root.AddComponent<EpisodePresentationController>();
            controller.BuildForTesting(new ScenarioDto
            {
                id = "scenario-1",
                title = "Scenario title",
                location = new LocationDto
                {
                    country = "Vietnam",
                    region = "Delta"
                }
            });

            Assert.That(
                controller.SelectFieldLocationForTesting("india"),
                Is.True);
            Assert.That(controller.IncomingLocationSelectedForTesting, Is.True);
            Assert.That(controller.NameEntryVisibleForTesting, Is.False);
            Assert.That(controller.MissionStartVisibleForTesting, Is.False);
            Assert.That(
                controller.BeginMissionForTesting("Lan", string.Empty),
                Is.False);

            Assert.That(
                controller.SelectFieldLocationForTesting("scenario-1"),
                Is.True);
            Assert.That(controller.BeginMissionForTesting(" ", "river-teal"), Is.False);
            Assert.That(controller.BeginMissionForTesting("Lan", string.Empty), Is.True);
            Assert.That(
                EpisodeSession.GetOrCreate().Progress.AvatarPresetId,
                Is.EqualTo(EpisodeProgress.FirstPersonObserverId));
            Assert.That(controller.LandingVisible, Is.False);

            Object.DestroyImmediate(root);
        }

        [Test]
        public void EscapeStyleClearReturnsFromSelectedLocationToGlobeExploration()
        {
            GameObject root = new GameObject("EpisodePresentationTest");
            EpisodePresentationController controller =
                root.AddComponent<EpisodePresentationController>();
            controller.BuildForTesting(new ScenarioDto
            {
                id = "scenario-1",
                title = "Scenario title",
                location = new LocationDto
                {
                    country = "Vietnam",
                    region = "Delta"
                }
            });

            Assert.That(
                controller.SelectFieldLocationForTesting("kenya"),
                Is.True);
            Assert.That(
                controller.SelectedFieldLocationIdForTesting,
                Is.EqualTo("kenya"));

            controller.ClearFieldLocationSelectionForTesting();

            Assert.That(controller.SelectedFieldLocationIdForTesting, Is.Empty);
            Assert.That(controller.IncomingLocationSelectedForTesting, Is.False);
            Assert.That(controller.NameEntryVisibleForTesting, Is.False);
            Assert.That(controller.MissionStartVisibleForTesting, Is.False);
            Object.DestroyImmediate(root);
        }
    }
}
