using System;

namespace AgriVerse.Client
{
    public static class ScenarioEndpoint
    {
        private const string ScenarioPath = "/api/scenario";

        public static string ForPlatform(
            bool isWebBuild,
            string editorApiBaseUrl,
            string webApiBaseUrl)
        {
            string selectedBaseUrl = isWebBuild ? webApiBaseUrl : editorApiBaseUrl;

            if (!Uri.TryCreate(selectedBaseUrl, UriKind.Absolute, out Uri baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(baseUri.UserInfo))
            {
                throw new ArgumentException(
                    "The API base URL must be an absolute HTTP or HTTPS origin.",
                    isWebBuild ? nameof(webApiBaseUrl) : nameof(editorApiBaseUrl));
            }

            return selectedBaseUrl.TrimEnd('/') + ScenarioPath;
        }
    }
}
