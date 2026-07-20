using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    internal sealed class EpisodePresentationView : MonoBehaviour
    {
        private GameObject landing;
        private GameObject guide;
        private GameObject glossary;
        private GameObject judge;
        private GameObject certificate;
        private Button glossaryButton;
        private Button judgeButton;
        private Button judgeTechnicalButton;
        private Button certificateButton;
        private Text guideText;
        private Text glossaryText;
        private Text judgeText;
        private Text certificateText;
        private Text landingError;
        private Text landingInstruction;
        private Text missionCountry;
        private Text missionRegion;
        private Text missionEpisode;
        private Text missionTagline;
        private Text incomingCountry;
        private Text incomingRegion;
        private Text incomingEpisode;
        private Text incomingTeaser;
        private GameObject missionReveal;
        private GameObject incomingReveal;
        private GameObject connectionStatus;
        private Text connectionStatusTitle;
        private Text connectionStatusBody;
        private Button retryConnectionButton;
        private Button missionStartButton;
        private Text missionStartLabel;
        private GlobeLandingRenderer globeRenderer;
        private CanvasGroup arrivalVeil;
        private float arrivalStartedAt;
        private FieldNetworkCatalog fieldCatalog;
        private FieldNetworkLandingState fieldState;
        private Action beginMission;
        private Action dismissGuide;
        private Action toggleGlossary;
        private Action toggleJudge;
        private Action toggleJudgeTechnical;
        private Action openCertificate;
        private Action<EpisodeEndingChoice> chooseEnding;
        private Action retryConnection;

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
        internal string SelectedFieldLocationId =>
            fieldState?.SelectedLocation?.Id ?? string.Empty;
        internal bool IncomingLocationSelected =>
            fieldState?.SelectedLocation != null &&
            !fieldState.SelectedLocation.IsPlayable;
        internal bool NameEntryVisible =>
            NameInput != null && NameInput.gameObject.activeInHierarchy;
        internal bool MissionStartVisible =>
            missionStartButton != null &&
            missionStartButton.gameObject.activeInHierarchy;
        internal bool MissionStartInteractable =>
            missionStartButton != null &&
            missionStartButton.interactable;
        internal bool ConnectionStatusVisible =>
            connectionStatus != null &&
            connectionStatus.activeInHierarchy;
        internal bool RetryVisible =>
            retryConnectionButton != null &&
            retryConnectionButton.gameObject.activeInHierarchy;
        internal string ConnectionStatusText =>
            (connectionStatusTitle?.text ?? string.Empty) +
            "\n" +
            (connectionStatusBody?.text ?? string.Empty);
        internal bool MissionConnectionRequired =>
            fieldState != null &&
            !fieldState.MissionServiceReady &&
            fieldState.SelectedLocation?.IsPlayable == true;
        internal string PlayerName =>
            NameInput?.text ?? string.Empty;
        internal int FieldNetworkPinCount =>
            globeRenderer?.PinCount ?? 0;

        internal void Build(
            Action onBegin,
            Action onDismissGuide,
            Action onToggleGlossary,
            Action onToggleJudge,
            Action onToggleJudgeTechnical,
            Action onOpenCertificate,
            Action<EpisodeEndingChoice> onChooseEnding,
            Action onRetryConnection)
        {
            beginMission = onBegin;
            dismissGuide = onDismissGuide;
            toggleGlossary = onToggleGlossary;
            toggleJudge = onToggleJudge;
            toggleJudgeTechnical = onToggleJudgeTechnical;
            openCertificate = onOpenCertificate;
            chooseEnding = onChooseEnding;
            retryConnection = onRetryConnection;
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
            RuntimePanelManager.GetOrCreate()
                .SetCinematicMode(true);
        }

        internal void HideLanding()
        {
            landing.SetActive(false);
            globeRenderer?.SetVisible(false);
            RuntimePanelManager.GetOrCreate()
                .SetCinematicMode(false);
            glossaryButton.gameObject.SetActive(true);
            judgeButton.gameObject.SetActive(false);
        }

        internal void ShowLandingForNewJourney(
            ScenarioDto scenario)
        {
            NameInput?.DeactivateInputField();
            if (NameInput != null)
            {
                NameInput.text = string.Empty;
            }
            guide.SetActive(false);
            glossary.SetActive(false);
            judge.SetActive(false);
            certificate.SetActive(false);
            glossaryButton.gameObject.SetActive(false);
            judgeButton.gameObject.SetActive(false);
            certificateButton.gameObject.SetActive(false);
            if (judgeTechnicalButton != null)
            {
                SetJudgeTechnicalVisible(false);
            }
            ConfigureFieldNetwork(
                scenario,
                FieldNetworkConnectionState.Ready);
            ClearFieldLocationSelection();
            landing.SetActive(true);
            globeRenderer?.SetVisible(true);
            RuntimePanelManager.GetOrCreate()
                .SetCinematicMode(true);
            if (arrivalVeil != null)
            {
                arrivalVeil.alpha = EpisodeAccessibility.ReducedMotion
                    ? 0f
                    : 1f;
                arrivalVeil.gameObject.SetActive(
                    arrivalVeil.alpha > 0f);
                arrivalStartedAt = Time.unscaledTime;
            }
        }

        internal void ShowLandingError(string value)
        {
            landingError.text = value ?? string.Empty;
        }

        internal void ConfigureFieldNetwork(
            ScenarioDto scenario,
            FieldNetworkConnectionState connectionState =
                FieldNetworkConnectionState.Ready)
        {
            fieldCatalog =
                FieldNetworkCatalog.CreateForScenario(
                    scenario,
                    SaltLineNarrative.Episode,
                    SaltLineNarrative.Tagline);
            fieldState =
                new FieldNetworkLandingState(
                    fieldCatalog,
                    connectionState);
            globeRenderer?.SetCatalog(fieldCatalog);
            RefreshLandingState();
        }

        internal void SetConnectionState(
            FieldNetworkConnectionState value)
        {
            fieldState?.SetConnectionState(value);
            RefreshConnectionState();
            RefreshMissionStart();
        }

        internal bool SelectFieldLocation(string id)
        {
            if (fieldState == null ||
                !fieldState.Select(id))
            {
                return false;
            }
            globeRenderer?.FocusLocation(
                fieldState.SelectedLocation);
            landingError.text = string.Empty;
            RefreshLandingState();
            return true;
        }

        internal void ClearFieldLocationSelection()
        {
            fieldState?.ClearSelection();
            globeRenderer?.ClearSelection();
            landingError.text = string.Empty;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            RefreshLandingState();
        }

        internal bool CanBeginMission(string playerName) =>
            fieldState != null &&
            fieldState.CanBeginMission(playerName);

        internal void FocusNextFieldLocation(int direction)
        {
            globeRenderer?.FocusNextPin(direction);
        }

        internal bool SelectKeyboardFocusedFieldLocation() =>
            globeRenderer != null &&
            globeRenderer.SelectKeyboardFocusedPin();

        internal void SetPlayerName(string playerName)
        {
            if (NameInput != null)
            {
                NameInput.text = playerName ?? string.Empty;
            }
            RefreshMissionStart();
        }

        private void Update()
        {
            if (!LandingVisible)
            {
                return;
            }

            if (arrivalVeil != null &&
                arrivalVeil.alpha > 0f)
            {
                float duration =
                    EpisodeAccessibility.ReducedMotion
                        ? .18f
                        : 1.5f;
                arrivalVeil.alpha = 1f - Mathf.Clamp01(
                    (Time.unscaledTime - arrivalStartedAt) /
                    duration);
                if (arrivalVeil.alpha <= 0f)
                {
                    arrivalVeil.gameObject.SetActive(false);
                }
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                bool reverse =
                    keyboard.leftShiftKey.isPressed ||
                    keyboard.rightShiftKey.isPressed;
                globeRenderer?.FocusNextPin(reverse ? -1 : 1);
            }
            if ((keyboard.enterKey.wasPressedThisFrame ||
                 keyboard.numpadEnterKey.wasPressedThisFrame) &&
                (NameInput == null ||
                 EventSystem.current == null ||
                 EventSystem.current.currentSelectedGameObject !=
                 NameInput.gameObject))
            {
                globeRenderer?.SelectKeyboardFocusedPin();
            }
            if (fieldState?.SelectedLocation != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                ClearFieldLocationSelection();
            }
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
            if (visible)
            {
                judgeButton.gameObject.SetActive(false);
            }
            else if (judgeTechnicalButton != null)
            {
                SetJudgeTechnicalVisible(false);
            }
        }

        internal void SetJudgeTechnicalVisible(bool visible)
        {
            if (judgeTechnicalButton == null) return;
            Text label =
                judgeTechnicalButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = visible
                    ? "HIDE TECHNICAL JSON"
                    : "TECHNICAL JSON";
            }
        }

        internal void SetJudgeAvailable(bool available)
        {
            judgeButton.gameObject.SetActive(
                available && !LandingVisible && !JudgeVisible);
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
                new Color(0f, .01f, .02f, .001f),
                true).gameObject;
            EpisodeUiFactory.Stretch(
                landing.GetComponent<RectTransform>(),
                Vector2.zero,
                Vector2.one);

            CinematicGradientGraphic leftShade =
                EpisodeUiFactory.Gradient(
                landing.transform,
                "LeftEdgeReadability",
                new Color(.002f, .035f, .045f, .58f),
                Color.clear,
                new Color(.002f, .035f, .045f, .58f),
                Color.clear);
            EpisodeUiFactory.Stretch(
                leftShade.rectTransform,
                Vector2.zero,
                new Vector2(.64f, 1f));
            CinematicGradientGraphic lowerShade =
                EpisodeUiFactory.Gradient(
                landing.transform,
                "LowerEdgeReadability",
                Color.clear,
                Color.clear,
                new Color(.002f, .025f, .034f, .54f),
                new Color(.002f, .025f, .034f, .54f));
            EpisodeUiFactory.Stretch(
                lowerShade.rectTransform,
                Vector2.zero,
                new Vector2(1f, .38f));

            Text title = EpisodeUiFactory.Text(
                landing.transform,
                "Title",
                25,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.OffWhite);
            title.text = "AGRIVERSE";
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.04f, .90f),
                new Vector2(.30f, .97f));

            Text network = EpisodeUiFactory.Text(
                landing.transform,
                "Network",
                14,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.Amber);
            network.text = "GLOBAL FIELD NETWORK";
            EpisodeUiFactory.Stretch(
                network.rectTransform,
                new Vector2(.04f, .85f),
                new Vector2(.34f, .91f));

            Text networkPromise = EpisodeUiFactory.Text(
                landing.transform,
                "NetworkPromise",
                15,
                TextAnchor.UpperLeft,
                new Color(.95f, .94f, .87f, .78f));
            networkPromise.text =
                "One planet. Many food systems. No easy answers.";
            EpisodeUiFactory.Stretch(
                networkPromise.rectTransform,
                new Vector2(.04f, .795f),
                new Vector2(.42f, .85f));

            landingInstruction = EpisodeUiFactory.Text(
                landing.transform,
                "LandingInstruction",
                14,
                TextAnchor.UpperRight,
                new Color(.95f, .94f, .87f, .82f));
            EpisodeUiFactory.Stretch(
                landingInstruction.rectTransform,
                new Vector2(.53f, .89f),
                new Vector2(.96f, .96f));

            RectTransform pinLayer = new GameObject(
                "FieldNetworkPins",
                typeof(RectTransform))
                .GetComponent<RectTransform>();
            pinLayer.SetParent(landing.transform, false);
            EpisodeUiFactory.Stretch(
                pinLayer,
                Vector2.zero,
                Vector2.one);

            BuildIncomingReveal(landing.transform);
            BuildMissionReveal(landing.transform);
            BuildConnectionStatus(landing.transform);

            Text hint = EpisodeUiFactory.Text(
                landing.transform,
                "GlobeControls",
                13,
                TextAnchor.LowerRight,
                new Color(.92f, .91f, .84f, .74f));
            hint.text =
                "DRAG TO ROTATE  ·  SCROLL TO ZOOM  ·  TAB TO EXPLORE PINS";
            EpisodeUiFactory.Stretch(
                hint.rectTransform,
                new Vector2(.52f, .025f),
                new Vector2(.96f, .08f));

            globeRenderer =
                landing.AddComponent<GlobeLandingRenderer>();
            globeRenderer.Initialize(
                pinLayer,
                location =>
                    SelectFieldLocation(location.Id));

            Image veil = EpisodeUiFactory.Panel(
                landing.transform,
                "OrbitalArrivalFade",
                new Color(.001f, .004f, .01f, 1f),
                false);
            EpisodeUiFactory.Stretch(
                veil.rectTransform,
                Vector2.zero,
                Vector2.one);
            arrivalVeil =
                veil.gameObject.AddComponent<CanvasGroup>();
            arrivalVeil.alpha = 1f;
            arrivalStartedAt = Time.unscaledTime;
        }

        private void BuildConnectionStatus(Transform root)
        {
            AtlasSurfaceGraphic surface =
                EpisodeUiFactory.AtlasLabel(
                    root,
                    "FieldNetworkConnectionStatus",
                    true);
            connectionStatus = surface.gameObject;
            EpisodeUiFactory.Stretch(
                surface.rectTransform,
                new Vector2(.62f, .69f),
                new Vector2(.96f, .86f));

            connectionStatusTitle = EpisodeUiFactory.Text(
                connectionStatus.transform,
                "ConnectionStatusTitle",
                14,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.Amber);
            EpisodeUiFactory.Stretch(
                connectionStatusTitle.rectTransform,
                new Vector2(.055f, .57f),
                new Vector2(.95f, .91f));

            connectionStatusBody = EpisodeUiFactory.Text(
                connectionStatus.transform,
                "ConnectionStatusBody",
                13,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.OffWhite);
            EpisodeUiFactory.Stretch(
                connectionStatusBody.rectTransform,
                new Vector2(.055f, .10f),
                new Vector2(.61f, .58f));

            retryConnectionButton = EpisodeUiFactory.Button(
                connectionStatus.transform,
                "RetryConnection",
                "RETRY CONNECTION",
                EpisodeButtonStyle.Secondary,
                12);
            EpisodeUiFactory.Stretch(
                retryConnectionButton.GetComponent<RectTransform>(),
                new Vector2(.65f, .17f),
                new Vector2(.95f, .49f));
            retryConnectionButton.onClick.AddListener(
                () => retryConnection?.Invoke());
            connectionStatusTitle.text =
                "CONNECTING TO FIELD NETWORK";
            connectionStatusBody.text =
                "Loading the playable field mission…";
            retryConnectionButton.gameObject.SetActive(false);
        }

        private void BuildIncomingReveal(Transform root)
        {
            incomingReveal = new GameObject(
                "IncomingLocationReveal",
                typeof(RectTransform));
            incomingReveal.transform.SetParent(root, false);
            EpisodeUiFactory.Stretch(
                incomingReveal.GetComponent<RectTransform>(),
                new Vector2(.05f, .08f),
                new Vector2(.48f, .39f));
            Image accent = EpisodeUiFactory.Panel(
                incomingReveal.transform,
                "IncomingAccent",
                EpisodeUiFactory.NetworkTeal,
                false);
            EpisodeUiFactory.Stretch(
                accent.rectTransform,
                new Vector2(0f, .05f),
                new Vector2(.009f, .95f));
            incomingCountry = EpisodeUiFactory.Text(
                incomingReveal.transform,
                "IncomingCountry",
                25,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.OffWhite);
            EpisodeUiFactory.Stretch(
                incomingCountry.rectTransform,
                new Vector2(.05f, .76f),
                new Vector2(.96f, .98f));
            Text status = EpisodeUiFactory.Text(
                incomingReveal.transform,
                "IncomingStatus",
                14,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.NetworkTeal);
            status.text = "INCOMING FIELD EPISODE";
            EpisodeUiFactory.Stretch(
                status.rectTransform,
                new Vector2(.05f, .64f),
                new Vector2(.96f, .77f));
            incomingRegion = EpisodeUiFactory.Text(
                incomingReveal.transform,
                "IncomingRegion",
                15,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.NetworkTeal);
            EpisodeUiFactory.Stretch(
                incomingRegion.rectTransform,
                new Vector2(.05f, .51f),
                new Vector2(.96f, .64f));
            incomingEpisode = EpisodeUiFactory.Text(
                incomingReveal.transform,
                "IncomingEpisode",
                18,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.OffWhite);
            EpisodeUiFactory.Stretch(
                incomingEpisode.rectTransform,
                new Vector2(.05f, .36f),
                new Vector2(.96f, .52f));
            incomingTeaser = EpisodeUiFactory.Text(
                incomingReveal.transform,
                "IncomingTeaser",
                15,
                TextAnchor.UpperLeft,
                new Color(.96f, .95f, .89f, .86f));
            EpisodeUiFactory.Stretch(
                incomingTeaser.rectTransform,
                new Vector2(.05f, .12f),
                new Vector2(.96f, .36f));
            Text futureHint = EpisodeUiFactory.Text(
                incomingReveal.transform,
                "IncomingHint",
                13,
                TextAnchor.LowerLeft,
                new Color(.96f, .95f, .89f, .68f));
            futureHint.text =
                "ESCAPE TO KEEP EXPLORING THE NETWORK";
            EpisodeUiFactory.Stretch(
                futureHint.rectTransform,
                new Vector2(.05f, .01f),
                new Vector2(.96f, .11f));
            incomingReveal.SetActive(false);
        }

        private void BuildMissionReveal(Transform root)
        {
            missionReveal = new GameObject(
                "AvailableMissionReveal",
                typeof(RectTransform));
            missionReveal.transform.SetParent(root, false);
            EpisodeUiFactory.Stretch(
                missionReveal.GetComponent<RectTransform>(),
                new Vector2(.05f, .065f),
                new Vector2(.59f, .43f));

            Image accent = EpisodeUiFactory.Panel(
                missionReveal.transform,
                "AvailableAccent",
                EpisodeUiFactory.Amber,
                false);
            EpisodeUiFactory.Stretch(
                accent.rectTransform,
                new Vector2(0f, .04f),
                new Vector2(.008f, .97f));
            missionCountry = EpisodeUiFactory.Text(
                missionReveal.transform,
                "MissionCountry",
                28,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.OffWhite);
            EpisodeUiFactory.Stretch(
                missionCountry.rectTransform,
                new Vector2(.035f, .80f),
                new Vector2(.98f, .98f));
            missionRegion = EpisodeUiFactory.Text(
                missionReveal.transform,
                "MissionRegion",
                17,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.Amber);
            EpisodeUiFactory.Stretch(
                missionRegion.rectTransform,
                new Vector2(.035f, .67f),
                new Vector2(.98f, .82f));
            missionEpisode = EpisodeUiFactory.Text(
                missionReveal.transform,
                "MissionEpisode",
                18,
                TextAnchor.UpperLeft,
                EpisodeUiFactory.OffWhite);
            EpisodeUiFactory.Stretch(
                missionEpisode.rectTransform,
                new Vector2(.035f, .53f),
                new Vector2(.98f, .68f));
            missionTagline = EpisodeUiFactory.Text(
                missionReveal.transform,
                "MissionTagline",
                15,
                TextAnchor.UpperLeft,
                new Color(.96f, .95f, .89f, .88f));
            EpisodeUiFactory.Stretch(
                missionTagline.rectTransform,
                new Vector2(.035f, .41f),
                new Vector2(.98f, .54f));

            Text nameLabel = EpisodeUiFactory.Text(
                missionReveal.transform,
                "NameLabel",
                14,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.OffWhite);
            nameLabel.text = SaltLineNarrative.NamePrompt;
            EpisodeUiFactory.Stretch(
                nameLabel.rectTransform,
                new Vector2(.035f, .30f),
                new Vector2(.98f, .41f));
            NameInput = EpisodeUiFactory.Input(
                missionReveal.transform,
                "Enter your name");
            EpisodeUiFactory.Stretch(
                NameInput.GetComponent<RectTransform>(),
                new Vector2(.035f, .10f),
                new Vector2(.61f, .30f));
            NameInput.onValueChanged.AddListener(_ =>
            {
                landingError.text = string.Empty;
                RefreshMissionStart();
            });

            missionStartButton = EpisodeUiFactory.Button(
                missionReveal.transform,
                "BeginMission",
                SaltLineNarrative.StartButton,
                EpisodeUiFactory.Amber,
                16);
            EpisodeUiFactory.Stretch(
                missionStartButton.GetComponent<RectTransform>(),
                new Vector2(.64f, .10f),
                new Vector2(.98f, .30f));
            missionStartButton.onClick.AddListener(
                () => beginMission?.Invoke());
            missionStartLabel =
                missionStartButton.GetComponentInChildren<Text>();

            landingError = EpisodeUiFactory.Text(
                missionReveal.transform,
                "LandingError",
                13,
                TextAnchor.MiddleLeft,
                new Color(1f, .76f, .48f, 1f));
            EpisodeUiFactory.Stretch(
                landingError.rectTransform,
                new Vector2(.035f, 0f),
                new Vector2(.98f, .09f));
            missionReveal.SetActive(false);
        }

        private void RefreshLandingState()
        {
            FieldNetworkLocation selected =
                fieldState?.SelectedLocation;
            bool available =
                selected != null && selected.IsPlayable;
            bool incoming =
                selected != null && !selected.IsPlayable;
            missionReveal?.SetActive(available);
            incomingReveal?.SetActive(incoming);

            if (selected == null)
            {
                landingInstruction.text =
                    "SELECT A FIELD PIN TO DISCOVER THE NETWORK";
                RefreshConnectionState();
                return;
            }

            if (incoming)
            {
                landingInstruction.text =
                    "INCOMING FIELD EPISODE  ·  ESCAPE TO RETURN";
                incomingCountry.text =
                    selected.Country.ToUpperInvariant();
                incomingRegion.text = selected.Region;
                incomingEpisode.text = selected.Episode;
                incomingTeaser.text = selected.Teaser;
                RefreshConnectionState();
                return;
            }

            landingInstruction.text =
                "AVAILABLE FIELD EPISODE  ·  ESCAPE TO RETURN";
            missionCountry.text =
                selected.Country.ToUpperInvariant();
            missionRegion.text = selected.Region;
            missionEpisode.text = selected.Episode;
            missionTagline.text = selected.Teaser;
            RefreshConnectionState();
            RefreshMissionStart();
        }

        private void RefreshMissionStart()
        {
            if (missionStartButton == null) return;
            bool connectionRequired =
                MissionConnectionRequired;
            missionStartButton.interactable =
                fieldState != null &&
                fieldState.CanBeginMission(NameInput?.text);
            if (missionStartLabel != null)
            {
                missionStartLabel.text =
                    connectionRequired
                        ? "CONNECTION REQUIRED"
                        : SaltLineNarrative.StartButton;
            }
            if (connectionRequired &&
                landingError != null)
            {
                landingError.text =
                    "Connection required to begin this mission.";
            }
            else if (landingError != null &&
                     string.Equals(
                         landingError.text,
                         "Connection required to begin this mission.",
                         StringComparison.Ordinal))
            {
                landingError.text = string.Empty;
            }
        }

        private void RefreshConnectionState()
        {
            if (connectionStatus == null) return;
            FieldNetworkConnectionState state =
                fieldState?.ConnectionState ??
                FieldNetworkConnectionState.Loading;
            bool ready =
                state == FieldNetworkConnectionState.Ready;
            connectionStatus.SetActive(!ready);
            if (ready) return;

            bool offline =
                state == FieldNetworkConnectionState.Offline;
            connectionStatusTitle.text = offline
                ? "FIELD NETWORK OFFLINE"
                : "CONNECTING TO FIELD NETWORK";
            connectionStatusBody.text = offline
                ? "The mission service could not be reached."
                : "Loading the playable field mission…";
            retryConnectionButton.gameObject.SetActive(offline);
            retryConnectionButton.interactable = offline;
        }

        internal bool LandingControlsUsableAt(
            Vector2 resolution)
        {
            if (resolution.x < 1280f ||
                resolution.y < 720f ||
                connectionStatus == null ||
                missionReveal == null)
            {
                return false;
            }

            RectTransform statusRect =
                connectionStatus.GetComponent<RectTransform>();
            RectTransform missionRect =
                missionReveal.GetComponent<RectTransform>();
            Vector2 statusSize = Vector2.Scale(
                statusRect.anchorMax - statusRect.anchorMin,
                resolution);
            Vector2 missionSize = Vector2.Scale(
                missionRect.anchorMax - missionRect.anchorMin,
                resolution);
            return statusSize.x >= 400f &&
                   statusSize.y >= 110f &&
                   missionSize.x >= 640f &&
                   missionSize.y >= 250f;
        }

        private void BuildGuide(Transform root)
        {
            guide = EpisodeUiFactory.SmokedGlass(
                root,
                "MaiGuidance",
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
                EpisodeButtonStyle.Primary,
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
            AtlasSurfaceGraphic drawer =
                EpisodeUiFactory.FieldPaper(
                glossary.transform,
                "GlossaryDrawer",
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
                EpisodeUiFactory.Ink);
            title.text = "FIELD GLOSSARY";
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.06f, .90f),
                new Vector2(.78f, .98f));
            Button close = EpisodeUiFactory.Button(
                drawer.transform,
                "CloseGlossary",
                "CLOSE",
                EpisodeButtonStyle.Secondary,
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
            glossaryText.color = EpisodeUiFactory.Ink;
            glossaryText.GetComponentInParent<ScrollRect>()
                .GetComponent<Image>().color =
                new Color(
                    EpisodeUiFactory.OffWhite.r,
                    EpisodeUiFactory.OffWhite.g,
                    EpisodeUiFactory.OffWhite.b,
                    .12f);
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
                new Vector2(.67f, .92f),
                new Vector2(.79f, .972f));
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
            AtlasSurfaceGraphic drawer =
                EpisodeUiFactory.FieldPaper(
                judge.transform,
                "JudgeViewDrawer",
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
                EpisodeUiFactory.Ink);
            title.text = "JUDGE VIEW";
            title.color = EpisodeUiFactory.Ink;
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.05f, .91f),
                new Vector2(.75f, .98f));
            Button close = EpisodeUiFactory.Button(
                drawer.transform,
                "CloseJudgeView",
                "CLOSE",
                EpisodeButtonStyle.Secondary,
                13);
            EpisodeUiFactory.Stretch(
                close.GetComponent<RectTransform>(),
                new Vector2(.82f, .915f),
                new Vector2(.95f, .98f));
            close.onClick.AddListener(() => toggleJudge?.Invoke());
            judgeTechnicalButton = EpisodeUiFactory.Button(
                drawer.transform,
                "JudgeTechnicalDisclosure",
                "TECHNICAL JSON",
                EpisodeButtonStyle.Secondary,
                12);
            EpisodeUiFactory.Stretch(
                judgeTechnicalButton.GetComponent<RectTransform>(),
                new Vector2(.62f, .915f),
                new Vector2(.80f, .98f));
            judgeTechnicalButton.onClick.AddListener(
                () => toggleJudgeTechnical?.Invoke());
            judgeText = RuntimeScrollableContent.Create(
                drawer.transform,
                "JudgeViewContent",
                new Vector2(.05f, .06f),
                new Vector2(.95f, .88f),
                14);
            judgeText.color = EpisodeUiFactory.Ink;
            judgeText.GetComponentInParent<ScrollRect>()
                .GetComponent<Image>().color =
                new Color(
                    EpisodeUiFactory.OffWhite.r,
                    EpisodeUiFactory.OffWhite.g,
                    EpisodeUiFactory.OffWhite.b,
                    .12f);
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
                new Vector2(.80f, .92f),
                new Vector2(.94f, .972f));
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
            AtlasSurfaceGraphic card =
                EpisodeUiFactory.FieldPaper(
                certificate.transform,
                "CertificateCard",
                true);
            EpisodeUiFactory.Stretch(
                card.rectTransform,
                new Vector2(.13f, .065f),
                new Vector2(.87f, .935f));
            Text title = EpisodeUiFactory.Text(
                card.transform,
                "CertificateHeading",
                25,
                TextAnchor.MiddleCenter,
                EpisodeUiFactory.Ink);
            title.text = SaltLineNarrative.CertificateHeading;
            EpisodeUiFactory.Stretch(
                title.rectTransform,
                new Vector2(.06f, .89f),
                new Vector2(.94f, .97f));
            AtlasSurfaceGraphic seal =
                EpisodeUiFactory.Stamp(
                    card.transform,
                    "ExpeditionSeal",
                    "AGRIVERSE\nEXPEDITION 01",
                    EpisodeUiFactory.Amber);
            EpisodeUiFactory.Stretch(
                seal.rectTransform,
                new Vector2(.76f, .79f),
                new Vector2(.92f, .88f));
            AtlasRouteGraphic certificateRoute =
                EpisodeUiFactory.Route(
                    card.transform,
                    "CertificateRoute",
                    new[]
                    {
                        new Vector2(.02f, .34f),
                        new Vector2(.26f, .62f),
                        new Vector2(.51f, .48f),
                        new Vector2(.75f, .70f),
                        new Vector2(.98f, .56f)
                    },
                    EpisodeUiFactory.Amber,
                    1.4f);
            EpisodeUiFactory.Stretch(
                certificateRoute.rectTransform,
                new Vector2(.08f, .75f),
                new Vector2(.92f, .84f));
            certificateText = RuntimeScrollableContent.Create(
                card.transform,
                "CertificateContent",
                new Vector2(.08f, .20f),
                new Vector2(.92f, .74f),
                17);
            certificateText.color = EpisodeUiFactory.Ink;
            certificateText.GetComponentInParent<ScrollRect>()
                .GetComponent<Image>().color =
                new Color(
                    EpisodeUiFactory.OffWhite.r,
                    EpisodeUiFactory.OffWhite.g,
                    EpisodeUiFactory.OffWhite.b,
                    .12f);

            Button home = EpisodeUiFactory.Button(
                card.transform,
                "ReturnHome",
                "RETURN TO FIELD NETWORK",
                EpisodeButtonStyle.Secondary,
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
                EpisodeButtonStyle.Primary,
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
