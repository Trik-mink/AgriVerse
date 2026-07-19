using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    public readonly struct FutureWalkVisualState
    {
        public FutureWalkVisualState(
            string year,
            string salinityValue,
            string salinityUnit,
            IReadOnlyList<string> evidenceSourceIds,
            Color fieldTint,
            float fieldDensity)
        {
            Year = year ?? string.Empty;
            SalinityValue = salinityValue ?? string.Empty;
            SalinityUnit = salinityUnit ?? string.Empty;
            EvidenceSourceIds =
                evidenceSourceIds ?? Array.Empty<string>();
            FieldTint = fieldTint;
            FieldDensity = fieldDensity;
        }

        public string Year { get; }
        public string SalinityValue { get; }
        public string SalinityUnit { get; }
        public IReadOnlyList<string> EvidenceSourceIds { get; }
        public Color FieldTint { get; }
        public float FieldDensity { get; }
    }

    /// <summary>
    /// Maps exact validated year values to a restrained visual treatment. Display strings
    /// remain untouched; normalized values affect presentation only.
    /// </summary>
    public static class FutureWalkVisualMapper
    {
        private static readonly Color HealthyRice =
            new Color(.76f, .88f, .58f, 1f);
        private static readonly Color StressedRice =
            new Color(.76f, .57f, .30f, 1f);

        public static FutureWalkVisualState Map(
            FutureYearPresentation year,
            KeyMetricDto metric)
        {
            if (year == null)
            {
                throw new ArgumentNullException(nameof(year));
            }
            float.TryParse(
                year.SalinityValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float salinity);
            float.TryParse(
                year.SustainabilityScore,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float sustainability);
            SalinityVisualState salinityState =
                SalinityVisualMapper.Map(salinity, metric);
            float health = Mathf.Clamp01(
                .25f +
                Mathf.Clamp01(sustainability / 100f) * .75f -
                salinityState.NormalizedToDanger * .25f);
            return new FutureWalkVisualState(
                year.Year,
                year.SalinityValue,
                year.SalinityUnit,
                year.EvidenceSourceIds,
                Color.Lerp(StressedRice, HealthyRice, health),
                Mathf.Lerp(.45f, 1f, health));
        }
    }

    /// <summary>
    /// Presentation-only Future Walk. It observes the canonical PlanSession JSON and the
    /// existing consequence year navigation, then applies reversible visual state to the
    /// instanced paddy fields. It never writes scored state or calls an API.
    /// </summary>
    public sealed class Episode3DFutureWalkController : MonoBehaviour
    {
        [SerializeField] private InstancedVegetationField[] paddyFields =
            Array.Empty<InstancedVegetationField>();

        private GameObject panel;
        private Text content;
        private Button originalButton;
        private Button revisedButton;
        private PlanSession session;
        private InvestigationController investigation;
        private ConsequencesController consequences;
        private string observedSimulatorJson = string.Empty;
        private int observedYear = -1;
        private bool showingRevised;

        public bool ShowingRevised => showingRevised;
        public string DisplayedText =>
            content == null ? string.Empty : content.text;

        public void Configure(
            InstancedVegetationField[] fields)
        {
            paddyFields =
                fields ?? Array.Empty<InstancedVegetationField>();
        }

        private void Awake()
        {
            BuildInterface();
            session = PlanSession.GetOrCreate();
        }

        private void Update()
        {
            if (investigation == null)
            {
                investigation =
                    FindFirstObjectByType<InvestigationController>();
            }
            if (consequences == null)
            {
                consequences =
                    FindFirstObjectByType<ConsequencesController>();
            }
            bool visible =
                consequences != null &&
                consequences.ConsequencesVisible &&
                !string.IsNullOrWhiteSpace(
                    session.SimulatorResultJson);
            panel.SetActive(visible);
            if (!visible) return;

            if (!string.Equals(
                    observedSimulatorJson,
                    session.SimulatorResultJson,
                    StringComparison.Ordinal))
            {
                observedSimulatorJson =
                    session.SimulatorResultJson;
                showingRevised = session.HasRevision;
                observedYear = -1;
            }
            if (observedYear != consequences.CurrentYearIndex)
            {
                observedYear = consequences.CurrentYearIndex;
                Refresh();
            }
            revisedButton.interactable = session.HasRevision;
        }

        public void ShowOriginal()
        {
            if (showingRevised)
            {
                showingRevised = false;
                Refresh();
            }
        }

        public void ShowRevised()
        {
            if (session.HasRevision && !showingRevised)
            {
                showingRevised = true;
                Refresh();
            }
        }

        private void Refresh()
        {
            string json =
                showingRevised && session.HasRevision
                    ? session.SimulatorResultJson
                    : session.OriginalSimulatorResultJson;
            if (string.IsNullOrWhiteSpace(json))
            {
                json = session.SimulatorResultJson;
            }

            FutureWalkResult result;
            try
            {
                result = FutureWalkMapper.Map(json);
            }
            catch (Exception error)
                when (error is FormatException ||
                      error is InvalidOperationException)
            {
                RuntimeScrollableContent.SetText(
                    content,
                    "The stored future could not be displayed: " +
                    error.Message);
                return;
            }
            int yearIndex = Mathf.Clamp(
                consequences.CurrentYearIndex,
                0,
                result.Years.Count - 1);
            FutureYearPresentation year =
                result.Years[yearIndex];
            KeyMetricDto metric =
                investigation?.Scenario?.crisis?.key_metric;
            FutureWalkVisualState visual =
                FutureWalkVisualMapper.Map(year, metric);
            foreach (InstancedVegetationField field in paddyFields)
            {
                field?.SetFuturePresentation(
                    visual.FieldTint,
                    visual.FieldDensity);
            }

            var text = new StringBuilder();
            text.Append(
                    showingRevised
                        ? "REVISED FUTURE"
                        : "ORIGINAL FUTURE")
                .Append(" · YEAR ")
                .Append(year.Year)
                .Append("\n\n")
                .Append(result.Headline)
                .Append("\n\nOverall fit  ")
                .Append(result.OverallFit)
                .Append("\nSalinity  ")
                .Append(year.SalinityValue)
                .Append(' ')
                .Append(year.SalinityUnit)
                .Append("\nIncome score  ")
                .Append(year.IncomeScore)
                .Append("\nSustainability score  ")
                .Append(year.SustainabilityScore)
                .Append("\nCost level  ")
                .Append(year.CostLevel)
                .Append("\n\nYields");
            foreach (FutureYieldItem item in year.YieldItems)
            {
                text.Append("\n")
                    .Append(item.CommodityId)
                    .Append("  ")
                    .Append(item.Value)
                    .Append(' ')
                    .Append(item.Unit);
            }
            text.Append("\n\n")
                .Append(year.Narrative)
                .Append("\n\nEvidence source IDs  ")
                .Append(string.Join(
                    ", ",
                    year.EvidenceSourceIds));
            RuntimeScrollableContent.SetText(
                content,
                text.ToString());

            originalButton.GetComponent<Image>().color =
                showingRevised
                    ? EpisodeUiFactory.RiverTeal
                    : EpisodeUiFactory.Amber;
            revisedButton.GetComponent<Image>().color =
                showingRevised
                    ? EpisodeUiFactory.Amber
                    : EpisodeUiFactory.RiverTeal;
        }

        private void BuildInterface()
        {
            EpisodeUiFactory.EnsureEventSystem();
            GameObject canvasObject = new GameObject(
                "FutureWalkCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12;
            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            Image card = EpisodeUiFactory.Panel(
                canvas.transform,
                "FutureWalkCard",
                new Color(.018f, .11f, .12f, .92f),
                true);
            EpisodeUiFactory.Stretch(
                card.rectTransform,
                new Vector2(.66f, .49f),
                new Vector2(.975f, .88f));
            Text title = EpisodeUiFactory.Text(
                card.transform,
                "FutureWalkTitle",
                21,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.OffWhite);
            title.text = "FUTURE WALK";
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.06f, .89f),
                new Vector2(.94f, .98f));

            originalButton = EpisodeUiFactory.Button(
                card.transform,
                "OriginalFuture",
                "ORIGINAL",
                EpisodeUiFactory.Amber,
                13);
            EpisodeUiFactory.Stretch(
                originalButton.GetComponent<RectTransform>(),
                new Vector2(.06f, .81f),
                new Vector2(.48f, .88f));
            originalButton.onClick.AddListener(ShowOriginal);
            revisedButton = EpisodeUiFactory.Button(
                card.transform,
                "RevisedFuture",
                "REVISED",
                EpisodeUiFactory.RiverTeal,
                13);
            EpisodeUiFactory.Stretch(
                revisedButton.GetComponent<RectTransform>(),
                new Vector2(.52f, .81f),
                new Vector2(.94f, .88f));
            revisedButton.onClick.AddListener(ShowRevised);

            content = RuntimeScrollableContent.Create(
                card.transform,
                "FutureWalkContent",
                new Vector2(.06f, .06f),
                new Vector2(.94f, .78f),
                15);
            panel = canvasObject;
            panel.SetActive(false);
            EpisodeAccessibility.ApplyAll();
        }
    }
}
