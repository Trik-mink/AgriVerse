using NUnit.Framework;

namespace AgriVerse.Client.Tests
{
    public sealed class FieldJournalFormatterTests
    {
        [Test]
        public void EveryJournalSectionUsesRecordedScenarioAndSessionData()
        {
            TestSiteDto site = new TestSiteDto
            {
                id = "site-a",
                label = "Canal bank",
                salinity_gL = 1.25f,
                season = "dry",
                seasonal_pattern = "seasonal_peak",
                freshwater_access = "limited",
                note = "Recorded field note.",
                measurement_grounding =
                    new MeasurementGroundingDto
                    {
                        source_ids = new[] { "S1" }
                    }
            };
            StakeholderDto stakeholder = new StakeholderDto
            {
                id = "person-a",
                name = "Person A",
                role = "Farmer"
            };
            ScenarioDto scenario = new ScenarioDto
            {
                units = new ScenarioUnitsDto { salinity = "g/L" },
                test_sites = new[] { site },
                stakeholders = new[] { stakeholder },
                interventions = new[]
                {
                    new InterventionDto
                    {
                        id = "intervention-a",
                        label = "Intervention A"
                    }
                },
                support_measure_options = new[]
                {
                    new SupportMeasureDto
                    {
                        id = "support-a",
                        description = "Support A"
                    }
                },
                sources = new[]
                {
                    new SourceDto
                    {
                        id = "S1",
                        title = "Published source",
                        publisher = "Publisher",
                        url = "https://example.test/source"
                    }
                }
            };
            var evidence = new EvidenceNotebook("scenario");
            evidence.Record(site);
            var interviews = new InterviewNotebook("scenario");
            interviews.AddQuestion(
                stakeholder.id,
                "What is changing?");
            interviews.AddReply(
                stakeholder.id,
                "This is the recorded reply.");
            var plan = new FieldJournalPlanState
            {
                TargetSiteId = site.id,
                InterventionIds = new[] { "intervention-a" },
                SupportMeasureIds = new[] { "support-a" },
                Parameters = "Parameter text",
                Rationale = "Rationale text",
                RevisionCount = 1
            };

            string sites = FieldJournalFormatter.Format(
                FieldJournalSection.Sites,
                scenario,
                evidence,
                interviews,
                plan);
            string people = FieldJournalFormatter.Format(
                FieldJournalSection.People,
                scenario,
                evidence,
                interviews,
                plan);
            string proposal = FieldJournalFormatter.Format(
                FieldJournalSection.Plan,
                scenario,
                evidence,
                interviews,
                plan);
            string sources = FieldJournalFormatter.Format(
                FieldJournalSection.Sources,
                scenario,
                evidence,
                interviews,
                plan);

            Assert.That(sites, Does.Contain("1.25 g/L"));
            Assert.That(sites, Does.Contain("Recorded field note."));
            Assert.That(sites, Does.Contain("S1"));
            Assert.That(people, Does.Contain("What is changing?"));
            Assert.That(
                people,
                Does.Contain("This is the recorded reply."));
            Assert.That(proposal, Does.Contain("Canal bank"));
            Assert.That(proposal, Does.Contain("Intervention A"));
            Assert.That(proposal, Does.Contain("Support A"));
            Assert.That(proposal, Does.Contain("Rationale text"));
            Assert.That(proposal, Does.Contain("Revision  1"));
            Assert.That(sources, Does.Contain("Published source"));
            Assert.That(sources, Does.Contain("Publisher"));
            Assert.That(
                sources,
                Does.Contain("https://example.test/source"));
        }
    }
}
