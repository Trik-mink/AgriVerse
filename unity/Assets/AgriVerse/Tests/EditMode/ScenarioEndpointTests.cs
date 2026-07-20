using System;
using NUnit.Framework;
using UnityEngine.Networking;

namespace AgriVerse.Client.Tests
{
    public sealed class ScenarioEndpointTests
    {
        [Test]
        public void ForPlatformUsesTheEditorApiBaseOutsideAWebBuild()
        {
            string url = ScenarioEndpoint.ForPlatform(
                isWebBuild: false,
                editorApiBaseUrl: "http://localhost:8787/",
                webApiBaseUrl: "https://api.example.test");

            Assert.That(url, Is.EqualTo("http://localhost:8787/api/scenario"));
        }

        [Test]
        public void ForPlatformUsesTheWebApiBaseInAWebBuild()
        {
            string url = ScenarioEndpoint.ForPlatform(
                isWebBuild: true,
                editorApiBaseUrl: "http://localhost:8787",
                webApiBaseUrl: "https://api.example.test/");

            Assert.That(url, Is.EqualTo("https://api.example.test/api/scenario"));
        }

        [TestCase("/relative")]
        [TestCase("file:///tmp/scenario.json")]
        public void ForPlatformRejectsUnsafeApiBaseUrls(string apiBaseUrl)
        {
            Assert.Throws<ArgumentException>(() =>
                ScenarioEndpoint.ForPlatform(
                    isWebBuild: false,
                    editorApiBaseUrl: apiBaseUrl,
                    webApiBaseUrl: "https://api.example.test"));
        }

        [Test]
        public void JudgeRequestSessionAddsAnOpaqueNonSecretHeader()
        {
            JudgeRequestSession.BeginNew();
            using (var request = UnityWebRequest.Get(
                       "https://api.example.test/api/scenario"))
            {
                JudgeRequestSession.Apply(request);
                string session = request.GetRequestHeader(
                    JudgeRequestSession.HeaderName);

                Assert.That(session, Has.Length.EqualTo(32));
                Assert.That(
                    Guid.TryParseExact(session, "N", out _),
                    Is.True);
            }
        }

        [Test]
        public void JudgeRequestErrorsUseTheSafeServerMessage()
        {
            const string body =
                "{\"error\":{\"code\":\"BUDGET_EXHAUSTED\"," +
                "\"message\":\"The hosted judge AI budget is exhausted. " +
                "No OpenAI request was made.\"}}";

            Assert.That(
                JudgeRequestSession.ReadableError(
                    503,
                    "HTTP/1.1 503 Service Unavailable",
                    body),
                Is.EqualTo(
                    "The hosted judge AI budget is exhausted. " +
                    "No OpenAI request was made."));
        }
    }
}
