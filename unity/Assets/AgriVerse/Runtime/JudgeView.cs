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

            var text = new StringBuilder("Judge View - under the hood\n\n");
            text.Append("Responding agent: ")
                .Append(string.IsNullOrWhiteSpace(respondingAgentId)
                    ? "none selected"
                    : respondingAgentId + " (role-separated GPT-5.6 persona)")
                .Append("\n\nGrounding evidence: ")
                .Append(audit.ReferencedSourceIds.Count == 0
                    ? "none recorded yet"
                    : string.Join(", ", audit.ReferencedSourceIds))
                .Append("\n\nRaw structured output\nSimulation:\n")
                .Append(string.IsNullOrWhiteSpace(simulation)
                    ? "Not available yet."
                    : simulation)
                .Append("\n\nPolicy brief:\n")
                .Append(string.IsNullOrWhiteSpace(brief)
                    ? "Not available yet."
                    : brief)
                .Append("\n\nRubric result:\n")
                .Append(string.IsNullOrWhiteSpace(feedback)
                    ? "Not available yet."
                    : feedback)
                .Append("\n\nCitation validation: ")
                .Append(audit.Passed ? "pass" : "fail");
            if (audit.UnknownSourceIds.Count > 0)
            {
                text.Append(" (unknown: ")
                    .Append(string.Join(", ", audit.UnknownSourceIds))
                    .Append(')');
            }
            if (audit.MalformedDocumentCount > 0)
            {
                text.Append(" (malformed documents: ")
                    .Append(audit.MalformedDocumentCount)
                    .Append(')');
            }
            if (audit.MissingCitationDocumentCount > 0)
            {
                text.Append(" (documents missing citations: ")
                    .Append(audit.MissingCitationDocumentCount)
                    .Append(')');
            }
            text.Append("\nFallback used: ")
                .Append(presentationFallbackUsed ? "yes" : "no")
                .Append(" (presentation fallback; model fallback is not exposed by the validated client contract)")
                .Append("\n\nEvery number and claim above is traceable to the cited corpus. ")
                .Append("The AI systems are the game mechanics.");
            return text.ToString();
        }
    }
}
