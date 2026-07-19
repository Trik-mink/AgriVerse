using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    /// <summary>
    /// Read-only view of PlanSession's canonical simulator JSON. Numeric JSON tokens remain
    /// strings here so display cannot round, replace null, or otherwise alter an outcome.
    /// </summary>
    public sealed class ConsequencesController : MonoBehaviour
    {
        private PlanSession session;
        private CanonicalJsonValue result;
        private GameObject stage;
        private Text contentText;
        private Button previousButton;
        private Button nextButton;
        private Button feedbackButton;
        private int yearIndex;
        private string displayedResultJson = string.Empty;

        public InvestigationLoadState LoadState { get; private set; } = InvestigationLoadState.NotStarted;
        public bool ConsequencesVisible => RuntimePanelManager.GetOrCreate().IsShowing(RuntimeActivityStage.Consequences);
        public bool FeedbackUnlocked { get; private set; }
        public int CurrentYearIndex => yearIndex;
        public string DisplayedContentForTesting => contentText == null ? string.Empty : contentText.text;

        private void Start() => StartCoroutine(LoadStoredResult());

        private void Update()
        {
            if (LoadState == InvestigationLoadState.Ready && session.SimulatorResult != null && displayedResultJson != session.SimulatorResultJson)
            {
                Activate();
            }
        }

        private IEnumerator LoadStoredResult()
        {
            session = PlanSession.GetOrCreate();
            while (session.SimulatorResult == null) yield return null;

            try
            {
                ReadCurrentResult();
            }
            catch (Exception error) when (error is FormatException || error is InvalidOperationException)
            {
                Debug.LogError("Stored simulator result could not be read: " + error.Message, this);
                LoadState = InvestigationLoadState.Failed;
                yield break;
            }

            stage = new GameObject("ConsequencesStage");
            stage.transform.SetParent(transform, false);
            CreateInterface();
            RuntimePanelManager.GetOrCreate().Register(RuntimeActivityStage.Consequences, stage);
            LoadState = InvestigationLoadState.Ready;
            Activate();
        }

        public void PreviousYear()
        {
            if (yearIndex <= 0) return;
            yearIndex--;
            Refresh();
        }

        public void NextYear()
        {
            if (yearIndex >= result.Property("years").Items.Count - 1) return;
            yearIndex++;
            Refresh();
        }

        public void UnlockFeedback()
        {
            FeedbackUnlocked = true;
            FeedbackController feedback = FindFirstObjectByType<FeedbackController>();
            if (feedback == null) { SetStatus("Feedback Controller is missing. Add its bootstrap to this scene."); return; }
            feedback.RequestFeedback();
        }

        private void Activate()
        {
            try { ReadCurrentResult(); }
            catch (FormatException error) { SetStatus("Stored simulation could not be read: " + error.Message); return; }
            RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Consequences);
            SetStatus("Simulation stored. Review its authoritative five-year results.");
            Refresh();
        }

        public void ShowFeedbackActivity() { }

        private void ReadCurrentResult()
        {
            if (displayedResultJson == session.SimulatorResultJson && result != null) return;
            CanonicalJsonValue candidate = CanonicalJsonParser.Parse(session.SimulatorResultJson);
            if (candidate.Property("years").Items.Count != 5 || candidate.Property("fit_assessment").Properties.Count != 5)
            {
                throw new FormatException("The simulator result must contain five years and five fit assessment fields.");
            }
            result = candidate;
            displayedResultJson = session.SimulatorResultJson;
            yearIndex = 0;
            FeedbackUnlocked = false;
        }

        private void CreateInterface()
        {
            var canvasObject = new GameObject("ConsequencesCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(stage.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            contentText = RuntimeScrollableContent.Create(canvas.transform, "ConsequencesContent", new Vector2(.03f, .1f), new Vector2(.62f, .82f), 15);
            previousButton = Button(canvas.transform, "PreviousYear", "Previous year");
            Stretch(previousButton.GetComponent<RectTransform>(), new Vector2(.03f, .04f), new Vector2(.21f, .08f));
            previousButton.onClick.AddListener(PreviousYear);
            nextButton = Button(canvas.transform, "NextYear", "Next year");
            Stretch(nextButton.GetComponent<RectTransform>(), new Vector2(.23f, .04f), new Vector2(.41f, .08f));
            nextButton.onClick.AddListener(NextYear);
            feedbackButton = Button(canvas.transform, "GetFeedback", "Get feedback");
            Stretch(feedbackButton.GetComponent<RectTransform>(), new Vector2(.43f, .04f), new Vector2(.62f, .08f));
            feedbackButton.onClick.AddListener(UnlockFeedback);
        }

        private void Refresh()
        {
            CanonicalJsonValue year = result.Property("years").Items[yearIndex];
            CanonicalJsonValue outcomes = year.Property("outcomes");
            CanonicalJsonValue fit = result.Property("fit_assessment");
            var text = new StringBuilder();
            text.Append(result.Property("headline").Text).Append("\n\nFit assessment\n")
                .Append("Salinity: ").Append(fit.Property("salinity").Text).Append("\nSeasonality: ").Append(fit.Property("seasonality").Text)
                .Append("\nFreshwater: ").Append(fit.Property("freshwater").Text).Append("\nFarmer capital: ").Append(fit.Property("farmer_capital").Text)
                .Append("\nOverall: ").Append(fit.Property("overall").Text).Append("\n\nYear ").Append(year.Property("year").Text)
                .Append("\nSalinity: ").Append(outcomes.Property("salinity").Property("value").Text).Append(' ').Append(outcomes.Property("salinity").Property("unit").Text)
                .Append("\nYield items:");
            foreach (CanonicalJsonValue item in outcomes.Property("yield").Property("items").Items)
            {
                text.Append("\n- ").Append(item.Property("commodity_id").Text).Append(": ")
                    .Append(item.Property("value").Text).Append(' ').Append(item.Property("unit").Text);
            }
            text.Append("\nIncome score: ").Append(outcomes.Property("income").Property("score").Text)
                .Append("\nSustainability score: ").Append(outcomes.Property("sustainability").Property("score").Text)
                .Append("\nCost level: ").Append(year.Property("cost_level").Text)
                .Append("\nNarrative: ").Append(year.Property("narrative").Text)
                .Append("\nEvidence source IDs: ").Append(Join(year.Property("evidence_source_ids").Items));
            text.Append("\n\nTradeoffs");
            foreach (CanonicalJsonValue tradeoff in result.Property("tradeoffs").Items)
            {
                text.Append('\n').Append(tradeoff.Property("category").Text).Append(": ").Append(tradeoff.Property("summary").Text);
            }
            RuntimeScrollableContent.SetText(contentText, text.ToString());
            previousButton.interactable = yearIndex > 0;
            nextButton.interactable = yearIndex < result.Property("years").Items.Count - 1;
        }

        public void ReturnFromFeedback()
        {
            if (result == null) ReadCurrentResult();
            RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Consequences);
            SetStatus("Review the stored consequences before revising the proposal.");
            Refresh();
        }

        private static void SetStatus(string value) => RuntimePanelManager.GetOrCreate().SetInstruction(value);

        private static string Join(IReadOnlyList<CanonicalJsonValue> values)
        {
            var text = new StringBuilder();
            for (int index = 0; index < values.Count; index++)
            {
                if (index > 0) text.Append(", ");
                text.Append(values[index].Text);
            }
            return text.ToString();
        }

        private static Text Text(Transform parent, string name, int size)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = EpisodeUiFactory.OffWhite;
            text.raycastTarget = false;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button Button(Transform parent, string name, string label)
        {
            Button button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            Image image = button.GetComponent<Image>();
            image.color = EpisodeUiFactory.RiverTeal;
            button.targetGraphic = image;
            Text text = Text(button.transform, "Label", 14);
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    internal enum CanonicalJsonKind { Object, Array, String, Primitive }

    /// <summary>Small, dependency-free JSON reader that preserves primitive token spelling.</summary>
    internal sealed class CanonicalJsonValue
    {
        public CanonicalJsonKind Kind { get; }
        public string Text { get; }
        public IReadOnlyDictionary<string, CanonicalJsonValue> Properties { get; }
        public IReadOnlyList<CanonicalJsonValue> Items { get; }

        public CanonicalJsonValue(CanonicalJsonKind kind, string text = null, Dictionary<string, CanonicalJsonValue> properties = null, List<CanonicalJsonValue> items = null)
        {
            Kind = kind;
            Text = text;
            Properties = properties ?? new Dictionary<string, CanonicalJsonValue>();
            Items = items ?? new List<CanonicalJsonValue>();
        }

        public CanonicalJsonValue Property(string name)
        {
            if (Kind != CanonicalJsonKind.Object || !Properties.TryGetValue(name, out CanonicalJsonValue value))
            {
                throw new FormatException("Expected simulator JSON field '" + name + "'.");
            }
            return value;
        }
    }

    internal static class CanonicalJsonParser
    {
        public static CanonicalJsonValue Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new FormatException("The simulator JSON was empty.");
            var parser = new Parser(json);
            CanonicalJsonValue value = parser.Value();
            parser.WhiteSpace();
            if (!parser.End) throw new FormatException("Unexpected data after simulator JSON.");
            return value;
        }

        private sealed class Parser
        {
            private readonly string source;
            private int position;
            public bool End => position >= source.Length;
            public Parser(string source) => this.source = source;

            public CanonicalJsonValue Value()
            {
                WhiteSpace();
                if (End) throw new FormatException("Unexpected end of JSON.");
                switch (source[position])
                {
                    case '{': return Object();
                    case '[': return Array();
                    case '"': return new CanonicalJsonValue(CanonicalJsonKind.String, String());
                    default: return Primitive();
                }
            }

            public void WhiteSpace()
            {
                while (!End && char.IsWhiteSpace(source[position])) position++;
            }

            private CanonicalJsonValue Object()
            {
                Expect('{');
                var values = new Dictionary<string, CanonicalJsonValue>();
                WhiteSpace();
                if (Try('}')) return new CanonicalJsonValue(CanonicalJsonKind.Object, properties: values);
                do
                {
                    WhiteSpace();
                    string key = String();
                    WhiteSpace();
                    Expect(':');
                    values.Add(key, Value());
                    WhiteSpace();
                } while (Try(','));
                Expect('}');
                return new CanonicalJsonValue(CanonicalJsonKind.Object, properties: values);
            }

            private CanonicalJsonValue Array()
            {
                Expect('[');
                var values = new List<CanonicalJsonValue>();
                WhiteSpace();
                if (Try(']')) return new CanonicalJsonValue(CanonicalJsonKind.Array, items: values);
                do { values.Add(Value()); WhiteSpace(); } while (Try(','));
                Expect(']');
                return new CanonicalJsonValue(CanonicalJsonKind.Array, items: values);
            }

            private CanonicalJsonValue Primitive()
            {
                int start = position;
                while (!End && source[position] != ',' && source[position] != ']' && source[position] != '}' && !char.IsWhiteSpace(source[position])) position++;
                if (start == position) throw new FormatException("Expected JSON primitive.");
                string primitive = source.Substring(start, position - start);
                if (primitive != "true" && primitive != "false" && primitive != "null" && !IsNumber(primitive))
                {
                    throw new FormatException("Invalid JSON primitive '" + primitive + "'.");
                }
                return new CanonicalJsonValue(CanonicalJsonKind.Primitive, primitive);
            }

            private string String()
            {
                Expect('"');
                var text = new StringBuilder();
                while (!End)
                {
                    char character = source[position++];
                    if (character == '"') return text.ToString();
                    if (character != '\\') { text.Append(character); continue; }
                    if (End) throw new FormatException("Unfinished JSON string escape.");
                    switch (source[position++])
                    {
                        case '"': text.Append('"'); break;
                        case '\\': text.Append('\\'); break;
                        case '/': text.Append('/'); break;
                        case 'b': text.Append('\b'); break;
                        case 'f': text.Append('\f'); break;
                        case 'n': text.Append('\n'); break;
                        case 'r': text.Append('\r'); break;
                        case 't': text.Append('\t'); break;
                        case 'u': text.Append((char)Convert.ToInt32(ReadUnicode(), 16)); break;
                        default: throw new FormatException("Invalid JSON string escape.");
                    }
                }
                throw new FormatException("Unfinished JSON string.");
            }

            private string ReadUnicode()
            {
                if (position + 4 > source.Length) throw new FormatException("Incomplete JSON unicode escape.");
                string hex = source.Substring(position, 4);
                for (int index = 0; index < hex.Length; index++) if (!Uri.IsHexDigit(hex[index])) throw new FormatException("Invalid JSON unicode escape.");
                position += 4;
                return hex;
            }

            private bool Try(char expected)
            {
                WhiteSpace();
                if (End || source[position] != expected) return false;
                position++;
                return true;
            }

            private void Expect(char expected)
            {
                WhiteSpace();
                if (End || source[position] != expected) throw new FormatException("Expected '" + expected + "' in simulator JSON.");
                position++;
            }

            private static bool IsNumber(string value)
            {
                int index = 0;
                if (value[index] == '-') index++;
                if (index >= value.Length) return false;
                if (value[index] == '0') index++;
                else { if (value[index] < '1' || value[index] > '9') return false; while (index < value.Length && char.IsDigit(value[index])) index++; }
                if (index < value.Length && value[index] == '.') { index++; int decimalStart = index; while (index < value.Length && char.IsDigit(value[index])) index++; if (decimalStart == index) return false; }
                if (index < value.Length && (value[index] == 'e' || value[index] == 'E')) { index++; if (index < value.Length && (value[index] == '+' || value[index] == '-')) index++; int exponentStart = index; while (index < value.Length && char.IsDigit(value[index])) index++; if (exponentStart == index) return false; }
                return index == value.Length;
            }
        }
    }
}
