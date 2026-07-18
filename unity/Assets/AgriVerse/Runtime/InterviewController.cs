using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    [Serializable]
    internal sealed class StakeholderMessageRequestDto
    {
        public string message;
        public ConversationTurnDto[] conversation;
    }

    [Serializable]
    internal sealed class StakeholderMessageResponseDto
    {
        public string message;
    }

    /// <summary>
    /// Primitive-only stakeholder interviews. GPT replies remain server-side; Unity
    /// sends only the selected public stakeholder ID, the student's question, and
    /// up to six prior public conversation turns.
    /// </summary>
    public sealed class InterviewController : MonoBehaviour
    {
        [SerializeField] private string editorApiBaseUrl = "http://localhost:8787";
        [SerializeField] private string webApiBaseUrl = "http://localhost:8787";

        private ScenarioDto scenario;
        private InterviewNotebookSession notebookSession;
        private StakeholderDto selected;
        private readonly List<TestSiteMarker> markers = new List<TestSiteMarker>();
        private Text selectedText;
        private Text conversationText;
        private Text gateText;
        private Text portraitBadge;
        private RawImage portrait;
        private InputField questionInput;
        private Button askButton;
        private Text askButtonLabel;
        private Button retryButton;
        private bool busy;
        private bool planningHandoffCompleted;
        private string lastFailedQuestion;
        private string lastFailedStakeholderId;
        private GameObject interviewStage;

        public InvestigationLoadState LoadState { get; private set; } = InvestigationLoadState.NotStarted;
        public ScenarioDto Scenario => scenario;
        public int MarkerCount => markers.Count;
        public bool IsBusy => busy;
        public bool InterviewsVisible => RuntimePanelManager.GetOrCreate().IsShowing(RuntimeActivityStage.Interviews);
        public bool PlanUnlocked => scenario != null && notebookSession?.Notebook != null &&
                                  notebookSession.Notebook.AreAllStakeholdersInterviewed(scenario.stakeholders);

        private void Start()
        {
            StartCoroutine(LoadScenario());
        }

        private void Update()
        {
            if (LoadState == InvestigationLoadState.Ready && !InterviewsVisible && !planningHandoffCompleted)
            {
                if (InvestigationComplete()) ActivateInterviewStage();
                return;
            }
            if (LoadState != InvestigationLoadState.Ready || Mouse.current == null ||
                !Mouse.current.leftButton.wasPressedThisFrame ||
                (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
            Camera sceneCamera = Camera.main;
            if (sceneCamera == null) return;
            if (Physics.Raycast(sceneCamera.ScreenPointToRay(Mouse.current.position.ReadValue()), out RaycastHit hit))
            {
                TestSiteMarker marker = hit.collider.GetComponent<TestSiteMarker>();
                if (marker != null) SelectStakeholder(marker.SiteId);
            }
        }

        public IEnumerator LoadScenario()
        {
            LoadState = InvestigationLoadState.Loading;
            string url;
            try { url = ScenarioEndpoint.ForPlatform(IsWebBuild, editorApiBaseUrl, webApiBaseUrl); }
            catch (ArgumentException error) { Fail("Scenario URL is not configured.", error.Message); yield break; }
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { Fail("Scenario unavailable.", request.error); yield break; }
                try { scenario = ScenarioDto.FromJson(request.downloadHandler.text); }
                catch (FormatException error) { Fail("Scenario response was invalid.", error.Message); yield break; }
            }
            if (scenario.stakeholders == null || scenario.stakeholders.Length == 0) { Fail("Scenario has no stakeholders.", "No public stakeholders were returned."); yield break; }
            notebookSession = InterviewNotebookSession.GetOrCreate();
            notebookSession.ConfigureScenario(scenario.id);
            interviewStage = new GameObject("InterviewStage");
            interviewStage.transform.SetParent(transform, false);
            CreateMarkers();
            CreateInterface();
            RuntimePanelManager.GetOrCreate().Register(RuntimeActivityStage.Interviews, interviewStage);
            LoadState = InvestigationLoadState.Ready;
        }

        public void SelectStakeholder(string stakeholderId)
        {
            if (scenario == null) return;
            for (int index = 0; index < scenario.stakeholders.Length; index++)
            {
                if (scenario.stakeholders[index].id == stakeholderId)
                {
                    selected = scenario.stakeholders[index];
                    StartCoroutine(LoadPortrait(selected));
                    SetStatus($"{selected.name} selected. Ask one focused question.");
                    Refresh();
                    return;
                }
            }
        }

        public void AskSelectedStakeholder()
        {
            if (PlanUnlocked) { ContinueToPlanning(); return; }
            if (selected == null || busy || questionInput == null) return;
            string question = questionInput.text.Trim();
            if (question.Length == 0) { SetStatus("Enter a question before asking."); return; }
            if (question.Length > 1400) { SetStatus("Questions must be 1,400 characters or fewer."); return; }
            StartCoroutine(SendQuestion(selected, question));
        }

        public void RetryLastQuestion()
        {
            if (selected != null && !busy && selected.id == lastFailedStakeholderId && !string.IsNullOrWhiteSpace(lastFailedQuestion))
            {
                StartCoroutine(SendQuestion(selected, lastFailedQuestion));
            }
        }

        public void ContinueToPlanning()
        {
            if (!PlanUnlocked) return;
            PlanController plan = FindFirstObjectByType<PlanController>();
            if (plan == null || !plan.BeginPlanning())
            {
                SetStatus("Plan Controller is unavailable. Add its bootstrap to this scene.");
                return;
            }
            ShowPlanActivity();
        }

        public void AskForTesting(string stakeholderId, string question)
        {
            SelectStakeholder(stakeholderId);
            if (selected != null && !busy && !string.IsNullOrWhiteSpace(question))
            {
                StartCoroutine(SendQuestion(selected, question));
            }
        }

        public void ShowPlanActivity()
        {
            planningHandoffCompleted = true;
        }

        public void ConfigureEndpointsForTesting(string editorBaseUrl, string webBaseUrl)
        {
            editorApiBaseUrl = editorBaseUrl;
            webApiBaseUrl = webBaseUrl;
        }

        private IEnumerator SendQuestion(StakeholderDto stakeholder, string question)
        {
            busy = true;
            lastFailedQuestion = question;
            lastFailedStakeholderId = stakeholder.id;
            SetStatus($"Listening to {stakeholder.name}…");
            Refresh();
            string baseUrl = (IsWebBuild ? webApiBaseUrl : editorApiBaseUrl).TrimEnd('/');
            string url = $"{baseUrl}/api/stakeholders/{UnityWebRequest.EscapeURL(stakeholder.id)}/messages";
            ConversationTurnDto[] history = RecentConversation(stakeholder.id);
            string json = JsonUtility.ToJson(new StakeholderMessageRequestDto { message = question, conversation = history });
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                busy = false;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetStatus($"Could not reach {stakeholder.name}: {ReadableError(request)}. Retry is available.");
                    Refresh();
                    yield break;
                }
                StakeholderMessageResponseDto response;
                try { response = JsonUtility.FromJson<StakeholderMessageResponseDto>(request.downloadHandler.text); }
                catch (ArgumentException error) { SetStatus($"Reply could not be read: {error.Message}"); Refresh(); yield break; }
                if (response == null || string.IsNullOrWhiteSpace(response.message)) { SetStatus("The stakeholder returned an empty reply. Retry is available."); Refresh(); yield break; }
                notebookSession.Notebook.AddQuestion(stakeholder.id, question);
                notebookSession.Notebook.AddReply(stakeholder.id, response.message);
                questionInput.text = string.Empty;
                lastFailedQuestion = string.Empty;
                lastFailedStakeholderId = string.Empty;
                SetStatus(PlanUnlocked ? "All stakeholder responses are recorded. Continue to planning when you finish reading." : $"Recorded {stakeholder.name}'s response.");
                Refresh();
            }
        }

        private ConversationTurnDto[] RecentConversation(string stakeholderId)
        {
            IReadOnlyList<ConversationTurnDto> allTurns = notebookSession.Notebook.ConversationFor(stakeholderId);
            int count = Math.Min(allTurns.Count, 6);
            var recent = new ConversationTurnDto[count];
            for (int index = 0; index < count; index++) recent[index] = allTurns[allTurns.Count - count + index];
            return recent;
        }

        private void CreateMarkers()
        {
            var root = new GameObject("StakeholderMarkers"); root.transform.SetParent(interviewStage.transform, false);
            for (int index = 0; index < scenario.stakeholders.Length; index++)
            {
                StakeholderDto stakeholder = scenario.stakeholders[index];
                GameObject markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                markerObject.name = $"StakeholderMarker_{stakeholder.id}";
                markerObject.transform.SetParent(root.transform, false);
                markerObject.transform.localPosition = new Vector3((index - ((scenario.stakeholders.Length - 1) * .5f)) * 3f, .5f, 4f);
                markerObject.GetComponent<MeshRenderer>().material.color = new Color(.5f, .5f, .5f);
                TestSiteMarker marker = markerObject.AddComponent<TestSiteMarker>(); marker.Configure(stakeholder.id); markers.Add(marker);
                var label = new GameObject("StakeholderLabel", typeof(TextMesh)).GetComponent<TextMesh>();
                label.transform.SetParent(markerObject.transform, false); label.transform.localPosition = Vector3.up;
                label.text = stakeholder.name; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 38; label.characterSize = .08f; label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center; label.color = Color.white;
            }
        }

        private void CreateInterface()
        {
            var canvasObject = new GameObject("InterviewCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(interviewStage.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            EnsureInputSystemEventSystem();
            portrait = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)).GetComponent<RawImage>(); portrait.transform.SetParent(canvas.transform, false); portrait.raycastTarget = false; Stretch(portrait.rectTransform, new Vector2(.03f, .68f), new Vector2(.15f, .82f));
            portraitBadge = Text(canvas.transform, "PortraitFallback", 16); Stretch(portraitBadge.rectTransform, new Vector2(.03f, .68f), new Vector2(.15f, .82f)); portraitBadge.alignment = TextAnchor.MiddleCenter;
            portrait.gameObject.SetActive(false); portraitBadge.gameObject.SetActive(false);
            selectedText = Text(canvas.transform, "Stakeholder", 18); Stretch(selectedText.rectTransform, new Vector2(.17f, .68f), new Vector2(.62f, .82f));
            conversationText = RuntimeScrollableContent.Create(canvas.transform, "Conversation", new Vector2(.03f, .27f), new Vector2(.62f, .65f), 15);
            questionInput = Input(canvas.transform); Stretch(questionInput.GetComponent<RectTransform>(), new Vector2(.03f, .12f), new Vector2(.62f, .23f));
            askButton = Button(canvas.transform, "AskButton", "Ask stakeholder"); askButtonLabel = askButton.GetComponentInChildren<Text>(); Stretch(askButton.GetComponent<RectTransform>(), new Vector2(.03f, .04f), new Vector2(.3f, .1f)); askButton.onClick.AddListener(AskSelectedStakeholder);
            retryButton = Button(canvas.transform, "RetryButton", "Retry"); Stretch(retryButton.GetComponent<RectTransform>(), new Vector2(.33f, .04f), new Vector2(.62f, .1f)); retryButton.onClick.AddListener(RetryLastQuestion);
            gateText = Text(canvas.transform, "PlanGate", 16); Stretch(gateText.rectTransform, new Vector2(.66f, .84f), new Vector2(.97f, .9f));
        }

        private IEnumerator LoadPortrait(StakeholderDto stakeholder)
        {
            portrait.texture = null; portrait.gameObject.SetActive(false); portraitBadge.gameObject.SetActive(true); portraitBadge.text = stakeholder.name + "\n" + stakeholder.role;
            string baseUrl = (IsWebBuild ? webApiBaseUrl : editorApiBaseUrl).TrimEnd('/');
            string assetName = stakeholder.name.ToLowerInvariant().Replace(".", string.Empty).Replace(" ", "-");
            string[] urls = { $"{baseUrl}/assets/characters/optimized/{assetName}.webp", $"{baseUrl}/assets/characters/optimized/{assetName}.jpg" };
            for (int index = 0; index < urls.Length; index++)
            {
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(urls[index]))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        portrait.texture = DownloadHandlerTexture.GetContent(request); portrait.gameObject.SetActive(true); portraitBadge.gameObject.SetActive(false); yield break;
                    }
                }
            }
            SetStatus($"Portrait unavailable for {stakeholder.name}; showing the name badge instead.");
        }

        private void Refresh()
        {
            if (selectedText == null) return;
            selectedText.text = selected == null ? "Select a gray stakeholder marker." : $"{selected.name}\n{selected.role}\n{selected.persona}";
            RuntimeScrollableContent.SetText(conversationText, FormatConversation());
            askButtonLabel.text = PlanUnlocked ? "Continue to planning" : "Ask stakeholder";
            askButton.interactable = !busy && (PlanUnlocked || selected != null);
            questionInput.interactable = !busy && !PlanUnlocked;
            retryButton.interactable = selected != null && !busy && selected.id == lastFailedStakeholderId && !string.IsNullOrWhiteSpace(lastFailedQuestion);
            gateText.text = PlanUnlocked ? string.Empty : $"Plan locked — {RespondedCount()}/{scenario.stakeholders.Length} stakeholders have replied.";
        }

        private string FormatConversation()
        {
            if (selected == null) return "Conversation history will appear here.";
            IReadOnlyList<ConversationTurnDto> turns = notebookSession.Notebook.ConversationFor(selected.id);
            if (turns.Count == 0) return "No response recorded yet. Ask a focused question.";
            var text = new StringBuilder("Conversation\n\n");
            for (int index = 0; index < turns.Count; index++) text.Append(turns[index].role == "student" ? "You: " : selected.name + ": ").Append(turns[index].text).Append("\n\n");
            return text.ToString();
        }

        private int RespondedCount() { int count = 0; for (int i = 0; i < scenario.stakeholders.Length; i++) if (notebookSession.Notebook.HasResponse(scenario.stakeholders[i].id)) count++; return count; }
        private bool InvestigationComplete()
        {
            EvidenceNotebookSession investigationSession = EvidenceNotebookSession.GetOrCreate();
            return investigationSession.Notebook != null && investigationSession.Notebook.AreAllSitesRecorded(scenario.test_sites);
        }
        private void ActivateInterviewStage()
        {
            RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Interviews);
            SetStatus("Investigation complete. Select a gray stakeholder marker to begin an interview.");
            Refresh();
        }
        private static string ReadableError(UnityWebRequest request) => request.responseCode > 0 ? $"server returned {request.responseCode}" : request.error;
        private void SetStatus(string message) { RuntimePanelManager.GetOrCreate().SetInstruction(message); }
        private void Fail(string userMessage, string diagnostic) { LoadState = InvestigationLoadState.Failed; SetStatus(userMessage); Debug.LogError(diagnostic, this); }
        private static bool IsWebBuild
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }
        private static void EnsureInputSystemEventSystem() { EventSystem eventSystem = FindFirstObjectByType<EventSystem>(); if (eventSystem == null) eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>(); foreach (BaseInputModule module in eventSystem.GetComponents<BaseInputModule>()) if (!(module is UnityEngine.InputSystem.UI.InputSystemUIInputModule)) module.enabled = false; if (eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null) eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(); }
        private static Text Text(Transform parent, string name, int size) { Text text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>(); text.transform.SetParent(parent, false); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.color = Color.white; text.raycastTarget = false; text.alignment = TextAnchor.UpperLeft; text.verticalOverflow = VerticalWrapMode.Overflow; return text; }
        private static InputField Input(Transform parent) { InputField input = new GameObject("QuestionInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField)).GetComponent<InputField>(); input.transform.SetParent(parent, false); input.GetComponent<Image>().color = new Color(.2f,.2f,.2f,.95f); Text text = Text(input.transform, "Text", 16); Stretch(text.rectTransform, new Vector2(.04f,.05f), new Vector2(.96f,.95f)); input.textComponent = text; Text placeholder = Text(input.transform, "Placeholder", 16); placeholder.text = "Type a focused question…"; placeholder.color = new Color(.7f,.7f,.7f,.8f); Stretch(placeholder.rectTransform, new Vector2(.04f,.05f), new Vector2(.96f,.95f)); input.placeholder = placeholder; input.lineType = InputField.LineType.MultiLineNewline; return input; }
        private static Button Button(Transform parent, string name, string label) { Button button = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)).GetComponent<Button>(); button.transform.SetParent(parent, false); button.GetComponent<Image>().color = new Color(.4f,.4f,.4f,1); Text text = Text(button.transform, "Label", 17); text.text = label; text.alignment = TextAnchor.MiddleCenter; Stretch(text.rectTransform, Vector2.zero, Vector2.one); button.targetGraphic = button.GetComponent<Image>(); return button; }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max) { rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
    }
}
