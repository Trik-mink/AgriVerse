using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace AgriVerse.Client
{
    /// <summary>
    /// Presentation-only director for authored Episode 1 copy, local identity, glossary,
    /// and guide cues. It observes the existing controllers and never calls a scored API.
    /// </summary>
    public sealed class EpisodePresentationController : MonoBehaviour
    {
        private readonly Queue<string> guideQueue = new Queue<string>();
        private readonly HashSet<string> queuedCueIds = new HashSet<string>();
        private EpisodePresentationView view;
        private ScenarioDto scenario;
        private EpisodeSession session;
        private bool missionStarted;
        private string activeGuideCue = string.Empty;
        private bool guideSuspended;
        private bool judgeTechnicalVisible;
        private Coroutine connectionAttempt;
        [SerializeField] private bool showArrivalGuideCues = true;

        public bool LandingVisible => view != null && view.LandingVisible;
        public bool GuideVisible => view != null && view.GuideVisible;
        public bool GlossaryVisible => view != null && view.GlossaryVisible;
        public bool CertificateAvailable { get; private set; }
        public bool CertificateVisible =>
            view != null && view.CertificateVisible;
        public bool MissionStarted => missionStarted;
        public bool InputBlocked =>
            view != null &&
            (view.LandingVisible ||
             view.GuideVisible ||
             view.GlossaryVisible ||
             view.JudgeVisible ||
             view.CertificateVisible);
        public string GuideTextForTesting => view?.GuideText ?? string.Empty;
        public string GlossaryTextForTesting => view?.GlossaryText ?? string.Empty;
        public string CertificateTextForTesting =>
            view?.CertificateText ?? string.Empty;
        public bool JudgeVisibleForTesting =>
            view != null && view.JudgeVisible;
        public string JudgeTextForTesting =>
            view?.JudgeText ?? string.Empty;
        public string SelectedFieldLocationIdForTesting =>
            view?.SelectedFieldLocationId ?? string.Empty;
        public bool IncomingLocationSelectedForTesting =>
            view != null && view.IncomingLocationSelected;
        public bool NameEntryVisibleForTesting =>
            view != null && view.NameEntryVisible;
        public bool MissionStartVisibleForTesting =>
            view != null && view.MissionStartVisible;
        public bool MissionStartInteractableForTesting =>
            view != null && view.MissionStartInteractable;
        public bool ConnectionStatusVisibleForTesting =>
            view != null && view.ConnectionStatusVisible;
        public string ConnectionStatusTextForTesting =>
            view?.ConnectionStatusText ?? string.Empty;
        public bool RetryVisibleForTesting =>
            view != null && view.RetryVisible;
        public bool MissionConnectionRequiredForTesting =>
            view != null && view.MissionConnectionRequired;
        public string PlayerNameForTesting =>
            view?.PlayerName ?? string.Empty;
        public int FieldNetworkPinCountForTesting =>
            view?.FieldNetworkPinCount ?? 0;

        private IEnumerator Start()
        {
            EnsureView();
            string reliabilityCaptureDirectory =
                ReliabilityCaptureDirectory(
                    Environment.GetCommandLineArgs());
            if (PackagedScenarioLoader.TryLoad(
                    out ScenarioDto packagedScenario,
                    out string packagedError))
            {
                ConfigureScenario(
                    packagedScenario,
                    FieldNetworkConnectionState.Loading);
            }
            else
            {
                Debug.LogError(packagedError, this);
            }

            while (true)
            {
                InvestigationController investigation =
                    FindFirstObjectByType<InvestigationController>();
                if (investigation != null &&
                    investigation.LoadState == InvestigationLoadState.Ready)
                {
                    CompleteScenarioConnection(
                        investigation.Scenario);
                    if (!string.IsNullOrWhiteSpace(
                            reliabilityCaptureDirectory))
                    {
                        yield return CaptureRecoveredLanding(
                            reliabilityCaptureDirectory);
                        yield break;
                    }
                    string captureDirectory =
                        LandingCaptureDirectory(
                            Environment.GetCommandLineArgs());
                    if (!string.IsNullOrWhiteSpace(
                            captureDirectory))
                    {
                        yield return CaptureLanding(
                            captureDirectory);
                    }
                    yield break;
                }
                if (investigation != null &&
                    investigation.LoadState ==
                    InvestigationLoadState.Failed)
                {
                    view.SetConnectionState(
                        FieldNetworkConnectionState.Offline);
                    if (!string.IsNullOrWhiteSpace(
                            reliabilityCaptureDirectory))
                    {
                        yield return CaptureOfflineRecovery(
                            reliabilityCaptureDirectory);
                    }
                    yield break;
                }
                yield return null;
            }
        }

        private void Update()
        {
            if (!missionStarted || scenario == null) return;
            RefreshPresentationState();
            RuntimePanelManager manager =
                FindFirstObjectByType<RuntimePanelManager>();
            if (manager == null || !manager.ActiveStage.HasValue)
            {
                ResumeGuideOutsideStage();
                return;
            }

            SuspendGuideDuringStage();

            RuntimeActivityStage stage = manager.ActiveStage.Value;
            if (stage == RuntimeActivityStage.Interviews)
            {
                QueueCue("interview-intro", SaltLineNarrative.AfterAllReadings);
                QueueCue("interview-station", SaltLineNarrative.InterviewIntro);
                InterviewNotebook notebook =
                    InterviewNotebookSession.GetOrCreate().Notebook;
                if (notebook != null &&
                    notebook.AreAllStakeholdersInterviewed(scenario.stakeholders))
                {
                    QueueCue(
                        "interviews-complete",
                        SaltLineNarrative.AfterAllInterviews);
                }
            }
            else if (stage == RuntimeActivityStage.Plan)
            {
                PlanSession plan = PlanSession.GetOrCreate();
                if (!string.IsNullOrWhiteSpace(plan.FeedbackResultJson))
                {
                    QueueCue(
                        "revision-intro-" + plan.RevisionCount,
                        SaltLineNarrative.RevisionIntro);
                }
                else
                {
                    QueueCue("planning-intro", SaltLineNarrative.PlanningIntro);
                }
            }
            else if (stage == RuntimeActivityStage.Consequences)
            {
                QueueCue(
                    "consequences-intro",
                    SaltLineNarrative.ConsequencesIntro);
                if (PlanSession.GetOrCreate().HasRevision)
                {
                    QueueCue(
                        "improved-result",
                        SaltLineNarrative.ImprovedResult);
                }
            }
            else if (stage == RuntimeActivityStage.Feedback)
            {
                QueueCue("feedback-intro", SaltLineNarrative.FeedbackIntro);
            }
            else if (stage == RuntimeActivityStage.Brief &&
                     !string.IsNullOrWhiteSpace(
                         PlanSession.GetOrCreate().PolicyBriefResultJson))
            {
                QueueCue("brief-intro", SaltLineNarrative.BriefIntro);
                QueueCue(
                    "ending",
                    SaltLineNarrative.Ending(session.Progress.PlayerName));
            }
        }

        public void BuildForTesting(ScenarioDto source)
        {
            EnsureView();
            ConfigureScenario(
                source,
                FieldNetworkConnectionState.Ready);
        }

        public void BuildOfflineForTesting(ScenarioDto source)
        {
            EnsureView();
            ConfigureScenario(
                source,
                FieldNetworkConnectionState.Offline);
        }

        public void BuildLoadingForTesting(ScenarioDto source)
        {
            EnsureView();
            ConfigureScenario(
                source,
                FieldNetworkConnectionState.Loading);
        }

        public void CompleteRetryForTesting(
            ScenarioDto source)
        {
            CompleteScenarioConnection(source);
        }

        public void SetPlayerNameForTesting(string value)
        {
            view?.SetPlayerName(value);
        }

        public bool LandingControlsUsableAtForTesting(
            Vector2 resolution) =>
            view != null &&
            view.LandingControlsUsableAt(resolution);

        public void RetryConnectionForTesting()
        {
            RetryConnection();
        }

        public bool BeginMissionForTesting(
            string playerName,
            string avatarPresetId)
        {
            view.SetPlayerName(playerName);
            return BeginMission();
        }

        public bool SelectFieldLocationForTesting(string id) =>
            view != null && view.SelectFieldLocation(id);

        public void ClearFieldLocationSelectionForTesting()
        {
            view?.ClearFieldLocationSelection();
        }

        public void ConfigureFor3D(bool showAuthoredArrivalCues)
        {
            showArrivalGuideCues = showAuthoredArrivalCues;
        }

        public void ToggleGlossary()
        {
            if (view == null || !missionStarted) return;
            if (!view.GlossaryVisible)
            {
                view.SetJudgeVisible(false);
                view.HideCertificate(CertificateAvailable);
            }
            view.SetGlossaryVisible(!view.GlossaryVisible);
        }

        public void RefreshForTesting()
        {
            RefreshPresentationState();
        }

        internal void RefreshGuideVisibilityForTesting()
        {
            RuntimePanelManager manager =
                FindFirstObjectByType<RuntimePanelManager>();
            if (manager == null || !manager.ActiveStage.HasValue)
            {
                ResumeGuideOutsideStage();
            }
            else
            {
                SuspendGuideDuringStage();
            }
        }

        public void DismissGuideForTesting()
        {
            if (view != null && view.GuideVisible)
            {
                DismissGuide();
            }
        }

        public void OpenCertificate()
        {
            if (!CertificateAvailable || view == null || session?.Progress == null)
            {
                return;
            }

            view.SetGlossaryVisible(false);
            view.SetJudgeVisible(false);
            view.ShowCertificate(BuildCertificateText());
        }

        public void OpenJudgeForTesting()
        {
            if (view != null && !view.JudgeVisible)
            {
                ToggleJudge();
            }
        }

        public void ChooseEndingForTesting(EpisodeEndingChoice choice)
        {
            ChooseEnding(choice);
        }

        public void ReturnToFieldNetworkForTesting()
        {
            PrepareReturnToFieldNetwork();
        }

        private void EnsureView()
        {
            if (view != null) return;
            view = new GameObject(
                "EpisodePresentationView").AddComponent<EpisodePresentationView>();
            view.transform.SetParent(transform, false);
            view.Build(
                () => BeginMission(),
                DismissGuide,
                ToggleGlossary,
                ToggleJudge,
                ToggleJudgeTechnical,
                OpenCertificate,
                ChooseEnding,
                RetryConnection);
        }

        private void ConfigureScenario(
            ScenarioDto source,
            FieldNetworkConnectionState connectionState)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.id)) return;
            scenario = source;
            session = EpisodeSession.GetOrCreate();
            session.ConfigureScenario(scenario.id);
            view.ConfigureFieldNetwork(
                scenario,
                connectionState);
        }

        private void CompleteScenarioConnection(
            ScenarioDto source)
        {
            if (source == null ||
                string.IsNullOrWhiteSpace(source.id))
            {
                view.SetConnectionState(
                    FieldNetworkConnectionState.Offline);
                return;
            }

            bool sameScenario =
                scenario != null &&
                string.Equals(
                    scenario.id,
                    source.id,
                    StringComparison.Ordinal);
            scenario = source;
            session = EpisodeSession.GetOrCreate();
            session.ConfigureScenario(scenario.id);
            if (sameScenario &&
                view.FieldNetworkPinCount > 0)
            {
                view.SetConnectionState(
                    FieldNetworkConnectionState.Ready);
            }
            else
            {
                view.ConfigureFieldNetwork(
                    scenario,
                    FieldNetworkConnectionState.Ready);
            }
        }

        private void RetryConnection()
        {
            if (connectionAttempt != null) return;
            connectionAttempt =
                StartCoroutine(RetryConnectionRoutine());
        }

        private IEnumerator RetryConnectionRoutine()
        {
            view.SetConnectionState(
                FieldNetworkConnectionState.Loading);
            InvestigationController investigation =
                FindFirstObjectByType<InvestigationController>();
            if (investigation == null)
            {
                view.SetConnectionState(
                    FieldNetworkConnectionState.Offline);
                connectionAttempt = null;
                yield break;
            }

            yield return investigation.LoadScenario();
            if (investigation.LoadState !=
                InvestigationLoadState.Ready)
            {
                view.SetConnectionState(
                    FieldNetworkConnectionState.Offline);
                connectionAttempt = null;
                yield break;
            }

            InterviewController interviews =
                FindFirstObjectByType<InterviewController>();
            while (interviews != null &&
                   interviews.LoadState ==
                   InvestigationLoadState.Loading)
            {
                yield return null;
            }
            if (interviews != null &&
                interviews.LoadState !=
                InvestigationLoadState.Ready)
            {
                yield return interviews.LoadScenario();
            }
            PlanController plan =
                FindFirstObjectByType<PlanController>();
            while (plan != null &&
                   plan.LoadState ==
                   InvestigationLoadState.Loading)
            {
                yield return null;
            }
            if (plan != null &&
                plan.LoadState !=
                InvestigationLoadState.Ready)
            {
                yield return plan.LoadScenario();
            }

            bool dependenciesReady =
                (interviews == null ||
                 interviews.LoadState ==
                 InvestigationLoadState.Ready) &&
                (plan == null ||
                 plan.LoadState ==
                 InvestigationLoadState.Ready);
            if (!dependenciesReady)
            {
                view.SetConnectionState(
                    FieldNetworkConnectionState.Offline);
                connectionAttempt = null;
                yield break;
            }

            CompleteScenarioConnection(
                investigation.Scenario);
            connectionAttempt = null;
        }

        private bool BeginMission()
        {
            if (scenario == null)
            {
                view.ShowLandingError("The field mission is still loading.");
                return false;
            }

            if (view.MissionConnectionRequired)
            {
                view.ShowLandingError(
                    "Connection required to begin this mission.");
                return false;
            }

            if (!view.CanBeginMission(view.NameInput.text))
            {
                view.ShowLandingError(
                    string.IsNullOrWhiteSpace(
                        view.SelectedFieldLocationId)
                        ? "Select the available field mission first."
                        : "Enter a name between 1 and 40 characters.");
                return false;
            }

            if (!session.Progress.SetIdentity(
                    view.NameInput.text,
                    EpisodeProgress.FirstPersonObserverId))
            {
                view.ShowLandingError(
                    "Enter a name between 1 and 40 characters.");
                return false;
            }

            JudgeRequestSession.BeginNew();
            missionStarted = true;
            view.HideLanding();
            RefreshPresentationState();
            if (showArrivalGuideCues)
            {
                QueueCue(
                    "arrival",
                    SaltLineNarrative.Arrival(
                        session.Progress.PlayerName));
                QueueCue(
                    "investigation-intro",
                    SaltLineNarrative.InvestigationIntro);
            }
            return true;
        }

        private void QueueCue(string cueId, string text)
        {
            if (!queuedCueIds.Add(cueId) ||
                string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            guideQueue.Enqueue(text);
            if (!view.GuideVisible && CanShowGuideCue())
            {
                ShowNextGuideCue();
            }
        }

        private void DismissGuide()
        {
            view.HideGuide();
            activeGuideCue = string.Empty;
            guideSuspended = false;
            ShowNextGuideCue();
        }

        private void ShowNextGuideCue()
        {
            if (!CanShowGuideCue() ||
                view.GuideVisible ||
                guideQueue.Count == 0)
            {
                return;
            }
            activeGuideCue = guideQueue.Dequeue();
            view.ShowGuide(activeGuideCue);
        }

        private bool CanShowGuideCue()
        {
            RuntimePanelManager manager =
                FindFirstObjectByType<RuntimePanelManager>();
            return manager == null || !manager.ActiveStage.HasValue;
        }

        private void SuspendGuideDuringStage()
        {
            if (!view.GuideVisible) return;
            guideSuspended =
                !string.IsNullOrWhiteSpace(activeGuideCue);
            view.HideGuide();
        }

        private void ResumeGuideOutsideStage()
        {
            if (view.GuideVisible) return;
            if (guideSuspended &&
                !string.IsNullOrWhiteSpace(activeGuideCue))
            {
                guideSuspended = false;
                view.ShowGuide(activeGuideCue);
                return;
            }
            ShowNextGuideCue();
        }

        private void ToggleJudge()
        {
            if (view == null || !missionStarted) return;
            bool show = !view.JudgeVisible;
            if (show)
            {
                view.SetGlossaryVisible(false);
                view.HideCertificate(CertificateAvailable);
                judgeTechnicalVisible = false;
                view.SetJudgeTechnicalVisible(false);
                view.SetJudgeText(BuildJudgeText());
            }
            view.SetJudgeVisible(show);
        }

        private void ToggleJudgeTechnical()
        {
            if (view == null ||
                !missionStarted ||
                !view.JudgeVisible)
            {
                return;
            }
            judgeTechnicalVisible = !judgeTechnicalVisible;
            view.SetJudgeTechnicalVisible(
                judgeTechnicalVisible);
            view.SetJudgeText(
                judgeTechnicalVisible
                    ? BuildJudgeText() +
                      "\n\n" +
                      JudgeViewFormatter
                          .FormatTechnicalDisclosure(
                              PlanSession.GetOrCreate())
                    : BuildJudgeText());
        }

        private void RefreshPresentationState()
        {
            if (view == null) return;
            PlanSession plan = PlanSession.GetOrCreate();
            view.SetJudgeAvailable(
                missionStarted &&
                plan.SimulatorResult != null &&
                !view.JudgeVisible);
            CertificateAvailable =
                missionStarted &&
                !string.IsNullOrWhiteSpace(plan.PolicyBriefResultJson);
            view.SetCertificateAvailable(
                CertificateAvailable && !view.CertificateVisible);
        }

        private string BuildJudgeText()
        {
            InterviewController interview =
                FindFirstObjectByType<InterviewController>();
            AnGiangRealitySpikeController reality =
                FindFirstObjectByType<AnGiangRealitySpikeController>();
            EvidenceNotebookSession evidenceSession =
                EvidenceNotebookSession.GetOrCreate();
            return JudgeViewFormatter.Format(
                scenario,
                interview?.SelectedStakeholderId ?? string.Empty,
                reality != null && reality.ProceduralFallbackActive,
                evidenceSession.Notebook,
                PlanSession.GetOrCreate());
        }

        private string BuildCertificateText()
        {
            PlanSession plan = PlanSession.GetOrCreate();
            var interventionLabels = new List<string>();
            foreach (string interventionId in
                     plan.InterventionIds ?? Array.Empty<string>())
            {
                string label = interventionId;
                foreach (InterventionDto intervention in
                         scenario.interventions ?? Array.Empty<InterventionDto>())
                {
                    if (intervention != null &&
                        intervention.id == interventionId)
                    {
                        label = intervention.label;
                        break;
                    }
                }
                if (!string.IsNullOrWhiteSpace(label))
                {
                    interventionLabels.Add(label);
                }
            }

            string location = JoinNonEmpty(
                scenario.location?.region,
                scenario.location?.country);
            var text = new StringBuilder();
            text.Append(session.Progress.PlayerName)
                .Append("\n\n")
                .Append(SaltLineNarrative.CertificateCompletion)
                .Append("\n\n")
                .Append(SaltLineNarrative.Episode);
            if (!string.IsNullOrWhiteSpace(location))
            {
                text.Append(" - ").Append(location);
            }
            text.Append("\n\n")
                .Append(SaltLineNarrative.CertificateRecommendation)
                .Append(": ")
                .Append(interventionLabels.Count == 0
                    ? "Recorded in the attached policy brief"
                    : string.Join(", ", interventionLabels))
                .Append("\n\n")
                .Append(SaltLineNarrative.CertificateEvidence)
                .Append("\n\n")
                .Append(DateTime.Now.ToString("MMMM d, yyyy"));
            return text.ToString();
        }

        private void ChooseEnding(EpisodeEndingChoice choice)
        {
            if (session?.Progress == null ||
                choice == EpisodeEndingChoice.None)
            {
                return;
            }

            session.Progress.ChooseEnding(choice);
            view.HideCertificate(CertificateAvailable);
            if (choice == EpisodeEndingChoice.ReturnHome)
            {
                ReturnToFieldNetwork();
            }
            else
            {
                RuntimePanelManager manager =
                    FindFirstObjectByType<RuntimePanelManager>();
                manager?.Clear();
                Episode3DAlphaController world =
                    FindFirstObjectByType<
                        Episode3DAlphaController>();
                world?.BeginFreeExploration();
                QueueCue(
                    "stay-another-season",
                    SaltLineNarrative.Ending(
                        session.Progress.PlayerName));
            }
        }

        private void ReturnToFieldNetwork()
        {
            PrepareReturnToFieldNetwork();
            if (Application.isPlaying)
            {
                Scene activeScene =
                    SceneManager.GetActiveScene();
                SceneManager.LoadScene(
                    activeScene.buildIndex);
            }
        }

        private void PrepareReturnToFieldNetwork()
        {
            if (view == null || scenario == null)
            {
                return;
            }

            RuntimePanelManager.GetOrCreate().Clear();
            FirstPersonWalker walker =
                FindFirstObjectByType<FirstPersonWalker>();
            walker?.SetMovementEnabled(false);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            string scenarioId = scenario.id;
            ResetScenarioSession(
                EpisodeSession.GetOrCreate().ConfigureScenario,
                scenarioId);
            ResetScenarioSession(
                EvidenceNotebookSession.GetOrCreate().ConfigureScenario,
                scenarioId);
            ResetScenarioSession(
                InterviewNotebookSession.GetOrCreate().ConfigureScenario,
                scenarioId);
            ResetScenarioSession(
                PlanSession.GetOrCreate().ConfigureScenario,
                scenarioId);

            guideQueue.Clear();
            queuedCueIds.Clear();
            activeGuideCue = string.Empty;
            guideSuspended = false;
            judgeTechnicalVisible = false;
            CertificateAvailable = false;
            missionStarted = false;
            view.ShowLandingForNewJourney(scenario);
        }

        private static void ResetScenarioSession(
            Action<string> configure,
            string scenarioId)
        {
            configure(string.Empty);
            configure(scenarioId);
        }

        private static string JoinNonEmpty(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first)) return second ?? string.Empty;
            if (string.IsNullOrWhiteSpace(second)) return first;
            return first + ", " + second;
        }

        private static string LandingCaptureDirectory(
            string[] arguments)
        {
            if (arguments == null) return null;
            for (int index = 0;
                 index < arguments.Length - 1;
                 index++)
            {
                if (string.Equals(
                        arguments[index],
                        "-agriverse-landing-capture-dir",
                        StringComparison.Ordinal))
                {
                    return arguments[index + 1];
                }
            }
            return null;
        }

        private static string ReliabilityCaptureDirectory(
            string[] arguments)
        {
            if (arguments == null) return null;
            for (int index = 0;
                 index < arguments.Length - 1;
                 index++)
            {
                if (string.Equals(
                        arguments[index],
                        "-agriverse-reliability-capture-dir",
                        StringComparison.Ordinal))
                {
                    return arguments[index + 1];
                }
            }
            return null;
        }

        private IEnumerator CaptureOfflineRecovery(
            string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1.2f);
            yield return CaptureLandingFrame(
                directory,
                "01_offline_globe_retry.png");

            float deadline =
                Time.unscaledTime + 90f;
            float nextRetry = Time.unscaledTime;
            while (view.ConnectionStatusVisible &&
                   Time.unscaledTime < deadline)
            {
                if (connectionAttempt == null &&
                    Time.unscaledTime >= nextRetry)
                {
                    RetryConnection();
                    nextRetry =
                        Time.unscaledTime + 1.5f;
                }
                yield return null;
            }

            if (view.ConnectionStatusVisible)
            {
                Debug.LogError(
                    "AGRIVERSE_RELIABILITY_CAPTURE timed out waiting for mission-service recovery.",
                    this);
                Application.Quit(2);
                yield break;
            }

            yield return CaptureRecoveredLanding(directory);
        }

        private IEnumerator CaptureRecoveredLanding(
            string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1.0f);
            yield return CaptureLandingFrame(
                directory,
                "02_after_retry.png");

            view.FocusNextFieldLocation(1);
            view.FocusNextFieldLocation(1);
            yield return new WaitForSecondsRealtime(1.25f);
            view.SelectKeyboardFocusedFieldLocation();
            yield return new WaitForSecondsRealtime(.55f);
            yield return CaptureLandingFrame(
                directory,
                "03_incoming_selected_by_tab.png");

            view.FocusNextFieldLocation(-1);
            yield return new WaitForSecondsRealtime(1.25f);
            view.SelectKeyboardFocusedFieldLocation();
            view.SetPlayerName("Lan");
            yield return new WaitForSecondsRealtime(.45f);
            yield return CaptureLandingFrame(
                directory,
                "04_vietnam_ready.png");
            yield return new WaitForSecondsRealtime(.4f);
            Application.Quit(0);
        }

        private IEnumerator CaptureLanding(
            string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1.8f);
            yield return CaptureLandingFrame(
                directory,
                "01_orbital_network.png");

            FieldNetworkLocation incoming =
                FieldNetworkCatalog.CreateForScenario(
                        scenario,
                        SaltLineNarrative.Episode,
                        SaltLineNarrative.Tagline)
                    .Locations
                    .FirstOrDefault(location => !location.IsPlayable);
            if (incoming != null)
            {
                view.SelectFieldLocation(incoming.Id);
            }
            yield return new WaitForSecondsRealtime(1.25f);
            yield return CaptureLandingFrame(
                directory,
                "02_incoming_selected.png");

            view.SelectFieldLocation(scenario.id);
            view.SetPlayerName(string.Empty);
            yield return new WaitForSecondsRealtime(1.25f);
            yield return CaptureLandingFrame(
                directory,
                "03_vietnam_selected_empty.png");

            view.SetPlayerName("Lan");
            yield return new WaitForSecondsRealtime(.35f);
            yield return CaptureLandingFrame(
                directory,
                "04_vietnam_ready.png");
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(0);
        }

        private static IEnumerator CaptureLandingFrame(
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
