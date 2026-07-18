using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    internal static class EpisodeUiFactory
    {
        internal static readonly Color DeepTeal =
            new Color(.018f, .075f, .08f, .96f);
        internal static readonly Color RiverTeal =
            new Color(.025f, .16f, .17f, .96f);
        internal static readonly Color Amber =
            new Color(.92f, .61f, .23f, 1f);
        internal static readonly Color OffWhite =
            new Color(.96f, .94f, .86f, 1f);

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
            Button button = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            Image image = button.GetComponent<Image>();
            image.color = color;
            button.targetGraphic = image;
            Text text = Text(
                button.transform,
                "Label",
                size,
                TextAnchor.MiddleCenter,
                OffWhite);
            text.text = label;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        internal static InputField Input(
            Transform parent,
            string placeholderText)
        {
            InputField input = new GameObject(
                "PlayerNameInput",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(InputField)).GetComponent<InputField>();
            input.transform.SetParent(parent, false);
            input.GetComponent<Image>().color =
                new Color(.06f, .14f, .14f, .98f);
            Text value = Text(
                input.transform,
                "Text",
                17,
                TextAnchor.MiddleLeft,
                OffWhite);
            Stretch(value.rectTransform, new Vector2(.04f, .08f), new Vector2(.96f, .92f));
            input.textComponent = value;
            Text placeholder = Text(
                input.transform,
                "Placeholder",
                16,
                TextAnchor.MiddleLeft,
                new Color(.85f, .83f, .76f, .58f));
            placeholder.text = placeholderText;
            Stretch(
                placeholder.rectTransform,
                new Vector2(.04f, .08f),
                new Vector2(.96f, .92f));
            input.placeholder = placeholder;
            input.characterLimit = 40;
            input.lineType = InputField.LineType.SingleLine;
            return input;
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
    }
}
