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
        private RectTransform cardRect;
        private Text content;
        private Text originalComparison;
        private Text revisedComparison;
        private ScrollRect contentScroll;
        private ScrollRect originalComparisonScroll;
        private ScrollRect revisedComparisonScroll;
        private Button originalButton;
        private Button revisedButton;
        private AtlasRouteGraphic originalRoute;
        private AtlasRouteGraphic revisedRoute;
        private readonly Text[] yearNodeLabels = new Text[5];
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
            bool comparisonVisible = session.HasRevision;
            cardRect.anchorMin = new Vector2(.025f, .045f);
            cardRect.anchorMax =
                new Vector2(.975f, .375f);
            contentScroll.gameObject.SetActive(!comparisonVisible);
            originalComparisonScroll.gameObject.SetActive(
                comparisonVisible);
            revisedComparisonScroll.gameObject.SetActive(
                comparisonVisible);
            originalRoute.gameObject.SetActive(
                comparisonVisible || !showingRevised);
            revisedRoute.gameObject.SetActive(
                comparisonVisible || showingRevised);

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
            for (int index = 0;
                 index < yearNodeLabels.Length;
                 index++)
            {
                yearNodeLabels[index].text =
                    index < result.Years.Count
                        ? "YEAR " + result.Years[index].Year
                        : string.Empty;
                yearNodeLabels[index].color =
                    index == yearIndex
                        ? EpisodeUiFactory.BrightAmber
                        : EpisodeUiFactory.MutedSand;
            }
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

            RuntimeScrollableContent.SetText(
                content,
                BuildSummary(
                    result,
                    year,
                    showingRevised
                        ? "REVISED FUTURE"
                        : "ORIGINAL FUTURE"));

            if (session.HasRevision)
            {
                FutureWalkResult original =
                    FutureWalkMapper.Map(
                        session.OriginalSimulatorResultJson);
                FutureWalkResult revised =
                    FutureWalkMapper.Map(
                        session.SimulatorResultJson);
                int originalIndex = Mathf.Clamp(
                    yearIndex,
                    0,
                    original.Years.Count - 1);
                int revisedIndex = Mathf.Clamp(
                    yearIndex,
                    0,
                    revised.Years.Count - 1);
                RuntimeScrollableContent.SetText(
                    originalComparison,
                    BuildSummary(
                        original,
                        original.Years[originalIndex],
                        "ORIGINAL FUTURE"));
                RuntimeScrollableContent.SetText(
                    revisedComparison,
                    BuildSummary(
                        revised,
                        revised.Years[revisedIndex],
                        "REVISED FUTURE"));
            }

            EpisodeUiFactory.SetButtonSelected(
                originalButton,
                !showingRevised);
            EpisodeUiFactory.SetButtonSelected(
                revisedButton,
                showingRevised);
        }

        private static string BuildSummary(
            FutureWalkResult result,
            FutureYearPresentation year,
            string heading)
        {
            var text = new StringBuilder();
            text.Append(heading)
                .Append(" · YEAR ")
                .Append(year.Year)
                .Append("\n")
                .Append(result.Headline)
                .Append("\n\nOverall fit  ")
                .Append(result.OverallFit)
                .Append("\nSalinity  ")
                .Append(year.SalinityValue)
                .Append(' ')
                .Append(year.SalinityUnit)
                .Append("  ·  Income score  ")
                .Append(year.IncomeScore)
                .Append("  ·  Sustainability  ")
                .Append(year.SustainabilityScore)
                .Append("  ·  Cost  ")
                .Append(year.CostLevel)
                .Append("\nYields");
            foreach (FutureYieldItem item in year.YieldItems)
            {
                text.Append("  ·  ")
                    .Append(item.CommodityId)
                    .Append(' ')
                    .Append(item.Value)
                    .Append(' ')
                    .Append(item.Unit);
            }
            text.Append("\n")
                .Append(year.Narrative)
                .Append("\nEvidence source IDs  ")
                .Append(string.Join(
                    ", ",
                    year.EvidenceSourceIds));
            return text.ToString();
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

            AtlasSurfaceGraphic card =
                EpisodeUiFactory.SmokedGlass(
                canvas.transform,
                "FutureWalkCard",
                false);
            EpisodeUiFactory.Stretch(
                card.rectTransform,
                new Vector2(.025f, .045f),
                new Vector2(.975f, .375f));
            cardRect = card.rectTransform;
            Text title = EpisodeUiFactory.Text(
                card.transform,
                "FutureWalkTitle",
                21,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.OffWhite);
            title.text =
                "FUTURE WALK  ·  RIVER OF CONSEQUENCES";
            title.color = EpisodeUiFactory.Amber;
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.06f, .89f),
                new Vector2(.94f, .98f));

            originalButton = EpisodeUiFactory.Button(
                card.transform,
                "OriginalFuture",
                "ORIGINAL",
                EpisodeButtonStyle.Tab,
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
                EpisodeButtonStyle.Tab,
                13);
            EpisodeUiFactory.Stretch(
                revisedButton.GetComponent<RectTransform>(),
                new Vector2(.52f, .81f),
                new Vector2(.94f, .88f));
            revisedButton.onClick.AddListener(ShowRevised);

            originalRoute =
                EpisodeUiFactory.Route(
                    card.transform,
                    "OriginalRiverRoute",
                    new[]
                    {
                        new Vector2(.02f, .44f),
                        new Vector2(.25f, .60f),
                        new Vector2(.50f, .46f),
                        new Vector2(.74f, .65f),
                        new Vector2(.98f, .54f)
                    },
                    new Color(
                        EpisodeUiFactory.MutedSand.r,
                        EpisodeUiFactory.MutedSand.g,
                        EpisodeUiFactory.MutedSand.b,
                        .78f),
                    1.4f);
            EpisodeUiFactory.Stretch(
                originalRoute.rectTransform,
                new Vector2(.06f, .58f),
                new Vector2(.94f, .79f));
            revisedRoute =
                EpisodeUiFactory.Route(
                    card.transform,
                    "RevisedRiverRoute",
                    new[]
                    {
                        new Vector2(.02f, .44f),
                        new Vector2(.25f, .50f),
                        new Vector2(.50f, .67f),
                        new Vector2(.74f, .58f),
                        new Vector2(.98f, .72f)
                    },
                    EpisodeUiFactory.BrightAmber,
                    1.8f);
            EpisodeUiFactory.Stretch(
                revisedRoute.rectTransform,
                new Vector2(.06f, .58f),
                new Vector2(.94f, .79f));
            for (int index = 0;
                 index < yearNodeLabels.Length;
                 index++)
            {
                Text label = EpisodeUiFactory.Text(
                    card.transform,
                    "YearNode_" + (index + 1),
                    12,
                    TextAnchor.MiddleCenter,
                    EpisodeUiFactory.MutedSand);
                float center = .07f + index * .215f;
                EpisodeUiFactory.Stretch(
                    label.rectTransform,
                    new Vector2(center - .05f, .53f),
                    new Vector2(center + .05f, .60f));
                yearNodeLabels[index] = label;
            }
            content = RuntimeScrollableContent.Create(
                card.transform,
                "FutureWalkContent",
                new Vector2(.06f, .18f),
                new Vector2(.94f, .52f),
                15);
            contentScroll =
                content.GetComponentInParent<ScrollRect>();
            originalComparison =
                RuntimeScrollableContent.Create(
                    card.transform,
                    "OriginalFutureComparison",
                    new Vector2(.035f, .18f),
                    new Vector2(.49f, .52f),
                    14);
            originalComparisonScroll =
                originalComparison.GetComponentInParent<
                    ScrollRect>();
            revisedComparison =
                RuntimeScrollableContent.Create(
                    card.transform,
                    "RevisedFutureComparison",
                    new Vector2(.51f, .18f),
                    new Vector2(.965f, .52f),
                    14);
            revisedComparisonScroll =
                revisedComparison.GetComponentInParent<
                    ScrollRect>();
            originalComparisonScroll.gameObject.SetActive(false);
            revisedComparisonScroll.gameObject.SetActive(false);
            panel = canvasObject;
            panel.SetActive(false);
            EpisodeAccessibility.ApplyAll();
        }
    }
}
