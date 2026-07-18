using System;
using NUnit.Framework;

namespace AgriVerse.Client.Tests
{
    public sealed class ScenarioDtoTests
    {
        [Test]
        public void FromJsonReadsTheScenarioIdentityAndIgnoresUnconsumedFields()
        {
            const string json =
                "{\"id\":\"scenario-1\",\"title\":\"A scenario title\",\"stakeholders\":[{\"name\":\"Example\"}]}";

            ScenarioDto scenario = ScenarioDto.FromJson(json);

            Assert.That(scenario.id, Is.EqualTo("scenario-1"));
            Assert.That(scenario.title, Is.EqualTo("A scenario title"));
        }

        [Test]
        public void FromJsonReadsSanitizedTestSiteFieldsUsedByTheWaterTestingActivity()
        {
            const string json =
                "{\"id\":\"scenario-1\",\"title\":\"A scenario title\",\"test_sites\":[{\"id\":\"site-1\",\"label\":\"Example site\",\"salinity_gL\":3.5,\"season\":\"dry\",\"seasonal_pattern\":\"brackish_dry_fresh_wet\",\"freshwater_access\":\"medium\",\"note\":\"A field note.\",\"measurement_grounding\":{\"source_ids\":[\"S1\",\"S8\"]}}]}";

            ScenarioDto scenario = ScenarioDto.FromJson(json);

            Assert.That(scenario.test_sites, Has.Length.EqualTo(1));
            Assert.That(scenario.test_sites[0].salinity_gL, Is.EqualTo(3.5f));
            Assert.That(scenario.test_sites[0].seasonal_pattern, Is.EqualTo("brackish_dry_fresh_wet"));
            Assert.That(scenario.test_sites[0].freshwater_access, Is.EqualTo("medium"));
            Assert.That(scenario.test_sites[0].note, Is.EqualTo("A field note."));
            Assert.That(scenario.test_sites[0].measurement_grounding.source_ids, Is.EqualTo(new[] { "S1", "S8" }));
        }

        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"id\":\"scenario-1\"}")]
        public void FromJsonRejectsResponsesWithoutAUsableTitle(string json)
        {
            Assert.Throws<FormatException>(() => ScenarioDto.FromJson(json));
        }
    }
}
