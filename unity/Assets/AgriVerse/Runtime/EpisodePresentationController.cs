using System.Collections;
using System.Collections.Generic;
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

        public bool LandingVisible => view != null && view.LandingVisible;
        public bool GuideVisible => view != null && view.GuideVisible;
        public bool GlossaryVisible => view != null && view.GlossaryVisible;
        public string GuideTextForTesting => view?.GuideText ?? string.Empty;
        public string GlossaryTextForTesting => view?.GlossaryText ?? string.Empty;

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
                    yield break;
                }
                yield return null;
            }
        }

        private void Update()
        {
            if (!missionStarted || scenario == null) return;
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
                QueueCue("planning-intro", SaltLineNarrative.PlanningIntro);
            }
            else if (stage == RuntimeActivityStage.Consequences)
            {
                QueueCue(
                    "consequences-intro",
                    SaltLineNarrative.ConsequencesIntro);
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

        public void ToggleGlossary()
        {
            if (view == null || !missionStarted) return;
            view.SetGlossaryVisible(!view.GlossaryVisible);
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
                SelectAvatar);
        }

        private void ConfigureScenario(ScenarioDto source)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.id)) return;
            scenario = source;
            session = EpisodeSession.GetOrCreate();
            session.ConfigureScenario(scenario.id);
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
            QueueCue(
                "arrival",
                SaltLineNarrative.Arrival(session.Progress.PlayerName));
            QueueCue(
                "investigation-intro",
                SaltLineNarrative.InvestigationIntro);
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
    }
}
