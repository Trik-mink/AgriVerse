using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
