using System;
using UnityEngine;

namespace AgriVerse.Client
{
    [Serializable]
    public sealed class FitAssessmentDto
    {
        public string salinity = string.Empty;
        public string seasonality = string.Empty;
        public string freshwater = string.Empty;
        public string farmer_capital = string.Empty;
        public string overall = string.Empty;
    }

    [Serializable]
    public sealed class SimulatorResultSummaryDto
    {
        public string contract_version = string.Empty;
        public string scenario_id = string.Empty;
        public string intervention_summary = string.Empty;
        public FitAssessmentDto fit_assessment = new FitAssessmentDto();
        public string headline = string.Empty;
    }

    public sealed class PlanSession : MonoBehaviour
    {
        private static PlanSession instance;
        public string ScenarioId { get; private set; }
        public string TargetSiteId { get; set; }
        public string[] InterventionIds { get; set; } = Array.Empty<string>();
        public string[] SupportMeasures { get; set; } = Array.Empty<string>();
        public string ParametersText { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public string SimulatorResultJson { get; private set; } = string.Empty;
        public SimulatorResultSummaryDto SimulatorResult { get; private set; }
        public string FeedbackResultJson { get; private set; } = string.Empty;
        public string PolicyBriefResultJson { get; private set; } = string.Empty;

        public static PlanSession GetOrCreate()
        {
            if (instance == null) instance = FindFirstObjectByType<PlanSession>();
            if (instance == null) instance = new GameObject("PlanSession").AddComponent<PlanSession>();
            return instance;
        }

        public void ConfigureScenario(string scenarioId)
        {
            if (ScenarioId == scenarioId) return;
            ScenarioId = scenarioId;
            TargetSiteId = string.Empty; InterventionIds = Array.Empty<string>(); SupportMeasures = Array.Empty<string>();
            ParametersText = string.Empty; Rationale = string.Empty; SimulatorResultJson = string.Empty; SimulatorResult = null;
            FeedbackResultJson = string.Empty; PolicyBriefResultJson = string.Empty;
        }

        public void StoreSimulatorResult(string rawJson, SimulatorResultSummaryDto result)
        {
            SimulatorResultJson = rawJson;
            SimulatorResult = result;
            FeedbackResultJson = string.Empty;
            PolicyBriefResultJson = string.Empty;
        }

        public void StoreFeedbackResult(string rawJson)
        {
            FeedbackResultJson = rawJson;
            PolicyBriefResultJson = string.Empty;
        }

        public void StorePolicyBriefResult(string rawJson) => PolicyBriefResultJson = rawJson;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
        }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
