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
            Assert.That(
                root.GetComponentsInChildren<Button>(true),
                Has.None.Matches<Button>(button =>
                    button.name.StartsWith("Avatar_")));
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
        public void LandingRejectsInvalidIdentityWithoutDismissing()
        {
            GameObject root = new GameObject("EpisodePresentationTest");
            EpisodePresentationController controller =
                root.AddComponent<EpisodePresentationController>();
            controller.BuildForTesting(new ScenarioDto
            {
                id = "scenario-1",
                title = "Scenario title"
            });

            Assert.That(controller.BeginMissionForTesting(" ", "river-teal"), Is.False);
            Assert.That(controller.BeginMissionForTesting("Lan", string.Empty), Is.True);
            Assert.That(
                EpisodeSession.GetOrCreate().Progress.AvatarPresetId,
                Is.EqualTo(EpisodeProgress.FirstPersonObserverId));
            Assert.That(controller.LandingVisible, Is.False);

            Object.DestroyImmediate(root);
        }
    }
}
