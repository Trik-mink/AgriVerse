using System;
using UnityEngine;

namespace AgriVerse.Client
{
    [Serializable]
    public sealed class ScenarioDto
    {
        public string id = string.Empty;
        public string title = string.Empty;
        public LocationDto location = new LocationDto();
        public CrisisDto crisis = new CrisisDto();
        public TestSiteDto[] test_sites = Array.Empty<TestSiteDto>();
        public StakeholderDto[] stakeholders = Array.Empty<StakeholderDto>();
        public InterventionDto[] interventions = Array.Empty<InterventionDto>();
        public SupportMeasureDto[] support_measure_options = Array.Empty<SupportMeasureDto>();
        public SourceDto[] sources = Array.Empty<SourceDto>();
        public RubricDto rubric = new RubricDto();
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

            scenario.location = scenario.location ?? new LocationDto();
            scenario.crisis = scenario.crisis ?? new CrisisDto();
            scenario.crisis.key_metric = scenario.crisis.key_metric ?? new KeyMetricDto();
            scenario.crisis.key_metric.danger_threshold =
                scenario.crisis.key_metric.danger_threshold ?? new MetricThresholdDto();
            scenario.test_sites = scenario.test_sites ?? Array.Empty<TestSiteDto>();
            scenario.stakeholders = scenario.stakeholders ?? Array.Empty<StakeholderDto>();
            scenario.interventions = scenario.interventions ?? Array.Empty<InterventionDto>();
            scenario.support_measure_options =
                scenario.support_measure_options ?? Array.Empty<SupportMeasureDto>();
            scenario.sources = scenario.sources ?? Array.Empty<SourceDto>();
            scenario.rubric = scenario.rubric ?? new RubricDto();
            scenario.rubric.criteria = scenario.rubric.criteria ?? Array.Empty<RubricCriterionDto>();
            scenario.units = scenario.units ?? new ScenarioUnitsDto();
            if (string.IsNullOrWhiteSpace(scenario.units.salinity))
            {
                scenario.units.salinity = scenario.crisis.key_metric.unit;
            }

            return scenario;
        }
    }

    [Serializable]
    public sealed class LocationDto
    {
        public string country = string.Empty;
        public string region = string.Empty;
        public string env_asset = string.Empty;
    }

    [Serializable]
    public sealed class CrisisDto
    {
        public string type = string.Empty;
        public string summary = string.Empty;
        public string driver_summary = string.Empty;
        public KeyMetricDto key_metric = new KeyMetricDto();
    }

    [Serializable]
    public sealed class KeyMetricDto
    {
        public string id = string.Empty;
        public string name = string.Empty;
        public string unit = string.Empty;
        public string direction_of_harm = string.Empty;
        public MetricThresholdDto danger_threshold = new MetricThresholdDto();
    }

    [Serializable]
    public sealed class MetricThresholdDto
    {
        public string @operator = string.Empty;
        public float value;
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

    [Serializable]
    public sealed class SourceDto
    {
        public string id = string.Empty;
        public string title = string.Empty;
        public string publisher = string.Empty;
        public string url = string.Empty;
    }

    [Serializable]
    public sealed class RubricDto
    {
        public RubricCriterionDto[] criteria = Array.Empty<RubricCriterionDto>();
    }

    [Serializable]
    public sealed class RubricCriterionDto
    {
        public string id = string.Empty;
        public string label = string.Empty;
        public string question = string.Empty;
    }
}
