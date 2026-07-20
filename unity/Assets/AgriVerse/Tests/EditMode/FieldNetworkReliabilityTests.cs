using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriVerse.Client.Tests
{
    public sealed class FieldNetworkReliabilityTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (EpisodeSession session in
                     Object.FindObjectsByType<EpisodeSession>(
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(session.gameObject);
            }
            foreach (RuntimePanelManager manager in
                     Object.FindObjectsByType<RuntimePanelManager>(
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(manager.gameObject);
            }
            foreach (EventSystem eventSystem in
                     Object.FindObjectsByType<EventSystem>(
                         FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(eventSystem.gameObject);
            }
            foreach (Camera camera in
                     Object.FindObjectsByType<Camera>(
                         FindObjectsSortMode.None))
            {
                if (camera != null &&
                    camera.gameObject.name == "OrbitalGlobeCamera")
                {
                    Object.DestroyImmediate(camera.gameObject);
                }
            }
        }

        [Test]
        public void PackagedScenarioSuppliesThePlayableCatalogNodeOffline()
        {
            Assert.That(
                PackagedScenarioLoader.TryLoad(
                    out ScenarioDto scenario,
                    out string error),
                Is.True,
                error);

            FieldNetworkCatalog catalog =
                FieldNetworkCatalog.CreateForScenario(
                    scenario,
                    SaltLineNarrative.Episode,
                    SaltLineNarrative.Tagline);

            Assert.That(catalog.Locations.Count, Is.EqualTo(5));
            Assert.That(catalog.ActiveLocation.Id, Is.EqualTo(scenario.id));
            Assert.That(
                catalog.ActiveLocation.Country,
                Is.EqualTo(scenario.location.country));
            Assert.That(
                catalog.Locations.Count(location => !location.IsPlayable),
                Is.EqualTo(4));
            TextAsset snapshot =
                Resources.Load<TextAsset>(
                    "ScenarioLandingSnapshot");
            Assert.That(snapshot.text, Does.Not.Contain("hidden_goal"));
            Assert.That(snapshot.text, Does.Not.Contain("prompt_file"));
        }

        [Test]
        public void LoadingStateExplainsThatTheFieldNetworkIsConnecting()
        {
            GameObject root = new GameObject("LoadingLandingTest");
            try
            {
                EpisodePresentationController controller =
                    root.AddComponent<EpisodePresentationController>();
                controller.BuildLoadingForTesting(
                    PackagedScenarioLoader.LoadRequired());

                Assert.That(controller.ConnectionStatusVisibleForTesting, Is.True);
                Assert.That(
                    controller.ConnectionStatusTextForTesting,
                    Does.Contain("CONNECTING TO FIELD NETWORK"));
                Assert.That(controller.RetryVisibleForTesting, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BackendUnavailableShowsOfflineRetryInsteadOfASilentGlobe()
        {
            GameObject root = new GameObject("OfflineLandingTest");
            try
            {
                EpisodePresentationController controller =
                    root.AddComponent<EpisodePresentationController>();
                controller.BuildOfflineForTesting(
                    PackagedScenarioLoader.LoadRequired());

                Assert.That(controller.LandingVisible, Is.True);
                Assert.That(controller.FieldNetworkPinCountForTesting, Is.EqualTo(5));
                Assert.That(controller.ConnectionStatusVisibleForTesting, Is.True);
                Assert.That(
                    controller.ConnectionStatusTextForTesting,
                    Does.Contain("FIELD NETWORK OFFLINE"));
                Assert.That(
                    controller.ConnectionStatusTextForTesting,
                    Does.Contain(
                        "The mission service could not be reached."));
                Assert.That(controller.RetryVisibleForTesting, Is.True);
                RuntimePanelManager.GetOrCreate()
                    .SetInstruction("Scenario unavailable.");
                Assert.That(
                    RuntimePanelManager.GetOrCreate()
                        .InstructionCanvasVisible,
                    Is.False,
                    "The atlas landing must suppress underlying stage status text.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RetrySuccessPreservesNameAndSelectionAndEnablesMission()
        {
            GameObject root = new GameObject("RetryLandingTest");
            try
            {
                ScenarioDto packaged =
                    PackagedScenarioLoader.LoadRequired();
                EpisodePresentationController controller =
                    root.AddComponent<EpisodePresentationController>();
                controller.BuildOfflineForTesting(packaged);
                Assert.That(
                    controller.SelectFieldLocationForTesting(packaged.id),
                    Is.True);
                controller.SetPlayerNameForTesting("Lan");

                Assert.That(
                    controller.MissionStartInteractableForTesting,
                    Is.False);
                Assert.That(
                    controller.MissionConnectionRequiredForTesting,
                    Is.True);

                controller.CompleteRetryForTesting(packaged);

                Assert.That(
                    controller.SelectedFieldLocationIdForTesting,
                    Is.EqualTo(packaged.id));
                Assert.That(controller.PlayerNameForTesting, Is.EqualTo("Lan"));
                Assert.That(
                    controller.MissionStartInteractableForTesting,
                    Is.True);
                Assert.That(controller.ConnectionStatusVisibleForTesting, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OnlyAConnectedAvailableScenarioCanBeginMission()
        {
            GameObject root = new GameObject("MissionGateTest");
            try
            {
                ScenarioDto scenario =
                    PackagedScenarioLoader.LoadRequired();
                EpisodePresentationController controller =
                    root.AddComponent<EpisodePresentationController>();
                controller.BuildOfflineForTesting(scenario);

                controller.SelectFieldLocationForTesting(scenario.id);
                Assert.That(
                    controller.BeginMissionForTesting("Lan", string.Empty),
                    Is.False,
                    "The packaged scenario must not make the scored mission playable offline.");

                controller.CompleteRetryForTesting(scenario);
                controller.SelectFieldLocationForTesting("india");
                Assert.That(
                    controller.BeginMissionForTesting("Lan", string.Empty),
                    Is.False,
                    "Incoming catalog previews are never playable.");

                controller.SelectFieldLocationForTesting(scenario.id);
                Assert.That(
                    controller.BeginMissionForTesting("Lan", string.Empty),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void TabFocusesAndFramesAPinEvenWhenEveryPinStartsHidden()
        {
            using (GlobeRendererFixture fixture =
                   new GlobeRendererFixture())
            {
                fixture.Renderer.SetAllPinsVisibleForTesting(false);

                fixture.Renderer.FocusNextPin(1);

                Assert.That(
                    fixture.Renderer.KeyboardFocusedLocationIdForTesting,
                    Is.EqualTo(
                        fixture.Catalog.Locations[0].Id));
                Assert.That(
                    fixture.Renderer.IsFramingKeyboardFocusForTesting,
                    Is.True);
            }
        }

        [Test]
        public void ShiftTabCyclesBackwardAcrossTheCompleteCatalog()
        {
            using (GlobeRendererFixture fixture =
                   new GlobeRendererFixture())
            {
                fixture.Renderer.SetAllPinsVisibleForTesting(false);

                fixture.Renderer.FocusNextPin(-1);
                string expected =
                    fixture.Catalog.Locations[
                        fixture.Catalog.Locations.Count - 1].Id;

                Assert.That(
                    fixture.Renderer.KeyboardFocusedLocationIdForTesting,
                    Is.EqualTo(expected));
            }
        }

        [Test]
        public void EnterSelectsTheModelFocusedLocation()
        {
            FieldNetworkLocation selected = null;
            using (GlobeRendererFixture fixture =
                   new GlobeRendererFixture(
                       location => selected = location))
            {
                fixture.Renderer.SetAllPinsVisibleForTesting(false);
                fixture.Renderer.FocusNextPin(1);

                Assert.That(
                    fixture.Renderer.SelectKeyboardFocusedPin(),
                    Is.True);
                Assert.That(selected, Is.Not.Null);
                Assert.That(
                    selected.Id,
                    Is.EqualTo(
                        fixture.Renderer
                            .KeyboardFocusedLocationIdForTesting));
            }
        }

        [Test]
        public void MouseDragZoomHoverAndClickRemainAvailable()
        {
            FieldNetworkLocation selected = null;
            using (GlobeRendererFixture fixture =
                   new GlobeRendererFixture(
                       location => selected = location))
            {
                EventSystem eventSystem =
                    new GameObject(
                        "MouseNavigationEventSystem",
                        typeof(EventSystem))
                        .GetComponent<EventSystem>();
                var pointer =
                    new PointerEventData(eventSystem)
                    {
                        delta = new Vector2(24f, -12f)
                    };
                Transform globeRoot =
                    (Transform)typeof(GlobeLandingRenderer)
                        .GetField(
                            "globeRoot",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic)
                        ?.GetValue(fixture.Renderer);
                Quaternion rotationBefore =
                    globeRoot.localRotation;

                fixture.Renderer.OnPointerDown(pointer);
                fixture.Renderer.OnDrag(pointer);
                fixture.Renderer.OnPointerUp(pointer);

                Assert.That(
                    globeRoot.localRotation,
                    Is.Not.EqualTo(rotationBefore));

                float zoomBefore =
                    (float)typeof(GlobeLandingRenderer)
                        .GetField(
                            "targetDistance",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic)
                        ?.GetValue(fixture.Renderer);
                pointer.scrollDelta = new Vector2(0f, 1f);
                fixture.Renderer.OnScroll(pointer);
                float zoomAfter =
                    (float)typeof(GlobeLandingRenderer)
                        .GetField(
                            "targetDistance",
                            BindingFlags.Instance |
                            BindingFlags.NonPublic)
                        ?.GetValue(fixture.Renderer);
                Assert.That(zoomAfter, Is.Not.EqualTo(zoomBefore));

                FieldNetworkPinView pin =
                    fixture.Renderer.GetComponentInChildren<
                        FieldNetworkPinView>(true);
                pin.SetVisible(true);
                pin.OnPointerEnter(pointer);
                Assert.That(
                    pin.transform.Find("PinLabel")
                        .gameObject.activeSelf,
                    Is.True);
                pin.GetComponent<Button>().onClick.Invoke();
                Assert.That(selected, Is.Not.Null);
                Assert.That(
                    selected.Id,
                    Is.EqualTo(pin.Location.Id));
            }
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void OfflineAndMissionControlsRemainUsableAtSupportedResolutions(
            int width,
            int height)
        {
            GameObject root = new GameObject("ResponsiveLandingTest");
            try
            {
                EpisodePresentationController controller =
                    root.AddComponent<EpisodePresentationController>();
                controller.BuildOfflineForTesting(
                    PackagedScenarioLoader.LoadRequired());

                Assert.That(
                    controller.LandingControlsUsableAtForTesting(
                        new Vector2(width, height)),
                    Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class GlobeRendererFixture :
            System.IDisposable
        {
            private readonly GameObject root;

            internal GlobeRendererFixture(
                System.Action<FieldNetworkLocation> onSelect = null)
            {
                root = new GameObject(
                    "GlobeNavigationTest",
                    typeof(RectTransform));
                RectTransform pinLayer = new GameObject(
                    "PinLayer",
                    typeof(RectTransform))
                    .GetComponent<RectTransform>();
                pinLayer.SetParent(root.transform, false);
                Renderer =
                    root.AddComponent<GlobeLandingRenderer>();
                Renderer.Initialize(
                    pinLayer,
                    onSelect ?? (_ => { }));
                Catalog =
                    FieldNetworkCatalog.CreateForScenario(
                        PackagedScenarioLoader.LoadRequired(),
                        SaltLineNarrative.Episode,
                        SaltLineNarrative.Tagline);
                Renderer.SetCatalog(Catalog);
            }

            internal GlobeLandingRenderer Renderer { get; }
            internal FieldNetworkCatalog Catalog { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
