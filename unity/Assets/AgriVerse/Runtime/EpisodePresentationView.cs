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
        private GameObject judge;
        private GameObject certificate;
        private Button glossaryButton;
        private Button judgeButton;
        private Button certificateButton;
        private Text guideText;
        private Text glossaryText;
        private Text judgeText;
        private Text certificateText;
        private Text landingError;
        private Action beginMission;
        private Action dismissGuide;
        private Action toggleGlossary;
        private Action toggleJudge;
        private Action openCertificate;
        private Action<EpisodeEndingChoice> chooseEnding;
        private Action<string> selectAvatar;

        internal InputField NameInput { get; private set; }
        internal bool LandingVisible => landing != null && landing.activeSelf;
        internal bool GuideVisible => guide != null && guide.activeSelf;
        internal bool GlossaryVisible => glossary != null && glossary.activeSelf;
        internal bool JudgeVisible => judge != null && judge.activeSelf;
        internal bool CertificateVisible =>
            certificate != null && certificate.activeSelf;
        internal string GuideText => guideText == null ? string.Empty : guideText.text;
        internal string GlossaryText =>
            glossaryText == null ? string.Empty : glossaryText.text;
        internal string JudgeText => judgeText == null ? string.Empty : judgeText.text;
        internal string CertificateText =>
            certificateText == null ? string.Empty : certificateText.text;

        internal void Build(
            Action onBegin,
            Action onDismissGuide,
            Action onToggleGlossary,
            Action onToggleJudge,
            Action onOpenCertificate,
            Action<EpisodeEndingChoice> onChooseEnding,
            Action<string> onSelectAvatar)
        {
            beginMission = onBegin;
            dismissGuide = onDismissGuide;
            toggleGlossary = onToggleGlossary;
            toggleJudge = onToggleJudge;
            openCertificate = onOpenCertificate;
            chooseEnding = onChooseEnding;
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
            BuildJudge(canvas.transform);
            BuildCertificate(canvas.transform);
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
            judgeButton.gameObject.SetActive(true);
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

        internal void SetJudgeText(string value)
        {
            RuntimeScrollableContent.SetText(judgeText, value ?? string.Empty);
        }

        internal void SetJudgeVisible(bool visible)
        {
            judge.SetActive(visible);
            judgeButton.gameObject.SetActive(!visible && !LandingVisible);
        }

        internal void SetCertificateAvailable(bool available)
        {
            certificateButton.gameObject.SetActive(available && !LandingVisible);
        }

        internal void ShowCertificate(string value)
        {
            RuntimeScrollableContent.SetText(
                certificateText,
                value ?? string.Empty);
            certificate.SetActive(true);
            certificateButton.gameObject.SetActive(false);
        }

        internal void HideCertificate(bool remainsAvailable)
        {
            certificate.SetActive(false);
            certificateButton.gameObject.SetActive(
                remainsAvailable && !LandingVisible);
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

        private void BuildJudge(Transform root)
        {
            judgeButton = EpisodeUiFactory.Button(
                root,
                "JudgeViewButton",
                "JUDGE VIEW",
                EpisodeUiFactory.RiverTeal,
                12);
            EpisodeUiFactory.Stretch(
                judgeButton.GetComponent<RectTransform>(),
                new Vector2(.015f, .80f),
                new Vector2(.125f, .852f));
            judgeButton.onClick.AddListener(() => toggleJudge?.Invoke());
            judgeButton.gameObject.SetActive(false);

            judge = EpisodeUiFactory.Panel(
                root,
                "JudgeViewBlocker",
                new Color(0f, .025f, .03f, .78f),
                true).gameObject;
            EpisodeUiFactory.Stretch(
                judge.GetComponent<RectTransform>(),
                Vector2.zero,
                Vector2.one);
            Image drawer = EpisodeUiFactory.Panel(
                judge.transform,
                "JudgeViewDrawer",
                EpisodeUiFactory.DeepTeal,
                true);
            EpisodeUiFactory.Stretch(
                drawer.rectTransform,
                new Vector2(.10f, .07f),
                new Vector2(.90f, .93f));
            Text title = EpisodeUiFactory.Text(
                drawer.transform,
                "JudgeViewTitle",
                22,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.OffWhite);
            title.text = "JUDGE VIEW";
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.05f, .91f),
                new Vector2(.75f, .98f));
            Button close = EpisodeUiFactory.Button(
                drawer.transform,
                "CloseJudgeView",
                "CLOSE",
                EpisodeUiFactory.RiverTeal,
                13);
            EpisodeUiFactory.Stretch(
                close.GetComponent<RectTransform>(),
                new Vector2(.82f, .915f),
                new Vector2(.95f, .98f));
            close.onClick.AddListener(() => toggleJudge?.Invoke());
            judgeText = RuntimeScrollableContent.Create(
                drawer.transform,
                "JudgeViewContent",
                new Vector2(.05f, .06f),
                new Vector2(.95f, .88f),
                14);
            judge.SetActive(false);
        }

        private void BuildCertificate(Transform root)
        {
            certificateButton = EpisodeUiFactory.Button(
                root,
                "CertificateButton",
                "CERTIFICATE",
                EpisodeUiFactory.Amber,
                12);
            EpisodeUiFactory.Stretch(
                certificateButton.GetComponent<RectTransform>(),
                new Vector2(.015f, .735f),
                new Vector2(.14f, .787f));
            certificateButton.onClick.AddListener(
                () => openCertificate?.Invoke());
            certificateButton.gameObject.SetActive(false);

            certificate = EpisodeUiFactory.Panel(
                root,
                "CertificateBlocker",
                new Color(0f, .025f, .03f, .82f),
                true).gameObject;
            EpisodeUiFactory.Stretch(
                certificate.GetComponent<RectTransform>(),
                Vector2.zero,
                Vector2.one);
            Image card = EpisodeUiFactory.Panel(
                certificate.transform,
                "CertificateCard",
                new Color(.07f, .15f, .15f, .99f),
                true);
            EpisodeUiFactory.Stretch(
                card.rectTransform,
                new Vector2(.14f, .07f),
                new Vector2(.86f, .93f));
            Text title = EpisodeUiFactory.Text(
                card.transform,
                "CertificateHeading",
                25,
                TextAnchor.MiddleCenter,
                EpisodeUiFactory.Amber);
            title.text = SaltLineNarrative.CertificateHeading;
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.06f, .89f),
                new Vector2(.94f, .97f));
            certificateText = RuntimeScrollableContent.Create(
                card.transform,
                "CertificateContent",
                new Vector2(.08f, .20f),
                new Vector2(.92f, .87f),
                17);

            Button home = EpisodeUiFactory.Button(
                card.transform,
                "ReturnHome",
                SaltLineNarrative.ReturnHome,
                EpisodeUiFactory.RiverTeal,
                14);
            EpisodeUiFactory.Stretch(
                home.GetComponent<RectTransform>(),
                new Vector2(.08f, .07f),
                new Vector2(.48f, .16f));
            home.onClick.AddListener(
                () => chooseEnding?.Invoke(EpisodeEndingChoice.ReturnHome));
            Button stay = EpisodeUiFactory.Button(
                card.transform,
                "StayAnotherSeason",
                SaltLineNarrative.StayAnotherSeason,
                EpisodeUiFactory.Amber,
                14);
            EpisodeUiFactory.Stretch(
                stay.GetComponent<RectTransform>(),
                new Vector2(.52f, .07f),
                new Vector2(.92f, .16f));
            stay.onClick.AddListener(
                () => chooseEnding?.Invoke(
                    EpisodeEndingChoice.StayAnotherSeason));
            certificate.SetActive(false);
        }
    }
}
