using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    [Serializable] internal sealed class FeedbackRequestBaseDto { public string target_site_id; public ProposalRequestDto proposal; }

    /// <summary>Requests and renders the backend-validated grader result without transforming it.</summary>
    public sealed class FeedbackController : MonoBehaviour
    {
        [SerializeField] private string editorApiBaseUrl = "http://localhost:8787";
        [SerializeField] private string webApiBaseUrl = "http://localhost:8787";

        private PlanSession session;
        private GameObject stage;
        private Text contentText;
        private Button retryButton;
        private Button backButton;
        private Button reviseButton;
        private Button briefButton;
        private bool busy;
        private bool retryAvailable;

        public InvestigationLoadState LoadState { get; private set; } = InvestigationLoadState.NotStarted;
        public bool FeedbackVisible => RuntimePanelManager.GetOrCreate().IsShowing(RuntimeActivityStage.Feedback);
        public bool IsBusy => busy;
        public string DisplayedContentForTesting => contentText == null ? string.Empty : contentText.text;

        private void Start()
        {
            session = PlanSession.GetOrCreate();
            stage = new GameObject("FeedbackStage");
            stage.transform.SetParent(transform, false);
            CreateInterface();
            RuntimePanelManager.GetOrCreate().Register(RuntimeActivityStage.Feedback, stage);
            LoadState = InvestigationLoadState.Ready;
        }

        public void ConfigureEndpointsForTesting(string editorBaseUrl, string webBaseUrl)
        {
            editorApiBaseUrl = editorBaseUrl;
            webApiBaseUrl = webBaseUrl;
        }

        public void RequestFeedback()
        {
            if (LoadState != InvestigationLoadState.Ready || busy) return;
            if (session.SimulatorResult == null || string.IsNullOrWhiteSpace(session.SimulatorResultJson) || string.IsNullOrWhiteSpace(session.TargetSiteId))
            {
                SetStatus("A saved simulation is required before feedback can be requested.");
                return;
            }
            RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Feedback);
            StartCoroutine(SendFeedback());
        }

        public void RetryFeedback()
        {
            if (!busy && retryAvailable) StartCoroutine(SendFeedback());
        }

        public void RevisePlan()
        {
            if (string.IsNullOrWhiteSpace(session.FeedbackResultJson)) return;
            PlanController plan = FindFirstObjectByType<PlanController>();
            if (plan == null) { SetStatus("Plan Controller is missing. Add its bootstrap to this scene."); return; }
            plan.BeginRevision();
        }

        public void GenerateBrief()
        {
            if (string.IsNullOrWhiteSpace(session.FeedbackResultJson)) return;
            PolicyBriefController brief = FindFirstObjectByType<PolicyBriefController>();
            if (brief == null) { SetStatus("Policy Brief Controller is missing. Add its bootstrap to this scene."); return; }
            brief.RequestBrief();
        }

        public void ReturnToConsequences()
        {
            ConsequencesController consequences = FindFirstObjectByType<ConsequencesController>();
            if (consequences == null) { SetStatus("Consequences Controller is missing. Add its bootstrap to this scene."); return; }
            consequences.ReturnFromFeedback();
        }

        public void ShowPlanActivity() { }
        public void ShowBriefActivity() { }

        private IEnumerator SendFeedback()
        {
            busy = true;
            retryAvailable = false;
            SetStatus("Requesting grounded feedback…");
            RefreshButtons();
            string requestJson = BuildRequestJson();
            string url = (IsWebBuild ? webApiBaseUrl : editorApiBaseUrl).TrimEnd('/') + "/api/feedback";
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                busy = false;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    retryAvailable = true;
                    SetStatus("Feedback failed: " + ReadableError(request) + ". Retry is available.");
                    RefreshButtons();
                    yield break;
                }
                try
                {
                    CanonicalJsonValue feedback = CanonicalJsonParser.Parse(request.downloadHandler.text);
                    ValidateFeedback(feedback);
                    session.StoreFeedbackResult(request.downloadHandler.text);
                    Render(feedback);
                    SetStatus("Feedback complete. Revise the plan or generate the policy brief.");
                }
                catch (Exception error) when (error is FormatException || error is InvalidOperationException)
                {
                    retryAvailable = true;
                    SetStatus("Feedback response was invalid: " + error.Message + ". Retry is available.");
                }
                RefreshButtons();
            }
        }

        private string BuildRequestJson()
        {
            var proposal = new ProposalRequestDto
            {
                intervention_ids = session.InterventionIds,
                support_measures = session.SupportMeasures,
                rationale = session.Rationale,
                parameters = new PlanParametersDto { student_parameters = session.ParametersText },
            };
            string prefix = JsonUtility.ToJson(new FeedbackRequestBaseDto { target_site_id = session.TargetSiteId, proposal = proposal });
            return prefix.Substring(0, prefix.Length - 1) + ",\"simulation\":" + session.SimulatorResultJson + "}";
        }

        private void ValidateFeedback(CanonicalJsonValue feedback)
        {
            if (feedback.Property("scenario_id").Text != session.ScenarioId) throw new FormatException("The feedback scenario did not match this session.");
            IReadOnlyList<CanonicalJsonValue> rubrics = feedback.Property("rubric_results").Items;
            if (rubrics.Count != 6) throw new FormatException("The feedback must include all six rubric results.");
            for (int index = 0; index < rubrics.Count; index++)
            {
                CanonicalJsonValue rubric = rubrics[index];
                rubric.Property("rubric_id"); rubric.Property("rating"); rubric.Property("feedback"); ValidateEvidence(rubric.Property("evidence"));
            }
            CanonicalJsonValue insight = feedback.Property("key_insight");
            insight.Property("text"); ValidateEvidence(insight.Property("evidence"));
            feedback.Property("revision_prompt"); feedback.Property("encouragement");
        }

        private void Render(CanonicalJsonValue feedback)
        {
            CanonicalJsonValue fit = feedback.Property("fit_assessment");
            var text = new StringBuilder("Feedback\n\nFit assessment\n")
                .Append("Salinity: ").Append(fit.Property("salinity").Text).Append("\nSeasonality: ").Append(fit.Property("seasonality").Text)
                .Append("\nFreshwater: ").Append(fit.Property("freshwater").Text).Append("\nFarmer capital: ").Append(fit.Property("farmer_capital").Text)
                .Append("\nOverall: ").Append(fit.Property("overall").Text).Append("\n\nRubric results");
            foreach (CanonicalJsonValue rubric in feedback.Property("rubric_results").Items)
            {
                text.Append("\n\n").Append(rubric.Property("rubric_id").Text).Append(": ").Append(rubric.Property("rating").Text)
                    .Append("\n").Append(rubric.Property("feedback").Text).Append("\nEvidence: ").Append(EvidenceText(rubric.Property("evidence")));
            }
            CanonicalJsonValue insight = feedback.Property("key_insight");
            text.Append("\n\nKey insight\n").Append(insight.Property("text").Text).Append("\nEvidence: ").Append(EvidenceText(insight.Property("evidence")))
                .Append("\n\nRevision prompt\n").Append(feedback.Property("revision_prompt").Text)
                .Append("\n\nEncouragement\n").Append(feedback.Property("encouragement").Text);
            SetScrollableText(text.ToString());
        }

        private static void ValidateEvidence(CanonicalJsonValue evidence)
        {
            evidence.Property("source_ids"); evidence.Property("simulation_years");
        }

        private static string EvidenceText(CanonicalJsonValue evidence)
        {
            return "source IDs: " + Join(evidence.Property("source_ids").Items) + "; simulation years: " + Join(evidence.Property("simulation_years").Items);
        }

        private void CreateInterface()
        {
            var canvasObject = new GameObject("FeedbackCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(stage.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            contentText = RuntimeScrollableContent.Create(canvas.transform, "FeedbackContent", new Vector2(.03f, .1f), new Vector2(.62f, .82f), 15);
            retryButton = Button(canvas.transform, "RetryFeedback", "Retry"); Stretch(retryButton.GetComponent<RectTransform>(), new Vector2(.03f, .04f), new Vector2(.17f, .08f)); retryButton.onClick.AddListener(RetryFeedback);
            backButton = Button(canvas.transform, "BackToConsequences", "Back"); Stretch(backButton.GetComponent<RectTransform>(), new Vector2(.18f, .04f), new Vector2(.32f, .08f)); backButton.onClick.AddListener(ReturnToConsequences);
            reviseButton = Button(canvas.transform, "RevisePlan", "Revise plan"); Stretch(reviseButton.GetComponent<RectTransform>(), new Vector2(.33f, .04f), new Vector2(.47f, .08f)); reviseButton.onClick.AddListener(RevisePlan);
            briefButton = Button(canvas.transform, "GenerateBrief", "Generate brief"); Stretch(briefButton.GetComponent<RectTransform>(), new Vector2(.48f, .04f), new Vector2(.62f, .08f)); briefButton.onClick.AddListener(GenerateBrief);
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            if (retryButton == null) return;
            bool complete = !string.IsNullOrWhiteSpace(session?.FeedbackResultJson);
            retryButton.interactable = !busy && retryAvailable;
            backButton.interactable = !busy;
            reviseButton.interactable = !busy && complete;
            briefButton.interactable = !busy && complete;
        }

        private void SetScrollableText(string value)
        {
            RuntimeScrollableContent.SetText(contentText, value);
        }

        private void SetStatus(string value){RuntimePanelManager.GetOrCreate().SetInstruction(value);}
        private static string ReadableError(UnityWebRequest request) => request.responseCode > 0 ? "server returned " + request.responseCode : request.error;
        private static string Join(IReadOnlyList<CanonicalJsonValue> values){var text=new StringBuilder();for(int i=0;i<values.Count;i++){if(i>0)text.Append(", ");text.Append(values[i].Text);}return text.ToString();}
        private static Text Text(Transform parent,string name,int size){Text text=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();text.transform.SetParent(parent,false);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=EpisodeUiFactory.OffWhite;text.raycastTarget=false;text.verticalOverflow=VerticalWrapMode.Overflow;return text;}
        private static Button Button(Transform parent,string name,string label){Button button=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)).GetComponent<Button>();button.transform.SetParent(parent,false);Image image=button.GetComponent<Image>();image.color=EpisodeUiFactory.RiverTeal;button.targetGraphic=image;Text text=Text(button.transform,"Label",14);text.text=label;text.alignment=TextAnchor.MiddleCenter;Stretch(text.rectTransform,Vector2.zero,Vector2.one);return button;}
        private static void Stretch(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;}
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
    }
}
