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
        private GameObject readingPanel;
        private Text readingText;
        private GameObject notebookPanel;
        private Text notebookText;
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
                        presentation.BeginMissionForTesting(
                            "Field Advisor",
                            "river-teal");
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
                    RuntimePanelManager manager =
                        FindFirstObjectByType<RuntimePanelManager>();
                    hudRoot.SetActive(
                        manager == null ||
                        !manager.ActiveStage.HasValue);
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
                readingText.text = FormatReading(activeSite);
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
                Image background =
                    journalTabs[index]?.GetComponent<Image>();
                if (background != null)
                {
                    background.color =
                        index == (int)journalSection
                            ? new Color(1f, .67f, .24f, 1f)
                            : new Color(.025f, .20f, .20f, .96f);
                }
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
            Font font =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf");
            GameObject canvasObject = new GameObject(
                "Episode3DAlphaHUD",
                typeof(Canvas),
                typeof(CanvasScaler));
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
                "FieldJournal",
                new Vector2(.16f, .11f),
                new Vector2(.94f, .89f),
                new Color(.018f, .11f, .12f, .97f));
            notebookPanel = notebook.gameObject;
            Text journalTitle = Label(
                notebook.transform,
                font,
                20,
                TextAnchor.MiddleLeft,
                new Vector2(.05f, .91f),
                new Vector2(.72f, .97f));
            journalTitle.text = "FIELD JOURNAL";
            journalTitle.color =
                new Color(1f, .76f, .34f, 1f);
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
            notebookText = RuntimeScrollableContent.Create(
                notebook.transform,
                "FieldJournalEntries",
                new Vector2(.05f, .06f),
                new Vector2(.95f, .79f),
                16);
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

        private static Button JournalButton(
            Transform parent,
            Font font,
            string name,
            string value,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            GameObject item = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            item.transform.SetParent(parent, false);
            Image image = item.GetComponent<Image>();
            image.color = new Color(.025f, .20f, .20f, .96f);
            Button button = item.GetComponent<Button>();
            button.targetGraphic = image;
            Text label = Label(
                item.transform,
                font,
                13,
                TextAnchor.MiddleCenter,
                Vector2.zero,
                Vector2.one);
            label.text = value;
            Stretch(
                item.GetComponent<RectTransform>(),
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
            }
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
