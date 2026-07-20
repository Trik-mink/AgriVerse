using System;
using UnityEngine;
using UnityEngine.Networking;

namespace AgriVerse.Client
{
    public static class ScenarioEndpoint
    {
        private const string ScenarioPath = "/api/scenario";
        public const string ProductionApiBaseUrl =
            "https://agriverse-judge-api.onrender.com";

        public static string ForPlatform(
            bool isWebBuild,
            string editorApiBaseUrl,
            string webApiBaseUrl)
        {
            return ApiRouteForRuntime(
                Application.isEditor,
                isWebBuild,
                editorApiBaseUrl,
                webApiBaseUrl,
                ScenarioPath);
        }

        public static string ApiBaseForPlatform(
            bool isWebBuild,
            string editorApiBaseUrl,
            string webApiBaseUrl)
        {
            return ApiBaseForRuntime(
                Application.isEditor,
                isWebBuild,
                editorApiBaseUrl,
                webApiBaseUrl);
        }

        public static string ApiBaseForRuntime(
            bool isEditor,
            bool isWebBuild,
            string editorApiBaseUrl,
            string webApiBaseUrl)
        {
            string selectedBaseUrl = isWebBuild
                ? webApiBaseUrl
                : isEditor
                    ? editorApiBaseUrl
                    : ProductionApiBaseUrl;
            if (!Uri.TryCreate(selectedBaseUrl, UriKind.Absolute, out Uri baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(baseUri.UserInfo))
            {
                throw new ArgumentException(
                    "The API base URL must be an absolute HTTP or HTTPS origin.",
                    isWebBuild
                        ? nameof(webApiBaseUrl)
                        : isEditor
                            ? nameof(editorApiBaseUrl)
                            : nameof(ProductionApiBaseUrl));
            }

            return selectedBaseUrl.TrimEnd('/');
        }

        public static string ApiRouteForPlatform(
            bool isWebBuild,
            string editorApiBaseUrl,
            string webApiBaseUrl,
            string route)
        {
            return ApiRouteForRuntime(
                Application.isEditor,
                isWebBuild,
                editorApiBaseUrl,
                webApiBaseUrl,
                route);
        }

        public static string ApiRouteForRuntime(
            bool isEditor,
            bool isWebBuild,
            string editorApiBaseUrl,
            string webApiBaseUrl,
            string route)
        {
            if (string.IsNullOrWhiteSpace(route) ||
                route[0] != '/' ||
                route.Contains("?") ||
                route.Contains("#"))
            {
                throw new ArgumentException(
                    "The API route must be an absolute path without a query or fragment.",
                    nameof(route));
            }

            return ApiBaseForRuntime(
                       isEditor,
                       isWebBuild,
                       editorApiBaseUrl,
                       webApiBaseUrl) +
                   route;
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
