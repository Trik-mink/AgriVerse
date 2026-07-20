using NUnit.Framework;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriVerse.Client.Tests
{
    public sealed class Episode3DAlphaTests
    {
        private const string ScenePath =
            "Assets/Scenes/Episode3DAlpha.unity";
        private const string ConfigPath =
            "Assets/AgriVerse/Art/Environment/WorldLab/" +
            "Episode3DWorldConfig.asset";

        [Test]
        public void InvestigationCanDisableLegacyPresentationForA3DAdapter()
        {
            GameObject root = new GameObject("InvestigationAdapterTest");
            try
            {
                InvestigationController investigation =
                    root.AddComponent<InvestigationController>();

                investigation.ConfigurePresentation(
                    createUi: false,
                    createMarkers: false);

                Assert.That(investigation.CreatesRuntimeUi, Is.False);
                Assert.That(investigation.CreatesRuntimeMarkers, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void InvestigationUsesCinematicChoicesAndABoundedReadingCard()
        {
            GameObject root =
                new GameObject("CinematicInvestigationTest");
            try
            {
                Episode3DAlphaController controller =
                    root.AddComponent<Episode3DAlphaController>();
                typeof(Episode3DAlphaController)
                    .GetField(
                        "configuredSiteIds",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic)
                    ?.SetValue(
                        controller,
                        new[] { "site-1", "site-2", "site-3" });
                if (root.transform.Find("Episode3DAlphaHUD") == null)
                {
                    typeof(Episode3DAlphaController)
                        .GetMethod(
                            "BuildInterface",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic)
                        ?.Invoke(controller, null);
                }
                Transform firstChoice = root.transform.Find(
                    "Episode3DAlphaHUD/MaiDialogue/" +
                    "PredictionChoice_1");
                Transform secondChoice = root.transform.Find(
                    "Episode3DAlphaHUD/MaiDialogue/" +
                    "PredictionChoice_2");
                Transform reading = root.transform.Find(
                    "Episode3DAlphaHUD/FieldReading");

                Assert.That(firstChoice, Is.Not.Null);
                Assert.That(secondChoice, Is.Not.Null);
                Assert.That(
                    firstChoice.Find("ChoiceNumber"),
                    Is.Not.Null);
                Assert.That(
                    secondChoice.Find("ChoiceNumber"),
                    Is.Not.Null);
                Assert.That(reading, Is.Not.Null);
                RectTransform readingRect =
                    reading as RectTransform;
                Assert.That(readingRect.anchorMin.x, Is.GreaterThan(.60f));
                Assert.That(
                    reading.GetComponentInChildren<ScrollRect>(true),
                    Is.Not.Null);
                Assert.That(
                    reading.GetComponent<AtlasSurfaceGraphic>()
                        ?.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.AtlasLabel));
                Assert.That(
                    reading.Find("SalinityInstrument")
                        ?.GetComponent<AtlasInstrumentGraphic>(),
                    Is.Not.Null);
                Assert.That(
                    reading.Find("EvidenceRecordedStamp"),
                    Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FieldJournalIsAnAtlasSpreadWithMapRouteAndBoundedDossier()
        {
            GameObject root = new GameObject("FieldJournalAtlasTest");
            try
            {
                Episode3DAlphaController controller =
                    root.AddComponent<Episode3DAlphaController>();
                typeof(Episode3DAlphaController)
                    .GetField(
                        "configuredSiteIds",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic)
                    ?.SetValue(
                        controller,
                        new[] { "site-1", "site-2", "site-3" });
                if (root.transform.Find("Episode3DAlphaHUD") == null)
                {
                    typeof(Episode3DAlphaController)
                        .GetMethod(
                            "BuildInterface",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic)
                        ?.Invoke(controller, null);
                }

                Transform journal = root.transform.Find(
                    "Episode3DAlphaHUD/FieldJournal");
                Assert.That(journal, Is.Not.Null);
                Assert.That(
                    journal.GetComponent<AtlasSurfaceGraphic>()
                        ?.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.FieldPaper));
                Assert.That(
                    journal.Find("JournalSiteMap"),
                    Is.Not.Null);
                Assert.That(
                    journal.Find("JournalSiteMap/InvestigationRoute")
                        ?.GetComponent<AtlasRouteGraphic>()?.NodeCount,
                    Is.EqualTo(3));
                Assert.That(
                    journal.Find("DossierRule"),
                    Is.Not.Null);
                Assert.That(
                    journal.GetComponentInChildren<ScrollRect>(true),
                    Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WorldHudUsesCompactEdgeMountedAtlasLabels()
        {
            GameObject root = new GameObject("AtlasHudTest");
            try
            {
                Episode3DAlphaController controller =
                    root.AddComponent<Episode3DAlphaController>();
                if (root.transform.Find("Episode3DAlphaHUD") == null)
                {
                    typeof(Episode3DAlphaController)
                        .GetMethod(
                            "BuildInterface",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic)
                        ?.Invoke(controller, null);
                }
                RectTransform objective = root.transform.Find(
                    "Episode3DAlphaHUD/Objective") as RectTransform;
                RectTransform progress = root.transform.Find(
                    "Episode3DAlphaHUD/Progress") as RectTransform;

                Assert.That(objective, Is.Not.Null);
                Assert.That(progress, Is.Not.Null);
                Assert.That(
                    objective.anchorMax.x - objective.anchorMin.x,
                    Is.LessThanOrEqualTo(.42f));
                Assert.That(
                    progress.anchorMax.x - progress.anchorMin.x,
                    Is.LessThanOrEqualTo(.25f));
                Assert.That(
                    objective.GetComponent<AtlasSurfaceGraphic>()
                        ?.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.AtlasLabel));
                Assert.That(
                    progress.GetComponent<AtlasSurfaceGraphic>()
                        ?.SurfaceKind,
                    Is.EqualTo(AtlasSurfaceKind.AtlasLabel));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AlphaSceneReusesWorldAndCompleteLoopWithoutLegacyMarkers()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            try
            {
                Episode3DAlphaController alpha =
                    FindInScene<Episode3DAlphaController>(scene);
                InvestigationController investigation =
                    FindInScene<InvestigationController>(scene);
                FirstPersonWalker walker =
                    FindInScene<FirstPersonWalker>(scene);
                WaterSampleHotspot[] hotspots =
                    FindAllInScene<WaterSampleHotspot>(scene);
                StakeholderHotspot[] stakeholderHotspots =
                    FindAllInScene<StakeholderHotspot>(scene);
                PlanningHotspot planningHotspot =
                    FindInScene<PlanningHotspot>(scene);
                MaiGuideController mai =
                    FindInScene<MaiGuideController>(scene);
                InterviewController interviews =
                    FindInScene<InterviewController>(scene);

                Assert.That(alpha, Is.Not.Null);
                Episode3DWorldConfig config =
                    AssetDatabase.LoadAssetAtPath<
                        Episode3DWorldConfig>(ConfigPath);
                Assert.That(config, Is.Not.Null);
                Assert.That(config.SiteAnchors, Has.Length.EqualTo(3));
                Assert.That(
                    config.StakeholderAnchors,
                    Has.Length.EqualTo(3));
                Assert.That(
                    config.StakeholderPresentationPrefabs,
                    Has.Length.EqualTo(
                        config.StakeholderAnchors.Length));
                Assert.That(
                    config.StakeholderPresentationPrefabs,
                    Has.All.Not.Null);
                Assert.That(
                    config.SiteAnchors.Select(anchor => anchor.SiteId),
                    Is.Unique);
                Assert.That(
                    alpha.ConfiguredSiteCount,
                    Is.EqualTo(config.SiteAnchors.Length));
                Assert.That(
                    alpha.ConfiguredStakeholderCount,
                    Is.EqualTo(
                        config.StakeholderAnchors.Length));
                Assert.That(investigation, Is.Not.Null);
                Assert.That(investigation.CreatesRuntimeUi, Is.False);
                Assert.That(investigation.CreatesRuntimeMarkers, Is.False);
                Assert.That(interviews, Is.Not.Null);
                Assert.That(interviews.CreatesRuntimeMarkers, Is.False);
                Assert.That(interviews.AutoActivates, Is.False);
                Assert.That(
                    FindInScene<PlanController>(scene),
                    Is.Not.Null);
                Assert.That(
                    FindInScene<ConsequencesController>(scene),
                    Is.Not.Null);
                Assert.That(
                    FindInScene<FeedbackController>(scene),
                    Is.Not.Null);
                Assert.That(
                    FindInScene<PolicyBriefController>(scene),
                    Is.Not.Null);
                Assert.That(
                    FindInScene<EpisodePresentationController>(scene),
                    Is.Not.Null);
                Assert.That(
                    FindInScene<Episode3DFutureWalkController>(scene),
                    Is.Not.Null);
                Assert.That(
                    FindInScene<PlayerPerformanceReporter>(scene),
                    Is.Not.Null);
                Assert.That(walker, Is.Not.Null);
                Assert.That(hotspots, Has.Length.EqualTo(3));
                Assert.That(
                    stakeholderHotspots,
                    Has.Length.EqualTo(3));
                Assert.That(planningHotspot, Is.Not.Null);
                Assert.That(alpha.HasPlanningHotspot, Is.True);
                Assert.That(
                    planningHotspot.GetComponentInChildren<
                        MeshFilter>(true),
                    Is.Not.Null);
                Assert.That(
                    stakeholderHotspots.All(hotspot =>
                    {
                        Animator animator =
                            hotspot.GetComponentInChildren<Animator>(true);
                        return animator != null &&
                               animator.avatar != null &&
                               animator.avatar.isHuman &&
                               hotspot.Character != null &&
                               hotspot.Character.LookTarget ==
                                   walker.ViewCamera.transform;
                    }),
                    Is.True,
                    "Each scenario-configured meeting point must retain " +
                    "its verified Humanoid presentation.");
                Assert.That(
                    stakeholderHotspots
                        .SelectMany(hotspot =>
                            hotspot.GetComponentsInChildren<
                                Renderer>(true))
                        .Where(renderer =>
                            renderer.gameObject.name !=
                            "MeetingFocusRing")
                        .All(renderer =>
                            renderer.gameObject.layer == 2),
                    Is.True,
                    "Character visuals must remain outside marker raycasts.");
                GameObject premiumStations =
                    scene.GetRootGameObjects()
                        .FirstOrDefault(root =>
                            root.name == "PremiumFieldStations");
                Assert.That(premiumStations, Is.Not.Null);
                Assert.That(
                    premiumStations.GetComponentsInChildren<
                        LODGroup>(true),
                    Has.Length.GreaterThanOrEqualTo(9));
                Assert.That(
                    premiumStations.GetComponentsInChildren<
                        Collider>(true)
                        .All(collider =>
                            collider.gameObject.layer == 2),
                    Is.True,
                    "Set dressing may collide with the player but must " +
                    "never intercept learning raycasts.");
                Assert.That(
                    hotspots.All(hotspot =>
                        hotspot.GetComponentInChildren<MeshFilter>(true) !=
                        null),
                    Is.True);
                Assert.That(mai, Is.Not.Null);
                Assert.That(mai.Animator, Is.Not.Null);
                Assert.That(mai.Animator.avatar.isHuman, Is.True);
                Assert.That(mai.LookTarget, Is.EqualTo(walker.ViewCamera.transform));
                Assert.That(alpha.SampleAudioClipCount, Is.EqualTo(3));
                AudioSource[] ambience = scene.GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<AudioSource>(true))
                    .Where(source => source.loop)
                    .ToArray();
                Assert.That(ambience, Has.Length.GreaterThanOrEqualTo(4));
                Assert.That(
                    ambience.All(source =>
                        source.clip != null &&
                        source.clip.frequency == 48000 &&
                        source.playOnAwake),
                    Is.True);
                Assert.That(
                    scene.GetRootGameObjects(),
                    Has.None.Matches<GameObject>(
                        root => root.name == "TestSiteMarkers"));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void PremiumStationsAndCharactersAreGroundedAndCannotFall()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            try
            {
                GameObject premiumStations =
                    scene.GetRootGameObjects()
                        .FirstOrDefault(root =>
                            root.name == "PremiumFieldStations");
                Assert.That(premiumStations, Is.Not.Null);
                Transform[] stationRoots =
                    premiumStations.transform.Cast<Transform>().ToArray();
                Assert.That(stationRoots, Has.Length.EqualTo(9));
                foreach (Transform station in stationRoots)
                {
                    Assert.That(
                        Vector3.Dot(station.up, Vector3.up),
                        Is.GreaterThan(.999f),
                        station.name + " wrapper must remain upright.");
                    Assert.That(
                        station.GetComponentsInChildren<Rigidbody>(true),
                        Is.Empty,
                        station.name + " must not fall after Play begins.");
                    AssertGrounded(station);
                }

                MaiGuideController mai =
                    FindInScene<MaiGuideController>(scene);
                Assert.That(mai, Is.Not.Null);
                Assert.That(
                    mai.GetComponentsInChildren<Rigidbody>(true),
                    Is.Empty);
                AssertGrounded(mai.transform);

                foreach (StakeholderHotspot hotspot in
                         FindAllInScene<StakeholderHotspot>(scene))
                {
                    Assert.That(hotspot.Character, Is.Not.Null);
                    Assert.That(
                        hotspot.Character.GetComponentsInChildren<
                            Rigidbody>(true),
                        Is.Empty);
                    AssertGrounded(hotspot.Character.transform);
                }

                string[] stableWorldAssets =
                {
                    "FieldShelter",
                    "SamplingDock",
                    "LocalWoodenBoat",
                    "Banana_01",
                    "Palmyra_01",
                    "Coconut_01",
                    "Broadleaf_01",
                    "Reeds_01"
                };
                foreach (string assetName in stableWorldAssets)
                {
                    Transform asset = FindTransform(
                        scene,
                        assetName);
                    Assert.That(
                        asset.GetComponentsInChildren<Rigidbody>(true),
                        Is.Empty,
                        assetName + " must remain static.");
                    AssertGrounded(asset);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertGrounded(Transform target)
        {
            Renderer[] renderers =
                target.GetComponentsInChildren<Renderer>(true)
                    .Where(renderer =>
                        renderer.gameObject.name != "MeetingFocusRing")
                    .ToArray();
            Assert.That(renderers, Is.Not.Empty, target.name);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }
            Assert.That(
                Mathf.Abs(bounds.min.y - target.position.y),
                Is.LessThan(.06f),
                target.name + " must rest on its placement surface.");
        }

        private static Transform FindTransform(
            Scene scene,
            string objectName)
        {
            Transform result = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item =>
                    item.name == objectName);
            Assert.That(result, Is.Not.Null, objectName);
            return result;
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static T[] FindAllInScene<T>(Scene scene)
            where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<T>(true))
                .ToArray();
    }
}
