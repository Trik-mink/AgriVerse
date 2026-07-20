using NUnit.Framework;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class RuntimePanelManagerTests
    {
        [SetUp]
        public void RemovePriorManagers()
        {
            foreach (RuntimePanelManager manager in Object.FindObjectsByType<RuntimePanelManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(manager.gameObject);
        }

        [Test]
        public void EveryStageTransitionLeavesOneLeftPanelAndOneInstructionSlot()
        {
            RuntimePanelManager manager = RuntimePanelManager.GetOrCreate();
            var contentByStage = new System.Collections.Generic.Dictionary<RuntimeActivityStage, UnityEngine.UI.Text>();
            foreach (RuntimeActivityStage stage in System.Enum.GetValues(typeof(RuntimeActivityStage)))
            {
                GameObject panel = new GameObject(stage + "Panel");
                manager.Register(stage, panel);
                UnityEngine.UI.Text content = RuntimeScrollableContent.Create(panel.transform, stage + "Content", new Vector2(.03f, .1f), new Vector2(.62f, .82f), 14);
                RuntimeScrollableContent.SetText(content, new string('x', 4000));
                contentByStage[stage] = content;
            }

            foreach (RuntimeActivityStage stage in System.Enum.GetValues(typeof(RuntimeActivityStage)))
            {
                manager.SetInstruction("Instruction for " + stage);
                manager.Show(stage);

                Assert.That(manager.ActiveStage, Is.EqualTo(stage));
                Assert.That(manager.ActivePanelCount, Is.EqualTo(1), "Stage panels must swap, never stack.");
                Assert.That(manager.ActiveInstructionTextCount, Is.EqualTo(1), "The shared instruction slot must swap text, never draw a second label.");
                Assert.That(manager.ActiveScrollableContentCount, Is.EqualTo(1), "The active stage must retain exactly one contracted scrollable content region.");
                Assert.That(RuntimeScrollableContent.MeetsContract(contentByStage[stage]), Is.True, "Long-form content must be wheel/trackpad scrollable with a visible-on-overflow scrollbar.");
                Assert.That(RuntimeScrollableContent.ActiveScrollViewsDoNotBlockSceneRaycasts(), Is.True, "Scrollable cards must not add a UI raycast blocker over scene markers.");
                Assert.That(RuntimeScrollableContent.IsBoundedToVisibleCard(contentByStage[stage]), Is.True, "A scroll interaction rect must stay within its visible card, never expand to the screen.");
                Assert.That(RuntimeScrollableContent.HasTopLeftTextGutter(contentByStage[stage]), Is.True, "Scrollable text must stay fully inside the card's top-left gutter.");
                Assert.That(
                    manager.InstructionCanvasVisible,
                    Is.EqualTo(
                        stage != RuntimeActivityStage.Interviews),
                    "Cinematic interviews own their status slot even when the shared instruction canvas is created after the stage swap.");
            }

            manager.Clear();
            Assert.That(manager.ActiveStage, Is.Null);
            Assert.That(manager.ActivePanelCount, Is.EqualTo(0));
        }

        [Test]
        public void CinematicModeHidesInstructionCanvasCreatedAfterTheStageSwap()
        {
            RuntimePanelManager manager =
                RuntimePanelManager.GetOrCreate();
            GameObject panel =
                new GameObject("InterviewPanel");
            manager.Register(
                RuntimeActivityStage.Interviews,
                panel);
            manager.Show(RuntimeActivityStage.Interviews);
            manager.SetInstruction(
                "This status belongs inside the cinematic shell.");

            Assert.That(
                manager.InstructionCanvasVisible,
                Is.False);
        }

        [Test]
        public void SharedInstructionUsesOneCompactEdgeMountedAtlasLabel()
        {
            RuntimePanelManager manager =
                RuntimePanelManager.GetOrCreate();
            manager.SetInstruction("One current instruction.");
            Transform card = manager.transform.Find(
                "RuntimeInstructionCanvas/RuntimeInstructionCard");
            RectTransform rect = card as RectTransform;

            Assert.That(card, Is.Not.Null);
            Assert.That(
                card.GetComponent<AtlasSurfaceGraphic>()?.SurfaceKind,
                Is.EqualTo(AtlasSurfaceKind.AtlasLabel));
            Assert.That(
                rect.anchorMax.x - rect.anchorMin.x,
                Is.LessThanOrEqualTo(.42f));
            Assert.That(
                card.GetComponentsInChildren<UnityEngine.UI.Text>(true),
                Has.Length.EqualTo(1));
        }
    }
}
