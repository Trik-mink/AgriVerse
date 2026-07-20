using NUnit.Framework;
using System.Linq;
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
            int asks = 0;
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
            presentation.Build(
                root.transform,
                scenario,
                _ => { },
                () => asks++,
                () => { },
                null);
            presentation.Refresh(null, string.Empty, "No samples recorded.", "Choose a perspective.", 0, 1, 0, 3, false, false, false);

            Assert.That(presentation.SelectionVisible, Is.True);
            Assert.That(root.GetComponentsInChildren<Button>(true), Has.Length.GreaterThanOrEqualTo(7));
            Assert.That(RuntimeScrollableContent.ActiveScrollViewsDoNotBlockSceneRaycasts(), Is.True);

            StakeholderDto selected = scenario.stakeholders[2];
            presentation.Refresh(
                selected,
                "The measured value is **4 g/L**.\n- Check S2.",
                "No samples recorded.",
                "Ready.",
                0,
                1,
                0,
                3,
                false,
                true,
                false);
            Assert.That(presentation.SelectionVisible, Is.False);
            Assert.That(presentation.QuestionInput.interactable, Is.True);
            Assert.That(presentation.RetryVisible, Is.True);
            Assert.That(presentation.SuggestedQuestionCount, Is.EqualTo(3));
            Assert.That(presentation.FreeTextVisible, Is.False);
            RectTransform dialogue =
                root.transform.Find(
                    "CinematicInterviewCanvas/InterviewDialogue")
                    as RectTransform;
            Assert.That(dialogue, Is.Not.Null);
            Assert.That(
                dialogue.anchorMax.y,
                Is.LessThanOrEqualTo(.40f),
                "The cinematic interview must preserve the character view.");
            Assert.That(
                dialogue.GetComponent<AtlasSurfaceGraphic>()
                    ?.SurfaceKind,
                Is.EqualTo(AtlasSurfaceKind.SmokedGlass));
            Text response = root
                .GetComponentsInChildren<Text>(true)
                .First(text =>
                    text.name == "InterviewConversation");
            Assert.That(response.text, Does.Not.Contain("**"));
            Assert.That(response.text, Does.Contain("<b>4 g/L</b>"));
            Assert.That(response.supportRichText, Is.True);
            foreach (Button button in
                     root.GetComponentsInChildren<Button>(true))
            {
                if (!button.name.StartsWith(
                        "SuggestedQuestion_"))
                {
                    continue;
                }
                Assert.That(
                    button.transform.Find("ChoiceNumber"),
                    Is.Not.Null,
                    button.name);
            }
            RectTransform customQuestion =
                root.transform.Find(
                    "CinematicInterviewCanvas/InterviewDialogue/" +
                    "SuggestedQuestions/AskYourOwnQuestion")
                    as RectTransform;
            Assert.That(customQuestion, Is.Not.Null);
            foreach (Button button in
                     root.GetComponentsInChildren<Button>(true))
            {
                if (!button.name.StartsWith(
                        "SuggestedQuestion_"))
                {
                    continue;
                }
                RectTransform question =
                    button.GetComponent<RectTransform>();
                Assert.That(
                    question.anchorMax.y,
                    Is.LessThanOrEqualTo(
                        customQuestion.anchorMin.y),
                    button.name +
                    " must not overlap Ask your own question.");
            }

            presentation.SelectSuggestedQuestionForTesting(1);
            Assert.That(
                presentation.QuestionInput.text,
                Does.Contain("evidence"));
            Assert.That(asks, Is.EqualTo(1));

            presentation.SelectCustomQuestionForTesting();
            Assert.That(presentation.FreeTextVisible, Is.True);

            presentation.ToggleEvidenceDrawer();
            Assert.That(presentation.EvidenceVisible, Is.True);
            Transform evidenceDrawer = root.transform.Find(
                "CinematicInterviewCanvas/EvidenceDrawer");
            Assert.That(
                evidenceDrawer.GetComponent<AtlasSurfaceGraphic>()
                    ?.SurfaceKind,
                Is.EqualTo(AtlasSurfaceKind.FieldPaper));
            Assert.That(
                evidenceDrawer.Find("EvidenceRoute"),
                Is.Not.Null);
            Assert.That(
                evidenceDrawer.Find("SourceStamp"),
                Is.Not.Null);
            presentation.ToggleEvidenceDrawer();
            Assert.That(presentation.EvidenceVisible, Is.False);
            Object.DestroyImmediate(root);
        }
    }
}
