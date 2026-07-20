using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client.Tests
{
    public sealed class CinematicUiThemeTests
    {
        [Test]
        public void ThemeMatchesTheApprovedFieldJournalPalette()
        {
            AssertColor(
                EpisodeUiFactory.DeepTeal,
                new Color32(6, 44, 45, 224));
            AssertColor(
                EpisodeUiFactory.SecondarySurface,
                new Color32(10, 58, 58, 235));
            AssertColor(
                EpisodeUiFactory.Amber,
                new Color32(230, 162, 60, 255));
            AssertColor(
                EpisodeUiFactory.BrightAmber,
                new Color32(240, 179, 77, 255));
            AssertColor(
                EpisodeUiFactory.OffWhite,
                new Color32(243, 233, 211, 255));
        }

        [Test]
        public void SharedButtonsExposeDistinctPrimarySecondaryChoiceAndTabStates()
        {
            GameObject root = new GameObject("CinematicThemeTest");
            try
            {
                Button primary = EpisodeUiFactory.Button(
                    root.transform,
                    "Primary",
                    "BEGIN",
                    EpisodeButtonStyle.Primary,
                    15);
                Button secondary = EpisodeUiFactory.Button(
                    root.transform,
                    "Secondary",
                    "BACK",
                    EpisodeButtonStyle.Secondary,
                    15);
                Button choice = EpisodeUiFactory.ChoiceButton(
                    root.transform,
                    "Choice",
                    2,
                    "Which evidence matters most?");
                Button tab = EpisodeUiFactory.Button(
                    root.transform,
                    "Tab",
                    "SOURCES",
                    EpisodeButtonStyle.Tab,
                    13);

                Assert.That(
                    primary.targetGraphic.color,
                    Is.EqualTo(EpisodeUiFactory.Amber));
                Assert.That(
                    primary.GetComponentInChildren<Text>().color,
                    Is.EqualTo(EpisodeUiFactory.Ink));
                Assert.That(
                    secondary.targetGraphic.GetComponent<Outline>(),
                    Is.Not.Null);
                Assert.That(
                    secondary.colors.fadeDuration,
                    Is.InRange(.12f, .22f));
                Assert.That(
                    choice.transform.Find("ChoiceNumber"),
                    Is.Not.Null);
                Assert.That(
                    choice.GetComponentInChildren<Text>().fontSize,
                    Is.GreaterThanOrEqualTo(13));
                Assert.That(
                    tab.targetGraphic.GetComponent<Outline>(),
                    Is.Not.Null);
                Assert.That(
                    primary.targetGraphic,
                    Is.TypeOf<AtlasSurfaceGraphic>());
                Assert.That(
                    secondary.targetGraphic,
                    Is.TypeOf<AtlasSurfaceGraphic>());
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CinematicPanelsUseAThinAmberHairlineWithoutBecomingOpaque()
        {
            GameObject root = new GameObject("CinematicPanelTest");
            try
            {
                Image panel = EpisodeUiFactory.CinematicPanel(
                    root.transform,
                    "Panel",
                    true);
                Outline outline = panel.GetComponent<Outline>();

                Assert.That(outline, Is.Not.Null);
                Assert.That(
                    Mathf.Abs(outline.effectDistance.x),
                    Is.LessThanOrEqualTo(1.1f));
                Assert.That(
                    Mathf.Abs(outline.effectDistance.y),
                    Is.LessThanOrEqualTo(1.1f));
                Assert.That(panel.color.a, Is.InRange(.82f, .92f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void LivingFieldAtlasExposesThreeDistinctSurfaceFamilies()
        {
            GameObject root = new GameObject("AtlasSurfaceTest");
            try
            {
                AtlasSurfaceGraphic paper =
                    EpisodeUiFactory.FieldPaper(
                        root.transform,
                        "EvidenceCard");
                AtlasSurfaceGraphic glass =
                    EpisodeUiFactory.SmokedGlass(
                        root.transform,
                        "InteractionOverlay");
                AtlasSurfaceGraphic label =
                    EpisodeUiFactory.AtlasLabel(
                        root.transform,
                        "WorldAnnotation");

                Assert.That(
                    paper.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.FieldPaper));
                Assert.That(
                    glass.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.SmokedGlass));
                Assert.That(
                    label.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.AtlasLabel));
                Assert.That(
                    paper.CornerCut,
                    Is.GreaterThan(glass.CornerCut));
                Assert.That(
                    label.CornerCut,
                    Is.GreaterThan(0f));
                Assert.That(
                    paper.transform.Find("PaperGrain"),
                    Is.Not.Null);
                Assert.That(
                    paper.transform.Find("ContourMarks"),
                    Is.Not.Null);
                Assert.That(
                    label.transform.Find("SurveyRule"),
                    Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AtlasSurfacesDoNotBlockTheWorldUnlessExplicitlyInteractive()
        {
            GameObject root = new GameObject("AtlasRaycastTest");
            try
            {
                AtlasSurfaceGraphic passive =
                    EpisodeUiFactory.SmokedGlass(
                        root.transform,
                        "Passive");
                AtlasSurfaceGraphic interactive =
                    EpisodeUiFactory.FieldPaper(
                        root.transform,
                        "Interactive",
                        true);

                Assert.That(passive.raycastTarget, Is.False);
                Assert.That(interactive.raycastTarget, Is.True);
                foreach (Graphic graphic in
                         passive.GetComponentsInChildren<Graphic>(true))
                {
                    Assert.That(
                        graphic.raycastTarget,
                        Is.False,
                        graphic.name);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ModelTextFormattingRemovesLiteralMarkdownWithoutChangingWords()
        {
            string formatted =
                EpisodeUiFactory.FormatModelText(
                    "The reading is **4 g/L**.\n- Check source S2.");

            Assert.That(formatted, Does.Not.Contain("**"));
            Assert.That(formatted, Does.Contain("<b>4 g/L</b>"));
            Assert.That(formatted, Does.Contain("• Check source S2."));
        }

        [Test]
        public void FieldRouteNodesScaleWithScenarioSiteCount()
        {
            Vector2[] nodes =
                EpisodeUiFactory.FieldRouteNodes(4);

            Assert.That(nodes, Has.Length.EqualTo(4));
            Assert.That(nodes[0].x, Is.LessThan(nodes[3].x));
            Assert.That(
                nodes,
                Has.All.Matches<Vector2>(
                    point =>
                        point.x >= 0f &&
                        point.x <= 1f &&
                        point.y >= 0f &&
                        point.y <= 1f));
        }

        private static void AssertColor(
            Color actual,
            Color32 expected)
        {
            Color expectedColor = expected;
            Assert.That(actual.r, Is.EqualTo(expectedColor.r).Within(.005f));
            Assert.That(actual.g, Is.EqualTo(expectedColor.g).Within(.005f));
            Assert.That(actual.b, Is.EqualTo(expectedColor.b).Within(.005f));
            Assert.That(actual.a, Is.EqualTo(expectedColor.a).Within(.005f));
        }
    }
}
