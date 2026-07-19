using System;
using System.Globalization;
using System.Text;

namespace AgriVerse.Client
{
    public enum FieldJournalSection
    {
        Sites,
        People,
        Plan,
        Sources
    }

    public sealed class FieldJournalPlanState
    {
        public string TargetSiteId { get; set; } = string.Empty;
        public string[] InterventionIds { get; set; } =
            Array.Empty<string>();
        public string[] SupportMeasureIds { get; set; } =
            Array.Empty<string>();
        public string Parameters { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public int RevisionCount { get; set; }
        public bool HasSimulation { get; set; }
        public bool HasFeedback { get; set; }
        public bool HasPolicyBrief { get; set; }

        public static FieldJournalPlanState From(
            PlanSession session)
        {
            if (session == null)
            {
                return new FieldJournalPlanState();
            }
            return new FieldJournalPlanState
            {
                TargetSiteId =
                    session.TargetSiteId ?? string.Empty,
                InterventionIds =
                    session.InterventionIds ??
                    Array.Empty<string>(),
                SupportMeasureIds =
                    session.SupportMeasures ??
                    Array.Empty<string>(),
                Parameters =
                    session.ParametersText ?? string.Empty,
                Rationale =
                    session.Rationale ?? string.Empty,
                RevisionCount = session.RevisionCount,
                HasSimulation =
                    !string.IsNullOrWhiteSpace(
                        session.SimulatorResultJson),
                HasFeedback =
                    !string.IsNullOrWhiteSpace(
                        session.FeedbackResultJson),
                HasPolicyBrief =
                    !string.IsNullOrWhiteSpace(
                        session.PolicyBriefResultJson)
            };
        }
    }

    /// <summary>
    /// Read-only field-journal projection over sanitized scenario and session state.
    /// It never mutates, recomputes, or renames authoritative values.
    /// </summary>
    public static class FieldJournalFormatter
    {
        public static string Format(
            FieldJournalSection section,
            ScenarioDto scenario,
            EvidenceNotebook evidence,
            InterviewNotebook interviews,
            FieldJournalPlanState plan)
        {
            switch (section)
            {
                case FieldJournalSection.People:
                    return FormatPeople(scenario, interviews);
                case FieldJournalSection.Plan:
                    return FormatPlan(scenario, plan);
                case FieldJournalSection.Sources:
                    return FormatSources(scenario);
                default:
                    return FormatSites(scenario, evidence);
            }
        }

        private static string FormatSites(
            ScenarioDto scenario,
            EvidenceNotebook evidence)
        {
            var text = new StringBuilder("SITES\n\n");
            if (evidence == null ||
                evidence.RecordedReadings.Count == 0)
            {
                return text.Append(
                    "No field readings recorded yet.").ToString();
            }

            string unit =
                scenario?.units?.salinity ?? string.Empty;
            foreach (RecordedReading reading in
                     evidence.RecordedReadings)
            {
                text.Append(reading.label).Append('\n')
                    .Append("Salinity  ")
                    .Append(
                        reading.salinity_gL.ToString(
                            "0.################",
                            CultureInfo.InvariantCulture))
                    .Append(' ').Append(unit).Append('\n')
                    .Append("Season  ").Append(reading.season)
                    .Append('\n')
                    .Append("Salt pattern  ")
                    .Append(Humanize(reading.seasonal_pattern))
                    .Append('\n')
                    .Append("Freshwater access  ")
                    .Append(reading.freshwater_access)
                    .Append('\n')
                    .Append(reading.note).Append('\n')
                    .Append("Source IDs  ")
                    .Append(string.Join(
                        ", ",
                        reading.source_ids ??
                        Array.Empty<string>()))
                    .Append("\n\n");
            }
            return text.ToString().TrimEnd();
        }

        private static string FormatPeople(
            ScenarioDto scenario,
            InterviewNotebook interviews)
        {
            var text = new StringBuilder("PEOPLE\n\n");
            StakeholderDto[] stakeholders =
                scenario?.stakeholders ??
                Array.Empty<StakeholderDto>();
            if (stakeholders.Length == 0)
            {
                return text.Append(
                    "No stakeholder records are available.")
                    .ToString();
            }

            foreach (StakeholderDto stakeholder in stakeholders)
            {
                if (stakeholder == null) continue;
                text.Append(stakeholder.name)
                    .Append("  ·  ")
                    .Append(stakeholder.role)
                    .Append('\n');
                var turns =
                    interviews?.ConversationFor(stakeholder.id) ??
                    Array.Empty<ConversationTurnDto>();
                if (turns.Count == 0)
                {
                    text.Append(
                        "No interview recorded yet.\n\n");
                    continue;
                }
                foreach (ConversationTurnDto turn in turns)
                {
                    text.Append(
                            turn.role == "student"
                                ? "You: "
                                : stakeholder.name + ": ")
                        .Append(turn.text)
                        .Append("\n\n");
                }
            }
            return text.ToString().TrimEnd();
        }

        private static string FormatPlan(
            ScenarioDto scenario,
            FieldJournalPlanState state)
        {
            state = state ?? new FieldJournalPlanState();
            var text = new StringBuilder("PLAN\n\n");
            if (string.IsNullOrWhiteSpace(state.TargetSiteId) &&
                state.InterventionIds.Length == 0 &&
                string.IsNullOrWhiteSpace(state.Rationale))
            {
                return text.Append(
                    "No proposal has been drafted yet.")
                    .ToString();
            }

            text.Append("Target site  ")
                .Append(SiteLabel(
                    scenario,
                    state.TargetSiteId))
                .Append("\n\nInterventions\n");
            AppendInterventions(
                text,
                scenario,
                state.InterventionIds);
            text.Append("\nSupport measures\n");
            AppendSupportMeasures(
                text,
                scenario,
                state.SupportMeasureIds);
            text.Append("\nParameters  ")
                .Append(EmptyFallback(state.Parameters))
                .Append("\n\nRationale\n")
                .Append(EmptyFallback(state.Rationale))
                .Append("\n\nRevision  ")
                .Append(state.RevisionCount)
                .Append("\nSimulation  ")
                .Append(state.HasSimulation ? "recorded" : "not run")
                .Append("\nFeedback  ")
                .Append(state.HasFeedback ? "recorded" : "not requested")
                .Append("\nPolicy brief  ")
                .Append(state.HasPolicyBrief ? "recorded" : "not generated");
            return text.ToString();
        }

        private static string FormatSources(ScenarioDto scenario)
        {
            var text = new StringBuilder("SOURCES\n\n");
            SourceDto[] sources =
                scenario?.sources ?? Array.Empty<SourceDto>();
            if (sources.Length == 0)
            {
                return text.Append(
                    "No source registry is available.").ToString();
            }
            foreach (SourceDto source in sources)
            {
                if (source == null) continue;
                text.Append('[').Append(source.id).Append("] ")
                    .Append(source.title).Append('\n')
                    .Append(source.publisher).Append('\n')
                    .Append(source.url).Append("\n\n");
            }
            return text.ToString().TrimEnd();
        }

        private static void AppendInterventions(
            StringBuilder text,
            ScenarioDto scenario,
            string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                text.Append("None selected\n");
                return;
            }
            foreach (string id in ids)
            {
                string label = id;
                foreach (InterventionDto intervention in
                         scenario?.interventions ??
                         Array.Empty<InterventionDto>())
                {
                    if (intervention != null &&
                        string.Equals(
                            intervention.id,
                            id,
                            StringComparison.Ordinal))
                    {
                        label = intervention.label;
                        break;
                    }
                }
                text.Append("• ").Append(label).Append('\n');
            }
        }

        private static void AppendSupportMeasures(
            StringBuilder text,
            ScenarioDto scenario,
            string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                text.Append("None selected\n");
                return;
            }
            foreach (string id in ids)
            {
                string label = id;
                foreach (SupportMeasureDto measure in
                         scenario?.support_measure_options ??
                         Array.Empty<SupportMeasureDto>())
                {
                    if (measure != null &&
                        string.Equals(
                            measure.id,
                            id,
                            StringComparison.Ordinal))
                    {
                        label = measure.description;
                        break;
                    }
                }
                text.Append("• ").Append(label).Append('\n');
            }
        }

        private static string SiteLabel(
            ScenarioDto scenario,
            string siteId)
        {
            foreach (TestSiteDto site in
                     scenario?.test_sites ??
                     Array.Empty<TestSiteDto>())
            {
                if (site != null &&
                    string.Equals(
                        site.id,
                        siteId,
                        StringComparison.Ordinal))
                {
                    return site.label;
                }
            }
            return EmptyFallback(siteId);
        }

        private static string EmptyFallback(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? "Not recorded"
                : value;

        private static string Humanize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace('_', ' ');
    }
}
