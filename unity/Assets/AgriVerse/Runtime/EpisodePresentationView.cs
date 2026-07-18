using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    internal sealed class EpisodePresentationView : MonoBehaviour
    {
        private readonly Dictionary<string, Button> avatarButtons =
            new Dictionary<string, Button>(StringComparer.Ordinal);

        private GameObject landing;
        private GameObject guide;
        private GameObject glossary;
        private Button glossaryButton;
        private Text guideText;
        private Text glossaryText;
        private Text landingError;
        private Action beginMission;
        private Action dismissGuide;
        private Action toggleGlossary;
        private Action<string> selectAvatar;

        internal InputField NameInput { get; private set; }
        internal bool LandingVisible => landing != null && landing.activeSelf;
        internal bool GuideVisible => guide != null && guide.activeSelf;
        internal bool GlossaryVisible => glossary != null && glossary.activeSelf;
        internal string GuideText => guideText == null ? string.Empty : guideText.text;
        internal string GlossaryText =>
            glossaryText == null ? string.Empty : glossaryText.text;

        internal void Build(
            Action onBegin,
            Action onDismissGuide,
            Action onToggleGlossary,
            Action<string> onSelectAvatar)
        {
            beginMission = onBegin;
            dismissGuide = onDismissGuide;
            toggleGlossary = onToggleGlossary;
            selectAvatar = onSelectAvatar;
            EpisodeUiFactory.EnsureEventSystem();

            GameObject canvasObject = new GameObject(
                "EpisodePresentationCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            BuildLanding(canvas.transform);
            BuildGuide(canvas.transform);
            BuildGlossary(canvas.transform);
        }

        internal void SetSelectedAvatar(string avatarId)
        {
            foreach (KeyValuePair<string, Button> entry in avatarButtons)
            {
                entry.Value.GetComponent<Image>().color =
                    entry.Key == avatarId
                        ? EpisodeUiFactory.Amber
                        : EpisodeUiFactory.RiverTeal;
            }
        }

        internal void HideLanding()
        {
            landing.SetActive(false);
            glossaryButton.gameObject.SetActive(true);
        }

        internal void ShowLandingError(string value)
        {
            landingError.text = value ?? string.Empty;
        }

        internal void ShowGuide(string value)
        {
            guideText.text = value ?? string.Empty;
            guide.SetActive(!string.IsNullOrWhiteSpace(value));
        }

        internal void HideGuide()
        {
            guide.SetActive(false);
        }

        internal void SetGlossaryVisible(bool visible)
        {
            glossary.SetActive(visible);
            glossaryButton.gameObject.SetActive(!visible && !LandingVisible);
        }

        private void BuildLanding(Transform root)
        {
            landing = EpisodeUiFactory.Panel(
                root,
                "LandingBlocker",
                new Color(0f, .025f, .03f, .62f),
                true).gameObject;
            EpisodeUiFactory.Stretch(
                landing.GetComponent<RectTransform>(),
                Vector2.zero,
                Vector2.one);

            Image card = EpisodeUiFactory.Panel(
                landing.transform,
                "LandingCard",
                EpisodeUiFactory.DeepTeal,
                true);
            EpisodeUiFactory.Stretch(
                card.rectTransform,
                new Vector2(.07f, .09f),
                new Vector2(.55f, .91f));

            Text title = EpisodeUiFactory.Text(
                card.transform,
                "Title",
                38,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.OffWhite);
            title.text = SaltLineNarrative.Title;
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.07f, .84f),
                new Vector2(.93f, .95f));

            Text episode = EpisodeUiFactory.Text(
                card.transform,
                "Episode",
                21,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.Amber);
            episode.text = SaltLineNarrative.Episode;
            EpisodeUiFactory.Stretch(
                episode.rectTransform,
                new Vector2(.07f, .76f),
                new Vector2(.93f, .84f));

            Text intro = EpisodeUiFactory.Text(
                card.transform,
                "Intro",
                16,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.OffWhite);
            intro.text = SaltLineNarrative.Tagline + "\n\n" + SaltLineNarrative.Intro;
            intro.lineSpacing = 1.1f;
            EpisodeUiFactory.Stretch(
                intro.rectTransform,
                new Vector2(.07f, .48f),
                new Vector2(.93f, .75f));

            Text nameLabel = EpisodeUiFactory.Text(
                card.transform,
                "NameLabel",
                15,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.OffWhite);
            nameLabel.text = SaltLineNarrative.NamePrompt;
            EpisodeUiFactory.Stretch(
                nameLabel.rectTransform,
                new Vector2(.07f, .42f),
                new Vector2(.93f, .48f));

            NameInput = EpisodeUiFactory.Input(card.transform, "Enter your name");
            EpisodeUiFactory.Stretch(
                NameInput.GetComponent<RectTransform>(),
                new Vector2(.07f, .34f),
                new Vector2(.93f, .42f));

            BuildAvatarChoices(card.transform);

            landingError = EpisodeUiFactory.Text(
                card.transform,
                "LandingError",
                13,
                TextAnchor.MiddleLeft,
                new Color(1f, .76f, .48f, 1f));
            EpisodeUiFactory.Stretch(
                landingError.rectTransform,
                new Vector2(.07f, .12f),
                new Vector2(.93f, .18f));

            Button start = EpisodeUiFactory.Button(
                card.transform,
                "BeginMission",
                SaltLineNarrative.StartButton,
                EpisodeUiFactory.Amber,
                17);
            EpisodeUiFactory.Stretch(
                start.GetComponent<RectTransform>(),
                new Vector2(.07f, .04f),
                new Vector2(.93f, .12f));
            start.onClick.AddListener(() => beginMission?.Invoke());
        }

        private void BuildAvatarChoices(Transform card)
        {
            string[] ids = { "river-teal", "sun-amber", "rice-green", "clay-red" };
            string[] labels = { "River", "Sun", "Rice", "Clay" };
            Color[] colors =
            {
                EpisodeUiFactory.RiverTeal,
                new Color(.62f, .38f, .12f, 1f),
                new Color(.18f, .34f, .16f, 1f),
                new Color(.46f, .20f, .13f, 1f)
            };

            for (int index = 0; index < ids.Length; index++)
            {
                string id = ids[index];
                Button avatar = EpisodeUiFactory.Button(
                    card,
                    "Avatar_" + id,
                    labels[index],
                    colors[index],
                    14);
                float left = .07f + index * .22f;
                EpisodeUiFactory.Stretch(
                    avatar.GetComponent<RectTransform>(),
                    new Vector2(left, .21f),
                    new Vector2(left + .18f, .31f));
                avatar.onClick.AddListener(() => selectAvatar?.Invoke(id));
                avatarButtons.Add(id, avatar);
            }
        }

        private void BuildGuide(Transform root)
        {
            guide = EpisodeUiFactory.Panel(
                root,
                "MaiGuidance",
                EpisodeUiFactory.DeepTeal,
                false).gameObject;
            EpisodeUiFactory.Stretch(
                guide.GetComponent<RectTransform>(),
                new Vector2(.15f, .035f),
                new Vector2(.85f, .235f));

            Text badge = EpisodeUiFactory.Text(
                guide.transform,
                "MaiBadge",
                18,
                TextAnchor.MiddleCenter,
                EpisodeUiFactory.Amber);
            badge.text = "MAI";
            EpisodeUiFactory.Stretch(
                badge.rectTransform,
                new Vector2(.035f, .16f),
                new Vector2(.15f, .84f));

            guideText = EpisodeUiFactory.Text(
                guide.transform,
                "MaiText",
                16,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.OffWhite);
            EpisodeUiFactory.Stretch(
                guideText.rectTransform,
                new Vector2(.18f, .15f),
                new Vector2(.84f, .85f));

            Button close = EpisodeUiFactory.Button(
                guide.transform,
                "DismissMai",
                "CONTINUE",
                EpisodeUiFactory.Amber,
                12);
            EpisodeUiFactory.Stretch(
                close.GetComponent<RectTransform>(),
                new Vector2(.86f, .27f),
                new Vector2(.97f, .73f));
            close.onClick.AddListener(() => dismissGuide?.Invoke());
            guide.SetActive(false);
        }

        private void BuildGlossary(Transform root)
        {
            glossaryButton = EpisodeUiFactory.Button(
                root,
                "GlossaryButton",
                "GLOSSARY",
                EpisodeUiFactory.RiverTeal,
                12);
            EpisodeUiFactory.Stretch(
                glossaryButton.GetComponent<RectTransform>(),
                new Vector2(.54f, .92f),
                new Vector2(.66f, .972f));
            glossaryButton.onClick.AddListener(() => toggleGlossary?.Invoke());
            glossaryButton.gameObject.SetActive(false);

            glossary = EpisodeUiFactory.Panel(
                root,
                "GlossaryBlocker",
                new Color(0f, .025f, .03f, .72f),
                true).gameObject;
            EpisodeUiFactory.Stretch(
                glossary.GetComponent<RectTransform>(),
                Vector2.zero,
                Vector2.one);
            Image drawer = EpisodeUiFactory.Panel(
                glossary.transform,
                "GlossaryDrawer",
                EpisodeUiFactory.DeepTeal,
                true);
            EpisodeUiFactory.Stretch(
                drawer.rectTransform,
                new Vector2(.19f, .10f),
                new Vector2(.81f, .90f));
            Text title = EpisodeUiFactory.Text(
                drawer.transform,
                "GlossaryTitle",
                22,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.OffWhite);
            title.text = "FIELD GLOSSARY";
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.06f, .90f),
                new Vector2(.78f, .98f));
            Button close = EpisodeUiFactory.Button(
                drawer.transform,
                "CloseGlossary",
                "CLOSE",
                EpisodeUiFactory.RiverTeal,
                13);
            EpisodeUiFactory.Stretch(
                close.GetComponent<RectTransform>(),
                new Vector2(.81f, .91f),
                new Vector2(.94f, .98f));
            close.onClick.AddListener(() => toggleGlossary?.Invoke());

            glossaryText = RuntimeScrollableContent.Create(
                drawer.transform,
                "GlossaryContent",
                new Vector2(.06f, .07f),
                new Vector2(.94f, .87f),
                16);
            var content = new StringBuilder();
            foreach (string entry in SaltLineNarrative.Glossary)
            {
                if (content.Length > 0) content.Append("\n\n");
                content.Append(entry);
            }
            RuntimeScrollableContent.SetText(glossaryText, content.ToString());
            glossary.SetActive(false);
        }
    }
}
