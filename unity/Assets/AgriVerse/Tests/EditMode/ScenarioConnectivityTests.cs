using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Networking;
using UnityEngine.TestTools;

namespace AgriVerse.Client.Tests
{
    public sealed class ScenarioConnectivityTests
    {
        [UnityTest]
        [Category("Integration")]
        public IEnumerator EditorRequestLoadsTheSanitizedScenario()
        {
            string apiBaseUrl =
                Environment.GetEnvironmentVariable("AGRIVERSE_API_BASE_URL");

            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                Assert.Ignore(
                    "Set AGRIVERSE_API_BASE_URL to run the live Editor request.");
            }

            string url = ScenarioEndpoint.ForPlatform(
                isWebBuild: false,
                editorApiBaseUrl: apiBaseUrl,
                webApiBaseUrl: "https://unused.example");

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                Assert.That(
                    request.result,
                    Is.EqualTo(UnityWebRequest.Result.Success),
                    request.error);

                string json = request.downloadHandler.text;
                ScenarioDto scenario = ScenarioDto.FromJson(json);

                Assert.That(scenario.title, Is.Not.Empty);
                Assert.That(json, Does.Not.Contain("\"hidden_goal\""));
                Assert.That(json, Does.Not.Contain("\"prompt_file\""));
            }
        }
    }
}
