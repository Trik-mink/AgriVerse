using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    /// <summary>
    /// Responsive, presentation-only interview shell. It owns no scenario state and delegates
    /// all selection, questions, retries, and evidence content back to InterviewController.
    /// </summary>
    public sealed class CinematicInterviewPresentation : MonoBehaviour
    {
        private static readonly Color RiverTeal = new Color(.025f, .12f, .13f, .88f);
        private static readonly Color DeepTeal = new Color(.018f, .075f, .08f, .94f);
        private static readonly Color Amber = new Color(.92f, .61f, .23f, 1f);
        private static readonly Color OffWhite = new Color(.96f, .93f, .84f, 1f);
        private readonly Dictionary<string, RawImage> cardPortraits = new Dictionary<string, RawImage>();

        private ScenarioDto scenario;
        private Action<string> select;
        private Action ask;
        private Action retry;
        private Action toggleEvidence;
        private Action returnToField;
        private GameObject selectionArea;
        private GameObject identityArea;
        private GameObject dialogueArea;
        private GameObject evidenceDrawer;
        private GameObject evidenceScrim;
        private Text heading;
        private Text mission;
        private Text evidenceChip;
        private Text interviewChip;
        private Text identity;
        private Text portraitFallback;
        private Text status;
        private Text conversation;
        private Text evidence;
        private Button evidenceButton;
        private Button askButton;
        private Button retryButton;
        private Button drawerContinueButton;
        private Button returnToFieldButton;
        private Text askLabel;
        private RawImage selectedPortrait;
        private bool selectedShowing;

        public InputField QuestionInput { get; private set; }
        public RawImage SelectedPortrait => selectedPortrait;
        public Text PortraitFallback => portraitFallback;
        public bool SelectionVisible => selectionArea != null && selectionArea.activeSelf;
        public bool EvidenceVisible => evidenceDrawer != null && evidenceDrawer.activeSelf;
        public bool RetryVisible => retryButton != null && retryButton.gameObject.activeSelf;

        public void Build(Transform parent, ScenarioDto source, Action<string> onSelect, Action onAsk, Action onRetry, Action onToggleEvidence, Action onReturnToField = null)
        {
            scenario = source;
            select = onSelect;
            ask = onAsk;
            retry = onRetry;
            toggleEvidence = onToggleEvidence;
            returnToField = onReturnToField;
            GameObject canvasObject = new GameObject("CinematicInterviewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            BuildHud(canvas.transform);
            BuildSelection(canvas.transform);
            BuildDialogue(canvas.transform);
            BuildEvidenceDrawer(canvas.transform);
        }

        public RawImage PortraitSlotFor(string stakeholderId) => cardPortraits.TryGetValue(stakeholderId, out RawImage slot) ? slot : null;

        public void ToggleEvidenceDrawer()
        {
            bool opening = !evidenceDrawer.activeSelf;
            evidenceDrawer.SetActive(opening);
            evidenceScrim.SetActive(opening);
            evidenceButton.gameObject.SetActive(!opening);
            if (opening)
            {
                identityArea.SetActive(false);
                dialogueArea.SetActive(false);
                toggleEvidence?.Invoke();
            }
            else
            {
                identityArea.SetActive(selectedShowing);
                dialogueArea.SetActive(selectedShowing);
            }
        }

        public void Refresh(StakeholderDto selected, string conversationText, string evidenceText, string statusText,
            int evidenceCount, int evidenceTotal, int interviewsComplete, int interviewTotal, bool busy, bool canRetry, bool planUnlocked)
        {
            bool hasSelection = selected != null;
            selectedShowing = hasSelection;
            selectionArea.SetActive(!hasSelection);
            bool drawerOpen = evidenceDrawer.activeSelf;
            identityArea.SetActive(hasSelection && !drawerOpen);
            dialogueArea.SetActive(hasSelection && !drawerOpen);
            evidenceButton.gameObject.SetActive(!drawerOpen);
            heading.text = scenario?.title ?? "Field investigation";
            mission.text = hasSelection ? "Interview " + selected.name : "Choose a stakeholder perspective";
            evidenceChip.text = "EVIDENCE  " + evidenceCount + "/" + evidenceTotal;
            interviewChip.text = "INTERVIEWS  " + interviewsComplete + "/" + interviewTotal;
            RuntimeScrollableContent.SetText(evidence, evidenceText);
            if (!hasSelection) return;

            identity.text = selected.name + "\n" + selected.role;
            status.text = StatusForPresentation(statusText, busy, planUnlocked);
            RuntimeScrollableContent.SetText(conversation, conversationText);
            askLabel.text = planUnlocked ? "CONTINUE" : busy ? "LISTENING…" : "ASK";
            askButton.interactable = !busy && (planUnlocked || hasSelection);
            QuestionInput.interactable = !busy && !planUnlocked;
            retryButton.gameObject.SetActive(canRetry);
            retryButton.interactable = !busy && canRetry;
            drawerContinueButton.gameObject.SetActive(planUnlocked);
            returnToFieldButton.gameObject.SetActive(
                returnToField != null && !busy && !planUnlocked);
        }

        private static string StatusForPresentation(string value, bool busy, bool planUnlocked)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (busy || planUnlocked || value.StartsWith("Could not", StringComparison.Ordinal) ||
                value.StartsWith("Reply could", StringComparison.Ordinal) || value.StartsWith("The stakeholder returned", StringComparison.Ordinal) ||
                value.StartsWith("Enter a question", StringComparison.Ordinal) || value.StartsWith("Questions must", StringComparison.Ordinal)) return value;
            return string.Empty;
        }

        private void BuildHud(Transform root)
        {
            Image accent = Panel(root, "HudAccent", Amber, false);
            Stretch(accent.rectTransform, new Vector2(.025f, .90f), new Vector2(.0285f, .975f));
            heading = Text(root, "EpisodeHeading", 20, TextAnchor.UpperLeft, OffWhite);
            Stretch(heading.rectTransform, new Vector2(.045f, .935f), new Vector2(.48f, .975f));
            mission = Text(root, "MissionHeading", 14, TextAnchor.UpperLeft, OffWhite);
            mission.color = new Color(OffWhite.r, OffWhite.g, OffWhite.b, .88f);
            Stretch(mission.rectTransform, new Vector2(.045f, .895f), new Vector2(.52f, .935f));

            Button evidenceChipButton = Button(root, "EvidenceChip", "EVIDENCE", RiverTeal, 13);
            Stretch(evidenceChipButton.GetComponent<RectTransform>(), new Vector2(.69f, .915f), new Vector2(.83f, .968f));
            evidenceChip = evidenceChipButton.GetComponentInChildren<Text>();
            evidenceChipButton.onClick.AddListener(ToggleEvidenceDrawer);
            Button interviewChipButton = Button(root, "InterviewChip", "INTERVIEWS", RiverTeal, 13);
            Stretch(interviewChipButton.GetComponent<RectTransform>(), new Vector2(.84f, .915f), new Vector2(.975f, .968f));
            interviewChip = interviewChipButton.GetComponentInChildren<Text>();

            evidenceButton = Button(root, "EvidenceTab", "E\nV\nI\nD\nE\nN\nC\nE", DeepTeal, 12);
            Stretch(evidenceButton.GetComponent<RectTransform>(), new Vector2(.947f, .34f), new Vector2(.987f, .63f));
            evidenceButton.GetComponentInChildren<Text>().lineSpacing = .78f;
            evidenceButton.onClick.AddListener(ToggleEvidenceDrawer);
        }

        private void BuildSelection(Transform root)
        {
            selectionArea = new GameObject("StakeholderSelection", typeof(RectTransform));
            selectionArea.transform.SetParent(root, false);
            Stretch(selectionArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            Text prompt = Text(selectionArea.transform, "SelectionPrompt", 18, TextAnchor.MiddleCenter, OffWhite);
            prompt.text = "Whose experience should shape your next decision?";
            Stretch(prompt.rectTransform, new Vector2(.22f, .47f), new Vector2(.78f, .54f));
            int count = scenario.stakeholders.Length;
            for (int index = 0; index < count; index++)
            {
                StakeholderDto stakeholder = scenario.stakeholders[index];
                float width = .20f;
                float start = .5f - (count * width + (count - 1) * .018f) * .5f;
                Button card = Button(selectionArea.transform, "StakeholderCard_" + stakeholder.id, string.Empty, RiverTeal, 14);
                Stretch(card.GetComponent<RectTransform>(), new Vector2(start + index * (width + .018f), .28f), new Vector2(start + index * (width + .018f) + width, .45f));
                string id = stakeholder.id;
                card.onClick.AddListener(() => select?.Invoke(id));
                RawImage portrait = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
                portrait.transform.SetParent(card.transform, false);
                portrait.color = new Color(1f, 1f, 1f, 0f);
                portrait.raycastTarget = false;
                Stretch(portrait.rectTransform, new Vector2(.06f, .15f), new Vector2(.37f, .85f));
                cardPortraits[stakeholder.id] = portrait;
                Text cardText = Text(card.transform, "Identity", 13, TextAnchor.MiddleLeft, OffWhite);
                cardText.text = stakeholder.name + "\n" + stakeholder.role;
                Stretch(cardText.rectTransform, new Vector2(.43f, .17f), new Vector2(.93f, .83f));
            }
        }

        private void BuildDialogue(Transform root)
        {
            identityArea = Panel(root, "StakeholderIdentity", RiverTeal, false).gameObject;
            Stretch(identityArea.GetComponent<RectTransform>(), new Vector2(.03f, .055f), new Vector2(.275f, .265f));
            selectedPortrait = new GameObject("SelectedPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>();
            selectedPortrait.transform.SetParent(identityArea.transform, false);
            selectedPortrait.raycastTarget = false;
            Stretch(selectedPortrait.rectTransform, new Vector2(.045f, .11f), new Vector2(.40f, .89f));
            portraitFallback = Text(identityArea.transform, "PortraitFallback", 14, TextAnchor.MiddleCenter, OffWhite);
            Stretch(portraitFallback.rectTransform, new Vector2(.045f, .11f), new Vector2(.40f, .89f));
            identity = Text(identityArea.transform, "StakeholderIdentityText", 15, TextAnchor.MiddleLeft, OffWhite);
            Stretch(identity.rectTransform, new Vector2(.46f, .15f), new Vector2(.94f, .86f));

            dialogueArea = Panel(root, "InterviewDialogue", DeepTeal, false).gameObject;
            Stretch(dialogueArea.GetComponent<RectTransform>(), new Vector2(.29f, .055f), new Vector2(.81f, .43f));
            conversation = RuntimeScrollableContent.Create(dialogueArea.transform, "InterviewConversation", new Vector2(.045f, .42f), new Vector2(.955f, .94f), 15);
            status = Text(dialogueArea.transform, "InterviewStatus", 13, TextAnchor.MiddleLeft, new Color(OffWhite.r, OffWhite.g, OffWhite.b, .9f));
            Stretch(status.rectTransform, new Vector2(.055f, .31f), new Vector2(.945f, .39f));
            QuestionInput = Input(dialogueArea.transform);
            Stretch(QuestionInput.GetComponent<RectTransform>(), new Vector2(.05f, .065f), new Vector2(.72f, .265f));
            askButton = Button(dialogueArea.transform, "AskAction", "ASK", Amber, 15);
            askLabel = askButton.GetComponentInChildren<Text>();
            Stretch(askButton.GetComponent<RectTransform>(), new Vector2(.75f, .065f), new Vector2(.95f, .265f));
            askButton.onClick.AddListener(() => ask?.Invoke());
            retryButton = Button(dialogueArea.transform, "RetryAction", "RETRY", RiverTeal, 11);
            Stretch(retryButton.GetComponent<RectTransform>(), new Vector2(.75f, .285f), new Vector2(.95f, .385f));
            retryButton.onClick.AddListener(() => retry?.Invoke());

            returnToFieldButton = Button(
                root,
                "ReturnToField",
                "RETURN TO FIELD",
                RiverTeal,
                12);
            Stretch(
                returnToFieldButton.GetComponent<RectTransform>(),
                new Vector2(.82f, .055f),
                new Vector2(.965f, .115f));
            returnToFieldButton.onClick.AddListener(
                () => returnToField?.Invoke());
            returnToFieldButton.gameObject.SetActive(false);
        }

        private void BuildEvidenceDrawer(Transform root)
        {
            evidenceScrim = Panel(root, "EvidenceFocusScrim", new Color(0f, .025f, .03f, .56f), false).gameObject;
            Stretch(evidenceScrim.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            evidenceDrawer = Panel(root, "EvidenceDrawer", DeepTeal, false).gameObject;
            Stretch(evidenceDrawer.GetComponent<RectTransform>(), new Vector2(.27f, .12f), new Vector2(.93f, .87f));
            Text title = Text(evidenceDrawer.transform, "EvidenceTitle", 19, TextAnchor.MiddleLeft, OffWhite);
            title.text = "EVIDENCE NOTEBOOK";
            Stretch(title.rectTransform, new Vector2(.07f, .91f), new Vector2(.79f, .98f));
            Button close = Button(evidenceDrawer.transform, "CloseEvidence", "×", RiverTeal, 19);
            Stretch(close.GetComponent<RectTransform>(), new Vector2(.83f, .91f), new Vector2(.94f, .98f));
            close.onClick.AddListener(ToggleEvidenceDrawer);
            evidence = RuntimeScrollableContent.Create(evidenceDrawer.transform, "CinematicEvidence", new Vector2(.06f, .18f), new Vector2(.94f, .87f), 16);
            drawerContinueButton = Button(evidenceDrawer.transform, "DrawerContinue", "CONTINUE", Amber, 15);
            Stretch(drawerContinueButton.GetComponent<RectTransform>(), new Vector2(.69f, .055f), new Vector2(.94f, .145f));
            drawerContinueButton.onClick.AddListener(() => ask?.Invoke());
            drawerContinueButton.gameObject.SetActive(false);
            evidenceScrim.SetActive(false);
            evidenceDrawer.SetActive(false);
        }

        public void SetSelectedPortrait(Texture texture, string fallback)
        {
            selectedPortrait.texture = texture;
            selectedPortrait.color = texture == null ? new Color(1f, 1f, 1f, 0f) : Color.white;
            portraitFallback.text = fallback;
            portraitFallback.gameObject.SetActive(texture == null);
        }

        public void SetCardPortrait(string stakeholderId, Texture texture)
        {
            RawImage image = PortraitSlotFor(stakeholderId);
            if (image == null || texture == null) return;
            image.texture = texture;
            image.color = Color.white;
        }

        private static Image Panel(Transform parent, string name, Color color, bool raycastable)
        {
            Image image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = raycastable;
            return image;
        }

        private static Text Text(Transform parent, string name, int size, TextAnchor anchor, Color color)
        {
            Text text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static InputField Input(Transform parent)
        {
            InputField input = new GameObject("QuestionInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)).GetComponent<InputField>();
            input.transform.SetParent(parent, false);
            input.GetComponent<Image>().color = new Color(.08f, .17f, .17f, .96f);
            Text value = Text(input.transform, "Text", 14, TextAnchor.MiddleLeft, OffWhite);
            Stretch(value.rectTransform, new Vector2(.05f, .08f), new Vector2(.94f, .92f));
            input.textComponent = value;
            Text placeholder = Text(input.transform, "Placeholder", 14, TextAnchor.MiddleLeft, new Color(.8f, .8f, .75f, .55f));
            placeholder.text = "Ask a focused question…";
            Stretch(placeholder.rectTransform, new Vector2(.05f, .08f), new Vector2(.94f, .92f));
            input.placeholder = placeholder;
            input.lineType = InputField.LineType.MultiLineNewline;
            return input;
        }

        private static Button Button(Transform parent, string name, string label, Color color, int size)
        {
            Button button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(parent, false);
            Image image = button.GetComponent<Image>();
            image.color = color;
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, .9f);
            colors.pressedColor = new Color(.78f, .78f, .78f, 1f);
            colors.disabledColor = new Color(.5f, .5f, .5f, .55f);
            button.colors = colors;
            Text text = Text(button.transform, "Label", size, TextAnchor.MiddleCenter, OffWhite);
            text.text = label;
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
}
