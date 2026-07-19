using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    public enum InvestigationLoadState
    {
        NotStarted,
        Loading,
        Ready,
        Failed
    }

    /// <summary>
    /// The intentionally unpolished Investigation stage. It creates only generic
    /// gray primitives; site content and the notebook values come from /api/scenario.
    /// </summary>
    public sealed class InvestigationController : MonoBehaviour
    {
        [Header("Existing Express backend")]
        [SerializeField]
        private string editorApiBaseUrl = "http://localhost:8787";

        [SerializeField]
        private string webApiBaseUrl = "http://localhost:8787";

        [Header("Runtime setup")]
        [SerializeField]
        private bool createRuntimeUi = true;

        [SerializeField]
        private bool createRuntimeMarkers = true;

        // Keep Unity's runtime primitive dependencies from being stripped in Web builds.
        [SerializeField] private MeshFilter preserveMeshFilter;
        [SerializeField] private MeshRenderer preserveMeshRenderer;
        [SerializeField] private BoxCollider preserveBoxCollider;
        [SerializeField] private SphereCollider preserveSphereCollider;

        private readonly List<TestSiteMarker> markers = new List<TestSiteMarker>();
        private ScenarioDto scenario;
        private EvidenceNotebookSession notebookSession;
        private EpisodeSession episodeSession;
        private TestSiteDto selectedSite;
        private int selectedSiteIndex = -1;
        private Text selectionText;
        private Text notebookText;
        private Text gateText;
        private Button collectButton;
        private readonly Button[] predictionButtons = new Button[2];
        private GameObject readingPanelObject;
        private GameObject notebookPanelObject;
        private Button notebookToggleButton;
        private GameObject interviewGateObject;
        private GameObject investigationStage;
        private GameObject markerRoot;
        private Canvas runtimeCanvas;
        private bool notebookOpen;

        public InvestigationLoadState LoadState { get; private set; } = InvestigationLoadState.NotStarted;
        public ScenarioDto Scenario => scenario;
        public bool CreatesRuntimeUi => createRuntimeUi;
        public bool CreatesRuntimeMarkers => createRuntimeMarkers;
        public int MarkerCount => markers.Count;
        public int RecordedReadingCount => notebookSession?.Notebook?.RecordedReadings.Count ?? 0;
        public bool SelectedReadingRevealed =>
            selectedSite != null &&
            episodeSession?.Progress != null &&
            episodeSession.Progress.HasPrediction(selectedSite.id);
        public bool InterviewsUnlocked => scenario != null &&
                                        notebookSession?.Notebook != null &&
                                        notebookSession.Notebook.AreAllSitesRecorded(scenario.test_sites);

        private void Start()
        {
            StartCoroutine(LoadScenario());
        }

        private void Update()
        {
            if (LoadState != InvestigationLoadState.Ready || Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            {
                return;
            }

            Camera sceneCamera = Camera.main;
            if (sceneCamera == null)
            {
                return;
            }

            Ray ray = sceneCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                TestSiteMarker marker = hit.collider.GetComponent<TestSiteMarker>();
                if (marker != null)
                {
                    SelectSite(marker.SiteId);
                }
            }
        }

        public IEnumerator LoadScenario()
        {
            LoadState = InvestigationLoadState.Loading;
            SetStatus("Loading scenario…");

            string scenarioUrl;
            try
            {
                scenarioUrl = ScenarioEndpoint.ForPlatform(
                    IsWebBuild,
                    editorApiBaseUrl,
                    webApiBaseUrl);
            }
            catch (ArgumentException error)
            {
                Fail("Scenario URL is not configured.", error.Message);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(scenarioUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Fail(
                        "Scenario unavailable.",
                        $"GET {scenarioUrl} failed ({request.responseCode}): {request.error}");
                    yield break;
                }

                try
                {
                    scenario = ScenarioDto.FromJson(request.downloadHandler.text);
                }
                catch (FormatException error)
                {
                    Fail("Scenario response was invalid.", error.Message);
                    yield break;
                }
            }

            if (scenario.test_sites == null || scenario.test_sites.Length == 0)
            {
                Fail("Scenario has no test sites.", "The sanitized scenario contained no test_sites.");
                yield break;
            }

            notebookSession = EvidenceNotebookSession.GetOrCreate();
            notebookSession.ConfigureScenario(scenario.id);
            episodeSession = EpisodeSession.GetOrCreate();
            episodeSession.ConfigureScenario(scenario.id);
            if (createRuntimeMarkers)
            {
                CreateMarkers();
            }
            if (createRuntimeUi)
            {
                CreateInterface();
            }

            LoadState = InvestigationLoadState.Ready;
            SetStatus("Select a gray test-site marker, then collect its sample.");
            RefreshInterface();
        }

        public void SelectSite(string siteId)
        {
            if (scenario == null || scenario.test_sites == null)
            {
                return;
            }

            for (int index = 0; index < scenario.test_sites.Length; index++)
            {
                if (scenario.test_sites[index].id == siteId)
                {
                    selectedSite = scenario.test_sites[index];
                    selectedSiteIndex = index;
                    notebookOpen = true;
                    SetStatus(SelectedReadingRevealed
                        ? $"{selectedSite.label} selected. Its recorded prediction allows the reading to be reviewed."
                        : $"{selectedSite.label} selected. Predict the reading before revealing it.");
                    RefreshInterface();
                    return;
                }
            }
        }

        public bool PredictSelectedSite(int choiceIndex)
        {
            if (selectedSite == null || selectedSiteIndex < 0 ||
                episodeSession?.Progress == null)
            {
                return false;
            }

            string[] ids = SaltLineNarrative.PredictionIds(selectedSiteIndex);
            if (choiceIndex < 0 || choiceIndex >= ids.Length)
            {
                return false;
            }

            bool recorded = episodeSession.Progress.RecordPrediction(
                selectedSite.id,
                ids[choiceIndex]);
            if (recorded)
            {
                SetStatus(SaltLineNarrative.AfterReading);
                RefreshInterface();
            }
            return recorded;
        }

        public bool CollectSelectedSample()
        {
            if (selectedSite == null || notebookSession?.Notebook == null)
            {
                return false;
            }
            if (!SelectedReadingRevealed)
            {
                SetStatus("Make a prediction before collecting this sample.");
                RefreshInterface();
                return false;
            }

            bool newlyRecorded = notebookSession.Notebook.Record(selectedSite);
            SetStatus(newlyRecorded
                ? $"Recorded {selectedSite.label}."
                : $"{selectedSite.label} is already recorded.");
            RefreshInterface();
            return newlyRecorded;
        }

        public void ShowInterviewActivity()
        {
            EnterInterviewPresentation();
            RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Interviews);
        }

        /// <summary>Preserves marker IDs/colliders while removing investigation-only geometry and UI.</summary>
        public void EnterInterviewPresentation()
        {
            if (markerRoot != null)
            {
                foreach (MeshRenderer renderer in markerRoot.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled = false;
                foreach (TextMesh label in markerRoot.GetComponentsInChildren<TextMesh>(true)) label.gameObject.SetActive(false);
            }
            if (runtimeCanvas != null) runtimeCanvas.enabled = false;
        }

        public void ConfigureEndpointsForTesting(string editorBaseUrl, string webBaseUrl)
        {
            editorApiBaseUrl = editorBaseUrl;
            webApiBaseUrl = webBaseUrl;
        }

        public void ConfigurePresentation(
            bool createUi,
            bool createMarkers)
        {
            createRuntimeUi = createUi;
            createRuntimeMarkers = createMarkers;
        }

        private void CreateMarkers()
        {
            markerRoot = new GameObject("TestSiteMarkers");
            markerRoot.transform.SetParent(transform, false);

            if (FindFirstObjectByType<MekongEnvironmentController>() == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "InvestigationGround";
                ground.transform.SetParent(markerRoot.transform, false);
                ground.transform.localScale = new Vector3(1.2f, 1f, 0.8f);
                SetGray(ground, new Color(0.32f, 0.32f, 0.32f));
            }

            for (int index = 0; index < scenario.test_sites.Length; index++)
            {
                TestSiteDto site = scenario.test_sites[index];
                GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                markerObject.name = $"TestSiteMarker_{site.id}";
                markerObject.transform.SetParent(markerRoot.transform, false);
                markerObject.transform.localPosition = new Vector3(
                    (index - ((scenario.test_sites.Length - 1) * 0.5f)) * 3f,
                    0.5f,
                    0f);
                markerObject.transform.localScale = Vector3.one;
                SetGray(markerObject, new Color(0.58f, 0.58f, 0.58f));

                TestSiteMarker marker = markerObject.AddComponent<TestSiteMarker>();
                marker.Configure(site.id);
                markers.Add(marker);
                CreateMarkerLabel(markerObject.transform, site.label);
            }
        }

        private void CreateMarkerLabel(Transform markerTransform, string label)
        {
            var labelObject = new GameObject("SiteLabel", typeof(TextMesh));
            labelObject.transform.SetParent(markerTransform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1f, 0f);

            TextMesh textMesh = labelObject.GetComponent<TextMesh>();
            textMesh.text = label;
            textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textMesh.fontSize = 38;
            textMesh.characterSize = 0.08f;
            textMesh.anchor = TextAnchor.LowerCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
        }

        private void CreateInterface()
        {
            var canvasObject = new GameObject(
                "InvestigationCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeCanvas = canvas;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            EnsureInputSystemEventSystem();

            Text title = CreateText(canvas.transform, "ScenarioTitle", 28, TextAnchor.UpperLeft);
            Stretch(title.rectTransform, new Vector2(0.03f, 0.93f), new Vector2(0.97f, 0.98f));
            title.text = scenario.title;

            investigationStage = new GameObject("InvestigationStage", typeof(RectTransform));
            investigationStage.transform.SetParent(canvas.transform, false);
            RectTransform stageRect = investigationStage.GetComponent<RectTransform>();
            Stretch(stageRect, Vector2.zero, Vector2.one);

            Image selectionPanel = CreatePanel(investigationStage.transform, "ReadingPanel");
            readingPanelObject = selectionPanel.gameObject;
            Stretch(selectionPanel.rectTransform, new Vector2(0.03f, 0.18f), new Vector2(0.62f, 0.82f));
            selectionText = CreateText(selectionPanel.transform, "SelectedReading", 17, TextAnchor.UpperLeft);
            Stretch(selectionText.rectTransform, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.94f));
            collectButton = CreateButton(selectionPanel.transform, "CollectSampleButton", "Collect sample");
            Stretch(collectButton.GetComponent<RectTransform>(), new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.14f));
            collectButton.onClick.AddListener(() => CollectSelectedSample());
            for (int index = 0; index < predictionButtons.Length; index++)
            {
                int captured = index;
                predictionButtons[index] = CreateButton(
                    selectionPanel.transform,
                    "PredictionChoice" + index,
                    "Prediction");
                Stretch(
                    predictionButtons[index].GetComponent<RectTransform>(),
                    index == 0 ? new Vector2(.06f, .05f) : new Vector2(.52f, .05f),
                    index == 0 ? new Vector2(.48f, .14f) : new Vector2(.94f, .14f));
                predictionButtons[index].onClick.AddListener(
                    () => PredictSelectedSite(captured));
            }

            Image notebookPanel = CreatePanel(canvas.transform, "EvidenceNotebook");
            notebookPanelObject = notebookPanel.gameObject;
            Stretch(notebookPanel.rectTransform, new Vector2(0.66f, 0.12f), new Vector2(0.97f, 0.82f));
            notebookText = RuntimeScrollableContent.Create(notebookPanel.transform, "NotebookReadings", new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f), 15);
            notebookToggleButton = CreateButton(canvas.transform, "NotebookToggle", "Evidence notebook");
            Stretch(notebookToggleButton.GetComponent<RectTransform>(), new Vector2(.78f, .05f), new Vector2(.97f, .1f));
            notebookToggleButton.onClick.AddListener(ToggleNotebook);

            gateText = CreateText(investigationStage.transform, "InterviewGate", 20, TextAnchor.UpperLeft);
            interviewGateObject = gateText.gameObject;
            Stretch(gateText.rectTransform, new Vector2(0.03f, 0.1f), new Vector2(0.62f, 0.16f));
            RuntimePanelManager panels = RuntimePanelManager.GetOrCreate();
            panels.Register(RuntimeActivityStage.Investigation, investigationStage);
            panels.Show(RuntimeActivityStage.Investigation);
        }

        private void ToggleNotebook()
        {
            notebookOpen = !notebookOpen;
            RefreshInterface();
        }

        private static void EnsureInputSystemEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }

            BaseInputModule[] modules = eventSystem.GetComponents<BaseInputModule>();
            for (int index = 0; index < modules.Length; index++)
            {
                if (!(modules[index] is InputSystemUIInputModule))
                {
                    modules[index].enabled = false;
                }
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void RefreshInterface()
        {
            if (selectionText == null || notebookText == null || gateText == null)
            {
                return;
            }

            if (selectedSite == null)
            {
                selectionText.text = "Selected site\n\nClick a gray marker to select a test site.";
                collectButton.interactable = false;
            }
            else
            {
                if (SelectedReadingRevealed)
                {
                    selectionText.text =
                        FormatSiteReading(selectedSite) +
                        "\n\nYour prediction: " +
                        PredictionLabelForSelectedSite();
                    collectButton.interactable =
                        !notebookSession.Notebook.HasRecorded(selectedSite.id);
                }
                else
                {
                    selectionText.text =
                        "Predict before revealing the reading\n\n" +
                        SaltLineNarrative.PredictionPrompt(selectedSiteIndex);
                }
            }

            readingPanelObject.SetActive(selectedSite != null);
            collectButton.gameObject.SetActive(
                selectedSite != null && SelectedReadingRevealed);
            string[] predictionLabels =
                SaltLineNarrative.PredictionLabels(selectedSiteIndex);
            for (int index = 0; index < predictionButtons.Length; index++)
            {
                bool visible = selectedSite != null && !SelectedReadingRevealed;
                predictionButtons[index].gameObject.SetActive(visible);
                if (index < predictionLabels.Length)
                {
                    predictionButtons[index].GetComponentInChildren<Text>().text =
                        predictionLabels[index];
                }
            }
            notebookPanelObject.SetActive(notebookOpen);
            notebookToggleButton.GetComponentInChildren<Text>().text = notebookOpen ? "Hide notebook" : "Evidence notebook";
            RuntimeScrollableContent.SetText(notebookText, FormatNotebook());
            gateText.text = InterviewsUnlocked
                ? "Interviews unlocked — all test sites are recorded."
                : $"Interviews locked — {RecordedReadingCount}/{scenario.test_sites.Length} test sites recorded.";
        }

        private string FormatSiteReading(TestSiteDto site)
        {
            string unit = scenario.units == null ? string.Empty : scenario.units.salinity;
            string sourceIds = site.measurement_grounding?.source_ids == null
                ? string.Empty
                : string.Join(", ", site.measurement_grounding.source_ids);
            return $"Selected site: {site.label}\n\n" +
                   $"Salinity: {site.salinity_gL:0.##} {unit}\n" +
                   $"Sample season: {site.season}\n" +
                   $"Salt pattern: {Humanize(site.seasonal_pattern)}\n" +
                   $"Freshwater: {site.freshwater_access}\n\n" +
                   $"{site.note}\n\n" +
                   $"Source IDs: {sourceIds}";
        }

        private string FormatNotebook()
        {
            var text = new StringBuilder("Evidence Notebook\n\n");
            if (notebookSession?.Notebook == null || notebookSession.Notebook.RecordedReadings.Count == 0)
            {
                text.Append("No samples recorded yet.");
                return text.ToString();
            }

            IReadOnlyList<RecordedReading> readings = notebookSession.Notebook.RecordedReadings;
            string unit = scenario.units == null ? string.Empty : scenario.units.salinity;
            for (int index = 0; index < readings.Count; index++)
            {
                RecordedReading reading = readings[index];
                text.Append(reading.label).Append('\n')
                    .Append("Salinity: ").Append(reading.salinity_gL.ToString("0.##"))
                    .Append(' ').Append(unit).Append('\n')
                    .Append("Sample season: ").Append(reading.season).Append('\n')
                    .Append("Salt pattern: ").Append(Humanize(reading.seasonal_pattern)).Append('\n')
                    .Append("Freshwater: ").Append(reading.freshwater_access).Append('\n')
                    .Append(reading.note).Append('\n')
                    .Append("Source IDs: ").Append(string.Join(", ", reading.source_ids));

                if (index < readings.Count - 1)
                {
                    text.Append("\n\n");
                }
            }

            return text.ToString();
        }

        private string PredictionLabelForSelectedSite()
        {
            if (selectedSite == null || episodeSession?.Progress == null)
            {
                return string.Empty;
            }

            string predictionId =
                episodeSession.Progress.PredictionFor(selectedSite.id);
            string[] ids = SaltLineNarrative.PredictionIds(selectedSiteIndex);
            string[] labels =
                SaltLineNarrative.PredictionLabels(selectedSiteIndex);
            for (int index = 0; index < ids.Length && index < labels.Length; index++)
            {
                if (ids[index] == predictionId)
                {
                    return labels[index];
                }
            }
            return predictionId;
        }

        private static Image CreatePanel(Transform parent, string name)
        {
            var panelObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            Image panel = panelObject.GetComponent<Image>();
            panel.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            panel.raycastTarget = false;
            return panel;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.38f, 0.38f, 0.38f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText(buttonObject.transform, "Label", 18, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetGray(GameObject primitive, Color color)
        {
            MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                RuntimePrimitiveMaterial.Apply(renderer, color);
            }
        }

        private void SetStatus(string message)
        {
            if (createRuntimeUi)
            {
                RuntimePanelManager.GetOrCreate().SetInstruction(message);
            }
        }

        private void Fail(string studentMessage, string diagnostic)
        {
            LoadState = InvestigationLoadState.Failed;
            SetStatus(studentMessage);
            Debug.LogError(diagnostic, this);
        }

        private static string Humanize(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace('_', ' ');
        }

        private static bool IsWebBuild
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
    }
}
