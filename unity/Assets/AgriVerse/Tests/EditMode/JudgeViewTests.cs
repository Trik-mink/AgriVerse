using NUnit.Framework;

namespace AgriVerse.Client.Tests
{
    public sealed class JudgeViewTests
    {
        [Test]
        public void CitationAuditPassesWhenEveryStructuredSourceIdIsRegistered()
        {
            ScenarioDto scenario = ScenarioWithSources("S1", "S5");
            const string simulation =
                "{\"years\":[{\"evidence_source_ids\":[\"S1\",\"S5\"]}]}";
            const string feedback =
                "{\"key_insight\":{\"evidence\":{\"source_ids\":[\"S5\"],\"simulation_years\":[1]}}}";

            CitationAuditResult result = CitationAudit.Validate(
                scenario,
                new[] { "S1" },
                simulation,
                feedback);

            Assert.That(result.Passed, Is.True);
            Assert.That(result.ReferencedSourceIds, Is.EqualTo(new[] { "S1", "S5" }));
            Assert.That(result.UnknownSourceIds, Is.Empty);
        }

        [Test]
        public void CitationAuditReportsUnknownIdsWithoutChangingTheRawOutput()
        {
            ScenarioDto scenario = ScenarioWithSources("S1");
            const string raw =
                "{\"evidence\":{\"source_ids\":[\"S1\",\"S99\"]}}";

            CitationAuditResult result = CitationAudit.Validate(
                scenario,
                null,
                raw);

            Assert.That(result.Passed, Is.False);
            Assert.That(result.UnknownSourceIds, Is.EqualTo(new[] { "S99" }));
            Assert.That(raw, Does.Contain("\"S99\""));
        }

        [Test]
        public void CitationAuditFailsAResultDocumentWithNoCitationField()
        {
            ScenarioDto scenario = ScenarioWithSources("S1");
            const string simulation =
                "{\"years\":[{\"narrative\":\"No citation attached\"}]}";

            CitationAuditResult result = CitationAudit.Validate(
                scenario,
                new[] { "S1" },
                simulation);

            Assert.That(result.Passed, Is.False);
            Assert.That(result.MissingCitationDocumentCount, Is.EqualTo(1));
        }

        [Test]
        public void PrimaryJudgeViewIsHumanReadableAndRawJsonIsSecondary()
        {
            ScenarioDto scenario = ScenarioWithSources("S1", "S5");
            var planObject = new UnityEngine.GameObject("JudgePlanSession");
            PlanSession plan = planObject.AddComponent<PlanSession>();
            plan.ConfigureScenario(scenario.id);
            plan.StoreSimulatorResult(
                "{\"headline\":\"Fields recover steadily\",\"fit_assessment\":{\"salinity\":\"fit\",\"seasonality\":\"fit\",\"freshwater\":\"fit\",\"farmer_capital\":\"fit\",\"overall\":\"fit\"},\"years\":[{\"year\":1,\"salinity\":{\"value\":3,\"unit\":\"g/L\"},\"income_score\":61,\"sustainability_score\":72,\"cost_level\":\"medium\",\"narrative\":\"The first season stabilizes.\",\"evidence_source_ids\":[\"S1\"]}],\"tradeoffs\":[\"Upfront coordination\"]}",
                new SimulatorResultSummaryDto());
            plan.StoreFeedbackResult(
                "{\"rubric_results\":[{\"rubric_id\":\"salinity_fit\",\"rating\":\"strong\",\"feedback\":\"The plan matches the measured water.\",\"evidence\":{\"source_ids\":[\"S1\"],\"simulation_years\":[1]}}],\"key_insight\":{\"text\":\"Keep the freshwater support explicit.\",\"evidence\":{\"source_ids\":[\"S5\"],\"simulation_years\":[1]}},\"revision_prompt\":\"Who maintains the support?\",\"encouragement\":\"The revision is evidence-led.\"}");
            plan.StorePolicyBriefResult(
                "{\"title\":\"A field plan\",\"problem_statement\":\"Seasonal salt pressure threatens the plot.\",\"evidence\":[{\"claim\":\"The reading is grounded.\",\"source_ids\":[\"S1\"]}],\"recommended_solution\":{\"summary\":\"Use a staged field response.\",\"evidence\":{\"source_ids\":[\"S5\"],\"simulation_years\":[1]}},\"next_steps\":[\"Monitor the next season\"]}");

            try
            {
                string primary = JudgeViewFormatter.Format(
                    scenario,
                    "researcher",
                    false,
                    null,
                    plan);
                string technical =
                    JudgeViewFormatter.FormatTechnicalDisclosure(plan);

                Assert.That(primary, Does.Contain("SIMULATOR"));
                Assert.That(primary, Does.Contain("Fields recover steadily"));
                Assert.That(primary, Does.Contain("OVERALL FIT"));
                Assert.That(primary, Does.Contain("GRADER"));
                Assert.That(primary, Does.Contain("SALINITY FIT"));
                Assert.That(primary, Does.Contain("POLICY BRIEF"));
                Assert.That(primary, Does.Contain("S1"));
                Assert.That(primary, Does.Not.Contain("{\""));
                Assert.That(primary, Does.Not.Contain("**"));
                Assert.That(
                    technical,
                    Does.Contain("RAW VALIDATED JSON"));
                Assert.That(
                    technical,
                    Does.Contain("{\"headline\""));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(planObject);
            }
        }

        private static ScenarioDto ScenarioWithSources(params string[] ids)
        {
            var sources = new SourceDto[ids.Length];
            for (int index = 0; index < ids.Length; index++)
            {
                sources[index] = new SourceDto { id = ids[index] };
            }
            return new ScenarioDto
            {
                id = "scenario-1",
                title = "Scenario",
                sources = sources
            };
        }
    }
}
