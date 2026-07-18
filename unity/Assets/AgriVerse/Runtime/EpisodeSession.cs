using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriVerse.Client
{
    public enum EpisodeEndingChoice
    {
        None,
        ReturnHome,
        StayAnotherSeason
    }

    /// <summary>
    /// Presentation-only student state. Nothing here is sent to a scored endpoint.
    /// </summary>
    public sealed class EpisodeProgress
    {
        private readonly Dictionary<string, string> predictions =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public EpisodeProgress(string scenarioId)
        {
            ScenarioId = scenarioId ?? string.Empty;
        }

        public string ScenarioId { get; }
        public string PlayerName { get; private set; } = string.Empty;
        public string AvatarPresetId { get; private set; } = string.Empty;
        public EpisodeEndingChoice EndingChoice { get; private set; }
        public bool HasIdentity =>
            !string.IsNullOrWhiteSpace(PlayerName) &&
            !string.IsNullOrWhiteSpace(AvatarPresetId);

        public bool SetIdentity(string playerName, string avatarPresetId)
        {
            string trimmedName = playerName?.Trim() ?? string.Empty;
            string trimmedAvatar = avatarPresetId?.Trim() ?? string.Empty;
            if (trimmedName.Length < 1 || trimmedName.Length > 40 ||
                string.IsNullOrWhiteSpace(trimmedAvatar))
            {
                return false;
            }

            PlayerName = trimmedName;
            AvatarPresetId = trimmedAvatar;
            return true;
        }

        public bool RecordPrediction(string siteId, string predictionId)
        {
            if (string.IsNullOrWhiteSpace(siteId) ||
                string.IsNullOrWhiteSpace(predictionId) ||
                predictions.ContainsKey(siteId))
            {
                return false;
            }

            predictions.Add(siteId, predictionId);
            return true;
        }

        public bool HasPrediction(string siteId) =>
            !string.IsNullOrWhiteSpace(siteId) && predictions.ContainsKey(siteId);

        public string PredictionFor(string siteId) =>
            HasPrediction(siteId) ? predictions[siteId] : string.Empty;

        public void ChooseEnding(EpisodeEndingChoice choice)
        {
            EndingChoice = choice;
        }
    }

    public sealed class EpisodeSession : MonoBehaviour
    {
        private static EpisodeSession instance;

        public EpisodeProgress Progress { get; private set; }

        public static EpisodeSession GetOrCreate()
        {
            if (instance == null) instance = FindFirstObjectByType<EpisodeSession>();
            if (instance == null)
            {
                instance = new GameObject("EpisodeSession").AddComponent<EpisodeSession>();
            }
            return instance;
        }

        public void ConfigureScenario(string scenarioId)
        {
            if (Progress == null || Progress.ScenarioId != scenarioId)
            {
                Progress = new EpisodeProgress(scenarioId);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
