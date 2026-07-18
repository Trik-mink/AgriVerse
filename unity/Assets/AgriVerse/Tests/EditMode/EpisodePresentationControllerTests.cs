using NUnit.Framework;
using UnityEngine;

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
            Assert.That(controller.BeginMissionForTesting("Lan", string.Empty), Is.False);
            Assert.That(controller.LandingVisible, Is.True);

            Object.DestroyImmediate(root);
        }
    }
}
