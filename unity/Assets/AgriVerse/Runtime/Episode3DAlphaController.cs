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
    /// First-person presentation adapter for scenario-driven investigations. It delegates
    /// scenario loading, predictions, and notebook recording to InvestigationController.
    /// </summary>
    public sealed class Episode3DAlphaController : MonoBehaviour
    {
        [SerializeField] private InvestigationController investigation;
        [SerializeField] private FirstPersonWalker walker;
        [SerializeField] private WaterSampleHotspot[] hotspots =
            Array.Empty<WaterSampleHotspot>();
        [SerializeField] private string[] configuredSiteIds =
            Array.Empty<string>();
        [SerializeField] private StakeholderHotspot[]
            stakeholderHotspots =
                Array.Empty<StakeholderHotspot>();
        [SerializeField] private string[] configuredStakeholderIds =
            Array.Empty<string>();
        [SerializeField] private PlanningHotspot planningHotspot;
        [SerializeField] private Animator maiAnimator;
        [SerializeField] private GameObject sampleVial;
        [SerializeField] private GameObject sampleFill;
        [SerializeField] private AudioClip waterScoop;
        [SerializeField] private AudioClip vialHandle;
        [SerializeField] private AudioClip vialCap;
        [SerializeField] private Vector3 captureApproachPosition;
        [SerializeField] private float captureApproachHeading;

        private CanvasGroup fade;
        private GameObject hudRoot;
        private Text objectiveText;
        private Text progressText;
        private Text reticleText;
        private Text promptText;
        private GameObject dialoguePanel;
        private Text dialogueText;
        private Text dialogueHint;
        private Button[] predictionButtons =
            Array.Empty<Button>();
        private GameObject readingPanel;
        private Text readingText;
        private AtlasInstrumentGraphic salinityInstrument;
        private RectTransform evidenceRecordedStamp;
        private Coroutine readingRevealRoutine;
        private GameObject notebookPanel;
        private Text notebookText;
        private Text journalRouteStampText;
        private Button[] journalTabs = Array.Empty<Button>();
        private Text accessibilityStatus;
        private FieldJournalSection journalSection =
            FieldJournalSection.Sites;
        private Text controlsText;
        private AudioSource oneShotAudio;
        private TestSiteDto activeSite;
        private WaterSampleHotspot activeHotspot;
        private StakeholderHotspot activeStakeholderHotspot;
        private int activeSiteIndex = -1;
        private int introPage;
        private bool notebookOpen;
        private Vector3 vialRestPosition;
        private Quaternion vialRestRotation;
        private Coroutine sampleRoutine;
        private Coroutine stakeholderFocusRoutine;
        private bool worldInputWasBlocked;
        private string animatedStakeholderId = string.Empty;
        private bool animatedStakeholderBusy;
        private int animatedStakeholderReplyCount = -1;
        private bool planningHandoffActive;

        public Episode3DAlphaState State { get; private set; } =
            Episode3DAlphaState.Loading;
        public TestSiteDto ActiveSite => activeSite;
        public string ConfiguredSiteId =>
            configuredSiteIds.Length == 0
                ? string.Empty
                : configuredSiteIds[0];
        public int ConfiguredSiteCount => configuredSiteIds.Length;
        public int ConfiguredStakeholderCount =>
            configuredStakeholderIds.Length;
        public bool HasPlanningHotspot =>
            planningHotspot != null;
        public bool SampleRecorded =>
            investigation != null &&
            investigation.RecordedReadingCount > 0;
        public string ReadingTextForTesting =>
            readingText == null ? string.Empty : readingText.text;
        public bool NotebookOpenForTesting => notebookOpen;
        public FieldJournalSection JournalSectionForTesting =>
            journalSection;
        public string JournalTextForTesting =>
            notebookText == null
                ? string.Empty
                : notebookText.text;
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
            Configure(
                sourceInvestigation,
                sourceWalker,
                new[] { sourceHotspot },
                new[] { siteId ?? string.Empty },
                sourceMaiAnimator,
                sourceVial,
                sourceFill,
                approachPosition,
                approachHeading,
                scoop,
                handle,
                cap);
        }

        public void Configure(
            InvestigationController sourceInvestigation,
            FirstPersonWalker sourceWalker,
            WaterSampleHotspot[] sourceHotspots,
            string[] siteIds,
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
            hotspots =
                sourceHotspots ??
                Array.Empty<WaterSampleHotspot>();
            configuredSiteIds =
                siteIds ??
                Array.Empty<string>();
            maiAnimator = sourceMaiAnimator;
            sampleVial = sourceVial;
            sampleFill = sourceFill;
            captureApproachPosition = approachPosition;
            captureApproachHeading = approachHeading;
            waterScoop = scoop;
            vialHandle = handle;
            vialCap = cap;
        }

        public void ConfigureStakeholders(
            StakeholderHotspot[] sourceHotspots,
            string[] stakeholderIds)
        {
            stakeholderHotspots =
                sourceHotspots ??
                Array.Empty<StakeholderHotspot>();
            configuredStakeholderIds =
                stakeholderIds ??
                Array.Empty<string>();
        }

        public void ConfigurePlanning(
            PlanningHotspot sourceHotspot)
        {
            planningHotspot = sourceHotspot;
        }

        public bool BeginPlanningHandoff()
        {
            if (planningHotspot == null ||
                State != Episode3DAlphaState.Complete)
            {
                return false;
            }
            planningHandoffActive = true;
            activeStakeholderHotspot?.SetFocused(false);
            RuntimePanelManager.GetOrCreate().Clear();
            RuntimePanelManager.GetOrCreate().SetCinematicMode(false);
            AnGiangRealitySpikeController spike =
                FindAnyObjectByType<
                    AnGiangRealitySpikeController>();
            if (spike != null)
            {
                spike.SetCinematicInterviewActive(false);
            }
            SetObjective(
                "Meet at the planning table to build the community proposal.");
            SetMovement(true);
            return true;
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
                   investigation.LoadState !=
                   InvestigationLoadState.Ready)
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

            if (hotspots.Length == 0 ||
                hotspots.Length != configuredSiteIds.Length)
            {
                Fail("The field sampling stations are not configured.");
                yield break;
            }
            for (int index = 0; index < hotspots.Length; index++)
            {
                if (hotspots[index] == null ||
                    !TrySelectSite(configuredSiteIds[index]))
                {
                    Fail(
                        "A configured field site is not available in this scenario.");
                    yield break;
                }
                hotspots[index].Bind(configuredSiteIds[index]);
            }
            TrySelectSite(configuredSiteIds[0]);
            if (stakeholderHotspots.Length !=
                configuredStakeholderIds.Length)
            {
                Fail("The stakeholder meeting points are not configured.");
                yield break;
            }
            for (int index = 0;
                 index < stakeholderHotspots.Length;
                 index++)
            {
                if (stakeholderHotspots[index] == null ||
                    StakeholderById(
                        configuredStakeholderIds[index]) == null)
                {
                    Fail(
                        "A configured stakeholder is not available in this scenario.");
                    yield break;
                }
                stakeholderHotspots[index].Bind(
                    configuredStakeholderIds[index]);
            }
            RefreshProgress();

            string captureDirectory =
                CaptureDirectoryFromArguments(
                    Environment.GetCommandLineArgs());
            EpisodePresentationController presentation =
                FindFirstObjectByType<EpisodePresentationController>();
            if (presentation != null)
            {
                if (!string.IsNullOrWhiteSpace(captureDirectory))
                {
                    while (!presentation.MissionStarted)
                    {
                        if (presentation
                            .SelectFieldLocationForTesting(
                                investigation.Scenario.id))
                        {
                            presentation.BeginMissionForTesting(
                                "Field Advisor",
                                "river-teal");
                        }
                        yield return null;
                    }
                }
                else
                {
                    while (!presentation.MissionStarted)
                    {
                        yield return null;
                    }
                }
            }
            yield return FadeFromBlack(2.2f);
            BeginIntro();
            if (!string.IsNullOrWhiteSpace(captureDirectory))
            {
                StartCoroutine(CaptureEvidence(captureDirectory));
            }
        }

        private void Update()
        {
            RefreshStakeholderAnimation();
            if (WorldInputIsBlocked())
            {
                worldInputWasBlocked = true;
                SetMovement(false);
                if (hudRoot != null)
                {
                    EpisodePresentationController presentation =
                        FindFirstObjectByType<
                            EpisodePresentationController>();
                    RuntimePanelManager manager =
                        FindFirstObjectByType<RuntimePanelManager>();
                    hudRoot.SetActive(
                        (presentation == null ||
                         !presentation.LandingVisible) &&
                        (manager == null ||
                         !manager.ActiveStage.HasValue));
                }
                return;
            }
            if (worldInputWasBlocked)
            {
                worldInputWasBlocked = false;
                if (!notebookOpen &&
                    (State == Episode3DAlphaState.Exploring ||
                     State == Episode3DAlphaState.Complete))
                {
                    SetMovement(true);
                }
            }
            if (hudRoot != null) hudRoot.SetActive(true);

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.nKey.wasPressedThisFrame &&
                State != Episode3DAlphaState.Loading &&
                State != Episode3DAlphaState.Failed)
            {
                ToggleNotebook();
            }
            if (notebookOpen)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    ToggleNotebook();
                }
                return;
            }

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
                if (State == Episode3DAlphaState.Complete)
                {
                    if (planningHandoffActive)
                    {
                        UpdatePlanningFocus();
                    }
                    else
                    {
                        UpdateStakeholderFocus();
                    }
                }
                else
                {
                    UpdateHotspotFocus();
                }
                if (activeHotspot != null &&
                    State == Episode3DAlphaState.Exploring &&
                    activeHotspot.IsFocused &&
                    keyboard.eKey.wasPressedThisFrame)
                {
                    BeginSiteInteraction();
                }
                else if (planningHandoffActive &&
                         planningHotspot != null &&
                         planningHotspot.IsFocused &&
                         keyboard.eKey.wasPressedThisFrame)
                {
                    BeginPlanningAtTable();
                }
                else if (activeStakeholderHotspot != null &&
                         State == Episode3DAlphaState.Complete &&
                         activeStakeholderHotspot.IsFocused &&
                         keyboard.eKey.wasPressedThisFrame)
                {
                    BeginStakeholderInteraction();
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
                State = Episode3DAlphaState.Reading;
                readingPanel.SetActive(true);
                ShowReadingPresentation(activeSite);
                dialoguePanel.SetActive(true);
                dialogueText.text =
                    "This field reading is already recorded in the Field Journal.";
                dialogueHint.text =
                    "Press E to return · Press N for the Field Journal";
            }
            else
            {
                State = Episode3DAlphaState.Predicting;
                ShowPredictionPrompt();
            }
            activeHotspot?.SetFocused(true);
            SetMovement(false);
            return true;
        }

        public bool SelectConfiguredSiteForTesting(string siteId) =>
            TrySelectSite(siteId);

        public void ContinueAfterReadingForTesting()
        {
            CloseReading();
        }

        public void BeginFreeExploration()
        {
            notebookOpen = false;
            notebookPanel.SetActive(false);
            dialoguePanel.SetActive(false);
            readingPanel.SetActive(false);
            promptText.gameObject.SetActive(true);
            reticleText.gameObject.SetActive(true);
            SetObjective(
                "Field mission complete · Free exploration");
            SetMovement(true);
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
                RefreshJournal();
                SetMovement(false);
                SetObjective(
                    "Field Journal · Press N to close · Escape also closes");
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
                        "Investigation complete. Meet the community stakeholders.");
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
                if (State == Episode3DAlphaState.Exploring ||
                    State == Episode3DAlphaState.Complete)
                {
                    SetMovement(true);
                }
            }
        }

        public void SelectJournalSectionForTesting(
            FieldJournalSection section)
        {
            SelectJournalSection(section);
        }

        private void SelectJournalSection(
            FieldJournalSection section)
        {
            journalSection = section;
            RefreshJournal();
        }

        private void RefreshJournal()
        {
            if (notebookText == null) return;
            EvidenceNotebookSession evidenceSession =
                FindFirstObjectByType<EvidenceNotebookSession>();
            InterviewNotebookSession interviewSession =
                FindFirstObjectByType<InterviewNotebookSession>();
            PlanSession planSession =
                FindFirstObjectByType<PlanSession>();
            RuntimeScrollableContent.SetText(
                notebookText,
                FieldJournalFormatter.Format(
                    journalSection,
                    investigation?.Scenario,
                    evidenceSession?.Notebook,
                    interviewSession?.Notebook,
                    FieldJournalPlanState.From(planSession)));
            for (int index = 0;
                 index < journalTabs.Length;
                 index++)
            {
                EpisodeUiFactory.SetButtonSelected(
                    journalTabs[index],
                    index == (int)journalSection);
            }
            RefreshAccessibilityStatus();
        }

        private void RefreshAccessibilityStatus()
        {
            if (accessibilityStatus == null) return;
            accessibilityStatus.text =
                $"TEXT {EpisodeAccessibility.TextScale:0.00}×  ·  " +
                $"CONTRAST {(EpisodeAccessibility.HighContrast ? "ON" : "OFF")}  ·  " +
                $"REDUCED MOTION {(EpisodeAccessibility.ReducedMotion ? "ON" : "OFF")}";
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
            if (walker?.ViewCamera == null || hotspots.Length == 0) return;
            Camera camera = walker.ViewCamera;
            Ray ray = camera.ViewportPointToRay(
                new Vector3(.5f, .5f, 0f));
            WaterSampleHotspot focusedHotspot = null;
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                6f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            Array.Sort(
                hits,
                (left, right) =>
                    left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                WaterSampleHotspot candidate =
                    hit.collider.GetComponentInParent<
                        WaterSampleHotspot>();
                if (candidate != null &&
                    Array.IndexOf(hotspots, candidate) >= 0 &&
                    hit.distance <= candidate.InteractionRange)
                {
                    focusedHotspot = candidate;
                    break;
                }
            }
            foreach (WaterSampleHotspot candidate in hotspots)
            {
                candidate?.SetFocused(candidate == focusedHotspot);
            }
            if (focusedHotspot != null)
            {
                TrySelectSite(focusedHotspot.SiteId);
            }
            reticleText.color = focusedHotspot != null
                ? new Color(1f, .74f, .30f, 1f)
                : new Color(.94f, .94f, .88f, .68f);
            promptText.text = focusedHotspot != null
                ? $"{activeSite.label}\nPress E to inspect the water"
                : string.Empty;
        }

        private bool TrySelectSite(string siteId)
        {
            if (investigation?.Scenario?.test_sites == null)
            {
                return false;
            }
            for (int index = 0;
                 index < investigation.Scenario.test_sites.Length;
                 index++)
            {
                TestSiteDto candidate =
                    investigation.Scenario.test_sites[index];
                if (!string.Equals(
                        candidate.id,
                        siteId,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                activeSite = candidate;
                activeSiteIndex = index;
                int hotspotIndex =
                    Array.IndexOf(configuredSiteIds, siteId);
                activeHotspot =
                    hotspotIndex >= 0 &&
                    hotspotIndex < hotspots.Length
                        ? hotspots[hotspotIndex]
                        : null;
                return true;
            }
            return false;
        }

        private void UpdateStakeholderFocus()
        {
            if (walker?.ViewCamera == null ||
                stakeholderHotspots.Length == 0)
            {
                return;
            }
            Camera camera = walker.ViewCamera;
            Ray ray = camera.ViewportPointToRay(
                new Vector3(.5f, .5f, 0f));
            StakeholderHotspot focusedHotspot = null;
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                6f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Collide);
            Array.Sort(
                hits,
                (left, right) =>
                    left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
            {
                StakeholderHotspot candidate =
                    hit.collider.GetComponentInParent<
                        StakeholderHotspot>();
                if (candidate != null &&
                    Array.IndexOf(
                        stakeholderHotspots,
                        candidate) >= 0 &&
                    hit.distance <= candidate.InteractionRange)
                {
                    focusedHotspot = candidate;
                    break;
                }
            }
            foreach (StakeholderHotspot candidate in
                     stakeholderHotspots)
            {
                candidate?.SetFocused(
                    candidate == focusedHotspot);
            }
            activeStakeholderHotspot = focusedHotspot;
            StakeholderDto stakeholder =
                focusedHotspot == null
                    ? null
                    : StakeholderById(
                        focusedHotspot.StakeholderId);
            reticleText.color = focusedHotspot != null
                ? new Color(1f, .74f, .30f, 1f)
                : new Color(.94f, .94f, .88f, .68f);
            promptText.text = stakeholder == null
                ? string.Empty
                : $"{stakeholder.name} · {stakeholder.role}\n" +
                  "Press E to begin the interview";
        }

        private void UpdatePlanningFocus()
        {
            if (walker?.ViewCamera == null ||
                planningHotspot == null)
            {
                return;
            }
            Ray ray = walker.ViewCamera.ViewportPointToRay(
                new Vector3(.5f, .5f, 0f));
            bool focused = false;
            foreach (RaycastHit hit in Physics.RaycastAll(
                         ray,
                         6f,
                         Physics.DefaultRaycastLayers,
                         QueryTriggerInteraction.Collide))
            {
                PlanningHotspot candidate =
                    hit.collider.GetComponentInParent<
                        PlanningHotspot>();
                if (candidate == planningHotspot &&
                    hit.distance <=
                    planningHotspot.InteractionRange)
                {
                    focused = true;
                    break;
                }
            }
            planningHotspot.SetFocused(focused);
            reticleText.color = focused
                ? new Color(1f, .74f, .30f, 1f)
                : new Color(.94f, .94f, .88f, .68f);
            promptText.text = focused
                ? "Planning table\nPress E to build the proposal"
                : string.Empty;
        }

        private void BeginPlanningAtTable()
        {
            PlanController plan =
                FindFirstObjectByType<PlanController>();
            if (plan == null || !plan.BeginPlanning())
            {
                SetObjective(
                    "The planning table is still loading.");
                return;
            }
            planningHandoffActive = false;
            planningHotspot.SetFocused(false);
        }

        private void BeginStakeholderInteraction()
        {
            if (activeStakeholderHotspot == null ||
                stakeholderFocusRoutine != null)
            {
                return;
            }
            stakeholderFocusRoutine =
                StartCoroutine(
                    FocusAndBeginStakeholderInteraction(
                        activeStakeholderHotspot));
        }

        private IEnumerator FocusAndBeginStakeholderInteraction(
            StakeholderHotspot hotspot)
        {
            SetMovement(false);
            Vector3 target =
                hotspot.Character != null
                    ? hotspot.Character.FocusPoint
                    : hotspot.transform.position + Vector3.up * 1.5f;
            if (walker != null && walker.ViewCamera != null)
            {
                Vector3 direction =
                    target - walker.ViewCamera.transform.position;
                float targetHeading =
                    Mathf.Atan2(direction.x, direction.z) *
                    Mathf.Rad2Deg;
                float planarDistance =
                    new Vector2(direction.x, direction.z).magnitude;
                float targetPitch =
                    -Mathf.Atan2(direction.y, planarDistance) *
                    Mathf.Rad2Deg;
                float startHeading = walker.ViewHeading;
                float startPitch = walker.ViewPitch;
                float duration =
                    EpisodeAccessibility.ReducedMotion
                        ? 0f
                        : .22f;
                if (duration <= 0f)
                {
                    walker.SetViewAngles(
                        targetHeading,
                        targetPitch);
                }
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float blend = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(elapsed / duration));
                    walker.SetViewAngles(
                        Mathf.LerpAngle(
                            startHeading,
                            targetHeading,
                            blend),
                        Mathf.Lerp(
                            startPitch,
                            targetPitch,
                            blend));
                    yield return null;
                }
            }

            InterviewController interviews =
                FindFirstObjectByType<InterviewController>();
            if (interviews == null ||
                !interviews.BeginInterviews())
            {
                SetObjective(
                    "The interview station is still loading.");
                SetMovement(true);
                stakeholderFocusRoutine = null;
                yield break;
            }
            interviews.SelectStakeholder(
                hotspot.StakeholderId);
            hotspot.SetFocused(false);
            stakeholderFocusRoutine = null;
        }

        private void RefreshStakeholderAnimation()
        {
            InterviewController interviews =
                FindFirstObjectByType<InterviewController>();
            string selectedId =
                interviews != null && interviews.InterviewsVisible
                    ? interviews.SelectedStakeholderId
                    : string.Empty;
            bool isBusy =
                interviews != null &&
                interviews.InterviewsVisible &&
                interviews.IsBusy;
            int replyCount =
                CountStakeholderReplies(selectedId);
            if (string.Equals(
                    selectedId,
                    animatedStakeholderId,
                    StringComparison.Ordinal) &&
                isBusy == animatedStakeholderBusy &&
                replyCount == animatedStakeholderReplyCount)
            {
                return;
            }

            animatedStakeholderId = selectedId;
            animatedStakeholderBusy = isBusy;
            animatedStakeholderReplyCount = replyCount;
            foreach (StakeholderHotspot hotspot in
                     stakeholderHotspots)
            {
                if (hotspot?.Character == null) continue;
                bool selected = string.Equals(
                    hotspot.StakeholderId,
                    selectedId,
                    StringComparison.Ordinal);
                hotspot.Character.SetConversationState(
                    selected && isBusy,
                    selected && !isBusy && replyCount > 0);
            }
        }

        private static int CountStakeholderReplies(
            string stakeholderId)
        {
            if (string.IsNullOrWhiteSpace(stakeholderId))
            {
                return 0;
            }
            InterviewNotebook notebook =
                InterviewNotebookSession.GetOrCreate().Notebook;
            if (notebook == null) return 0;
            int count = 0;
            foreach (ConversationTurnDto turn in
                     notebook.ConversationFor(stakeholderId))
            {
                if (turn.role == "stakeholder") count++;
            }
            return count;
        }

        private StakeholderDto StakeholderById(string stakeholderId)
        {
            StakeholderDto[] stakeholders =
                investigation?.Scenario?.stakeholders;
            if (stakeholders == null) return null;
            foreach (StakeholderDto stakeholder in stakeholders)
            {
                if (stakeholder != null &&
                    string.Equals(
                        stakeholder.id,
                        stakeholderId,
                        StringComparison.Ordinal))
                {
                    return stakeholder;
                }
            }
            return null;
        }

        private bool WorldInputIsBlocked()
        {
            RuntimePanelManager manager =
                FindFirstObjectByType<RuntimePanelManager>();
            if (manager != null && manager.ActiveStage.HasValue)
            {
                return true;
            }
            EpisodePresentationController presentation =
                FindFirstObjectByType<EpisodePresentationController>();
            return presentation != null &&
                   presentation.InputBlocked;
        }

        private void ShowPredictionPrompt()
        {
            string[] labels =
                SaltLineNarrative.PredictionLabels(activeSiteIndex);
            dialoguePanel.SetActive(true);
            dialogueText.text =
                SaltLineNarrative.PredictionPrompt(activeSiteIndex);
            dialogueHint.text =
                "Choose before the field reading is revealed.";
            for (int index = 0;
                 index < predictionButtons.Length;
                 index++)
            {
                Button choice = predictionButtons[index];
                choice.gameObject.SetActive(true);
                Transform label =
                    choice.transform.Find("ChoiceLabel");
                if (label != null &&
                    label.TryGetComponent(out Text text))
                {
                    text.text = labels[index];
                }
            }
            SetObjective("Predict the reading before testing the water.");
            PlayMai("Mai_Idle");
        }

        private void ShowCollectPrompt()
        {
            HidePredictionButtons();
            dialoguePanel.SetActive(true);
            dialogueText.text =
                "Prediction recorded. The measurement is still hidden.";
            dialogueHint.text = "Press E to collect the water sample";
            SetObjective("Collect the sample to reveal the field reading.");
        }

        private void HidePredictionButtons()
        {
            foreach (Button button in predictionButtons)
            {
                button?.gameObject.SetActive(false);
            }
        }

        private IEnumerator CollectSample()
        {
            HidePredictionButtons();
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
            ShowReadingPresentation(activeSite);
            dialoguePanel.SetActive(true);
            dialogueText.text = SaltLineNarrative.AfterReading;
            dialogueHint.text =
                "Added to Field Journal · Press E to continue · Press N to open";
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
            readingPanel.SetActive(false);
            dialoguePanel.SetActive(false);
            activeHotspot?.SetFocused(false);
            bool complete =
                investigation.RecordedReadingCount >=
                investigation.Scenario.test_sites.Length;
            State = complete
                ? Episode3DAlphaState.Complete
                : Episode3DAlphaState.Exploring;
            PlayMai("Mai_Idle");
            if (complete)
            {
                SetObjective(
                    "Investigation complete. Walk to the community meeting points.");
                SetMovement(true);
            }
            else
            {
                SetMovement(true);
                SetObjective(
                    $"{investigation.RecordedReadingCount} of " +
                    $"{investigation.Scenario.test_sites.Length} samples recorded. " +
                    "Continue to the next field station.");
            }
        }

        private void RefreshProgress()
        {
            int total =
                investigation?.Scenario?.test_sites?.Length ?? 0;
            int recorded =
                investigation?.RecordedReadingCount ?? 0;
            progressText.text =
                $"EVIDENCE  {recorded}/{total}     FIELD WALK";
            if (journalRouteStampText != null)
            {
                journalRouteStampText.text =
                    $"SAMPLES  ·  {recorded}/{total}";
            }
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

        private void ShowReadingPresentation(TestSiteDto site)
        {
            if (site == null) return;
            RuntimeScrollableContent.SetText(
                readingText,
                FormatReading(site));
            float largest = Mathf.Max(site.salinity_gL, .01f);
            TestSiteDto[] sites =
                investigation?.Scenario?.test_sites;
            if (sites != null)
            {
                foreach (TestSiteDto candidate in sites)
                {
                    largest = Mathf.Max(
                        largest,
                        candidate.salinity_gL);
                }
            }
            float target =
                Mathf.Clamp01(site.salinity_gL / largest);
            if (readingRevealRoutine != null)
            {
                StopCoroutine(readingRevealRoutine);
            }
            readingRevealRoutine = StartCoroutine(
                AnimateReadingInstrument(target));
        }

        private IEnumerator AnimateReadingInstrument(float target)
        {
            if (salinityInstrument == null) yield break;
            if (EpisodeAccessibility.ReducedMotion)
            {
                salinityInstrument.SetMeasurement(target);
                if (evidenceRecordedStamp != null)
                {
                    evidenceRecordedStamp.localScale = Vector3.one;
                }
                yield break;
            }
            salinityInstrument.SetMeasurement(0f);
            if (evidenceRecordedStamp != null)
            {
                evidenceRecordedStamp.localScale =
                    Vector3.one * 1.08f;
            }
            float elapsed = 0f;
            const float duration = .62f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float amount = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed / duration));
                salinityInstrument.SetMeasurement(
                    Mathf.Lerp(0f, target, amount));
                if (evidenceRecordedStamp != null)
                {
                    evidenceRecordedStamp.localScale =
                        Vector3.one *
                        Mathf.Lerp(1.08f, 1f, amount);
                }
                yield return null;
            }
            salinityInstrument.SetMeasurement(target);
            readingRevealRoutine = null;
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
                if (enabled)
                {
                    walker.CaptureCursor();
                }
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
            if (EpisodeAccessibility.ReducedMotion)
            {
                fade.alpha = 0f;
                fade.blocksRaycasts = false;
                yield break;
            }
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
            EpisodeUiFactory.EnsureEventSystem();
            Font font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject(
                "Episode3DAlphaHUD",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            hudRoot = canvasObject;
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            AtlasSurfaceGraphic objective =
                EpisodeUiFactory.AtlasLabel(
                    canvas.transform,
                    "Objective");
            Stretch(
                objective.rectTransform,
                new Vector2(.025f, .91f),
                new Vector2(.405f, .968f));
            objectiveText = Label(
                objective.transform,
                font,
                16,
                TextAnchor.MiddleLeft,
                new Vector2(.035f, .08f),
                new Vector2(.965f, .92f));

            AtlasSurfaceGraphic progress =
                EpisodeUiFactory.AtlasLabel(
                    canvas.transform,
                    "Progress");
            Stretch(
                progress.rectTransform,
                new Vector2(.785f, .915f),
                new Vector2(.975f, .968f));
            progressText = Label(
                progress.transform,
                font,
                15,
                TextAnchor.MiddleCenter,
                new Vector2(.04f, .08f),
                new Vector2(.96f, .92f));
            progressText.color =
                EpisodeUiFactory.Amber;

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

            AtlasSurfaceGraphic dialogue =
                EpisodeUiFactory.SmokedGlass(
                    canvas.transform,
                    "MaiDialogue");
            Stretch(
                dialogue.rectTransform,
                new Vector2(.095f, .035f),
                new Vector2(.905f, .29f));
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
                EpisodeUiFactory.Amber;
            dialogueText = Label(
                dialogue.transform,
                font,
                18,
                TextAnchor.UpperLeft,
                new Vector2(.035f, .40f),
                new Vector2(.965f, .76f));
            dialogueHint = Label(
                dialogue.transform,
                font,
                13,
                TextAnchor.MiddleLeft,
                new Vector2(.035f, .30f),
                new Vector2(.965f, .40f));
            dialogueHint.color =
                EpisodeUiFactory.MutedSand;
            predictionButtons = new Button[2];
            for (int index = 0;
                 index < predictionButtons.Length;
                 index++)
            {
                int selectedIndex = index;
                predictionButtons[index] =
                    EpisodeUiFactory.ChoiceButton(
                        dialogue.transform,
                        "PredictionChoice_" + (index + 1),
                        index + 1,
                        string.Empty);
                float left = .035f + index * .47f;
                Stretch(
                    predictionButtons[index]
                        .GetComponent<RectTransform>(),
                    new Vector2(left, .055f),
                    new Vector2(left + .445f, .285f));
                predictionButtons[index].onClick.AddListener(
                    () => ChoosePrediction(selectedIndex));
                predictionButtons[index].gameObject.SetActive(false);
            }
            dialoguePanel.SetActive(false);

            AtlasSurfaceGraphic reading =
                EpisodeUiFactory.AtlasLabel(
                    canvas.transform,
                    "FieldReading");
            Stretch(
                reading.rectTransform,
                new Vector2(.635f, .28f),
                new Vector2(.965f, .82f));
            readingPanel = reading.gameObject;
            Text readingTitle = Label(
                reading.transform,
                font,
                15,
                TextAnchor.UpperLeft,
                new Vector2(.06f, .06f),
                new Vector2(.94f, .96f));
            readingTitle.text = "FIELD READING  ·  RECORDED EVIDENCE";
            readingTitle.color = EpisodeUiFactory.Amber;
            Stretch(
                readingTitle.rectTransform,
                new Vector2(.06f, .86f),
                new Vector2(.94f, .96f));
            salinityInstrument =
                EpisodeUiFactory.Instrument(
                    reading.transform,
                    "SalinityInstrument");
            Stretch(
                salinityInstrument.rectTransform,
                new Vector2(.04f, .20f),
                new Vector2(.34f, .84f));
            Text scaleLabel = Label(
                reading.transform,
                font,
                12,
                TextAnchor.UpperCenter,
                new Vector2(.035f, .08f),
                new Vector2(.34f, .20f));
            scaleLabel.name = "InstrumentLabel";
            scaleLabel.text = "VIAL SCALE  ·  MEASURED";
            scaleLabel.color = EpisodeUiFactory.Amber;
            readingText = RuntimeScrollableContent.Create(
                reading.transform,
                "FieldReadingContent",
                new Vector2(.36f, .18f),
                new Vector2(.95f, .84f),
                16);
            AtlasSurfaceGraphic stamp =
                EpisodeUiFactory.Stamp(
                    reading.transform,
                    "EvidenceRecordedStamp",
                    "EVIDENCE RECORDED",
                    EpisodeUiFactory.Amber);
            evidenceRecordedStamp = stamp.rectTransform;
            Stretch(
                evidenceRecordedStamp,
                new Vector2(.55f, .045f),
                new Vector2(.94f, .15f));
            readingPanel.SetActive(false);

            AtlasSurfaceGraphic notebook =
                EpisodeUiFactory.FieldPaper(
                    canvas.transform,
                    "FieldJournal");
            Stretch(
                notebook.rectTransform,
                new Vector2(.09f, .075f),
                new Vector2(.94f, .91f));
            notebookPanel = notebook.gameObject;
            Text journalTitle = Label(
                notebook.transform,
                font,
                20,
                TextAnchor.MiddleLeft,
                new Vector2(.05f, .91f),
                new Vector2(.72f, .97f));
            journalTitle.text = "FIELD JOURNAL";
            journalTitle.color = EpisodeUiFactory.Ink;
            string[] tabLabels =
                { "SITES", "PEOPLE", "PLAN", "SOURCES" };
            journalTabs = new Button[tabLabels.Length];
            for (int index = 0;
                 index < tabLabels.Length;
                 index++)
            {
                int selectedIndex = index;
                journalTabs[index] = JournalButton(
                    notebook.transform,
                    font,
                    "Journal_" + tabLabels[index],
                    tabLabels[index],
                    new Vector2(.05f + index * .17f, .82f),
                    new Vector2(.205f + index * .17f, .895f));
                journalTabs[index].onClick.AddListener(
                    () =>
                        SelectJournalSection(
                            (FieldJournalSection)selectedIndex));
            }
            Button closeJournal = JournalButton(
                notebook.transform,
                font,
                "CloseFieldJournal",
                "CLOSE  N",
                new Vector2(.80f, .91f),
                new Vector2(.95f, .97f));
            closeJournal.onClick.AddListener(ToggleNotebook);
            AtlasSurfaceGraphic siteMap =
                EpisodeUiFactory.AtlasLabel(
                    notebook.transform,
                    "JournalSiteMap");
            Stretch(
                siteMap.rectTransform,
                new Vector2(.045f, .12f),
                new Vector2(.365f, .79f));
            Text mapTitle = Label(
                siteMap.transform,
                font,
                13,
                TextAnchor.UpperLeft,
                new Vector2(.08f, .83f),
                new Vector2(.92f, .96f));
            mapTitle.name = "SpecimenLabel";
            mapTitle.text =
                "FIELD ROUTE  ·  THREE WATER STATIONS";
            mapTitle.color = EpisodeUiFactory.Amber;
            AtlasRouteGraphic route =
                EpisodeUiFactory.Route(
                    siteMap.transform,
                    "InvestigationRoute",
                    EpisodeUiFactory.FieldRouteNodes(
                        Mathf.Max(
                            1,
                            configuredSiteIds.Length)),
                    EpisodeUiFactory.BrightAmber,
                    1.5f);
            Stretch(
                route.rectTransform,
                new Vector2(.08f, .10f),
                new Vector2(.92f, .80f));
            AtlasSurfaceGraphic routeStamp =
                EpisodeUiFactory.Stamp(
                    siteMap.transform,
                    "RouteEvidenceStamp",
                    "SAMPLES  ·  0/0",
                    EpisodeUiFactory.NetworkTeal);
            journalRouteStampText =
                routeStamp.transform.Find("StampLabel")
                    ?.GetComponent<Text>();
            Stretch(
                routeStamp.rectTransform,
                new Vector2(.14f, .05f),
                new Vector2(.70f, .17f));
            AtlasSurfaceGraphic dossierRule =
                EpisodeUiFactory.AtlasLabel(
                    notebook.transform,
                    "DossierRule");
            Stretch(
                dossierRule.rectTransform,
                new Vector2(.382f, .11f),
                new Vector2(.386f, .79f));
            notebookText = RuntimeScrollableContent.Create(
                notebook.transform,
                "FieldJournalEntries",
                new Vector2(.405f, .10f),
                new Vector2(.95f, .79f),
                16);
            ScrollRect journalScroll =
                notebookText.GetComponentInParent<ScrollRect>();
            journalScroll.GetComponent<Image>().color =
                new Color(
                    EpisodeUiFactory.OffWhite.r,
                    EpisodeUiFactory.OffWhite.g,
                    EpisodeUiFactory.OffWhite.b,
                    .10f);
            notebookText.color = EpisodeUiFactory.Ink;
            Button textSmaller = JournalButton(
                notebook.transform,
                font,
                "JournalTextSmaller",
                "A−",
                new Vector2(.05f, .012f),
                new Vector2(.115f, .052f));
            textSmaller.onClick.AddListener(() =>
            {
                EpisodeAccessibility.ChangeTextScale(-.1f);
                RefreshAccessibilityStatus();
            });
            Button textLarger = JournalButton(
                notebook.transform,
                font,
                "JournalTextLarger",
                "A+",
                new Vector2(.125f, .012f),
                new Vector2(.19f, .052f));
            textLarger.onClick.AddListener(() =>
            {
                EpisodeAccessibility.ChangeTextScale(.1f);
                RefreshAccessibilityStatus();
            });
            Button contrast = JournalButton(
                notebook.transform,
                font,
                "JournalContrast",
                "CONTRAST",
                new Vector2(.205f, .012f),
                new Vector2(.33f, .052f));
            contrast.onClick.AddListener(() =>
            {
                EpisodeAccessibility.ToggleHighContrast();
                RefreshAccessibilityStatus();
            });
            Button motion = JournalButton(
                notebook.transform,
                font,
                "JournalReducedMotion",
                "MOTION",
                new Vector2(.345f, .012f),
                new Vector2(.455f, .052f));
            motion.onClick.AddListener(() =>
            {
                EpisodeAccessibility.ToggleReducedMotion();
                RefreshAccessibilityStatus();
            });
            accessibilityStatus = Label(
                notebook.transform,
                font,
                11,
                TextAnchor.MiddleRight,
                new Vector2(.47f, .008f),
                new Vector2(.95f, .055f));
            notebookPanel.SetActive(false);

            controlsText = Label(
                canvas.transform,
                font,
                14,
                TextAnchor.MiddleCenter,
                new Vector2(.20f, .008f),
                new Vector2(.80f, .045f));
            controlsText.text =
                "WASD MOVE   ·   MOUSE LOOK   ·   E INTERACT   ·   N FIELD JOURNAL   ·   ESC RELEASE CURSOR";
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
            EpisodeAccessibility.ApplyAll();
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
            bool themed =
                Mathf.Abs(
                    color.r -
                    EpisodeUiFactory.DeepTeal.r) < .03f &&
                Mathf.Abs(
                    color.g -
                    EpisodeUiFactory.DeepTeal.g) < .03f;
            if (themed)
            {
                Outline outline = item.AddComponent<Outline>();
                outline.effectColor = new Color(
                    EpisodeUiFactory.Amber.r,
                    EpisodeUiFactory.Amber.g,
                    EpisodeUiFactory.Amber.b,
                    .38f);
                outline.effectDistance =
                    new Vector2(1f, -1f);
            }
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
            text.color = EpisodeUiFactory.OffWhite;
            text.raycastTarget = false;
            Stretch(text.rectTransform, anchorMin, anchorMax);
            return text;
        }

        private static Button JournalButton(
            Transform parent,
            Font font,
            string name,
            string value,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            Button button = EpisodeUiFactory.Button(
                parent,
                name,
                value,
                name.StartsWith(
                    "Journal_",
                    StringComparison.Ordinal)
                    ? EpisodeButtonStyle.Tab
                    : EpisodeButtonStyle.Secondary,
                13);
            Stretch(
                button.GetComponent<RectTransform>(),
                anchorMin,
                anchorMax);
            return button;
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
            ToggleNotebook();
            ContinueAfterReadingForTesting();
            bool hudWasVisible =
                hudRoot != null && hudRoot.activeSelf;
            if (hudRoot != null)
            {
                hudRoot.SetActive(false);
            }
            yield return CaptureStation(
                directory,
                "SamplingDock",
                "07_canal_station.png");
            yield return CaptureStation(
                directory,
                "FieldStation_ResearchPost_A",
                "08_research_station.png");
            yield return CaptureStation(
                directory,
                "FieldStation_DistrictOffice_A",
                "09_district_office.png");
            yield return CaptureStation(
                directory,
                "FieldStation_PlanningTable_A",
                "10_planning_station.png");
            if (hudRoot != null)
            {
                hudRoot.SetActive(hudWasVisible);
            }
            for (int index = 1;
                 index < configuredSiteIds.Length;
                 index++)
            {
                SelectConfiguredSiteForTesting(
                    configuredSiteIds[index]);
                BeginSiteInteraction();
                ChoosePrediction(0);
                BeginCollection();
                while (State == Episode3DAlphaState.Sampling)
                {
                    yield return null;
                }
                ContinueAfterReadingForTesting();
            }
            InterviewController interviews =
                FindFirstObjectByType<InterviewController>();
            if (interviews != null &&
                interviews.BeginInterviews() &&
                interviews.Scenario.stakeholders.Length > 0)
            {
                StakeholderHotspot captureHotspot =
                    stakeholderHotspots.Length == 0
                        ? null
                        : stakeholderHotspots[
                            stakeholderHotspots.Length - 1];
                if (captureHotspot != null && walker != null)
                {
                    Vector3 position =
                        captureHotspot.transform.position +
                        Vector3.back * 2.8f;
                    position.y =
                        captureHotspot.transform.position.y;
                    walker.Teleport(position, 0f, 0f);
                    Vector3 focusPoint =
                        captureHotspot.Character != null
                            ? captureHotspot.Character.FocusPoint
                            : captureHotspot.transform.position +
                              Vector3.up * 1.5f;
                    Vector3 direction =
                        focusPoint -
                        walker.ViewCamera.transform.position;
                    float heading =
                        Mathf.Atan2(
                            direction.x,
                            direction.z) *
                        Mathf.Rad2Deg;
                    float pitch =
                        -Mathf.Atan2(
                            direction.y,
                            new Vector2(
                                direction.x,
                                direction.z).magnitude) *
                        Mathf.Rad2Deg;
                    walker.SetViewAngles(heading, pitch);
                }
                StakeholderDto stakeholder =
                    interviews.Scenario.stakeholders[
                        interviews.Scenario.stakeholders.Length - 1];
                interviews.SelectStakeholder(stakeholder.id);
                yield return new WaitForSecondsRealtime(.8f);
                EpisodePresentationController presentation =
                    FindFirstObjectByType<
                        EpisodePresentationController>();
                presentation?.DismissGuideForTesting();
                presentation?.DismissGuideForTesting();
                yield return new WaitForSecondsRealtime(.35f);
                yield return Capture(
                    directory,
                    "06_interview_selected.png");

                InterviewNotebook interviewNotebook =
                    InterviewNotebookSession
                        .GetOrCreate()
                        .Notebook;
                foreach (StakeholderDto participant in
                         interviews.Scenario.stakeholders)
                {
                    int priorTurns =
                        interviewNotebook
                            .ConversationFor(participant.id)
                            .Count;
                    interviews.AskForTesting(
                        participant.id,
                        "What condition should the community weigh " +
                        "most before making its field plan?");
                    yield return new WaitForSecondsRealtime(.1f);
                    yield return WaitForCaptureCondition(
                        () =>
                            !interviews.IsBusy &&
                            interviewNotebook
                                .ConversationFor(participant.id)
                                .Count >= priorTurns + 2,
                        150f,
                        "stakeholder response " + participant.id);
                }
                yield return new WaitForSecondsRealtime(.35f);
                yield return Capture(
                    directory,
                    "11_interview_response.png");

                PlanController plan =
                    FindFirstObjectByType<PlanController>();
                ConsequencesController consequences =
                    FindFirstObjectByType<ConsequencesController>();
                FeedbackController feedback =
                    FindFirstObjectByType<FeedbackController>();
                PolicyBriefController brief =
                    FindFirstObjectByType<PolicyBriefController>();
                yield return WaitForCaptureCondition(
                    () =>
                        plan != null &&
                        plan.LoadState ==
                        InvestigationLoadState.Ready,
                    30f,
                    "planning stage");
                if (plan != null && plan.BeginPlanning())
                {
                    plan.ConfigureForTesting(
                        investigation.Scenario.test_sites[0].id,
                        investigation.Scenario.interventions[0].id,
                        "The proposal should fit the recorded field " +
                        "conditions and the stakeholder evidence.");
                    presentation?.DismissGuideForTesting();
                    yield return new WaitForSecondsRealtime(.45f);
                    yield return Capture(
                        directory,
                        "12_planning_table.png");

                    string priorSimulation =
                        plan.Session.SimulatorResultJson;
                    plan.SubmitPlan();
                    yield return new WaitForSecondsRealtime(.1f);
                    yield return WaitForCaptureCondition(
                        () =>
                            !plan.IsBusy &&
                            !string.IsNullOrWhiteSpace(
                                plan.Session.SimulatorResultJson) &&
                            !string.Equals(
                                priorSimulation,
                                plan.Session.SimulatorResultJson,
                                StringComparison.Ordinal),
                        180f,
                        "first simulation");
                    yield return WaitForCaptureCondition(
                        () =>
                            consequences != null &&
                            consequences.ConsequencesVisible,
                        30f,
                        "first Future Walk");
                    presentation?.DismissGuideForTesting();
                    yield return new WaitForSecondsRealtime(.45f);
                    yield return Capture(
                        directory,
                        "13_future_walk.png");

                    consequences?.UnlockFeedback();
                    yield return WaitForCaptureCondition(
                        () =>
                            feedback != null &&
                            !feedback.IsBusy &&
                            !string.IsNullOrWhiteSpace(
                                plan.Session.FeedbackResultJson),
                        180f,
                        "grounded feedback");
                    yield return new WaitForSecondsRealtime(.35f);
                    yield return Capture(
                        directory,
                        "14_grounded_feedback.png");

                    feedback?.RevisePlan();
                    yield return new WaitForSecondsRealtime(.25f);
                    plan.ConfigureForTesting(
                        plan.Session.TargetSiteId,
                        investigation.Scenario.interventions[0].id,
                        "The revised proposal responds to the " +
                        "recorded evidence, stakeholder concerns, " +
                        "and all decision factors.");
                    plan.SubmitPlan();
                    yield return new WaitForSecondsRealtime(.1f);
                    yield return WaitForCaptureCondition(
                        () =>
                            !plan.IsBusy &&
                            plan.Session.HasRevision &&
                            plan.Session.RevisionCount > 0 &&
                            !string.IsNullOrWhiteSpace(
                                plan.Session.SimulatorResultJson),
                        180f,
                        "revised simulation");
                    yield return WaitForCaptureCondition(
                        () =>
                            consequences != null &&
                            consequences.ConsequencesVisible,
                        30f,
                        "revised Future Walk");
                    presentation?.DismissGuideForTesting();
                    yield return new WaitForSecondsRealtime(.45f);
                    yield return Capture(
                        directory,
                        "15_original_revised_comparison.png");

                    consequences?.UnlockFeedback();
                    yield return WaitForCaptureCondition(
                        () =>
                            feedback != null &&
                            !feedback.IsBusy &&
                            !string.IsNullOrWhiteSpace(
                                plan.Session.FeedbackResultJson),
                        180f,
                        "revised grounded feedback");
                    feedback?.GenerateBrief();
                    yield return WaitForCaptureCondition(
                        () =>
                            brief != null &&
                            !brief.IsBusy &&
                            !string.IsNullOrWhiteSpace(
                                plan.Session.PolicyBriefResultJson),
                        180f,
                        "policy brief");
                    yield return new WaitForSecondsRealtime(.45f);
                    yield return Capture(
                        directory,
                        "16_policy_brief.png");

                    presentation?.RefreshForTesting();
                    presentation?.OpenCertificate();
                    yield return new WaitForSecondsRealtime(.45f);
                    yield return Capture(
                        directory,
                        "17_certificate.png");
                }
            }
            yield return new WaitForSecondsRealtime(.4f);
            Application.Quit(0);
        }

        private static IEnumerator WaitForCaptureCondition(
            Func<bool> condition,
            float timeoutSeconds,
            string label)
        {
            float deadline =
                Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            if (!condition())
            {
                Debug.LogError(
                    "Gate C capture timed out while waiting for " +
                    label + ".");
            }
        }

        private IEnumerator CaptureStation(
            string directory,
            string targetName,
            string filename)
        {
            Transform target = null;
            foreach (Transform candidate in
                     FindObjectsByType<Transform>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (string.Equals(
                        candidate.name,
                        targetName,
                        StringComparison.Ordinal))
                {
                    target = candidate;
                    break;
                }
            }
            if (target == null || walker?.ViewCamera == null)
            {
                Debug.LogWarning(
                    "Station capture target was unavailable: " +
                    targetName);
                yield break;
            }

            Renderer[] renderers =
                target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) yield break;
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            Vector3 viewDirection =
                (target.forward + target.right * .28f).normalized;
            float distance =
                Mathf.Max(bounds.extents.x, bounds.extents.z) +
                3.5f;
            Vector3 groundPosition =
                bounds.center - viewDirection * distance;
            groundPosition.y = target.position.y;
            Vector3 focus =
                bounds.center +
                Vector3.up * Mathf.Min(.35f, bounds.extents.y * .15f);
            Vector3 eye =
                groundPosition + Vector3.up * walker.EyeHeight;
            Vector3 direction = focus - eye;
            float heading =
                Mathf.Atan2(direction.x, direction.z) *
                Mathf.Rad2Deg;
            float pitch =
                -Mathf.Atan2(
                    direction.y,
                    new Vector2(direction.x, direction.z).magnitude) *
                Mathf.Rad2Deg;
            walker.Teleport(groundPosition, heading, pitch);
            yield return new WaitForSecondsRealtime(1.15f);
            yield return Capture(directory, filename);
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
