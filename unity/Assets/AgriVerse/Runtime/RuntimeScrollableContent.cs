using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    /// <summary>
    /// Shared content-region contract: readable text must remain reachable through wheel,
    /// trackpad, keyboard-drag, and a visible-on-overflow scrollbar.
    /// </summary>
    public static class RuntimeScrollableContent
    {
        public static Text Create(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize)
        {
            ScrollRect scroll = new GameObject(name + "Scroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask), typeof(ScrollRect)).GetComponent<ScrollRect>();
            scroll.transform.SetParent(parent, false);
            Stretch(scroll.GetComponent<RectTransform>(), anchorMin, anchorMax);
            Image viewportImage = scroll.GetComponent<Image>();
            viewportImage.color = EpisodeUiFactory.DeepTeal;
            Outline outline =
                scroll.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(
                EpisodeUiFactory.Amber.r,
                EpisodeUiFactory.Amber.g,
                EpisodeUiFactory.Amber.b,
                .34f);
            outline.effectDistance = new Vector2(1f, -1f);
            // Scrolling is handled by RuntimeScrollInput inside this card's rect. Keeping UI
            // graphics non-raycastable preserves the world-marker click path beneath the card.
            viewportImage.raycastTarget = false;
            scroll.GetComponent<Mask>().showMaskGraphic = false;

            RectTransform content = new GameObject(name + "Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(scroll.transform, false);
            // Keep text inside the mask rather than flush against its clipped left edge.
            content.anchorMin = new Vector2(.04f, 1f);
            content.anchorMax = new Vector2(.88f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.anchoredPosition = new Vector2(0f, -8f);

            Text text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(content, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = Mathf.Max(14, fontSize);
            text.color = EpisodeUiFactory.OffWhite;
            text.alignment = TextAnchor.UpperLeft;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = new Vector2(0f, 1f);
            text.rectTransform.anchorMax = new Vector2(1f, 1f);
            text.rectTransform.pivot = new Vector2(0f, 1f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            Scrollbar scrollbar = CreateScrollbar(scroll.transform, name + "Scrollbar");
            scroll.viewport = scroll.GetComponent<RectTransform>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scroll.verticalScrollbarSpacing = 4f;
            scroll.gameObject.AddComponent<RuntimeScrollInput>();
            return text;
        }

        public static void SetText(Text text, string value)
        {
            text.text = value;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, text.preferredHeight);
            text.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, text.preferredHeight);
        }

        public static bool MeetsContract(Text text)
        {
            if (text == null) return false;
            ScrollRect scroll = text.GetComponentInParent<ScrollRect>();
            return scroll != null && scroll.vertical && scroll.verticalScrollbar != null &&
                   scroll.verticalScrollbarVisibility == ScrollRect.ScrollbarVisibility.AutoHide &&
                   scroll.GetComponent<RuntimeScrollInput>() != null && DoesNotBlockSceneRaycasts(scroll);
        }

        public static bool ScrollToBottom(Text text)
        {
            ScrollRect scroll = text == null ? null : text.GetComponentInParent<ScrollRect>();
            if (scroll == null) return false;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
            return scroll.verticalNormalizedPosition <= .001f;
        }

        public static bool ActiveScrollViewsDoNotBlockSceneRaycasts()
        {
            foreach (ScrollRect scroll in Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (!DoesNotBlockSceneRaycasts(scroll)) return false;
            return true;
        }

        public static bool IsBoundedToVisibleCard(Text text)
        {
            ScrollRect scroll = text == null ? null : text.GetComponentInParent<ScrollRect>();
            if (scroll == null) return false;
            RectTransform rect = scroll.GetComponent<RectTransform>();
            return rect.anchorMin != Vector2.zero || rect.anchorMax != Vector2.one;
        }

        public static bool HasTopLeftTextGutter(Text text)
        {
            if (text == null) return false;
            RectTransform content = text.rectTransform.parent as RectTransform;
            return content != null && content.anchorMin.x > 0f && content.anchorMax.x < 1f &&
                   content.anchoredPosition.y < 0f && text.rectTransform.anchorMin == new Vector2(0f, 1f) &&
                   text.rectTransform.anchorMax == new Vector2(1f, 1f) && text.rectTransform.pivot == new Vector2(0f, 1f);
        }

        private static Scrollbar CreateScrollbar(Transform parent, string name)
        {
            Scrollbar scrollbar = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar)).GetComponent<Scrollbar>();
            scrollbar.transform.SetParent(parent, false);
            Image track = scrollbar.GetComponent<Image>();
            track.color = EpisodeUiFactory.SecondarySurface;
            track.raycastTarget = false;
            RectTransform barRect = scrollbar.GetComponent<RectTransform>();
            Stretch(barRect, new Vector2(.93f, .03f), new Vector2(.99f, .97f));

            Image handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            handle.transform.SetParent(scrollbar.transform, false);
            handle.color = EpisodeUiFactory.Amber;
            handle.raycastTarget = false;
            Stretch(handle.rectTransform, Vector2.zero, Vector2.one);
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            return scrollbar;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool DoesNotBlockSceneRaycasts(ScrollRect scroll)
        {
            foreach (Graphic graphic in scroll.GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget) return false;
            return true;
        }
    }

    /// <summary>Bounded wheel and trackpad input for a non-raycastable scroll card.</summary>
    internal sealed class RuntimeScrollInput : MonoBehaviour
    {
        private ScrollRect scroll;
        private RectTransform card;
        private bool dragging;
        private Vector2 lastPointerPosition;

        private void Awake()
        {
            scroll = GetComponent<ScrollRect>();
            card = GetComponent<RectTransform>();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || scroll == null || card == null) return;
            Vector2 pointer = mouse.position.ReadValue();
            bool insideCard = RectTransformUtility.RectangleContainsScreenPoint(card, pointer, null);
            if (insideCard)
            {
                float wheelDelta = mouse.scroll.ReadValue().y;
                if (!Mathf.Approximately(wheelDelta, 0f))
                    scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + wheelDelta / 600f);
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    dragging = true;
                    lastPointerPosition = pointer;
                }
            }

            if (dragging && mouse.leftButton.isPressed)
            {
                float height = Mathf.Max(card.rect.height, 1f);
                scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + (pointer.y - lastPointerPosition.y) / height);
                lastPointerPosition = pointer;
            }
            if (!mouse.leftButton.isPressed) dragging = false;
        }
    }
}
