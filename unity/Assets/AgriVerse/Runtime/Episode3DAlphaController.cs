using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    public enum Episode3DAlphaState
    {
        Loading,
        Intro,
        Exploring,
        Predicting,
        ReadyToCollect,
        Sampling,
        Reading,
        Complete,
        Failed
    }

    /// <summary>
    /// Gate-B presentation adapter for one scenario-driven investigation. It delegates
    /// scenario loading, predictions, and notebook recording to InvestigationController.
    /// </summary>
    public sealed class Episode3DAlphaController : MonoBehaviour
    {
        [SerializeField] private InvestigationController investigation;
        [SerializeField] private FirstPersonWalker walker;
        [SerializeField] private WaterSampleHotspot hotspot;
        [SerializeField] private string configuredSiteId = string.Empty;
        [SerializeField] private Animator maiAnimator;
        [SerializeField] private GameObject sampleVial;
        [SerializeField] private GameObject sampleFill;
        [SerializeField] private AudioClip waterScoop;
        [SerializeField] private AudioClip vialHandle;
        [SerializeField] private AudioClip vialCap;
        [SerializeField] private Vector3 captureApproachPosition;
        [SerializeField] private float captureApproachHeading;

        private CanvasGroup fade;
        private Text objectiveText;
        private Text progressText;
        private Text reticleText;
        private Text promptText;
        private GameObject dialoguePanel;
        private Text dialogueText;
        private Text dialogueHint;
        private GameObject readingPanel;
        private Text readingText;
        private GameObject notebookPanel;
        private Text notebookText;
        private Text controlsText;
        private AudioSource oneShotAudio;
        private TestSiteDto activeSite;
        private int activeSiteIndex = -1;
        private int introPage;
        private bool notebookOpen;
        private Vector3 vialRestPosition;
        private Quaternion vialRestRotation;
        private Coroutine sampleRoutine;

        public Episode3DAlphaState State { get; private set; } =
            Episode3DAlphaState.Loading;
        public TestSiteDto ActiveSite => activeSite;
        public string ConfiguredSiteId => configuredSiteId;
        public bool SampleRecorded =>
            investigation != null &&
            investigation.RecordedReadingCount > 0;
        public string ReadingTextForTesting =>
            readingText == null ? string.Empty : readingText.text;
        public bool NotebookOpenForTesting => notebookOpen;
        public bool ReadingPanelVisibleForTesting =>
            readingPanel != null && readingPanel.activeSelf;
        public bool DialogueVisibleForTesting =>
            dialoguePanel != null && dialoguePanel.activeSelf;
        public string ObjectiveTextForTesting =>
            objectiveText == null ? string.Empty : objectiveText.text;
        public int SampleAudioClipCount =>
            (waterScoop != null ? 1 : 0) +
            (vialHandle != null ? 1 : 0) +
            (vialCap != null ? 1 : 0);

        public void Configure(
            InvestigationController sourceInvestigation,
            FirstPersonWalker sourceWalker,
            WaterSampleHotspot sourceHotspot,
            string siteId,
            Animator sourceMaiAnimator,
            GameObject sourceVial,
            GameObject sourceFill,
            Vector3 approachPosition,
            float approachHeading,
            AudioClip scoop,
            AudioClip handle,
            AudioClip cap)
        {
            investigation = sourceInvestigation;
            walker = sourceWalker;
            hotspot = sourceHotspot;
            configuredSiteId = siteId ?? string.Empty;
            maiAnimator = sourceMaiAnimator;
            sampleVial = sourceVial;
            sampleFill = sourceFill;
            captureApproachPosition = approachPosition;
            captureApproachHeading = approachHeading;
            waterScoop = scoop;
            vialHandle = handle;
            vialCap = cap;
        }

        private void Awake()
        {
            Application.runInBackground = true;
            BuildInterface();
            oneShotAudio = gameObject.AddComponent<AudioSource>();
            oneShotAudio.playOnAwake = false;
            oneShotAudio.spatialBlend = 0f;
            oneShotAudio.volume = .72f;
            if (sampleVial != null)
            {
                vialRestPosition = sampleVial.transform.localPosition;
                vialRestRotation = sampleVial.transform.localRotation;
                sampleVial.SetActive(false);
            }
            if (sampleFill != null)
            {
                sampleFill.SetActive(false);
            }
        }

        private IEnumerator Start()
        {
            SetMovement(false);
            SetObjective("Loading the field mission…");
            while (investigation != null &&
                   (investigation.LoadState ==
                        InvestigationLoadState.NotStarted ||
                    investigation.LoadState ==
                        InvestigationLoadState.Loading))
            {
                yield return null;
            }

            if (investigation == null ||
                investigation.LoadState != InvestigationLoadState.Ready ||
                investigation.Scenario?.test_sites == null ||
                investigation.Scenario.test_sites.Length == 0)
            {
                Fail("The field scenario could not be loaded.");
                yield break;
            }

            for (int index = 0;
                 index < investigation.Scenario.test_sites.Length;
                 index++)
            {
                if (string.Equals(
                        investigation.Scenario.test_sites[index].id,
                        configuredSiteId,
                        StringComparison.Ordinal))
                {
                    activeSite =
                        investigation.Scenario.test_sites[index];
                    activeSiteIndex = index;
                    break;
                }
            }
            if (activeSite == null)
            {
                Fail(
                    "The configured field site is not available in this scenario.");
                yield break;
            }
            hotspot.Bind(activeSite.id);
            RefreshProgress();
            yield return FadeFromBlack(2.2f);
            BeginIntro();

            string captureDirectory =
                CaptureDirectoryFromArguments(
                    Environment.GetCommandLineArgs());
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                StartCoroutine(CaptureEvidence(captureDirectory));
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.nKey.wasPressedThisFrame &&
                State != Episode3DAlphaState.Loading &&
                State != Episode3DAlphaState.Failed)
            {
                ToggleNotebook();
            }
            if (notebookOpen) return;

            if (State == Episode3DAlphaState.Intro)
            {
                if (keyboard.eKey.wasPressedThisFrame ||
                    keyboard.enterKey.wasPressedThisFrame)
                {
                    AdvanceIntro();
                }
                return;
            }

            if (State == Episode3DAlphaState.Exploring ||
                State == Episode3DAlphaState.Complete)
            {
                UpdateHotspotFocus();
                if (hotspot.IsFocused &&
                    keyboard.eKey.wasPressedThisFrame)
                {
                    BeginSiteInteraction();
                }
                return;
            }

            if (State == Episode3DAlphaState.Predicting)
            {
                if (keyboard.digit1Key.wasPressedThisFrame)
                {
                    ChoosePrediction(0);
                }
                else if (keyboard.digit2Key.wasPressedThisFrame)
                {
                    ChoosePrediction(1);
                }
                return;
            }

            if (State == Episode3DAlphaState.ReadyToCollect &&
                keyboard.eKey.wasPressedThisFrame)
            {
                BeginCollection();
                return;
            }

            if (State == Episode3DAlphaState.Reading &&
                keyboard.eKey.wasPressedThisFrame)
            {
                CloseReading();
            }
        }

        public void AdvanceIntro()
        {
            if (State != Episode3DAlphaState.Intro) return;
            if (introPage == 0)
            {
                introPage = 1;
                dialogueText.text = SaltLineNarrative.InvestigationIntro;
                dialogueHint.text = "Press E to begin the field walk";
                PlayMai("Mai_Talk");
                return;
            }

            dialoguePanel.SetActive(false);
            State = Episode3DAlphaState.Exploring;
            SetMovement(true);
            PlayMai("Mai_Idle");
            SetObjective(
                "Walk beside the canal and find the first water-sampling point.");
            promptText.text = string.Empty;
        }

        public bool BeginSiteInteraction()
        {
            if ((State != Episode3DAlphaState.Exploring &&
                 State != Episode3DAlphaState.Complete) ||
                activeSite == null)
            {
                return false;
            }

            investigation.SelectSite(activeSite.id);
            if (investigation.SelectedReadingRevealed)
            {
                State = Episode3DAlphaState.ReadyToCollect;
                ShowCollectPrompt();
            }
            else
            {
                State = Episode3DAlphaState.Predicting;
                ShowPredictionPrompt();
            }
            hotspot.SetFocused(true);
            SetMovement(false);
            return true;
        }

        public bool ChoosePrediction(int choiceIndex)
        {
            if (State != Episode3DAlphaState.Predicting ||
                !investigation.PredictSelectedSite(choiceIndex))
            {
                return false;
            }

            State = Episode3DAlphaState.ReadyToCollect;
            ShowCollectPrompt();
            return true;
        }

        public bool BeginCollection()
        {
            if (State != Episode3DAlphaState.ReadyToCollect ||
                sampleRoutine != null)
            {
                return false;
            }

            sampleRoutine = StartCoroutine(CollectSample());
            return true;
        }

        public void ToggleNotebook()
        {
            notebookOpen = !notebookOpen;
            notebookPanel.SetActive(notebookOpen);
            if (notebookOpen)
            {
                notebookText.text = FormatNotebook();
                SetObjective("Evidence notebook · Press N to close");
                dialoguePanel.SetActive(false);
                readingPanel.SetActive(false);
                promptText.gameObject.SetActive(false);
                reticleText.gameObject.SetActive(false);
            }
            else
            {
                if (State == Episode3DAlphaState.Reading)
                {
                    SetObjective(
                        "Review the reading, then continue to the next test.");
                }
                else if (State == Episode3DAlphaState.Complete)
                {
                    SetObjective(
                        "First sample recorded. Continue along the canal to the next test.");
                }
                promptText.gameObject.SetActive(true);
                reticleText.gameObject.SetActive(true);
                bool showReading =
                    State == Episode3DAlphaState.Reading;
                readingPanel.SetActive(showReading);
                dialoguePanel.SetActive(
                    showReading ||
                    State == Episode3DAlphaState.Intro ||
                    State == Episode3DAlphaState.Predicting ||
                    State == Episode3DAlphaState.ReadyToCollect ||
                    State == Episode3DAlphaState.Sampling);
            }
        }

        private void BeginIntro()
        {
            State = Episode3DAlphaState.Intro;
            introPage = 0;
            dialoguePanel.SetActive(true);
            dialogueText.text = SaltLineNarrative.Arrival(PlayerName());
            dialogueHint.text = "Press E to continue";
            PlayMai("Mai_Wave");
            SetObjective("Listen to Mai at the canal field base.");
        }

        private void UpdateHotspotFocus()
        {
            if (walker?.ViewCamera == null || hotspot == null) return;
            Camera camera = walker.ViewCamera;
            Ray ray = camera.ViewportPointToRay(
                new Vector3(.5f, .5f, 0f));
            int mask = 1 << hotspot.gameObject.layer;
            bool focused =
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    hotspot.InteractionRange,
                    mask,
                    QueryTriggerInteraction.Collide) &&
                (hit.collider.gameObject == hotspot.gameObject ||
                 hit.collider.transform.IsChildOf(hotspot.transform));
            hotspot.SetFocused(focused);
            reticleText.color = focused
                ? new Color(1f, .74f, .30f, 1f)
                : new Color(.94f, .94f, .88f, .68f);
            promptText.text = focused
                ? $"{activeSite.label}\nPress E to inspect the water"
                : string.Empty;
        }

        private void ShowPredictionPrompt()
        {
            string[] labels =
                SaltLineNarrative.PredictionLabels(activeSiteIndex);
            dialoguePanel.SetActive(true);
            dialogueText.text =
                SaltLineNarrative.PredictionPrompt(activeSiteIndex);
            dialogueHint.text =
                $"1  {labels[0]}     2  {labels[1]}";
            SetObjective("Predict the reading before testing the water.");
            PlayMai("Mai_Idle");
        }

        private void ShowCollectPrompt()
        {
            dialoguePanel.SetActive(true);
            dialogueText.text =
                "Prediction recorded. The measurement is still hidden.";
            dialogueHint.text = "Press E to collect the water sample";
            SetObjective("Collect the sample to reveal the field reading.");
        }

        private IEnumerator CollectSample()
        {
            State = Episode3DAlphaState.Sampling;
            dialoguePanel.SetActive(true);
            dialogueText.text = "Collecting a physical water sample…";
            dialogueHint.text = string.Empty;
            PlayClip(vialHandle);
            if (sampleVial != null)
            {
                sampleVial.SetActive(true);
                sampleVial.transform.localPosition = vialRestPosition;
                sampleVial.transform.localRotation = vialRestRotation;
            }
            yield return AnimateVial(
                vialRestPosition,
                vialRestPosition + new Vector3(-.10f, -.19f, .08f),
                .55f);
            PlayClip(waterScoop);
            if (sampleFill != null) sampleFill.SetActive(true);
            yield return new WaitForSecondsRealtime(.42f);
            PlayClip(vialCap);
            yield return AnimateVial(
                sampleVial == null
                    ? vialRestPosition
                    : sampleVial.transform.localPosition,
                vialRestPosition,
                .5f);

            bool recorded = investigation.CollectSelectedSample();
            if (!recorded && investigation.RecordedReadingCount == 0)
            {
                sampleRoutine = null;
                Fail("The sample could not be recorded.");
                yield break;
            }

            if (sampleVial != null) sampleVial.SetActive(false);
            State = Episode3DAlphaState.Reading;
            readingPanel.SetActive(true);
            readingText.text = FormatReading(activeSite);
            dialoguePanel.SetActive(true);
            dialogueText.text = SaltLineNarrative.AfterReading;
            dialogueHint.text =
                "Press E to continue · Press N for the Evidence Notebook";
            SetObjective("Review the reading, then continue to the next test.");
            RefreshProgress();
            PlayMai("Mai_Talk");
            sampleRoutine = null;
        }

        private IEnumerator AnimateVial(
            Vector3 from,
            Vector3 to,
            float duration)
        {
            if (sampleVial == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float amount = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / duration));
                sampleVial.transform.localPosition =
                    Vector3.LerpUnclamped(from, to, amount);
                sampleVial.transform.localRotation =
                    vialRestRotation *
                    Quaternion.Euler(
                        Mathf.Lerp(0f, -24f, amount),
                        0f,
                        Mathf.Lerp(0f, 8f, amount));
                yield return null;
            }
            sampleVial.transform.localPosition = to;
        }

        private void CloseReading()
        {
            State = Episode3DAlphaState.Complete;
            readingPanel.SetActive(false);
            dialoguePanel.SetActive(false);
            hotspot.SetFocused(false);
            SetMovement(true);
            PlayMai("Mai_Idle");
            SetObjective(
                "First sample recorded. Continue along the canal to the next test.");
        }

        private void RefreshProgress()
        {
            int total =
                investigation?.Scenario?.test_sites?.Length ?? 0;
            int recorded =
                investigation?.RecordedReadingCount ?? 0;
            progressText.text =
                $"EVIDENCE  {recorded}/{total}     FIELD WALK";
        }

        private string FormatReading(TestSiteDto site)
        {
            ScenarioDto scenario = investigation.Scenario;
            string unit = scenario.units?.salinity ?? string.Empty;
            string sources =
                site.measurement_grounding?.source_ids == null
                    ? string.Empty
                    : string.Join(
                        ", ",
                        site.measurement_grounding.source_ids);
            return
                $"{site.label}\n\n" +
                $"SALINITY  {site.salinity_gL.ToString("0.##", CultureInfo.InvariantCulture)} {unit}\n" +
                $"SEASON  {site.season}\n" +
                $"SALT PATTERN  {Humanize(site.seasonal_pattern)}\n" +
                $"FRESHWATER ACCESS  {site.freshwater_access}\n\n" +
                $"{site.note}\n\n" +
                $"SOURCE IDs  {sources}";
        }

        private string FormatNotebook()
        {
            EvidenceNotebook notebook =
                EvidenceNotebookSession.GetOrCreate().Notebook;
            var text = new StringBuilder("EVIDENCE NOTEBOOK\n\n");
            if (notebook == null ||
                notebook.RecordedReadings.Count == 0)
            {
                return text.Append(
                    "No field readings recorded yet.").ToString();
            }

            string unit =
                investigation.Scenario.units?.salinity ??
                string.Empty;
            foreach (RecordedReading reading in
                     notebook.RecordedReadings)
            {
                text.Append(reading.label).Append('\n')
                    .Append("Salinity  ")
                    .Append(
                        reading.salinity_gL.ToString(
                            "0.##",
                            CultureInfo.InvariantCulture))
                    .Append(' ').Append(unit).Append('\n')
                    .Append("Season  ").Append(reading.season).Append('\n')
                    .Append("Salt pattern  ")
                    .Append(Humanize(reading.seasonal_pattern)).Append('\n')
                    .Append("Freshwater access  ")
                    .Append(reading.freshwater_access).Append('\n')
                    .Append(reading.note).Append('\n')
                    .Append("Source IDs  ")
                    .Append(string.Join(", ", reading.source_ids))
                    .Append("\n\n");
            }
            return text.ToString().TrimEnd();
        }

        private string PlayerName()
        {
            EpisodeSession session = EpisodeSession.GetOrCreate();
            string playerName = session.Progress?.PlayerName;
            return string.IsNullOrWhiteSpace(playerName)
                ? "Advisor"
                : playerName;
        }

        private void SetMovement(bool enabled)
        {
            if (walker != null)
            {
                walker.SetMovementEnabled(enabled);
            }
        }

        private void PlayMai(string stateName)
        {
            if (maiAnimator == null ||
                string.IsNullOrWhiteSpace(stateName))
            {
                return;
            }
            maiAnimator.CrossFadeInFixedTime(stateName, .18f);
        }

        private void PlayClip(AudioClip clip)
        {
            if (clip != null && oneShotAudio != null)
            {
                oneShotAudio.PlayOneShot(clip);
            }
        }

        private IEnumerator FadeFromBlack(float duration)
        {
            if (fade == null) yield break;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fade.alpha = 1f -
                             Mathf.SmoothStep(
                                 0f,
                                 1f,
                                 Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            fade.alpha = 0f;
            fade.blocksRaycasts = false;
        }

        private void Fail(string message)
        {
            State = Episode3DAlphaState.Failed;
            SetMovement(false);
            dialoguePanel.SetActive(true);
            dialogueText.text = message;
            dialogueHint.text =
                "Confirm the Express backend is running, then relaunch.";
            SetObjective("Field mission unavailable");
        }

        private void BuildInterface()
        {
            Font font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject(
                "Episode3DAlphaHUD",
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            Image objective = Panel(
                canvas.transform,
                "Objective",
                new Vector2(.025f, .90f),
                new Vector2(.55f, .972f),
                new Color(.025f, .15f, .16f, .86f));
            objectiveText = Label(
                objective.transform,
                font,
                18,
                TextAnchor.MiddleLeft,
                new Vector2(.035f, .08f),
                new Vector2(.965f, .92f));

            Image progress = Panel(
                canvas.transform,
                "Progress",
                new Vector2(.705f, .91f),
                new Vector2(.975f, .968f),
                new Color(.025f, .15f, .16f, .80f));
            progressText = Label(
                progress.transform,
                font,
                15,
                TextAnchor.MiddleCenter,
                new Vector2(.04f, .08f),
                new Vector2(.96f, .92f));
            progressText.color =
                new Color(1f, .78f, .34f, 1f);

            reticleText = Label(
                canvas.transform,
                font,
                24,
                TextAnchor.MiddleCenter,
                new Vector2(.487f, .475f),
                new Vector2(.513f, .525f));
            reticleText.text = "+";
            reticleText.color =
                new Color(.94f, .94f, .88f, .68f);

            promptText = Label(
                canvas.transform,
                font,
                16,
                TextAnchor.MiddleCenter,
                new Vector2(.31f, .39f),
                new Vector2(.69f, .47f));
            promptText.color =
                new Color(1f, .91f, .72f, 1f);

            Image dialogue = Panel(
                canvas.transform,
                "MaiDialogue",
                new Vector2(.12f, .055f),
                new Vector2(.88f, .245f),
                new Color(.018f, .11f, .12f, .91f));
            dialoguePanel = dialogue.gameObject;
            Text identity = Label(
                dialogue.transform,
                font,
                14,
                TextAnchor.UpperLeft,
                new Vector2(.035f, .74f),
                new Vector2(.965f, .94f));
            identity.text = "MAI  ·  FIELD COORDINATOR";
            identity.color =
                new Color(1f, .72f, .28f, 1f);
            dialogueText = Label(
                dialogue.transform,
                font,
                18,
                TextAnchor.UpperLeft,
                new Vector2(.035f, .25f),
                new Vector2(.965f, .76f));
            dialogueHint = Label(
                dialogue.transform,
                font,
                15,
                TextAnchor.MiddleRight,
                new Vector2(.035f, .035f),
                new Vector2(.965f, .25f));
            dialogueHint.color =
                new Color(1f, .80f, .43f, 1f);
            dialoguePanel.SetActive(false);

            Image reading = Panel(
                canvas.transform,
                "FieldReading",
                new Vector2(.035f, .28f),
                new Vector2(.47f, .86f),
                new Color(.018f, .11f, .12f, .94f));
            readingPanel = reading.gameObject;
            readingText = Label(
                reading.transform,
                font,
                17,
                TextAnchor.UpperLeft,
                new Vector2(.06f, .06f),
                new Vector2(.94f, .94f));
            readingPanel.SetActive(false);

            Image notebook = Panel(
                canvas.transform,
                "EvidenceDrawer",
                new Vector2(.50f, .18f),
                new Vector2(.965f, .86f),
                new Color(.018f, .11f, .12f, .97f));
            notebookPanel = notebook.gameObject;
            notebookText = Label(
                notebook.transform,
                font,
                16,
                TextAnchor.UpperLeft,
                new Vector2(.06f, .06f),
                new Vector2(.94f, .94f));
            notebookPanel.SetActive(false);

            controlsText = Label(
                canvas.transform,
                font,
                14,
                TextAnchor.MiddleCenter,
                new Vector2(.20f, .008f),
                new Vector2(.80f, .045f));
            controlsText.text =
                "WASD MOVE   ·   MOUSE LOOK   ·   E INTERACT   ·   N EVIDENCE   ·   ESC RELEASE CURSOR";
            controlsText.color =
                new Color(.93f, .92f, .84f, .78f);

            Image fadeImage = Panel(
                canvas.transform,
                "WakeFade",
                Vector2.zero,
                Vector2.one,
                Color.black);
            fade = fadeImage.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 1f;
            fade.blocksRaycasts = true;
        }

        private void SetObjective(string value)
        {
            if (objectiveText != null)
            {
                objectiveText.text = value ?? string.Empty;
            }
        }

        private static Image Panel(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            GameObject item = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            Stretch(image.rectTransform, anchorMin, anchorMax);
            return image;
        }

        private static Text Label(
            Transform parent,
            Font font,
            int size,
            TextAnchor alignment,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject item = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            item.transform.SetParent(parent, false);
            Text text = item.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(.96f, .95f, .89f, 1f);
            text.raycastTarget = false;
            Stretch(text.rectTransform, anchorMin, anchorMax);
            return text;
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string Humanize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace('_', ' ');

        private static string CaptureDirectoryFromArguments(
            string[] args)
        {
            if (args == null) return null;
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(
                        args[index],
                        "-agriverse-alpha-capture-dir",
                        StringComparison.Ordinal))
                {
                    return args[index + 1];
                }
            }
            return null;
        }

        private IEnumerator CaptureEvidence(string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture(directory, "01_arrival.png");
            AdvanceIntro();
            AdvanceIntro();
            yield return new WaitForSecondsRealtime(.6f);
            walker.Teleport(
                captureApproachPosition,
                captureApproachHeading,
                -3f);
            yield return new WaitForSecondsRealtime(.8f);
            yield return Capture(directory, "02_canal_walk.png");
            BeginSiteInteraction();
            yield return new WaitForSecondsRealtime(.35f);
            yield return Capture(directory, "03_prediction.png");
            ChoosePrediction(0);
            BeginCollection();
            while (State == Episode3DAlphaState.Sampling)
            {
                yield return null;
            }
            yield return new WaitForSecondsRealtime(.4f);
            yield return Capture(directory, "04_reading.png");
            ToggleNotebook();
            yield return new WaitForSecondsRealtime(.35f);
            yield return Capture(directory, "05_evidence.png");
            yield return new WaitForSecondsRealtime(.4f);
            Application.Quit(0);
        }

        private static IEnumerator Capture(
            string directory,
            string filename)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(
                Path.Combine(directory, filename));
            yield return new WaitForSecondsRealtime(.3f);
        }
    }
}
