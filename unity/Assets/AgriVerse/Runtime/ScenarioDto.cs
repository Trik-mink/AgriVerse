using System;
using UnityEngine;

namespace AgriVerse.Client
{
    [Serializable]
    public sealed class ScenarioDto
    {
        public string id = string.Empty;
        public string title = string.Empty;
        public TestSiteDto[] test_sites = Array.Empty<TestSiteDto>();
        public StakeholderDto[] stakeholders = Array.Empty<StakeholderDto>();
        public InterventionDto[] interventions = Array.Empty<InterventionDto>();
        public SupportMeasureDto[] support_measure_options = Array.Empty<SupportMeasureDto>();
        public string farmer_capital = string.Empty;
        public ScenarioUnitsDto units = new ScenarioUnitsDto();

        public static ScenarioDto FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new FormatException("The scenario response was empty.");
            }

            ScenarioDto scenario;
            try
            {
                scenario = JsonUtility.FromJson<ScenarioDto>(json);
            }
            catch (ArgumentException error)
            {
                throw new FormatException("The scenario response was not valid JSON.", error);
            }

            if (scenario == null ||
                string.IsNullOrWhiteSpace(scenario.id) ||
                string.IsNullOrWhiteSpace(scenario.title))
            {
                throw new FormatException("The scenario response is missing its id or title.");
            }

            return scenario;
        }
    }

    [Serializable]
    public sealed class TestSiteDto
    {
        public string id = string.Empty;
        public string label = string.Empty;
        public float salinity_gL;
        public string season = string.Empty;
        public string seasonal_pattern = string.Empty;
        public string freshwater_access = string.Empty;
        public string note = string.Empty;
        public MeasurementGroundingDto measurement_grounding = new MeasurementGroundingDto();
    }

    [Serializable]
    public sealed class MeasurementGroundingDto
    {
        public string[] source_ids = Array.Empty<string>();
    }

    [Serializable]
    public sealed class ScenarioUnitsDto
    {
        public string salinity = string.Empty;
    }

    [Serializable]
    public sealed class StakeholderDto
    {
        public string id = string.Empty;
        public string name = string.Empty;
        public string role = string.Empty;
        public string persona = string.Empty;
    }

    [Serializable]
    public sealed class InterventionDto
    {
        public string id = string.Empty;
        public string label = string.Empty;
        public string description = string.Empty;
        public string cost = string.Empty;
        public string income = string.Empty;
        public string sustainability = string.Empty;
        public string capital_need = string.Empty;
    }

    [Serializable]
    public sealed class SupportMeasureDto
    {
        public string id = string.Empty;
        public string description = string.Empty;
    }
}
