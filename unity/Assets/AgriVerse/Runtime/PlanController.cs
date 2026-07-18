using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    [Serializable] internal sealed class PlanParametersDto { public string student_parameters; }
    [Serializable] internal sealed class ProposalRequestDto
    {
        public string[] intervention_ids;
        public PlanParametersDto parameters;
        public string[] support_measures;
        public string rationale;
    }
    [Serializable] internal sealed class SimulationRequestDto { public string target_site_id; public ProposalRequestDto proposal; }

    /// <summary>Primitive plan form. It sends the existing simulation contract and stores the complete validated response JSON.</summary>
    public sealed class PlanController : MonoBehaviour
    {
        [SerializeField] private string editorApiBaseUrl = "http://localhost:8787";
        [SerializeField] private string webApiBaseUrl = "http://localhost:8787";
        private ScenarioDto scenario;
        private PlanSession session;
        private GameObject planStage;
        private Dropdown siteDropdown;
        private InputField parametersInput;
        private InputField rationaleInput;
        private Text resultText;
        private Button submitButton;
        private Button retryButton;
        private readonly List<Button> interventionButtons = new List<Button>();
        private readonly List<Button> supportButtons = new List<Button>();
        private readonly List<Button> siteButtons = new List<Button>();
        private bool busy;
        private bool retryAvailable;
        private bool revisionRequested;

        public InvestigationLoadState LoadState { get; private set; } = InvestigationLoadState.NotStarted;
        public bool PlanVisible => RuntimePanelManager.GetOrCreate().IsShowing(RuntimeActivityStage.Plan);
        public PlanSession Session => session;
        public bool IsBusy => busy;

        private void Start() { StartCoroutine(LoadScenario()); }

        public IEnumerator LoadScenario()
        {
            LoadState = InvestigationLoadState.Loading;
            string url;
            try { url = ScenarioEndpoint.ForPlatform(IsWebBuild, editorApiBaseUrl, webApiBaseUrl); }
            catch (ArgumentException error) { Fail("Plan URL is not configured.", error.Message); yield break; }
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) { Fail("Scenario unavailable.", request.error); yield break; }
                try { scenario = ScenarioDto.FromJson(request.downloadHandler.text); }
                catch (FormatException error) { Fail("Scenario response was invalid.", error.Message); yield break; }
            }
            session = PlanSession.GetOrCreate(); session.ConfigureScenario(scenario.id);
            planStage = new GameObject("PlanStage"); planStage.transform.SetParent(transform, false);
            CreateInterface(); RuntimePanelManager.GetOrCreate().Register(RuntimeActivityStage.Plan, planStage); LoadState = InvestigationLoadState.Ready;
        }

        public void ConfigureEndpointsForTesting(string editorBaseUrl, string webBaseUrl) { editorApiBaseUrl = editorBaseUrl; webApiBaseUrl = webBaseUrl; }
        public void ConfigureForTesting(string siteId, string interventionId, string rationale)
        {
            for (int index = 0; index < scenario.test_sites.Length; index++) if (scenario.test_sites[index].id == siteId) SelectSite(index);
            session.InterventionIds = new[] { interventionId }; rationaleInput.text = rationale; RefreshButtons();
        }

        public void SubmitPlan()
        {
            if (busy || !CanSubmit()) { SetStatus("Choose a target site, intervention, and rationale before simulating."); return; }
            session.TargetSiteId = scenario.test_sites[siteDropdown.value].id;
            session.ParametersText = parametersInput.text.Trim(); session.Rationale = rationaleInput.text.Trim();
            StartCoroutine(SendSimulation());
        }
        public void RetrySimulation() { if (!busy && retryAvailable) StartCoroutine(SendSimulation()); }

        private IEnumerator SendSimulation()
        {
            busy = true; retryAvailable = false; SetStatus("Running the five-year model…"); RefreshButtons();
            var body = new SimulationRequestDto
            {
                target_site_id = session.TargetSiteId,
                proposal = new ProposalRequestDto { intervention_ids = session.InterventionIds, support_measures = session.SupportMeasures, rationale = session.Rationale, parameters = new PlanParametersDto { student_parameters = session.ParametersText } }
            };
            string url = (IsWebBuild ? webApiBaseUrl : editorApiBaseUrl).TrimEnd('/') + "/api/simulations";
            using (var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body))); request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest(); busy = false;
                if (request.result != UnityWebRequest.Result.Success) { retryAvailable = true; SetStatus($"Simulation failed: {(request.responseCode > 0 ? "server returned " + request.responseCode : request.error)}. Retry is available."); RefreshButtons(); yield break; }
                SimulatorResultSummaryDto result;
                try { result = JsonUtility.FromJson<SimulatorResultSummaryDto>(request.downloadHandler.text); }
                catch (ArgumentException error) { retryAvailable = true; SetStatus("Simulation response was invalid: " + error.Message); RefreshButtons(); yield break; }
                if (result == null || result.fit_assessment == null || string.IsNullOrWhiteSpace(result.fit_assessment.overall)) { retryAvailable = true; SetStatus("Simulation response was incomplete. Retry is available."); RefreshButtons(); yield break; }
                session.StoreSimulatorResult(request.downloadHandler.text, result);
                resultText.text = "Simulation stored. Fit assessment overall: " + result.fit_assessment.overall;
                revisionRequested = false;
                SetStatus("Simulation complete. Review the stored consequences."); RefreshButtons();
            }
        }

        private void CreateInterface()
        {
            var canvasObject = new GameObject("PlanCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvasObject.transform.SetParent(planStage.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            Text heading = Text(canvas.transform, "PlanHeading", 21); heading.text = "Design a proposal"; Stretch(heading.rectTransform, new Vector2(.03f,.77f), new Vector2(.62f,.83f));
            siteDropdown = Dropdown(canvas.transform, "TargetSite"); siteDropdown.options.Add(new Dropdown.OptionData("Select target site")); foreach (TestSiteDto site in scenario.test_sites) siteDropdown.options.Add(new Dropdown.OptionData(site.label)); siteDropdown.gameObject.SetActive(false);
            for (int index=0; index<scenario.test_sites.Length; index++) { int captured=index; Button button=Button(canvas.transform,"TargetSite_"+scenario.test_sites[index].id,scenario.test_sites[index].label); Stretch(button.GetComponent<RectTransform>(),new Vector2(.03f+index*.2f,.7f),new Vector2(.21f+index*.2f,.75f)); button.onClick.AddListener(()=>SelectSite(captured)); siteButtons.Add(button); }
            Text interventions = Text(canvas.transform, "Interventions", 16); interventions.text = "Interventions"; Stretch(interventions.rectTransform, new Vector2(.03f,.63f), new Vector2(.62f,.68f));
            for (int index=0; index<scenario.interventions.Length; index++) { int captured=index; Button button=Button(canvas.transform,"Intervention_"+scenario.interventions[index].id,scenario.interventions[index].label); Stretch(button.GetComponent<RectTransform>(),new Vector2(.03f + (index%2)*.3f,.56f-(index/2)*.07f),new Vector2(.31f+(index%2)*.3f,.62f-(index/2)*.07f)); button.onClick.AddListener(()=>ToggleIntervention(captured)); interventionButtons.Add(button); }
            Text supports = Text(canvas.transform,"SupportMeasures",16); supports.text="Support measures"; Stretch(supports.rectTransform,new Vector2(.03f,.39f),new Vector2(.62f,.44f));
            for(int index=0; index<scenario.support_measure_options.Length; index++){int captured=index; Button button=Button(canvas.transform,"Support_"+scenario.support_measure_options[index].id,scenario.support_measure_options[index].description); Stretch(button.GetComponent<RectTransform>(),new Vector2(.03f,.32f-index*.06f),new Vector2(.62f,.37f-index*.06f)); button.onClick.AddListener(()=>ToggleSupport(captured)); supportButtons.Add(button);}
            parametersInput=Input(canvas.transform,"ParametersInput","Optional parameters"); Stretch(parametersInput.GetComponent<RectTransform>(),new Vector2(.03f,.14f),new Vector2(.3f,.22f));
            rationaleInput=Input(canvas.transform,"RationaleInput","Evidence-based rationale"); Stretch(rationaleInput.GetComponent<RectTransform>(),new Vector2(.32f,.14f),new Vector2(.62f,.22f));
            submitButton=Button(canvas.transform,"SimulateButton","Run simulation"); Stretch(submitButton.GetComponent<RectTransform>(),new Vector2(.03f,.04f),new Vector2(.31f,.1f)); submitButton.onClick.AddListener(SubmitPlan);
            retryButton=Button(canvas.transform,"RetrySimulation","Retry"); Stretch(retryButton.GetComponent<RectTransform>(),new Vector2(.33f,.04f),new Vector2(.62f,.1f)); retryButton.onClick.AddListener(RetrySimulation);
            resultText=Text(canvas.transform,"SimulationConfirmation",18); Stretch(resultText.rectTransform,new Vector2(.03f,.10f),new Vector2(.62f,.13f));
        }

        private void ToggleIntervention(int index) { session.InterventionIds = Toggle(session.InterventionIds,scenario.interventions[index].id); RefreshButtons(); }
        private void SelectSite(int index) { siteDropdown.value = index + 1; RefreshButtons(); }
        private void ToggleSupport(int index) { session.SupportMeasures = Toggle(session.SupportMeasures,scenario.support_measure_options[index].id); RefreshButtons(); }
        private static string[] Toggle(string[] values,string value){var list=new List<string>(values);if(list.Contains(value))list.Remove(value);else list.Add(value);return list.ToArray();}
        private bool InterviewsComplete(){InterviewNotebookSession interviews=InterviewNotebookSession.GetOrCreate();return interviews.Notebook!=null&&interviews.Notebook.AreAllStakeholdersInterviewed(scenario.stakeholders);}
        private bool CanSubmit(){return siteDropdown!=null&&siteDropdown.value>0&&session.InterventionIds.Length>0&&!string.IsNullOrWhiteSpace(rationaleInput.text);}
        private void ActivatePlanStage(){RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Plan); SetStatus(revisionRequested ? "Revise the saved proposal, then run a new simulation." : "Interviews complete. Build a proposal, then run the model."); RefreshButtons();}
        public bool BeginPlanning(){if(LoadState!=InvestigationLoadState.Ready||!InterviewsComplete())return false;revisionRequested=false;ActivatePlanStage();return true;}
        public void ShowConsequencesActivity() { }
        public void BeginRevision(){if(LoadState!=InvestigationLoadState.Ready)return;revisionRequested=true;RuntimePanelManager.GetOrCreate().Show(RuntimeActivityStage.Plan);SetStatus("Revise the saved proposal, then run a new simulation.");RefreshButtons();}
        private void RefreshButtons(){if(submitButton==null)return;submitButton.interactable=!busy;retryButton.interactable=!busy&&retryAvailable;for(int i=0;i<siteButtons.Count;i++)siteButtons[i].GetComponent<Image>().color=siteDropdown.value==i+1?new Color(.65f,.65f,.65f):new Color(.4f,.4f,.4f);for(int i=0;i<interventionButtons.Count;i++)interventionButtons[i].GetComponent<Image>().color=Array.IndexOf(session.InterventionIds,scenario.interventions[i].id)>=0?new Color(.65f,.65f,.65f):new Color(.4f,.4f,.4f);for(int i=0;i<supportButtons.Count;i++)supportButtons[i].GetComponent<Image>().color=Array.IndexOf(session.SupportMeasures,scenario.support_measure_options[i].id)>=0?new Color(.65f,.65f,.65f):new Color(.4f,.4f,.4f);}
        private void SetStatus(string message){RuntimePanelManager.GetOrCreate().SetInstruction(message);}
        private void Fail(string message,string diagnostic){LoadState=InvestigationLoadState.Failed;SetStatus(message);Debug.LogError(diagnostic,this);}
        private static Text Text(Transform parent,string name,int size){Text text=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text)).GetComponent<Text>();text.transform.SetParent(parent,false);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=Color.white;text.raycastTarget=false;text.verticalOverflow=VerticalWrapMode.Overflow;return text;}
        private static InputField Input(Transform parent,string name,string placeholderText){InputField input=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(InputField)).GetComponent<InputField>();input.transform.SetParent(parent,false);input.GetComponent<Image>().color=new Color(.2f,.2f,.2f,.95f);Text text=Text(input.transform,"Text",14);Stretch(text.rectTransform,new Vector2(.04f,.05f),new Vector2(.96f,.95f));input.textComponent=text;Text placeholder=Text(input.transform,"Placeholder",14);placeholder.text=placeholderText;placeholder.color=Color.gray;Stretch(placeholder.rectTransform,new Vector2(.04f,.05f),new Vector2(.96f,.95f));input.placeholder=placeholder;input.lineType=InputField.LineType.MultiLineNewline;return input;}
        private static Button Button(Transform parent,string name,string label){Button button=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button)).GetComponent<Button>();button.transform.SetParent(parent,false);Image image=button.GetComponent<Image>();image.color=new Color(.4f,.4f,.4f);button.targetGraphic=image;Text text=Text(button.transform,"Label",13);text.text=label;text.alignment=TextAnchor.MiddleCenter;Stretch(text.rectTransform,Vector2.zero,Vector2.one);return button;}
        private static Dropdown Dropdown(Transform parent,string name){Dropdown dropdown=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Dropdown)).GetComponent<Dropdown>();dropdown.transform.SetParent(parent,false);dropdown.GetComponent<Image>().color=new Color(.35f,.35f,.35f);Text caption=Text(dropdown.transform,"Caption",15);caption.alignment=TextAnchor.MiddleLeft;Stretch(caption.rectTransform,new Vector2(.04f,0),new Vector2(.96f,1));dropdown.captionText=caption;return dropdown;}
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
