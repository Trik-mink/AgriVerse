using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client.Tests
{
    public sealed class CinematicInterviewPresentationTests
    {
        [Test]
        public void SelectionCardsSwapIntoOneCompactDialogueAndEvidenceDrawer()
        {
            GameObject root = new GameObject("CinematicInterviewTest");
            CinematicInterviewPresentation presentation = root.AddComponent<CinematicInterviewPresentation>();
            ScenarioDto scenario = new ScenarioDto
            {
                title = "Scenario title",
                test_sites = new[] { new TestSiteDto { id = "site" } },
                stakeholders = new[]
                {
                    new StakeholderDto { id = "one", name = "One", role = "Role", persona = "Persona" },
                    new StakeholderDto { id = "two", name = "Two", role = "Role", persona = "Persona" },
                    new StakeholderDto { id = "three", name = "Three", role = "Role", persona = "Persona" }
                }
            };
            presentation.Build(root.transform, scenario, _ => { }, () => { }, () => { }, null);
            presentation.Refresh(null, string.Empty, "No samples recorded.", "Choose a perspective.", 0, 1, 0, 3, false, false, false);

            Assert.That(presentation.SelectionVisible, Is.True);
            Assert.That(root.GetComponentsInChildren<Button>(true), Has.Length.GreaterThanOrEqualTo(7));
            Assert.That(RuntimeScrollableContent.ActiveScrollViewsDoNotBlockSceneRaycasts(), Is.True);

            StakeholderDto selected = scenario.stakeholders[2];
            presentation.Refresh(selected, "No response recorded yet.", "No samples recorded.", "Ready.", 0, 1, 0, 3, false, true, false);
            Assert.That(presentation.SelectionVisible, Is.False);
            Assert.That(presentation.QuestionInput.interactable, Is.True);
            Assert.That(presentation.RetryVisible, Is.True);

            presentation.ToggleEvidenceDrawer();
            Assert.That(presentation.EvidenceVisible, Is.True);
            presentation.ToggleEvidenceDrawer();
            Assert.That(presentation.EvidenceVisible, Is.False);
            Object.DestroyImmediate(root);
        }
    }
}
