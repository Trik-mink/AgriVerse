using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    internal enum EpisodeButtonStyle
    {
        Primary,
        Secondary,
        Choice,
        Tab,
        Disabled
    }

    internal static class EpisodeUiFactory
    {
        internal static readonly Color DeepTeal =
            new Color32(6, 44, 45, 224);
        internal static readonly Color SecondarySurface =
            new Color32(10, 58, 58, 235);
        internal static readonly Color RiverTeal =
            SecondarySurface;
        internal static readonly Color Amber =
            new Color32(230, 162, 60, 255);
        internal static readonly Color BrightAmber =
            new Color32(240, 179, 77, 255);
        internal static readonly Color OffWhite =
            new Color32(243, 233, 211, 255);
        internal static readonly Color MutedSand =
            new Color32(177, 168, 148, 255);
        internal static readonly Color WarmCoral =
            new Color32(220, 111, 84, 255);
        internal static readonly Color RiceGreen =
            new Color32(142, 168, 94, 255);
        internal static readonly Color NetworkTeal =
            new Color32(92, 174, 166, 255);
        internal static readonly Color Ink =
            new Color32(19, 43, 40, 255);
        internal const float FastTransitionSeconds = .16f;

        internal static AtlasSurfaceGraphic FieldPaper(
            Transform parent,
            string name,
            bool raycastable = false)
        {
            AtlasSurfaceGraphic surface = AtlasSurface(
                parent,
                name,
                AtlasSurfaceKind.FieldPaper,
                new Color32(229, 215, 181, 247),
                14f,
                raycastable);
            AddAtlasDecoration(
                surface.transform,
                "PaperGrain",
                AtlasDecorationKind.PaperGrain,
                new Color(Ink.r, Ink.g, Ink.b, .10f));
            AddAtlasDecoration(
                surface.transform,
                "ContourMarks",
                AtlasDecorationKind.Contours,
                new Color(Ink.r, Ink.g, Ink.b, .15f));
            return surface;
        }

        internal static AtlasSurfaceGraphic SmokedGlass(
            Transform parent,
            string name,
            bool raycastable = false)
        {
            return AtlasSurface(
                parent,
                name,
                AtlasSurfaceKind.SmokedGlass,
                new Color(DeepTeal.r, DeepTeal.g, DeepTeal.b, .86f),
                4f,
                raycastable);
        }

        internal static AtlasSurfaceGraphic AtlasLabel(
            Transform parent,
            string name,
            bool raycastable = false)
        {
            AtlasSurfaceGraphic surface = AtlasSurface(
                parent,
                name,
                AtlasSurfaceKind.AtlasLabel,
                new Color(SecondarySurface.r, SecondarySurface.g, SecondarySurface.b, .90f),
                7f,
                raycastable);
            AddAtlasDecoration(
                surface.transform,
                "SurveyRule",
                AtlasDecorationKind.SurveyRule,
                new Color(Amber.r, Amber.g, Amber.b, .62f));
            return surface;
        }

        internal static AtlasInstrumentGraphic Instrument(
            Transform parent,
            string name)
        {
            AtlasInstrumentGraphic instrument =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(AtlasInstrumentGraphic))
                    .GetComponent<AtlasInstrumentGraphic>();
            instrument.transform.SetParent(parent, false);
            instrument.raycastTarget = false;
            return instrument;
        }

        internal static AtlasRouteGraphic Route(
            Transform parent,
            string name,
            Vector2[] nodes,
            Color color,
            float width = 2f)
        {
            AtlasRouteGraphic route =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(AtlasRouteGraphic))
                    .GetComponent<AtlasRouteGraphic>();
            route.transform.SetParent(parent, false);
            route.SetRoute(nodes, color, width);
            return route;
        }

        internal static Vector2[] FieldRouteNodes(int count)
        {
            int safeCount = Mathf.Max(1, count);
            Vector2[] nodes = new Vector2[safeCount];
            for (int index = 0; index < safeCount; index++)
            {
                float amount =
                    safeCount == 1
                        ? .5f
                        : index / (float)(safeCount - 1);
                nodes[index] = new Vector2(
                    Mathf.Lerp(.12f, .88f, amount),
                    Mathf.Lerp(.18f, .78f, amount) +
                    Mathf.Sin(amount * Mathf.PI) * .07f);
            }
            return nodes;
        }

        internal static AtlasSurfaceGraphic Stamp(
            Transform parent,
            string name,
            string label,
            Color color)
        {
            AtlasSurfaceGraphic stamp = AtlasLabel(
                parent,
                name);
            stamp.color = new Color(
                color.r,
                color.g,
                color.b,
                .18f);
            Text text = Text(
                stamp.transform,
                "StampLabel",
                14,
                TextAnchor.MiddleCenter,
                color);
            text.text = label;
            Stretch(
                text.rectTransform,
                new Vector2(.05f, .06f),
                new Vector2(.95f, .94f));
            stamp.rectTransform.localRotation =
                Quaternion.Euler(0f, 0f, -2.5f);
            return stamp;
        }

        internal static string FormatModelText(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string safe = value
                .Replace("<", "‹")
                .Replace(">", "›");
            safe = Regex.Replace(
                safe,
                @"\*\*(.+?)\*\*",
                "<b>$1</b>");
            safe = Regex.Replace(
                safe,
                @"(?m)^\s*[-*]\s+",
                "• ");
            return safe.Replace("**", string.Empty);
        }

        internal static Image Panel(
            Transform parent,
            string name,
            Color color,
            bool raycastable)
        {
            Image image = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = raycastable;
            return image;
        }

        internal static CinematicGradientGraphic Gradient(
            Transform parent,
            string name,
            Color topLeft,
            Color topRight,
            Color bottomLeft,
            Color bottomRight)
        {
            CinematicGradientGraphic graphic =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(CinematicGradientGraphic))
                    .GetComponent<CinematicGradientGraphic>();
            graphic.transform.SetParent(parent, false);
            graphic.SetColors(
                topLeft,
                topRight,
                bottomLeft,
                bottomRight);
            graphic.raycastTarget = false;
            return graphic;
        }

        internal static Image CinematicPanel(
            Transform parent,
            string name,
            bool raycastable,
            float opacity = .88f,
            bool hairline = true)
        {
            Color color = DeepTeal;
            color.a = Mathf.Clamp(opacity, .82f, .92f);
            Image image = Panel(
                parent,
                name,
                color,
                raycastable);
            if (hairline)
            {
                Outline outline =
                    image.gameObject.AddComponent<Outline>();
                outline.effectColor =
                    new Color(
                        Amber.r,
                        Amber.g,
                        Amber.b,
                        .42f);
                outline.effectDistance = new Vector2(1f, -1f);
                outline.useGraphicAlpha = true;
            }
            return image;
        }

        internal static Text Text(
            Transform parent,
            string name,
            int size,
            TextAnchor alignment,
            Color color)
        {
            Text text = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.supportRichText = false;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        internal static Button Button(
            Transform parent,
            string name,
            string label,
            Color color,
            int size)
        {
            EpisodeButtonStyle style =
                ColorsApproximatelyEqual(color, Amber) ||
                ColorsApproximatelyEqual(color, BrightAmber)
                    ? EpisodeButtonStyle.Primary
                    : EpisodeButtonStyle.Secondary;
            Button button = Button(
                parent,
                name,
                label,
                style,
                size);
            if (style == EpisodeButtonStyle.Secondary &&
                !ColorsApproximatelyEqual(color, RiverTeal) &&
                !ColorsApproximatelyEqual(color, DeepTeal))
            {
                button.targetGraphic.color = color;
            }
            return button;
        }

        internal static Button Button(
            Transform parent,
            string name,
            string label,
            EpisodeButtonStyle style,
            int size)
        {
            Button button = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(AtlasSurfaceGraphic),
                typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            AtlasSurfaceGraphic surface =
                button.GetComponent<AtlasSurfaceGraphic>();
            Color surfaceColor =
                style == EpisodeButtonStyle.Primary
                    ? Amber
                    : style == EpisodeButtonStyle.Disabled
                        ? new Color(
                            SecondarySurface.r,
                            SecondarySurface.g,
                            SecondarySurface.b,
                            .52f)
                        : SecondarySurface;
            surface.Configure(
                style == EpisodeButtonStyle.Primary
                    ? AtlasSurfaceKind.FieldPaper
                    : style == EpisodeButtonStyle.Choice
                        ? AtlasSurfaceKind.SmokedGlass
                        : AtlasSurfaceKind.AtlasLabel,
                surfaceColor,
                style == EpisodeButtonStyle.Choice ? 4f : 7f);
            surface.raycastTarget = true;
            button.targetGraphic = surface;
            ConfigureButtonStates(button, style);
            if (style != EpisodeButtonStyle.Primary)
            {
                AddHairline(surface);
            }
            Text text = Text(
                button.transform,
                "Label",
                Mathf.Max(13, size),
                TextAnchor.MiddleCenter,
                style == EpisodeButtonStyle.Primary
                    ? Ink
                    : style == EpisodeButtonStyle.Disabled
                        ? MutedSand
                        : OffWhite);
            text.text = label;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            button.interactable =
                style != EpisodeButtonStyle.Disabled;
            return button;
        }

        internal static Button ChoiceButton(
            Transform parent,
            string name,
            int number,
            string label)
        {
            Button button = Button(
                parent,
                name,
                string.Empty,
                EpisodeButtonStyle.Choice,
                14);
            Text labelText =
                button.GetComponentInChildren<Text>();
            labelText.name = "ChoiceLabel";
            labelText.text = label ?? string.Empty;
            labelText.alignment = TextAnchor.MiddleLeft;
            Stretch(
                labelText.rectTransform,
                new Vector2(.19f, .08f),
                new Vector2(.96f, .92f));

            AtlasSurfaceGraphic capsule = AtlasSurface(
                button.transform,
                "ChoiceNumber",
                AtlasSurfaceKind.AtlasLabel,
                Amber,
                6f,
                false);
            capsule.rectTransform.anchorMin =
                new Vector2(.035f, .22f);
            capsule.rectTransform.anchorMax =
                new Vector2(.155f, .78f);
            capsule.rectTransform.offsetMin = Vector2.zero;
            capsule.rectTransform.offsetMax = Vector2.zero;
            Text numberText = Text(
                capsule.transform,
                "Number",
                13,
                TextAnchor.MiddleCenter,
                Ink);
            numberText.text = number.ToString();
            Stretch(
                numberText.rectTransform,
                Vector2.zero,
                Vector2.one);
            return button;
        }

        internal static InputField Input(
            Transform parent,
            string placeholderText)
        {
            return Input(
                parent,
                "PlayerNameInput",
                placeholderText,
                false);
        }

        internal static InputField Input(
            Transform parent,
            string name,
            string placeholderText,
            bool multiline)
        {
            InputField input = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(AtlasSurfaceGraphic),
                typeof(InputField)).GetComponent<InputField>();
            input.transform.SetParent(parent, false);
            AtlasSurfaceGraphic inputSurface =
                input.GetComponent<AtlasSurfaceGraphic>();
            inputSurface.Configure(
                AtlasSurfaceKind.AtlasLabel,
                new Color(
                    SecondarySurface.r,
                    SecondarySurface.g,
                    SecondarySurface.b,
                    .94f),
                6f);
            inputSurface.raycastTarget = true;
            input.targetGraphic = inputSurface;
            AddHairline(inputSurface);
            Text value = Text(
                input.transform,
                "Text",
                17,
                TextAnchor.MiddleLeft,
                OffWhite);
            value.verticalOverflow =
                VerticalWrapMode.Truncate;
            Stretch(value.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
            input.textComponent = value;
            Text placeholder = Text(
                input.transform,
                "Placeholder",
                16,
                TextAnchor.MiddleLeft,
                new Color(.85f, .83f, .76f, .58f));
            placeholder.text = placeholderText;
            placeholder.verticalOverflow =
                VerticalWrapMode.Truncate;
            Stretch(
                placeholder.rectTransform,
                new Vector2(.04f, .08f),
                new Vector2(.96f, .92f));
            input.placeholder = placeholder;
            input.characterLimit = multiline ? 0 : 40;
            input.lineType = multiline
                ? InputField.LineType.MultiLineNewline
                : InputField.LineType.SingleLine;
            return input;
        }

        internal static void SetButtonSelected(
            Button button,
            bool selected)
        {
            if (button == null) return;
            Graphic graphic = button.targetGraphic;
            if (graphic != null)
            {
                graphic.color = selected
                    ? Amber
                    : SecondarySurface;
            }
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = selected ? Ink : OffWhite;
            }
        }

        internal static void Stretch(
            RectTransform rect,
            Vector2 minimum,
            Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject(
                    "EventSystem",
                    typeof(EventSystem)).GetComponent<EventSystem>();
            }

            foreach (BaseInputModule module in
                     eventSystem.GetComponents<BaseInputModule>())
            {
                if (!(module is InputSystemUIInputModule))
                {
                    module.enabled = false;
                }
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void ConfigureButtonStates(
            Button button,
            EpisodeButtonStyle style)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                style == EpisodeButtonStyle.Primary
                    ? new Color(1f, 1f, .86f, 1f)
                    : new Color(1.18f, 1.18f, 1.08f, 1f);
            colors.selectedColor =
                new Color(1f, .94f, .76f, 1f);
            colors.pressedColor =
                new Color(.82f, .82f, .76f, 1f);
            colors.disabledColor =
                new Color(.48f, .48f, .45f, .56f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration =
                EpisodeAccessibility.ReducedMotion
                    ? 0f
                    : FastTransitionSeconds;
            button.colors = colors;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.Automatic,
                wrapAround = true
            };
        }

        private static void AddHairline(Graphic image)
        {
            if (image == null ||
                image.GetComponent<Outline>() != null)
            {
                return;
            }
            Outline outline =
                image.gameObject.AddComponent<Outline>();
            outline.effectColor =
                new Color(
                    Amber.r,
                    Amber.g,
                    Amber.b,
                    .62f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }

        private static bool ColorsApproximatelyEqual(
            Color first,
            Color second) =>
            Mathf.Abs(first.r - second.r) < .02f &&
            Mathf.Abs(first.g - second.g) < .02f &&
            Mathf.Abs(first.b - second.b) < .02f;

        private static AtlasSurfaceGraphic AtlasSurface(
            Transform parent,
            string name,
            AtlasSurfaceKind kind,
            Color color,
            float cornerCut,
            bool raycastable)
        {
            AtlasSurfaceGraphic surface =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(AtlasSurfaceGraphic))
                    .GetComponent<AtlasSurfaceGraphic>();
            surface.transform.SetParent(parent, false);
            surface.Configure(kind, color, cornerCut);
            surface.raycastTarget = raycastable;
            return surface;
        }

        private static void AddAtlasDecoration(
            Transform parent,
            string name,
            AtlasDecorationKind kind,
            Color color)
        {
            AtlasDecorationGraphic decoration =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(AtlasDecorationGraphic))
                    .GetComponent<AtlasDecorationGraphic>();
            decoration.transform.SetParent(parent, false);
            decoration.Configure(kind, color);
            Stretch(
                decoration.rectTransform,
                Vector2.zero,
                Vector2.one);
        }

    }

    internal sealed class CinematicGradientGraphic :
        MaskableGraphic
    {
        private Color topLeft = Color.clear;
        private Color topRight = Color.clear;
        private Color bottomLeft = Color.clear;
        private Color bottomRight = Color.clear;

        internal void SetColors(
            Color upperLeft,
            Color upperRight,
            Color lowerLeft,
            Color lowerRight)
        {
            topLeft = upperLeft;
            topRight = upperRight;
            bottomLeft = lowerLeft;
            bottomRight = lowerRight;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(
            VertexHelper helper)
        {
            helper.Clear();
            Rect rect = rectTransform.rect;
            AddVertex(
                helper,
                new Vector2(rect.xMin, rect.yMin),
                bottomLeft);
            AddVertex(
                helper,
                new Vector2(rect.xMin, rect.yMax),
                topLeft);
            AddVertex(
                helper,
                new Vector2(rect.xMax, rect.yMax),
                topRight);
            AddVertex(
                helper,
                new Vector2(rect.xMax, rect.yMin),
                bottomRight);
            helper.AddTriangle(0, 1, 2);
            helper.AddTriangle(2, 3, 0);
        }

        private static void AddVertex(
            VertexHelper helper,
            Vector2 position,
            Color tint)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = tint;
            helper.AddVert(vertex);
        }
    }
}
