using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class LivingFieldAtlasScreenTests
    {
        [Test]
        public void PlanningSurfaceUsesMapRouteAndScenarioDrivenFieldTokens()
        {
            GameObject root = new GameObject("AtlasPlanTest");
            try
            {
                PlanController plan =
                    root.AddComponent<PlanController>();
                SetField(
                    plan,
                    "scenario",
                    new ScenarioDto
                    {
                        id = "scenario",
                        test_sites = new[]
                        {
                            new TestSiteDto
                            {
                                id = "s1",
                                label = "Site one"
                            },
                            new TestSiteDto
                            {
                                id = "s2",
                                label = "Site two"
                            }
                        },
                        interventions = new[]
                        {
                            new InterventionDto
                            {
                                id = "i1",
                                label = "Intervention one"
                            },
                            new InterventionDto
                            {
                                id = "i2",
                                label = "Intervention two"
                            }
                        },
                        support_measure_options = new[]
                        {
                            new SupportMeasureDto
                            {
                                id = "m1",
                                description = "Support one"
                            }
                        }
                    });
                PlanSession session = PlanSession.GetOrCreate();
                session.ConfigureScenario("scenario");
                SetField(plan, "session", session);
                GameObject stage = new GameObject("PlanStage");
                stage.transform.SetParent(root.transform, false);
                SetField(plan, "planStage", stage);
                Invoke(plan, "CreateInterface");

                Transform card = root.transform.Find(
                    "PlanStage/PlanCanvas/PlanCard");
                Assert.That(
                    card.GetComponent<AtlasSurfaceGraphic>()
                        ?.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.FieldPaper));
                Assert.That(
                    card.Find("PlanningMap/PlanningRoute")
                        ?.GetComponent<AtlasRouteGraphic>(),
                    Is.Not.Null);
                Assert.That(
                    card.Find("InterventionTokenTray"),
                    Is.Not.Null);
                Assert.That(
                    card.Find("InterventionTokenTray")
                        .GetComponentsInChildren<UnityEngine.UI.Button>(
                            true).Length,
                    Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(root);
                foreach (PlanSession session in
                         Object.FindObjectsByType<PlanSession>(
                             FindObjectsSortMode.None))
                {
                    Object.DestroyImmediate(session.gameObject);
                }
            }
        }

        [Test]
        public void PolicyBriefUsesAFieldReportSealAndSourceTagRail()
        {
            GameObject root = new GameObject("AtlasBriefTest");
            try
            {
                PolicyBriefController brief =
                    root.AddComponent<PolicyBriefController>();
                GameObject stage =
                    new GameObject("PolicyBriefStage");
                stage.transform.SetParent(root.transform, false);
                SetField(brief, "stage", stage);
                Invoke(brief, "CreateInterface");

                Transform document = root.transform.Find(
                    "PolicyBriefStage/PolicyBriefCanvas/" +
                    "PolicyBriefDocument");
                Assert.That(
                    document.GetComponent<AtlasSurfaceGraphic>()
                        ?.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.FieldPaper));
                Assert.That(document.Find("AtlasSeal"), Is.Not.Null);
                Assert.That(
                    document.Find("SourceTagRail"),
                    Is.Not.Null);
                Assert.That(
                    (document as RectTransform).anchorMax.y,
                    Is.LessThanOrEqualTo(.90f));
                Assert.That(
                    document.Find("RetryBrief").gameObject.activeSelf,
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetField(
            object target,
            string name,
            object value)
        {
            target.GetType()
                .GetField(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?.SetValue(target, value);
        }

        private static void Invoke(
            object target,
            string name)
        {
            target.GetType()
                .GetMethod(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?.Invoke(target, null);
        }
    }
}
