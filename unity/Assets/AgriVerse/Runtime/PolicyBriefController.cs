using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    [Serializable] internal sealed class StakeholderConcernDto { public string stakeholder_id; public string concern; }
    [Serializable] internal sealed class PolicyBriefRequestBaseDto { public string target_site_id; public ProposalRequestDto proposal; public StakeholderConcernDto[] stakeholder_concerns; }

    /// <summary>Renders the backend-validated capstone brief from the revised, stored proposal.</summary>
    public sealed class PolicyBriefController : MonoBehaviour
    {
        [SerializeField] private string editorApiBaseUrl = "http://localhost:8787";
        [SerializeField] private string webApiBaseUrl = "http://localhost:8787";

        private PlanSession session;
        private GameObject stage;
        private Text contentText;
        private Button retryButton;
        private bool busy;
        private bool retryAvailable;

        public InvestigationLoadState LoadState { get; private set; } = InvestigationLoadState.NotStarted;
        public bool BriefVisible => RuntimePanelManager.GetOrCreate().IsShowing(RuntimeActivityStage.Brief);
        public bool IsBusy => busy;
        public string DisplayedContentForTesting => contentText == null ? string.Empty : contentText.text;
        public bool ScrollToFinalLineForTesting() =>
            DisplayedContentForTesting.Contains("Investigation complete") && RuntimeScrollableContent.ScrollToBottom(contentText);

        private void Start()
        {
            session = PlanSession.GetOrCreate();
            stage = new GameObject("PolicyBriefStage");
            stage.transform.SetParent(transform, false);
            CreateInterface();
            RuntimePanelManager.GetOrCreate().Register(RuntimeActivityStage.Brief, stage);
            LoadState = InvestigationLoadState.Ready;
        }

        public void ConfigureEndpointsForTesting(string editorBaseUrl, string webBaseUrl){editorApiBaseUrl=editorBaseUrl;webApiBaseUrl=webBaseUrl;}

        public void RequestBrief()
        {
            if (LoadState != InvestigationLoadState.Ready || busy) return;
            if (session.SimulatorResult == null || string.IsNullOrWhiteSpace(session.FeedbackResultJson) || string.IsNullOrWhiteSpace(session.TargetSiteId))
            {
                SetStatus("Completed feedback and a saved simulation are required before the policy brief.");
                return;
            }
            RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Brief);
            StartCoroutine(SendBrief());
        }

        public void RetryBrief(){if(!busy&&retryAvailable)StartCoroutine(SendBrief());}

        private IEnumerator SendBrief()
        {
            busy=true;retryAvailable=false;SetStatus("Generating the policy brief…");RefreshButtons();
            string url=(IsWebBuild?webApiBaseUrl:editorApiBaseUrl).TrimEnd('/')+"/api/policy-briefs";
            using(var request=new UnityWebRequest(url,UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildRequestJson()));request.downloadHandler=new DownloadHandlerBuffer();request.SetRequestHeader("Content-Type","application/json");
                yield return request.SendWebRequest();busy=false;
                if(request.result!=UnityWebRequest.Result.Success){retryAvailable=true;SetStatus("Policy brief failed: "+ReadableError(request)+". Retry is available.");RefreshButtons();yield break;}
                try
                {
                    CanonicalJsonValue brief=CanonicalJsonParser.Parse(request.downloadHandler.text);ValidateBrief(brief);session.StorePolicyBriefResult(request.downloadHandler.text);Render(brief);SetStatus("Investigation complete.");
                }
                catch(Exception error) when(error is FormatException||error is InvalidOperationException){retryAvailable=true;SetStatus("Policy brief response was invalid: "+error.Message+". Retry is available.");}
                RefreshButtons();
            }
        }

        private string BuildRequestJson()
        {
            var proposal=new ProposalRequestDto{intervention_ids=session.InterventionIds,support_measures=session.SupportMeasures,rationale=session.Rationale,parameters=new PlanParametersDto{student_parameters=session.ParametersText}};
            string prefix=JsonUtility.ToJson(new PolicyBriefRequestBaseDto{target_site_id=session.TargetSiteId,proposal=proposal,stakeholder_concerns=StakeholderConcerns()});
            return prefix.Substring(0,prefix.Length-1)+",\"simulation\":"+session.SimulatorResultJson+"}";
        }

        private static StakeholderConcernDto[] StakeholderConcerns()
        {
            InterviewNotebook notebook=InterviewNotebookSession.GetOrCreate().Notebook;
            if(notebook==null)return Array.Empty<StakeholderConcernDto>();
            var concerns=new List<StakeholderConcernDto>();
            foreach(StakeholderConversation conversation in notebook.Conversations)
            {
                for(int index=conversation.turns.Count-1;index>=0;index--)
                {
                    ConversationTurnDto turn=conversation.turns[index];
                    if(turn.role=="stakeholder"&&!string.IsNullOrWhiteSpace(turn.text)){concerns.Add(new StakeholderConcernDto{stakeholder_id=conversation.stakeholder_id,concern=turn.text});break;}
                }
            }
            return concerns.ToArray();
        }

        private void ValidateBrief(CanonicalJsonValue brief)
        {
            if(brief.Property("scenario_id").Text!=session.ScenarioId)throw new FormatException("The policy brief scenario did not match this session.");
            CanonicalJsonValue problem=brief.Property("problem_statement");problem.Property("text");problem.Property("source_ids");
            if(brief.Property("evidence").Items.Count==0)throw new FormatException("The policy brief did not include evidence.");
            foreach(CanonicalJsonValue item in brief.Property("evidence").Items){item.Property("claim");item.Property("source_ids");}
            CanonicalJsonValue recommendation=brief.Property("recommended_solution");recommendation.Property("summary");CanonicalJsonValue fit=recommendation.Property("fit_assessment");fit.Property("salinity");fit.Property("seasonality");fit.Property("freshwater");fit.Property("farmer_capital");fit.Property("overall");CanonicalJsonValue rationale=recommendation.Property("factor_rationale");rationale.Property("salinity");rationale.Property("seasonality");rationale.Property("freshwater");rationale.Property("farmer_capital");ValidateEvidence(recommendation.Property("evidence"));
            CanonicalJsonValue outcomes=brief.Property("projected_outcomes");ValidateOutcomes(outcomes.Property("year_1"));ValidateOutcomes(outcomes.Property("year_5"));outcomes.Property("summary");
            if(brief.Property("tradeoffs_and_risks").Items.Count<2)throw new FormatException("The policy brief did not include two tradeoffs or risks.");
            foreach(CanonicalJsonValue risk in brief.Property("tradeoffs_and_risks").Items){risk.Property("category");risk.Property("risk");risk.Property("mitigation");ValidateEvidence(risk.Property("evidence"));}
            foreach(CanonicalJsonValue balance in brief.Property("stakeholder_balance").Items){balance.Property("stakeholder_id");balance.Property("concern");balance.Property("response");}
            if(brief.Property("next_steps").Items.Count==0)throw new FormatException("The policy brief did not include next steps.");
            foreach(CanonicalJsonValue step in brief.Property("next_steps").Items){step.Property("order");step.Property("action");step.Property("owner_stakeholder_id");}
        }

        private void Render(CanonicalJsonValue brief)
        {
            var text=new StringBuilder("Policy brief\n\n").Append(brief.Property("title").Text);
            CanonicalJsonValue problem=brief.Property("problem_statement");text.Append("\n\nProblem statement\n").Append(problem.Property("text").Text).Append("\nSource IDs: ").Append(Join(problem.Property("source_ids").Items));
            text.Append("\n\nEvidence");foreach(CanonicalJsonValue evidence in brief.Property("evidence").Items)text.Append("\n- ").Append(evidence.Property("claim").Text).Append("\n  Source IDs: ").Append(Join(evidence.Property("source_ids").Items));
            CanonicalJsonValue recommendation=brief.Property("recommended_solution");CanonicalJsonValue fit=recommendation.Property("fit_assessment");CanonicalJsonValue rationale=recommendation.Property("factor_rationale");text.Append("\n\nRecommended solution\n").Append(recommendation.Property("summary").Text).Append("\nFit assessment — salinity: ").Append(fit.Property("salinity").Text).Append(", seasonality: ").Append(fit.Property("seasonality").Text).Append(", freshwater: ").Append(fit.Property("freshwater").Text).Append(", farmer capital: ").Append(fit.Property("farmer_capital").Text).Append(", overall: ").Append(fit.Property("overall").Text).Append("\nSalinity: ").Append(rationale.Property("salinity").Text).Append("\nSeasonality: ").Append(rationale.Property("seasonality").Text).Append("\nFreshwater: ").Append(rationale.Property("freshwater").Text).Append("\nFarmer capital: ").Append(rationale.Property("farmer_capital").Text).Append("\nEvidence: ").Append(EvidenceText(recommendation.Property("evidence")));
            CanonicalJsonValue outcomes=brief.Property("projected_outcomes");text.Append("\n\nProjected outcomes\nYear 1\n").Append(OutcomeText(outcomes.Property("year_1"))).Append("\nYear 5\n").Append(OutcomeText(outcomes.Property("year_5"))).Append("\n").Append(outcomes.Property("summary").Text);
            text.Append("\n\nTradeoffs and risks");foreach(CanonicalJsonValue risk in brief.Property("tradeoffs_and_risks").Items)text.Append("\n\n").Append(risk.Property("category").Text).Append("\nRisk: ").Append(risk.Property("risk").Text).Append("\nMitigation: ").Append(risk.Property("mitigation").Text).Append("\nEvidence: ").Append(EvidenceText(risk.Property("evidence")));
            text.Append("\n\nStakeholder balance");foreach(CanonicalJsonValue balance in brief.Property("stakeholder_balance").Items)text.Append("\n\n").Append(balance.Property("stakeholder_id").Text).Append("\nConcern: ").Append(balance.Property("concern").Text).Append("\nResponse: ").Append(balance.Property("response").Text);
            text.Append("\n\nNext steps");foreach(CanonicalJsonValue step in brief.Property("next_steps").Items)text.Append("\n").Append(step.Property("order").Text).Append(". ").Append(step.Property("action").Text).Append(" (owner: ").Append(step.Property("owner_stakeholder_id").Text).Append(")");
            text.Append("\n\nInvestigation complete");SetScrollableText(text.ToString());
        }

        private static void ValidateOutcomes(CanonicalJsonValue outcomes){CanonicalJsonValue salinity=outcomes.Property("salinity");salinity.Property("value");salinity.Property("unit");outcomes.Property("yield").Property("items");CanonicalJsonValue income=outcomes.Property("income");income.Property("score");income.Property("scale_min");income.Property("scale_max");income.Property("projected_value");income.Property("currency");income.Property("basis");CanonicalJsonValue sustainability=outcomes.Property("sustainability");sustainability.Property("score");sustainability.Property("scale_min");sustainability.Property("scale_max");}
        private static void ValidateEvidence(CanonicalJsonValue evidence){evidence.Property("source_ids");evidence.Property("simulation_years");}
        private static string OutcomeText(CanonicalJsonValue outcomes){var text=new StringBuilder("Salinity: ").Append(outcomes.Property("salinity").Property("value").Text).Append(' ').Append(outcomes.Property("salinity").Property("unit").Text).Append("\nYield items:");foreach(CanonicalJsonValue item in outcomes.Property("yield").Property("items").Items)text.Append("\n- ").Append(item.Property("commodity_id").Text).Append(": ").Append(item.Property("value").Text).Append(' ').Append(item.Property("unit").Text);CanonicalJsonValue income=outcomes.Property("income");CanonicalJsonValue sustainability=outcomes.Property("sustainability");text.Append("\nIncome score: ").Append(income.Property("score").Text).Append(" (scale ").Append(income.Property("scale_min").Text).Append("–").Append(income.Property("scale_max").Text).Append(")").Append("\nIncome projected value: ").Append(income.Property("projected_value").Text).Append(" ").Append(income.Property("currency").Text).Append(" ").Append(income.Property("basis").Text).Append("\nSustainability score: ").Append(sustainability.Property("score").Text).Append(" (scale ").Append(sustainability.Property("scale_min").Text).Append("–").Append(sustainability.Property("scale_max").Text).Append(")");return text.ToString();}
        private static string EvidenceText(CanonicalJsonValue evidence)=>"source IDs: "+Join(evidence.Property("source_ids").Items)+"; simulation years: "+Join(evidence.Property("simulation_years").Items);

        private void CreateInterface(){var canvasObject=new GameObject("PolicyBriefCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.transform.SetParent(stage.transform,false);Canvas canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;CanvasScaler scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);contentText=RuntimeScrollableContent.Create(canvas.transform,"PolicyBriefContent",new Vector2(.03f,.1f),new Vector2(.62f,.82f),13);retryButton=Button(canvas.transform,"RetryBrief","Retry policy brief");Stretch(retryButton.GetComponent<RectTransform>(),new Vector2(.03f,.04f),new Vector2(.62f,.08f));retryButton.onClick.AddListener(RetryBrief);RefreshButtons();}
        private void RefreshButtons(){if(retryButton!=null)retryButton.interactable=!busy&&retryAvailable;}
        private void SetScrollableText(string value){RuntimeScrollableContent.SetText(contentText,value);}
        private void SetStatus(string value){RuntimePanelManager.GetOrCreate().SetInstruction(value);}
        private static string ReadableError(UnityWebRequest request)=>request.responseCode>0?"server returned "+request.responseCode:request.error;
        private static string Join(IReadOnlyList<CanonicalJsonValue> values){var text=new StringBuilder();for(int i=0;i<values.Count;i++){if(i>0)text.Append(", ");text.Append(values[i].Text);}return text.ToString();}
        private static Text Text(Transform parent,string name,int size){Text text=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();text.transform.SetParent(parent,false);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=Color.white;text.raycastTarget=false;text.verticalOverflow=VerticalWrapMode.Overflow;return text;}
        private static Button Button(Transform parent,string name,string label){Button button=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)).GetComponent<Button>();button.transform.SetParent(parent,false);Image image=button.GetComponent<Image>();image.color=new Color(.4f,.4f,.4f);button.targetGraphic=image;Text text=Text(button.transform,"Label",13);text.text=label;text.alignment=TextAnchor.MiddleCenter;Stretch(text.rectTransform,Vector2.zero,Vector2.one);return button;}
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
