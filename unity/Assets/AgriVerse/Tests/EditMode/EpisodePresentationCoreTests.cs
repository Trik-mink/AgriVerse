using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class EpisodePresentationCoreTests
    {
        [Test]
        public void IdentityAndPredictionsStayLocalAndScenarioScoped()
        {
            var progress = new EpisodeProgress("scenario-1");

            Assert.That(progress.SetIdentity("  Lan Nguyen  ", "sun-amber"), Is.True);
            Assert.That(progress.PlayerName, Is.EqualTo("Lan Nguyen"));
            Assert.That(progress.AvatarPresetId, Is.EqualTo("sun-amber"));
            Assert.That(progress.RecordPrediction("mid", "on_the_edge"), Is.True);
            Assert.That(progress.RecordPrediction("mid", "safe"), Is.False);
            Assert.That(progress.PredictionFor("mid"), Is.EqualTo("on_the_edge"));
            Assert.That(progress.HasPrediction("mid"), Is.True);
        }

        [Test]
        public void IdentityRejectsBlankOrOversizedNames()
        {
            var progress = new EpisodeProgress("scenario-1");

            Assert.That(progress.SetIdentity("   ", "river-teal"), Is.False);
            Assert.That(progress.SetIdentity(new string('x', 41), "river-teal"), Is.False);
            Assert.That(progress.SetIdentity("Mai", string.Empty), Is.False);
        }

        [Test]
        public void AuthoredArrivalInterpolatesThePlayerWithoutChangingItsMeaning()
        {
            string arrival = SaltLineNarrative.Arrival("Lan");

            Assert.That(arrival, Does.Contain("Lan? Good - you're here."));
            Assert.That(arrival, Does.Contain("after tomorrow's tide"));
            Assert.That(arrival, Does.Not.Contain("[PLAYER]"));
        }

        [Test]
        public void SanitizedScenarioIncludesPresentationContextAndSourceRegistry()
        {
            const string json =
                "{\"id\":\"scenario-1\",\"title\":\"Scenario\",\"location\":{\"country\":\"Vietnam\",\"region\":\"Delta\"}," +
                "\"crisis\":{\"key_metric\":{\"id\":\"salinity\",\"name\":\"Salinity\",\"unit\":\"g/L\",\"direction_of_harm\":\"higher\",\"danger_threshold\":{\"operator\":\"greater_than_or_equal\",\"value\":4}}}," +
                "\"sources\":[{\"id\":\"S1\",\"title\":\"Source\",\"publisher\":\"Publisher\",\"url\":\"https://example.com\"}]," +
                "\"rubric\":{\"criteria\":[{\"id\":\"fit\",\"label\":\"Fit\",\"question\":\"Does it fit?\"}]}}";

            ScenarioDto scenario = ScenarioDto.FromJson(json);

            Assert.That(scenario.location.region, Is.EqualTo("Delta"));
            Assert.That(scenario.crisis.key_metric.unit, Is.EqualTo("g/L"));
            Assert.That(scenario.crisis.key_metric.danger_threshold.value, Is.EqualTo(4f));
            Assert.That(scenario.sources, Has.Length.EqualTo(1));
            Assert.That(scenario.sources[0].id, Is.EqualTo("S1"));
            Assert.That(scenario.rubric.criteria[0].label, Is.EqualTo("Fit"));
        }

        [Test]
        public void SalinityVisualMappingKeepsTheExactReadingAndUsesTheScenarioThreshold()
        {
            var metric = new KeyMetricDto
            {
                unit = "g/L",
                direction_of_harm = "higher",
                danger_threshold = new MetricThresholdDto { value = 4f }
            };

            SalinityVisualState safe = SalinityVisualMapper.Map(0.5f, metric);
            SalinityVisualState severe = SalinityVisualMapper.Map(12f, metric);

            Assert.That(safe.ExactValue, Is.EqualTo(0.5f));
            Assert.That(safe.Unit, Is.EqualTo("g/L"));
            Assert.That(safe.NormalizedToDanger, Is.EqualTo(0.125f).Within(.0001f));
            Assert.That(severe.NormalizedToDanger, Is.EqualTo(1f));
        }

        [Test]
        public void FutureWalkMappingPreservesFiveAuthoritativeYearsAndNumericTokenSpelling()
        {
            FutureWalkResult result = FutureWalkMapper.Map(SimulatorJson());

            Assert.That(result.Years, Has.Count.EqualTo(5));
            Assert.That(result.Years[0].Year, Is.EqualTo("1"));
            Assert.That(result.Years[0].SalinityValue, Is.EqualTo("1.2300"));
            Assert.That(result.Years[0].SalinityUnit, Is.EqualTo("g/L"));
            Assert.That(result.Years[4].Narrative, Is.EqualTo("Year 5 narrative"));
            Assert.That(result.Years[4].EvidenceSourceIds, Is.EqualTo(new[] { "S1", "S5" }));
        }

        [Test]
        public void PlanSessionRetainsTheOriginalFutureWhenARevisionReplacesTheCurrentOne()
        {
            GameObject root = new GameObject("PlanSessionHistoryTest");
            PlanSession session = root.AddComponent<PlanSession>();
            session.ConfigureScenario("scenario-1");
            var original = new SimulatorResultSummaryDto
            {
                scenario_id = "scenario-1",
                fit_assessment = new FitAssessmentDto { overall = "mismatch" }
            };
            var revised = new SimulatorResultSummaryDto
            {
                scenario_id = "scenario-1",
                fit_assessment = new FitAssessmentDto { overall = "fit" }
            };

            session.StoreSimulatorResult("original-json", original);
            session.StoreSimulatorResult("revised-json", revised);

            Assert.That(session.OriginalSimulatorResultJson, Is.EqualTo("original-json"));
            Assert.That(session.SimulatorResultJson, Is.EqualTo("revised-json"));
            Assert.That(session.OriginalSimulatorResult.fit_assessment.overall, Is.EqualTo("mismatch"));
            Assert.That(session.RevisionCount, Is.EqualTo(1));
            Assert.That(session.HasRevision, Is.True);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void FutureWalkComparisonMapsOriginalAndRevisedWithoutRecomputing()
        {
            GameObject root = new GameObject("FutureWalkComparisonTest");
            PlanSession session = root.AddComponent<PlanSession>();
            session.ConfigureScenario("scenario-1");
            string originalJson = SimulatorJson().Replace(
                "\"headline\":\"Headline\"",
                "\"headline\":\"Original future\"");
            string revisedJson = SimulatorJson().Replace(
                "\"headline\":\"Headline\"",
                "\"headline\":\"Revised future\"");
            session.StoreSimulatorResult(
                originalJson,
                new SimulatorResultSummaryDto());
            session.StoreSimulatorResult(
                revisedJson,
                new SimulatorResultSummaryDto());

            FutureWalkComparison comparison =
                FutureWalkMapper.MapComparison(session);

            Assert.That(comparison.HasRevision, Is.True);
            Assert.That(comparison.Original.Headline, Is.EqualTo("Original future"));
            Assert.That(comparison.Revised.Headline, Is.EqualTo("Revised future"));
            Assert.That(
                comparison.Original.Years[0].SalinityValue,
                Is.EqualTo("1.2300"));
            Object.DestroyImmediate(root);
        }

        private static string SimulatorJson()
        {
            var years = new StringBuilder();
            for (int year = 1; year <= 5; year++)
            {
                if (year > 1) years.Append(',');
                years.Append("{\"year\":").Append(year)
                    .Append(",\"outcomes\":{\"salinity\":{\"value\":1.2300,\"unit\":\"g/L\"},")
                    .Append("\"yield\":{\"items\":[{\"commodity_id\":\"rice\",\"value\":5.5,\"unit\":\"t/ha\"}]},")
                    .Append("\"income\":{\"score\":57},\"sustainability\":{\"score\":63}},")
                    .Append("\"cost_level\":\"low\",\"narrative\":\"Year ").Append(year)
                    .Append(" narrative\",\"evidence_source_ids\":[\"S1\",\"S5\"]}");
            }

            return "{\"headline\":\"Headline\",\"fit_assessment\":{\"salinity\":\"fit\",\"seasonality\":\"fit\"," +
                   "\"freshwater\":\"fit\",\"farmer_capital\":\"fit\",\"overall\":\"fit\"},\"years\":[" +
                   years + "],\"tradeoffs\":[]}";
        }
    }
}
