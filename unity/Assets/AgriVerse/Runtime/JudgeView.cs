using System;
using System.Collections.Generic;
using System.Text;

namespace AgriVerse.Client
{
    public sealed class CitationAuditResult
    {
        public bool Passed { get; set; }
        public IReadOnlyList<string> ReferencedSourceIds { get; set; } =
            Array.Empty<string>();
        public IReadOnlyList<string> UnknownSourceIds { get; set; } =
            Array.Empty<string>();
        public int MalformedDocumentCount { get; set; }
        public int MissingCitationDocumentCount { get; set; }
    }

    public static class CitationAudit
    {
        public static CitationAuditResult Validate(
            ScenarioDto scenario,
            IEnumerable<string> recordedSourceIds,
            params string[] rawJsonDocuments)
        {
            var known = new HashSet<string>(StringComparer.Ordinal);
            foreach (SourceDto source in scenario?.sources ?? Array.Empty<SourceDto>())
            {
                if (source != null && !string.IsNullOrWhiteSpace(source.id))
                {
                    known.Add(source.id);
                }
            }

            var referenced = new SortedSet<string>(StringComparer.Ordinal);
            if (recordedSourceIds != null)
            {
                foreach (string sourceId in recordedSourceIds)
                {
                    if (!string.IsNullOrWhiteSpace(sourceId))
                    {
                        referenced.Add(sourceId);
                    }
                }
            }

            int malformed = 0;
            int missingCitations = 0;
            foreach (string document in rawJsonDocuments ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(document)) continue;
                try
                {
                    var documentSources =
                        new SortedSet<string>(StringComparer.Ordinal);
                    Collect(
                        CanonicalJsonParser.Parse(document),
                        documentSources);
                    if (documentSources.Count == 0)
                    {
                        missingCitations++;
                    }
                    foreach (string sourceId in documentSources)
                    {
                        referenced.Add(sourceId);
                    }
                }
                catch (FormatException)
                {
                    malformed++;
                }
            }

            var unknown = new List<string>();
            foreach (string sourceId in referenced)
            {
                if (!known.Contains(sourceId)) unknown.Add(sourceId);
            }

            return new CitationAuditResult
            {
                Passed =
                    malformed == 0 &&
                    missingCitations == 0 &&
                    unknown.Count == 0,
                ReferencedSourceIds = new List<string>(referenced),
                UnknownSourceIds = unknown,
                MalformedDocumentCount = malformed,
                MissingCitationDocumentCount = missingCitations
            };
        }

        private static void Collect(
            CanonicalJsonValue value,
            ISet<string> sourceIds)
        {
            if (value.Kind == CanonicalJsonKind.Object)
            {
                foreach (KeyValuePair<string, CanonicalJsonValue> property in
                         value.Properties)
                {
                    if ((property.Key == "source_ids" ||
                         property.Key == "evidence_source_ids") &&
                        property.Value.Kind == CanonicalJsonKind.Array)
                    {
                        foreach (CanonicalJsonValue source in property.Value.Items)
                        {
                            if (!string.IsNullOrWhiteSpace(source.Text))
                            {
                                sourceIds.Add(source.Text);
                            }
                        }
                    }
                    Collect(property.Value, sourceIds);
                }
            }
            else if (value.Kind == CanonicalJsonKind.Array)
            {
                foreach (CanonicalJsonValue item in value.Items)
                {
                    Collect(item, sourceIds);
                }
            }
        }
    }

    public static class JudgeViewFormatter
    {
        public static string Format(
            ScenarioDto scenario,
            string respondingAgentId,
            bool presentationFallbackUsed,
            EvidenceNotebook evidence,
            PlanSession plan)
        {
            var recordedSources = new List<string>();
            if (evidence != null)
            {
                foreach (RecordedReading reading in evidence.RecordedReadings)
                {
                    recordedSources.AddRange(
                        reading.source_ids ?? Array.Empty<string>());
                }
            }

            string simulation = plan?.SimulatorResultJson ?? string.Empty;
            string feedback = plan?.FeedbackResultJson ?? string.Empty;
            string brief = plan?.PolicyBriefResultJson ?? string.Empty;
            CitationAuditResult audit = CitationAudit.Validate(
                scenario,
                recordedSources,
                simulation,
                feedback,
                brief);

            var text = new StringBuilder(
                "JUDGE VIEW · VALIDATED FIELD RECORD\n" +
                "How the evidence, agents, simulator, grader, and final brief connect.\n\n" +
                "AGENT & GROUNDING\n");
            text.Append("Responding stakeholder: ")
                .Append(string.IsNullOrWhiteSpace(respondingAgentId)
                    ? "none selected"
                    : HumanLabel(respondingAgentId) +
                      " · role-separated GPT-5.6 agent")
                .Append("\nCitation validation: ")
                .Append(audit.Passed ? "PASS" : "REVIEW REQUIRED")
                .Append("\nGrounding source IDs: ")
                .Append(audit.ReferencedSourceIds.Count == 0
                    ? "none recorded yet"
                    : string.Join(" · ", audit.ReferencedSourceIds));
            AppendSourceRegister(text, scenario, audit.ReferencedSourceIds);

            if (audit.UnknownSourceIds.Count > 0)
            {
                text.Append("\nUnknown source IDs: ")
                    .Append(string.Join(", ", audit.UnknownSourceIds))
                    .Append('.');
            }
            if (audit.MalformedDocumentCount > 0)
            {
                text.Append("\nMalformed validated documents: ")
                    .Append(audit.MalformedDocumentCount)
                    .Append('.');
            }
            if (audit.MissingCitationDocumentCount > 0)
            {
                text.Append("\nDocuments missing citations: ")
                    .Append(audit.MissingCitationDocumentCount)
                    .Append('.');
            }

            AppendDocument(
                text,
                "SIMULATOR · FIVE-YEAR CONSEQUENCES",
                simulation);
            AppendDocument(
                text,
                "GRADER · GROUNDED FEEDBACK",
                feedback);
            AppendDocument(
                text,
                "POLICY BRIEF · FINAL RECOMMENDATION",
                brief);

            text.Append("\n\nPRESENTATION INTEGRITY\n")
                .Append("Environment fallback: ")
                .Append(presentationFallbackUsed
                    ? "procedural fallback active"
                    : "primary presentation active")
                .Append("\nValidated output remains authoritative. " +
                        "The optional technical inspector exposes the exact stored JSON.");
            return EpisodeUiFactory.FormatModelText(text.ToString());
        }

        public static string FormatTechnicalDisclosure(PlanSession plan)
        {
            var text = new StringBuilder(
                "RAW VALIDATED JSON · OPTIONAL TECHNICAL INSPECTOR\n" +
                "These are the exact stored response documents. " +
                "They are shown for inspectability, not as the primary reading experience.");
            AppendRaw(
                text,
                "SIMULATOR RESULT",
                plan?.SimulatorResultJson);
            AppendRaw(
                text,
                "GRADER RESULT",
                plan?.FeedbackResultJson);
            AppendRaw(
                text,
                "POLICY BRIEF RESULT",
                plan?.PolicyBriefResultJson);
            return text.ToString();
        }

        private static void AppendDocument(
            StringBuilder text,
            string heading,
            string rawJson)
        {
            text.Append("\n\n").Append(heading).Append('\n');
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                text.Append("Not available yet.");
                return;
            }

            try
            {
                AppendValue(
                    text,
                    CanonicalJsonParser.Parse(rawJson),
                    string.Empty,
                    0);
            }
            catch (FormatException)
            {
                text.Append(
                    "The stored document could not be rendered as a validated record.");
            }
        }

        private static void AppendValue(
            StringBuilder text,
            CanonicalJsonValue value,
            string label,
            int depth)
        {
            string indent = new string(' ', depth * 2);
            if (value.Kind == CanonicalJsonKind.Object)
            {
                if (!string.IsNullOrWhiteSpace(label))
                {
                    text.Append(indent)
                        .Append(HumanLabel(label).ToUpperInvariant())
                        .Append('\n');
                }
                foreach (KeyValuePair<string, CanonicalJsonValue> property in
                         value.Properties)
                {
                    AppendValue(
                        text,
                        property.Value,
                        property.Key,
                        string.IsNullOrWhiteSpace(label)
                            ? depth
                            : depth + 1);
                }
                return;
            }

            if (value.Kind == CanonicalJsonKind.Array)
            {
                text.Append(indent)
                    .Append(HumanLabel(label).ToUpperInvariant())
                    .Append('\n');
                if (value.Items.Count == 0)
                {
                    text.Append(indent).Append("  None recorded.\n");
                    return;
                }
                for (int index = 0; index < value.Items.Count; index++)
                {
                    CanonicalJsonValue item = value.Items[index];
                    string itemLabel = ItemLabel(item, index);
                    if (item.Kind == CanonicalJsonKind.Object ||
                        item.Kind == CanonicalJsonKind.Array)
                    {
                        AppendValue(
                            text,
                            item,
                            itemLabel,
                            depth + 1);
                    }
                    else
                    {
                        text.Append(indent)
                            .Append("  • ")
                            .Append(SafeText(item.Text))
                            .Append('\n');
                    }
                }
                return;
            }

            string readableLabel =
                string.Equals(label, "overall", StringComparison.Ordinal)
                    ? "OVERALL FIT"
                    : HumanLabel(label);
            text.Append(indent)
                .Append(readableLabel)
                .Append(": ")
                .Append(SafeText(value.Text))
                .Append('\n');
        }

        private static string ItemLabel(
            CanonicalJsonValue item,
            int index)
        {
            if (item.Kind == CanonicalJsonKind.Object)
            {
                string[] identityFields =
                {
                    "year",
                    "rubric_id",
                    "category",
                    "title",
                    "claim"
                };
                foreach (string field in identityFields)
                {
                    if (item.Properties.TryGetValue(
                            field,
                            out CanonicalJsonValue identity) &&
                        !string.IsNullOrWhiteSpace(identity.Text))
                    {
                        return field == "year"
                            ? "Year " + identity.Text
                            : identity.Text;
                    }
                }
            }
            return "Item " + (index + 1);
        }

        private static void AppendSourceRegister(
            StringBuilder text,
            ScenarioDto scenario,
            IReadOnlyList<string> referencedSourceIds)
        {
            if (scenario?.sources == null ||
                referencedSourceIds == null ||
                referencedSourceIds.Count == 0)
            {
                return;
            }

            var referenced =
                new HashSet<string>(
                    referencedSourceIds,
                    StringComparer.Ordinal);
            bool headingWritten = false;
            foreach (SourceDto source in scenario.sources)
            {
                if (source == null ||
                    !referenced.Contains(source.id))
                {
                    continue;
                }
                if (!headingWritten)
                {
                    text.Append("\nSource register:");
                    headingWritten = true;
                }
                text.Append("\n• ")
                    .Append(source.id)
                    .Append(" · ")
                    .Append(string.IsNullOrWhiteSpace(source.title)
                        ? "Registered scenario source"
                        : SafeText(source.title));
            }
        }

        private static string HumanLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string[] parts = value.Replace('-', '_').Split('_');
            for (int index = 0; index < parts.Length; index++)
            {
                if (parts[index].Length == 0) continue;
                parts[index] =
                    char.ToUpperInvariant(parts[index][0]) +
                    parts[index].Substring(1);
            }
            return string.Join(" ", parts);
        }

        private static string SafeText(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? "not recorded"
                : value.Trim();

        private static void AppendRaw(
            StringBuilder text,
            string heading,
            string rawJson)
        {
            text.Append("\n\n").Append(heading).Append('\n')
                .Append(string.IsNullOrWhiteSpace(rawJson)
                    ? "Not available yet."
                    : rawJson);
        }
    }
}
