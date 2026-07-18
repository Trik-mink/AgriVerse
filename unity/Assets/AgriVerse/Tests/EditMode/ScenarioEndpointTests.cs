using System;
using NUnit.Framework;

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
    }
}
