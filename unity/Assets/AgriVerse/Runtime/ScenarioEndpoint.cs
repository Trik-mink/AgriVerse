using System;
using UnityEngine;
using UnityEngine.Networking;

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

    public static class JudgeRequestSession
    {
        [Serializable]
        private sealed class ErrorBody
        {
            public ApiError error;
        }

        [Serializable]
        private sealed class ApiError
        {
            public string message;
        }

        public const string HeaderName = "X-AgriVerse-Session";
        private static string sessionId;

        public static string SessionId =>
            string.IsNullOrWhiteSpace(sessionId)
                ? sessionId = Guid.NewGuid().ToString("N")
                : sessionId;

        public static void BeginNew()
        {
            sessionId = Guid.NewGuid().ToString("N");
        }

        public static void Apply(UnityWebRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            request.SetRequestHeader(HeaderName, SessionId);
        }

        public static string ReadableError(UnityWebRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            return ReadableError(
                request.responseCode,
                request.error,
                request.downloadHandler?.text);
        }

        public static string ReadableError(
            long responseCode,
            string transportError,
            string responseBody)
        {
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                try
                {
                    ErrorBody parsed =
                        JsonUtility.FromJson<ErrorBody>(responseBody);
                    if (!string.IsNullOrWhiteSpace(
                            parsed?.error?.message))
                    {
                        return parsed.error.message.Trim();
                    }
                }
                catch (ArgumentException)
                {
                    // Fall back to the bounded transport status below.
                }
            }

            return responseCode > 0
                ? "server returned " + responseCode
                : string.IsNullOrWhiteSpace(transportError)
                    ? "the service was unavailable"
                    : transportError;
        }
    }
}
