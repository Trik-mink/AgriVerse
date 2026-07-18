using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriVerse.Client
{
    public readonly struct SalinityVisualState
    {
        public SalinityVisualState(float exactValue, string unit, float normalizedToDanger)
        {
            ExactValue = exactValue;
            Unit = unit ?? string.Empty;
            NormalizedToDanger = normalizedToDanger;
        }

        public float ExactValue { get; }
        public string Unit { get; }
        public float NormalizedToDanger { get; }
    }

    public static class SalinityVisualMapper
    {
        public static SalinityVisualState Map(float value, KeyMetricDto metric)
        {
            float threshold = metric?.danger_threshold?.value ?? 0f;
            float normalized = 0f;
            if (threshold > 0f)
            {
                normalized = metric.direction_of_harm == "lower"
                    ? (value <= 0f ? 1f : threshold / value)
                    : value / threshold;
            }

            return new SalinityVisualState(
                value,
                metric?.unit ?? string.Empty,
                Mathf.Clamp01(normalized));
        }
    }

    public sealed class FutureYieldItem
    {
        public string CommodityId { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    public sealed class FutureYearPresentation
    {
        public string Year { get; set; } = string.Empty;
        public string SalinityValue { get; set; } = string.Empty;
        public string SalinityUnit { get; set; } = string.Empty;
        public string IncomeScore { get; set; } = string.Empty;
        public string SustainabilityScore { get; set; } = string.Empty;
        public string CostLevel { get; set; } = string.Empty;
        public string Narrative { get; set; } = string.Empty;
        public IReadOnlyList<FutureYieldItem> YieldItems { get; set; } =
            Array.Empty<FutureYieldItem>();
        public IReadOnlyList<string> EvidenceSourceIds { get; set; } =
            Array.Empty<string>();
    }

    public sealed class FutureWalkResult
    {
        public string Headline { get; set; } = string.Empty;
        public string OverallFit { get; set; } = string.Empty;
        public IReadOnlyList<FutureYearPresentation> Years { get; set; } =
            Array.Empty<FutureYearPresentation>();
    }

    public sealed class FutureWalkComparison
    {
        public FutureWalkResult Original { get; set; }
        public FutureWalkResult Revised { get; set; }
        public bool HasRevision => Original != null && Revised != null;
    }

    public static class FutureWalkMapper
    {
        public static FutureWalkComparison MapComparison(PlanSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            FutureWalkResult original =
                string.IsNullOrWhiteSpace(session.OriginalSimulatorResultJson)
                    ? null
                    : Map(session.OriginalSimulatorResultJson);
            FutureWalkResult revised =
                session.HasRevision &&
                !string.IsNullOrWhiteSpace(session.SimulatorResultJson)
                    ? Map(session.SimulatorResultJson)
                    : null;
            return new FutureWalkComparison
            {
                Original = original,
                Revised = revised
            };
        }

        public static FutureWalkResult Map(string simulatorJson)
        {
            CanonicalJsonValue root = CanonicalJsonParser.Parse(simulatorJson);
            IReadOnlyList<CanonicalJsonValue> yearValues = root.Property("years").Items;
            if (yearValues.Count != 5)
            {
                throw new FormatException("Future Walk requires exactly five simulator years.");
            }

            var years = new List<FutureYearPresentation>(yearValues.Count);
            foreach (CanonicalJsonValue yearValue in yearValues)
            {
                CanonicalJsonValue outcomes = yearValue.Property("outcomes");
                CanonicalJsonValue salinity = outcomes.Property("salinity");
                var yieldItems = new List<FutureYieldItem>();
                foreach (CanonicalJsonValue item in outcomes.Property("yield").Property("items").Items)
                {
                    yieldItems.Add(new FutureYieldItem
                    {
                        CommodityId = item.Property("commodity_id").Text,
                        Value = item.Property("value").Text,
                        Unit = item.Property("unit").Text
                    });
                }

                var sourceIds = new List<string>();
                foreach (CanonicalJsonValue source in yearValue.Property("evidence_source_ids").Items)
                {
                    sourceIds.Add(source.Text);
                }

                years.Add(new FutureYearPresentation
                {
                    Year = yearValue.Property("year").Text,
                    SalinityValue = salinity.Property("value").Text,
                    SalinityUnit = salinity.Property("unit").Text,
                    IncomeScore = outcomes.Property("income").Property("score").Text,
                    SustainabilityScore =
                        outcomes.Property("sustainability").Property("score").Text,
                    CostLevel = yearValue.Property("cost_level").Text,
                    Narrative = yearValue.Property("narrative").Text,
                    YieldItems = yieldItems,
                    EvidenceSourceIds = sourceIds
                });
            }

            return new FutureWalkResult
            {
                Headline = root.Property("headline").Text,
                OverallFit = root.Property("fit_assessment").Property("overall").Text,
                Years = years
            };
        }
    }
}
