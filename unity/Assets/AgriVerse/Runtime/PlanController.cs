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
            var canvasObject = new GameObject(
                "PlanCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(
                planStage.transform,
                false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 16;
            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            AtlasSurfaceGraphic card = EpisodeUiFactory.FieldPaper(
                canvas.transform,
                "PlanCard",
                true);
            Stretch(
                card.rectTransform,
                new Vector2(.035f, .035f),
                new Vector2(.965f, .53f));
            Text heading = Text(
                card.transform,
                "PlanHeading",
                22);
            heading.text = "FIELD PLAN  ·  DESIGN A PROPOSAL";
            heading.color = EpisodeUiFactory.Ink;
            Stretch(
                heading.rectTransform,
                new Vector2(.025f, .88f),
                new Vector2(.72f, .98f));

            AtlasSurfaceGraphic planningMap =
                EpisodeUiFactory.AtlasLabel(
                    card.transform,
                    "PlanningMap");
            Stretch(
                planningMap.rectTransform,
                new Vector2(.025f, .11f),
                new Vector2(.245f, .84f));
            AtlasRouteGraphic planningRoute =
                EpisodeUiFactory.Route(
                    planningMap.transform,
                    "PlanningRoute",
                    new[]
                    {
                        new Vector2(.16f, .18f),
                        new Vector2(.52f, .52f),
                        new Vector2(.82f, .78f)
                    },
                    EpisodeUiFactory.BrightAmber,
                    1.5f);
            Stretch(
                planningRoute.rectTransform,
                new Vector2(.08f, .42f),
                new Vector2(.92f, .94f));
            Text mapLabel = Text(
                planningMap.transform,
                "PlanningMapLabel",
                12);
            mapLabel.text = "TARGET FIELD";
            mapLabel.color = EpisodeUiFactory.Amber;
            Stretch(
                mapLabel.rectTransform,
                new Vector2(.08f, .88f),
                new Vector2(.92f, .98f));

            siteDropdown = Dropdown(
                card.transform,
                "TargetSite");
            siteDropdown.options.Add(
                new Dropdown.OptionData("Select target site"));
            foreach (TestSiteDto site in scenario.test_sites)
            {
                siteDropdown.options.Add(
                    new Dropdown.OptionData(site.label));
            }
            siteDropdown.gameObject.SetActive(false);
            for (int index = 0;
                 index < scenario.test_sites.Length;
                 index++)
            {
                int captured = index;
                float height =
                    .34f / scenario.test_sites.Length;
                float bottom =
                    .05f +
                    (scenario.test_sites.Length - 1 - index) *
                    height;
                Button button =
                    EpisodeUiFactory.ChoiceButton(
                        planningMap.transform,
                        "TargetSite_" +
                        scenario.test_sites[index].id,
                        index + 1,
                        scenario.test_sites[index].label);
                Stretch(
                    button.GetComponent<RectTransform>(),
                    new Vector2(.08f, bottom),
                    new Vector2(.92f, bottom + height - .025f));
                button.onClick.AddListener(
                    () => SelectSite(captured));
                siteButtons.Add(button);
            }

            AtlasSurfaceGraphic interventionTray =
                EpisodeUiFactory.SmokedGlass(
                    card.transform,
                    "InterventionTokenTray");
            Stretch(
                interventionTray.rectTransform,
                new Vector2(.27f, .47f),
                new Vector2(.71f, .84f));
            Text interventions = Text(
                card.transform,
                "Interventions",
                14);
            interventions.text =
                "INTERVENTION TOKENS  ·  SELECT ONE OR MORE";
            interventions.color = EpisodeUiFactory.Ink;
            Stretch(
                interventions.rectTransform,
                new Vector2(.27f, .84f),
                new Vector2(.71f, .90f));
            for (int index = 0;
                 index < scenario.interventions.Length;
                 index++)
            {
                int captured = index;
                float width =
                    .92f / scenario.interventions.Length;
                float left = .04f + index * width;
                Button button =
                    EpisodeUiFactory.ChoiceButton(
                        interventionTray.transform,
                        "Intervention_" +
                        scenario.interventions[index].id,
                        index + 1,
                        scenario.interventions[index].label);
                Stretch(
                    button.GetComponent<RectTransform>(),
                    new Vector2(left, .12f),
                    new Vector2(
                        left + width - .025f,
                        .88f));
                button.onClick.AddListener(
                    () => ToggleIntervention(captured));
                interventionButtons.Add(button);
            }

            AtlasSurfaceGraphic supportTray =
                EpisodeUiFactory.AtlasLabel(
                    card.transform,
                    "SupportTokenTray");
            Stretch(
                supportTray.rectTransform,
                new Vector2(.27f, .25f),
                new Vector2(.71f, .43f));
            Text supports = Text(
                card.transform,
                "SupportMeasures",
                14);
            supports.text = "SUPPORT MEASURES";
            supports.color = EpisodeUiFactory.Ink;
            Stretch(
                supports.rectTransform,
                new Vector2(.27f, .43f),
                new Vector2(.71f, .47f));
            for (int index = 0;
                 index <
                 scenario.support_measure_options.Length;
                 index++)
            {
                int captured = index;
                float width =
                    .92f /
                    Mathf.Max(
                        1,
                        scenario
                            .support_measure_options.Length);
                float left = .04f + index * width;
                Button button =
                    EpisodeUiFactory.ChoiceButton(
                        supportTray.transform,
                        "Support_" +
                        scenario
                            .support_measure_options[index].id,
                        index + 1,
                        scenario
                            .support_measure_options[index]
                            .description);
                Stretch(
                    button.GetComponent<RectTransform>(),
                    new Vector2(left, .12f),
                    new Vector2(
                        left + width - .025f,
                        .88f));
                button.onClick.AddListener(
                    () => ToggleSupport(captured));
                supportButtons.Add(button);
            }

            parametersInput = Input(
                card.transform,
                "ParametersInput",
                "Optional parameters");
            Stretch(
                parametersInput.GetComponent<RectTransform>(),
                new Vector2(.27f, .06f),
                new Vector2(.43f, .21f));
            rationaleInput = Input(
                card.transform,
                "RationaleInput",
                "Evidence-based rationale");
            Stretch(
                rationaleInput.GetComponent<RectTransform>(),
                new Vector2(.445f, .06f),
                new Vector2(.71f, .21f));

            resultText = Text(
                card.transform,
                "SimulationConfirmation",
                14);
            resultText.color = EpisodeUiFactory.RiceGreen;
            Stretch(
                resultText.rectTransform,
                new Vector2(.74f, .56f),
                new Vector2(.975f, .84f));
            submitButton = EpisodeUiFactory.Button(
                card.transform,
                "SimulateButton",
                "RUN SIMULATION",
                EpisodeButtonStyle.Primary,
                15);
            Stretch(
                submitButton.GetComponent<RectTransform>(),
                new Vector2(.73f, .31f),
                new Vector2(.975f, .49f));
            submitButton.onClick.AddListener(SubmitPlan);
            retryButton = EpisodeUiFactory.Button(
                card.transform,
                "RetrySimulation",
                "RETRY",
                EpisodeButtonStyle.Secondary,
                14);
            Stretch(
                retryButton.GetComponent<RectTransform>(),
                new Vector2(.73f, .12f),
                new Vector2(.975f, .27f));
            retryButton.onClick.AddListener(
                RetrySimulation);
            EpisodeAccessibility.ApplyAll();
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
        private void RefreshButtons(){if(submitButton==null)return;submitButton.interactable=!busy;retryButton.interactable=!busy&&retryAvailable;for(int i=0;i<siteButtons.Count;i++)EpisodeUiFactory.SetButtonSelected(siteButtons[i],siteDropdown.value==i+1);for(int i=0;i<interventionButtons.Count;i++)EpisodeUiFactory.SetButtonSelected(interventionButtons[i],Array.IndexOf(session.InterventionIds,scenario.interventions[i].id)>=0);for(int i=0;i<supportButtons.Count;i++)EpisodeUiFactory.SetButtonSelected(supportButtons[i],Array.IndexOf(session.SupportMeasures,scenario.support_measure_options[i].id)>=0);}
        private void SetStatus(string message){RuntimePanelManager.GetOrCreate().SetInstruction(message);}
        private void Fail(string message,string diagnostic){LoadState=InvestigationLoadState.Failed;SetStatus(message);Debug.LogError(diagnostic,this);}
        private static Text Text(Transform parent,string name,int size)=>EpisodeUiFactory.Text(parent,name,Mathf.Max(13,size),TextAnchor.UpperLeft,EpisodeUiFactory.OffWhite);
        private static InputField Input(Transform parent,string name,string placeholderText)=>EpisodeUiFactory.Input(parent,name,placeholderText,true);
        private static Dropdown Dropdown(Transform parent,string name){Dropdown dropdown=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Dropdown)).GetComponent<Dropdown>();dropdown.transform.SetParent(parent,false);dropdown.GetComponent<Image>().color=EpisodeUiFactory.SecondarySurface;Text caption=Text(dropdown.transform,"Caption",15);caption.alignment=TextAnchor.MiddleLeft;Stretch(caption.rectTransform,new Vector2(.04f,0),new Vector2(.96f,1));dropdown.captionText=caption;return dropdown;}
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
