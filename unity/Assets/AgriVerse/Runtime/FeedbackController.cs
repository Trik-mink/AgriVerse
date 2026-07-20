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
            string url = ScenarioEndpoint.ApiRouteForPlatform(
                IsWebBuild,
                editorApiBaseUrl,
                webApiBaseUrl,
                "/api/feedback");
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestJson));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                JudgeRequestSession.Apply(request);
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
            var canvasObject = new GameObject(
                "FeedbackCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(stage.transform, false);
            Canvas canvas =
                canvasObject.GetComponent<Canvas>();
            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 16;
            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            AtlasSurfaceGraphic card =
                EpisodeUiFactory.FieldPaper(
                canvas.transform,
                "FeedbackCard",
                true);
            Stretch(
                card.rectTransform,
                new Vector2(.04f, .04f),
                new Vector2(.96f, .55f));
            Text heading = EpisodeUiFactory.Text(
                card.transform,
                "FeedbackHeading",
                21,
                TextAnchor.MiddleLeft,
                EpisodeUiFactory.Ink);
            heading.text =
                "GROUNDED FEEDBACK  ·  REVIEW, THEN REVISE";
            Stretch(
                heading.rectTransform,
                new Vector2(.03f, .88f),
                new Vector2(.88f, .98f));
            AtlasSurfaceGraphic rubricRail =
                EpisodeUiFactory.AtlasLabel(
                    card.transform,
                    "RubricTagRail");
            Stretch(
                rubricRail.rectTransform,
                new Vector2(.03f, .20f),
                new Vector2(.21f, .85f));
            for (int index = 0; index < 6; index++)
            {
                Text marker = EpisodeUiFactory.Text(
                    rubricRail.transform,
                    "RubricMarker_" + (index + 1),
                    12,
                    TextAnchor.MiddleLeft,
                    index == 0
                        ? EpisodeUiFactory.Amber
                        : EpisodeUiFactory.OffWhite);
                marker.text =
                    (index + 1).ToString("00") +
                    "  REVIEW";
                float top = .94f - index * .15f;
                Stretch(
                    marker.rectTransform,
                    new Vector2(.10f, top - .10f),
                    new Vector2(.94f, top));
            }
            contentText = RuntimeScrollableContent.Create(
                card.transform,
                "FeedbackContent",
                new Vector2(.235f, .20f),
                new Vector2(.97f, .86f),
                15);
            contentText.color = EpisodeUiFactory.Ink;
            contentText.supportRichText = true;
            contentText.GetComponentInParent<ScrollRect>()
                .GetComponent<Image>().color =
                new Color(
                    EpisodeUiFactory.OffWhite.r,
                    EpisodeUiFactory.OffWhite.g,
                    EpisodeUiFactory.OffWhite.b,
                    .12f);
            retryButton = Button(
                card.transform,
                "RetryFeedback",
                "RETRY",
                EpisodeButtonStyle.Secondary);
            Stretch(
                retryButton.GetComponent<RectTransform>(),
                new Vector2(.03f, .045f),
                new Vector2(.20f, .16f));
            retryButton.onClick.AddListener(RetryFeedback);
            backButton = Button(
                card.transform,
                "BackToConsequences",
                "BACK TO FUTURE WALK",
                EpisodeButtonStyle.Secondary);
            Stretch(
                backButton.GetComponent<RectTransform>(),
                new Vector2(.215f, .045f),
                new Vector2(.45f, .16f));
            backButton.onClick.AddListener(ReturnToConsequences);
            reviseButton = Button(
                card.transform,
                "RevisePlan",
                "REVISE PLAN",
                EpisodeButtonStyle.Secondary);
            Stretch(
                reviseButton.GetComponent<RectTransform>(),
                new Vector2(.465f, .045f),
                new Vector2(.68f, .16f));
            reviseButton.onClick.AddListener(RevisePlan);
            briefButton = Button(
                card.transform,
                "GenerateBrief",
                "GENERATE POLICY BRIEF",
                EpisodeButtonStyle.Primary);
            Stretch(
                briefButton.GetComponent<RectTransform>(),
                new Vector2(.695f, .045f),
                new Vector2(.97f, .16f));
            briefButton.onClick.AddListener(GenerateBrief);
            EpisodeAccessibility.ApplyAll();
            RefreshButtons();
        }

        private void RefreshButtons()
        {
            if (retryButton == null) return;
            bool complete = !string.IsNullOrWhiteSpace(session?.FeedbackResultJson);
            retryButton.gameObject.SetActive(retryAvailable);
            retryButton.interactable = !busy && retryAvailable;
            backButton.interactable = !busy;
            reviseButton.interactable = !busy && complete;
            briefButton.interactable = !busy && complete;
        }

        private void SetScrollableText(string value)
        {
            RuntimeScrollableContent.SetText(
                contentText,
                EpisodeUiFactory.FormatModelText(value));
        }

        private void SetStatus(string value){RuntimePanelManager.GetOrCreate().SetInstruction(value);}
        private static string ReadableError(UnityWebRequest request) =>
            JudgeRequestSession.ReadableError(request);
        private static string Join(IReadOnlyList<CanonicalJsonValue> values){var text=new StringBuilder();for(int i=0;i<values.Count;i++){if(i>0)text.Append(", ");text.Append(values[i].Text);}return text.ToString();}
        private static Button Button(Transform parent,string name,string label,EpisodeButtonStyle style)=>EpisodeUiFactory.Button(parent,name,label,style,14);
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
