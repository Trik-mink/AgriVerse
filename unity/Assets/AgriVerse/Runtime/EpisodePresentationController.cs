using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

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
        private string selectedAvatarId = string.Empty;
        private bool missionStarted;
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

        private IEnumerator Start()
        {
            EnsureView();
            while (scenario == null)
            {
                InvestigationController investigation =
                    FindFirstObjectByType<InvestigationController>();
                if (investigation != null &&
                    investigation.LoadState == InvestigationLoadState.Ready)
                {
                    ConfigureScenario(investigation.Scenario);
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
                yield return null;
            }
        }

        private void Update()
        {
            if (!missionStarted || scenario == null) return;
            RefreshPresentationState();
            RuntimePanelManager manager =
                FindFirstObjectByType<RuntimePanelManager>();
            if (manager == null || !manager.ActiveStage.HasValue) return;

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
            ConfigureScenario(source);
        }

        public bool BeginMissionForTesting(
            string playerName,
            string avatarPresetId)
        {
            selectedAvatarId = avatarPresetId ?? string.Empty;
            view.SetSelectedAvatar(selectedAvatarId);
            view.NameInput.text = playerName ?? string.Empty;
            return BeginMission();
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

        public void ChooseEndingForTesting(EpisodeEndingChoice choice)
        {
            ChooseEnding(choice);
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
                OpenCertificate,
                ChooseEnding,
                SelectAvatar);
        }

        private void ConfigureScenario(ScenarioDto source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.id)) return;
            scenario = source;
            session = EpisodeSession.GetOrCreate();
            session.ConfigureScenario(scenario.id);
            view.SetLandingLocation(
                scenario.location?.country,
                scenario.location?.region);
        }

        private void SelectAvatar(string avatarId)
        {
            selectedAvatarId = avatarId ?? string.Empty;
            view.SetSelectedAvatar(selectedAvatarId);
            view.ShowLandingError(string.Empty);
        }

        private bool BeginMission()
        {
            if (scenario == null)
            {
                view.ShowLandingError("The field mission is still loading.");
                return false;
            }

            if (!session.Progress.SetIdentity(
                    view.NameInput.text,
                    selectedAvatarId))
            {
                view.ShowLandingError(
                    "Enter a name (1-40 characters) and choose a portrait.");
                return false;
            }

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
            if (!view.GuideVisible)
            {
                ShowNextGuideCue();
            }
        }

        private void DismissGuide()
        {
            view.HideGuide();
            ShowNextGuideCue();
        }

        private void ShowNextGuideCue()
        {
            if (guideQueue.Count > 0)
            {
                view.ShowGuide(guideQueue.Dequeue());
            }
        }

        private void ToggleJudge()
        {
            if (view == null || !missionStarted) return;
            bool show = !view.JudgeVisible;
            if (show)
            {
                view.SetGlossaryVisible(false);
                view.HideCertificate(CertificateAvailable);
                view.SetJudgeText(BuildJudgeText());
            }
            view.SetJudgeVisible(show);
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
                QueueCue(
                    "return-home",
                    SaltLineNarrative.Ending(session.Progress.PlayerName));
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

        private static IEnumerator CaptureLanding(
            string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(1.1f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(
                Path.Combine(
                    directory,
                    "00_globe_identity.png"));
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(0);
        }
    }
}
