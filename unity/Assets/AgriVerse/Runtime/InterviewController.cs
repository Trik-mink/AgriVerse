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
        [SerializeField] private bool createRuntimeMarkers = true;
        [SerializeField] private bool autoActivate = true;

        private ScenarioDto scenario;
        private InterviewNotebookSession notebookSession;
        private StakeholderDto selected;
        private readonly List<TestSiteMarker> markers = new List<TestSiteMarker>();
        private Text selectedText;
        private Text conversationText;
        private Text gateText;
        private Text portraitBadge;
        private RawImage portrait;

        public string SelectedStakeholderId =>
            selected == null ? string.Empty : selected.id;
        private InputField questionInput;
        private Button askButton;
        private Text askButtonLabel;
        private Button retryButton;
        private bool busy;
        private bool planningHandoffCompleted;
        private string lastFailedQuestion;
        private string lastFailedStakeholderId;
        private GameObject interviewStage;
        private CinematicInterviewPresentation presentation;
        private string statusMessage;

        public InvestigationLoadState LoadState { get; private set; } = InvestigationLoadState.NotStarted;
        public ScenarioDto Scenario => scenario;
        public int MarkerCount => markers.Count;
        public bool IsBusy => busy;
        public bool CreatesRuntimeMarkers => createRuntimeMarkers;
        public bool AutoActivates => autoActivate;
        public bool InterviewsVisible => RuntimePanelManager.GetOrCreate().IsShowing(RuntimeActivityStage.Interviews);
        public bool PlanUnlocked => scenario != null && notebookSession?.Notebook != null &&
                                  notebookSession.Notebook.AreAllStakeholdersInterviewed(scenario.stakeholders);

        private void Start()
        {
            StartCoroutine(LoadScenario());
        }

        private void Update()
        {
            if (autoActivate &&
                LoadState == InvestigationLoadState.Ready &&
                !InterviewsVisible &&
                !planningHandoffCompleted)
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
            if (createRuntimeMarkers)
            {
                CreateMarkers();
            }
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
                    bool changedSelection = selected == null || selected.id != scenario.stakeholders[index].id;
                    selected = scenario.stakeholders[index];
                    if (changedSelection) StartCoroutine(LoadPortrait(selected));
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
            Episode3DAlphaController alpha =
                FindFirstObjectByType<Episode3DAlphaController>();
            if (alpha != null && alpha.BeginPlanningHandoff())
            {
                ShowPlanActivity();
                return;
            }
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

        public void ConfigurePresentation(
            bool createMarkers,
            bool activateAutomatically)
        {
            createRuntimeMarkers = createMarkers;
            autoActivate = activateAutomatically;
        }

        public bool BeginInterviews()
        {
            if (LoadState != InvestigationLoadState.Ready ||
                planningHandoffCompleted ||
                !InvestigationComplete())
            {
                return false;
            }
            ActivateInterviewStage();
            return true;
        }

        public void ReturnToField()
        {
            if (busy || autoActivate)
            {
                return;
            }
            AnGiangRealitySpikeController spike =
                FindAnyObjectByType<AnGiangRealitySpikeController>();
            if (spike != null)
            {
                spike.SetCinematicInterviewActive(false);
            }
            RuntimePanelManager.GetOrCreate().Clear();
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
                SetStatus(
                    "Added to Field Journal · " +
                    stakeholder.name +
                    "'s perspective." +
                    (PlanUnlocked
                        ? " All interviews are complete."
                        : string.Empty));
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
                RuntimePrimitiveMaterial.Apply(markerObject.GetComponent<MeshRenderer>(), new Color(.5f, .5f, .5f));
                // Cards are the visible selection affordance. The logical marker remains intact
                // for IDs, colliders, regression coverage, and non-cinematic fallback behavior.
                markerObject.GetComponent<MeshRenderer>().enabled = false;
                TestSiteMarker marker = markerObject.AddComponent<TestSiteMarker>(); marker.Configure(stakeholder.id); markers.Add(marker);
                var label = new GameObject("StakeholderLabel", typeof(TextMesh)).GetComponent<TextMesh>();
                label.transform.SetParent(markerObject.transform, false); label.transform.localPosition = Vector3.up;
                label.text = stakeholder.name; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 38; label.characterSize = .08f; label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center; label.color = Color.white;
                label.gameObject.SetActive(false);
            }
        }

        private void CreateInterface()
        {
            EnsureInputSystemEventSystem();
            presentation = new GameObject("CinematicInterviewPresentation").AddComponent<CinematicInterviewPresentation>();
            presentation.transform.SetParent(interviewStage.transform, false);
            presentation.Build(
                interviewStage.transform,
                scenario,
                SelectStakeholder,
                AskSelectedStakeholder,
                RetryLastQuestion,
                null,
                autoActivate ? null : ReturnToField);
            questionInput = presentation.QuestionInput;
            portrait = presentation.SelectedPortrait;
            portraitBadge = presentation.PortraitFallback;
            for (int index = 0; index < scenario.stakeholders.Length; index++) StartCoroutine(LoadCardPortrait(scenario.stakeholders[index]));
        }

        private IEnumerator LoadPortrait(StakeholderDto stakeholder)
        {
            presentation.SetSelectedPortrait(null, stakeholder.name);
            string[] urls = PortraitUrls(stakeholder);
            for (int index = 0; index < urls.Length; index++)
            {
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(urls[index]))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        if (selected != null && selected.id == stakeholder.id)
                            presentation.SetSelectedPortrait(DownloadHandlerTexture.GetContent(request), stakeholder.name);
                        yield break;
                    }
                }
            }
            SetStatus($"Portrait unavailable for {stakeholder.name}; showing the name badge instead.");
        }

        private IEnumerator LoadCardPortrait(StakeholderDto stakeholder)
        {
            string[] urls = PortraitUrls(stakeholder);
            for (int index = 0; index < urls.Length; index++)
            {
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(urls[index]))
                {
                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        presentation.SetCardPortrait(stakeholder.id, DownloadHandlerTexture.GetContent(request));
                        yield break;
                    }
                }
            }
        }

        private string[] PortraitUrls(StakeholderDto stakeholder)
        {
            string baseUrl = (IsWebBuild ? webApiBaseUrl : editorApiBaseUrl).TrimEnd('/');
            string assetName = stakeholder.name.ToLowerInvariant().Replace(".", string.Empty).Replace(" ", "-");
            return new[] { $"{baseUrl}/assets/characters/optimized/{assetName}.webp", $"{baseUrl}/assets/characters/optimized/{assetName}.jpg" };
        }

        private void Refresh()
        {
            if (presentation == null) return;
            EvidenceNotebookSession evidenceSession = EvidenceNotebookSession.GetOrCreate();
            int evidenceCount = evidenceSession.Notebook?.RecordedReadings.Count ?? 0;
            presentation.Refresh(selected, FormatConversation(), FormatEvidence(), statusMessage,
                evidenceCount, scenario.test_sites?.Length ?? 0, RespondedCount(), scenario.stakeholders.Length, busy,
                selected != null && selected.id == lastFailedStakeholderId && !string.IsNullOrWhiteSpace(lastFailedQuestion), PlanUnlocked);
        }

        private string FormatEvidence()
        {
            EvidenceNotebookSession evidenceSession = EvidenceNotebookSession.GetOrCreate();
            if (evidenceSession.Notebook == null || evidenceSession.Notebook.RecordedReadings.Count == 0) return "No samples recorded yet.";
            StringBuilder text = new StringBuilder();
            string unit = scenario.units == null ? string.Empty : scenario.units.salinity;
            foreach (RecordedReading reading in evidenceSession.Notebook.RecordedReadings)
            {
                text.Append(reading.label).Append('\n').Append("Salinity: ").Append(reading.salinity_gL.ToString("0.##")).Append(' ').Append(unit)
                    .Append('\n').Append("Season: ").Append(reading.season).Append('\n').Append("Freshwater: ").Append(reading.freshwater_access)
                    .Append('\n').Append(reading.note).Append('\n').Append("Source IDs: ").Append(string.Join(", ", reading.source_ids)).Append("\n\n");
            }
            return text.ToString();
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
            InvestigationController investigation = FindAnyObjectByType<InvestigationController>();
            if (investigation != null) investigation.EnterInterviewPresentation();
            AnGiangRealitySpikeController spike = FindAnyObjectByType<AnGiangRealitySpikeController>();
            if (spike != null) spike.SetCinematicInterviewActive(true);
            RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Interviews);
            RuntimePanelManager.GetOrCreate().SetCinematicMode(true);
            SetStatus("Choose a stakeholder perspective.");
            Refresh();
        }
        private static string ReadableError(UnityWebRequest request) => request.responseCode > 0 ? $"server returned {request.responseCode}" : request.error;
        private void SetStatus(string message) { statusMessage = message; RuntimePanelManager.GetOrCreate().SetInstruction(message); }
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
    }
}
