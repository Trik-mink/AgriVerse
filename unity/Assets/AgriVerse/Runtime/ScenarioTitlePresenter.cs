using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    public sealed class ScenarioTitlePresenter : MonoBehaviour
    {
        [Header("Scene reference")]
        [SerializeField]
        private Text scenarioTitleText;

        [Header("Existing Express backend")]
        [SerializeField]
        private string editorApiBaseUrl = "http://localhost:8787";

        [SerializeField]
        private string webApiBaseUrl = "http://localhost:8787";

        private void Start()
        {
            if (scenarioTitleText == null)
            {
                Debug.LogError(
                    "ScenarioTitlePresenter needs a UI text reference.",
                    this);
                return;
            }

            scenarioTitleText.supportRichText = false;
            StartCoroutine(LoadScenario());
        }

        public IEnumerator LoadScenario()
        {
            if (scenarioTitleText == null)
            {
                yield break;
            }

            scenarioTitleText.text = "Loading scenario…";

            string scenarioUrl;
            try
            {
                scenarioUrl = ScenarioEndpoint.ForPlatform(
                    IsWebBuild,
                    editorApiBaseUrl,
                    webApiBaseUrl);
            }
            catch (ArgumentException error)
            {
                ShowError("Scenario URL is not configured.", error.Message);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(scenarioUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    ShowError(
                        "Scenario unavailable.",
                        $"GET {scenarioUrl} failed ({request.responseCode}): {request.error}");
                    yield break;
                }

                try
                {
                    ScenarioDto scenario = ScenarioDto.FromJson(
                        request.downloadHandler.text);
                    scenarioTitleText.text = scenario.title;
                }
                catch (FormatException error)
                {
                    ShowError("Scenario response was invalid.", error.Message);
                }
            }
        }

        private void ShowError(string studentMessage, string diagnostic)
        {
            scenarioTitleText.text = studentMessage;
            Debug.LogError(diagnostic, this);
        }

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
