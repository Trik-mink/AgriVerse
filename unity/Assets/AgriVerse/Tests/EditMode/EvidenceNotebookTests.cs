using NUnit.Framework;

namespace AgriVerse.Client.Tests
{
    public sealed class EvidenceNotebookTests
    {
        [Test]
        public void RecordCopiesTheSanitizedReadingOnceAndKeepsTheSourceIds()
        {
            var notebook = new EvidenceNotebook("scenario-1");
            TestSiteDto site = Site("site-1", "S1", "S8");

            Assert.That(notebook.Record(site), Is.True);
            Assert.That(notebook.Record(site), Is.False);
            Assert.That(notebook.RecordedReadings, Has.Count.EqualTo(1));

            RecordedReading reading = notebook.RecordedReadings[0];
            Assert.That(reading.site_id, Is.EqualTo("site-1"));
            Assert.That(reading.salinity_gL, Is.EqualTo(3.5f));
            Assert.That(reading.season, Is.EqualTo("dry"));
            Assert.That(reading.seasonal_pattern, Is.EqualTo("brackish_dry_fresh_wet"));
            Assert.That(reading.freshwater_access, Is.EqualTo("medium"));
            Assert.That(reading.note, Is.EqualTo("Field note"));
            Assert.That(reading.source_ids, Is.EqualTo(new[] { "S1", "S8" }));
        }

        [Test]
        public void InterviewsUnlockOnlyWhenEveryScenarioSiteHasBeenRecorded()
        {
            var notebook = new EvidenceNotebook("scenario-1");
            TestSiteDto[] sites = { Site("site-1"), Site("site-2"), Site("site-3") };

            Assert.That(notebook.AreAllSitesRecorded(sites), Is.False);

            notebook.Record(sites[0]);
            notebook.Record(sites[1]);
            Assert.That(notebook.AreAllSitesRecorded(sites), Is.False);

            notebook.Record(sites[2]);
            Assert.That(notebook.AreAllSitesRecorded(sites), Is.True);
        }

        private static TestSiteDto Site(string id, params string[] sourceIds)
        {
            return new TestSiteDto
            {
                id = id,
                label = "Example site",
                salinity_gL = 3.5f,
                season = "dry",
                seasonal_pattern = "brackish_dry_fresh_wet",
                freshwater_access = "medium",
                note = "Field note",
                measurement_grounding = new MeasurementGroundingDto { source_ids = sourceIds }
            };
        }
    }

    public sealed class InterviewNotebookTests
    {
        [Test]
        public void RecordsOrderedQuestionAndReplyPairsAndUnlocksPlanningAfterEveryStakeholderReplies()
        {
            var notebook = new InterviewNotebook("scenario-1");
            StakeholderDto[] stakeholders =
            {
                Stakeholder("farmer"), Stakeholder("researcher"), Stakeholder("official")
            };

            notebook.AddQuestion("farmer", "What would help?");
            notebook.AddReply("farmer", "Reliable fresh water would help.");
            notebook.AddQuestion("researcher", "What should be measured?");
            notebook.AddReply("researcher", "Track salinity through the dry season.");
            Assert.That(notebook.AreAllStakeholdersInterviewed(stakeholders), Is.False);

            notebook.AddQuestion("official", "What can the district support?");
            notebook.AddReply("official", "We can coordinate a shared schedule.");

            Assert.That(notebook.ConversationFor("farmer"), Has.Count.EqualTo(2));
            Assert.That(notebook.ConversationFor("farmer")[0].role, Is.EqualTo("student"));
            Assert.That(notebook.ConversationFor("farmer")[1].role, Is.EqualTo("stakeholder"));
            Assert.That(notebook.AreAllStakeholdersInterviewed(stakeholders), Is.True);
        }

        private static StakeholderDto Stakeholder(string id)
        {
            return new StakeholderDto { id = id, name = id, role = "role", persona = "persona" };
        }
    }
}
