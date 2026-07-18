using System;
using System.Collections.Generic;
using UnityEngine;

namespace AgriVerse.Client
{
    [Serializable]
    public sealed class RecordedReading
    {
        public string site_id = string.Empty;
        public string label = string.Empty;
        public float salinity_gL;
        public string season = string.Empty;
        public string seasonal_pattern = string.Empty;
        public string freshwater_access = string.Empty;
        public string note = string.Empty;
        public string[] source_ids = Array.Empty<string>();
    }

    /// <summary>
    /// Scenario-scoped investigation evidence. It deliberately contains only the
    /// same public test-site fields returned by the Express scenario endpoint.
    /// </summary>
    public sealed class EvidenceNotebook
    {
        private readonly List<RecordedReading> recordedReadings = new List<RecordedReading>();

        public EvidenceNotebook(string scenarioId)
        {
            ScenarioId = scenarioId ?? string.Empty;
        }

        public string ScenarioId { get; }

        public IReadOnlyList<RecordedReading> RecordedReadings => recordedReadings;

        public bool Record(TestSiteDto site)
        {
            if (site == null || string.IsNullOrWhiteSpace(site.id) || HasRecorded(site.id))
            {
                return false;
            }

            string[] sourceIds = site.measurement_grounding == null ||
                                 site.measurement_grounding.source_ids == null
                ? Array.Empty<string>()
                : (string[])site.measurement_grounding.source_ids.Clone();

            recordedReadings.Add(new RecordedReading
            {
                site_id = site.id,
                label = site.label,
                salinity_gL = site.salinity_gL,
                season = site.season,
                seasonal_pattern = site.seasonal_pattern,
                freshwater_access = site.freshwater_access,
                note = site.note,
                source_ids = sourceIds
            });
            return true;
        }

        public bool HasRecorded(string siteId)
        {
            for (int index = 0; index < recordedReadings.Count; index++)
            {
                if (recordedReadings[index].site_id == siteId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool AreAllSitesRecorded(TestSiteDto[] sites)
        {
            if (sites == null || sites.Length == 0 || recordedReadings.Count != sites.Length)
            {
                return false;
            }

            for (int index = 0; index < sites.Length; index++)
            {
                if (sites[index] == null || !HasRecorded(sites[index].id))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Keeps evidence alive while the student moves through later scene stages.
    /// A different scenario starts a deliberately fresh notebook.
    /// </summary>
    public sealed class EvidenceNotebookSession : MonoBehaviour
    {
        private static EvidenceNotebookSession instance;
        private EvidenceNotebook notebook;

        public EvidenceNotebook Notebook => notebook;

        public static EvidenceNotebookSession GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<EvidenceNotebookSession>();
            if (instance != null)
            {
                return instance;
            }

            var sessionObject = new GameObject("EvidenceNotebookSession");
            instance = sessionObject.AddComponent<EvidenceNotebookSession>();
            return instance;
        }

        public void ConfigureScenario(string scenarioId)
        {
            if (notebook == null || notebook.ScenarioId != scenarioId)
            {
                notebook = new EvidenceNotebook(scenarioId);
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
