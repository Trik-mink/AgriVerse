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
