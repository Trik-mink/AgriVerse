using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    internal sealed class FieldNetworkPinView :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private FieldNetworkLocation location;
        private Action<FieldNetworkLocation> select;
        private Text marker;
        private GameObject label;
        private GameObject leader;
        private Text labelText;
        private bool selected;
        private bool hovered;
        private bool keyboardFocused;

        internal FieldNetworkLocation Location => location;
        internal RectTransform RectTransform =>
            transform as RectTransform;
        internal bool IsVisible => gameObject.activeInHierarchy;

        internal void Build(
            Transform parent,
            FieldNetworkLocation source,
            Action<FieldNetworkLocation> onSelect)
        {
            location = source;
            select = onSelect;
            transform.SetParent(parent, false);
            gameObject.name = "FieldPin_" + location.Id;

            RectTransform rect = RectTransform;
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(42f, 42f);

            Button button = gameObject.AddComponent<Button>();
            marker = EpisodeUiFactory.Text(
                transform,
                "PinMarker",
                27,
                TextAnchor.MiddleCenter,
                location.IsPlayable
                    ? EpisodeUiFactory.Amber
                    : EpisodeUiFactory.NetworkTeal);
            marker.text = "●";
            marker.raycastTarget = true;
            EpisodeUiFactory.Stretch(
                marker.rectTransform,
                Vector2.zero,
                Vector2.one);
            button.targetGraphic = marker;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            Color emphasis = location.IsPlayable
                ? EpisodeUiFactory.BrightAmber
                : new Color(0.54f, .92f, .87f, 1f);
            colors.highlightedColor = emphasis;
            colors.selectedColor = emphasis;
            colors.pressedColor = emphasis;
            colors.disabledColor =
                new Color(.5f, .5f, .5f, .35f);
            colors.fadeDuration = .18f;
            button.colors = colors;
            button.navigation = new Navigation
            {
                mode = Navigation.Mode.Automatic,
                wrapAround = true
            };
            button.onClick.AddListener(
                () => select?.Invoke(location));

            AtlasRouteGraphic leader =
                EpisodeUiFactory.Route(
                    transform,
                    "PinLeaderLine",
                    new[]
                    {
                        new Vector2(0f, .5f),
                        new Vector2(1f, .5f)
                    },
                    location.IsPlayable
                        ? EpisodeUiFactory.Amber
                        : EpisodeUiFactory.NetworkTeal,
                    1f);
            this.leader = leader.gameObject;
            RectTransform leaderRect = leader.rectTransform;
            leaderRect.anchorMin = new Vector2(.62f, .44f);
            leaderRect.anchorMax = new Vector2(1.28f, .56f);
            leaderRect.offsetMin = Vector2.zero;
            leaderRect.offsetMax = Vector2.zero;

            AtlasSurfaceGraphic labelSurface =
                EpisodeUiFactory.AtlasLabel(
                transform,
                "PinLabel",
                false);
            if (!location.IsPlayable)
            {
                labelSurface.color = new Color(
                    EpisodeUiFactory.SecondarySurface.r,
                    EpisodeUiFactory.SecondarySurface.g,
                    EpisodeUiFactory.SecondarySurface.b,
                    .86f);
            }
            label = labelSurface.gameObject;
            RectTransform labelRect = labelSurface.rectTransform;
            labelRect.anchorMin = new Vector2(1f, .5f);
            labelRect.anchorMax = new Vector2(1f, .5f);
            labelRect.pivot = new Vector2(0f, .5f);
            labelRect.anchoredPosition = new Vector2(22f, 0f);
            labelRect.sizeDelta = new Vector2(184f, 56f);

            labelText = EpisodeUiFactory.Text(
                label.transform,
                "LocationName",
                14,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.OffWhite);
            labelText.text =
                location.Country.ToUpperInvariant() +
                "  ·  " +
                (location.IsPlayable ? "AVAILABLE" : "INCOMING") +
                "\n" +
                location.Episode;
            EpisodeUiFactory.Stretch(
                labelText.rectTransform,
                new Vector2(.08f, .05f),
                new Vector2(.96f, .95f));
            label.SetActive(false);
            this.leader.SetActive(false);
        }

        internal void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        internal void SetSelected(bool value)
        {
            selected = value;
            RefreshLabel();
        }

        internal void SetKeyboardFocused(bool value)
        {
            keyboardFocused = value;
            RefreshLabel();
        }

        internal void SetPulse(float scale)
        {
            RectTransform.localScale = Vector3.one * scale;
        }

        internal void Focus()
        {
            if (!IsVisible || EventSystem.current == null) return;
            if (EventSystem.current.currentSelectedGameObject !=
                gameObject)
            {
                EventSystem.current.SetSelectedGameObject(
                    gameObject);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            RefreshLabel();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            RefreshLabel();
        }

        public void OnSelect(BaseEventData eventData)
        {
            hovered = true;
            RefreshLabel();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            hovered = false;
            RefreshLabel();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            bool visible =
                selected || hovered || keyboardFocused;
            label.SetActive(visible);
            leader?.SetActive(visible);
            if (labelText != null)
            {
                labelText.color = selected
                    ? (location.IsPlayable
                        ? EpisodeUiFactory.Amber
                        : EpisodeUiFactory.NetworkTeal)
                    : EpisodeUiFactory.OffWhite;
            }
        }
    }
}
