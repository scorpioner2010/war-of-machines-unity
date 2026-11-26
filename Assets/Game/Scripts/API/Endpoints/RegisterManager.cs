using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using RAV.Scripts.API.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace RAV.Scripts.API.Endpoints
{
    public abstract class RegisterManager
    {
        private const int LocalRequestTimeoutSeconds = 20;
        private const int RemoteRequestTimeoutSeconds = 10;
        private static readonly Regex ValidationErrorsPattern = new Regex("\"errors\"\\s*:\\s*\\{(?<errors>[^}]*)\\}", RegexOptions.Singleline);
        private static readonly Regex ValidationFieldPattern = new Regex("\"(?:\\\\.|[^\"\\\\])*\"\\s*:\\s*\\[(?<messages>[^\\]]*)\\]", RegexOptions.Singleline);
        private static readonly Regex JsonStringPattern = new Regex("\"(?:\\\\.|[^\"\\\\])*\"", RegexOptions.Singleline);

        [Serializable]
        private sealed class ApiErrorPayload
        {
            public string title;
            public string detail;
            public string message;
        }

        [Serializable]
        private sealed class JsonStringPayload
        {
            public string value;
        }

        public static async UniTask<(bool isSuccess, string message)> SendRegisterRequest(string username, string password)
        {
            string json = JsonUtility.ToJson(new AuthRequest
            {
                username = username,
                password = password
            });

            (bool completed, bool success, string response) result = await SendAuthPostRequest("/auth/register", json);
            return (result.success, result.response);
        }

        public static async UniTask<(bool isSuccess, string message, string token, string refreshToken, string expiresAtUtc)> SendLoginRequest(string username, string password)
        {
            string json = JsonUtility.ToJson(new AuthRequest
            {
                username = username,
                password = password
            });

            (bool completed, bool success, string response) result = await SendAuthPostRequest("/auth/login", json);

            if (result.success)
            {
                TokenResponse tokenResponse = JsonUtility.FromJson<TokenResponse>(result.response);
                return (true, result.response, tokenResponse.token, tokenResponse.refreshToken, tokenResponse.expiresAtUtc);
            }

            return (false, result.response, string.Empty, string.Empty, string.Empty);
        }

        private static async UniTask<(bool completed, bool success, string response)> SendAuthPostRequest(string endpoint, string json)
        {
            string[] apiBases = HttpLink.GetBaseCandidates();
            string lastResponse = string.Empty;
            if (apiBases == null || apiBases.Length == 0)
            {
                return (false, false, "No API endpoint is configured for this environment.");
            }

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                string url = apiBase + endpoint;

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = GetRequestTimeoutSeconds(apiBase);
                    request.SetRequestHeader("Content-Type", "application/json");

                    try
                    {
                        await request.SendWebRequest();
                    }
                    catch (UnityWebRequestException) { }

                    string response = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        HttpLink.SetResolvedBase(apiBase);
                        return (true, true, response);
                    }

                    if (ShouldTryNextBase(request, response))
                    {
                        lastResponse = FormatFailureMessage(request.responseCode, request.result, response, request.error);
                        continue;
                    }

                    HttpLink.SetResolvedBase(apiBase);
                    return (true, false, FormatFailureMessage(request.responseCode, request.result, response, request.error));
                }
            }

            return (false, false, string.IsNullOrWhiteSpace(lastResponse)
                ? "Could not reach the selected server. Check your connection and try again."
                : lastResponse);
        }

        private static string FormatFailureMessage(long responseCode, UnityWebRequest.Result result, string response, string requestError)
        {
            string body = response != null ? response.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(body))
            {
                if (result == UnityWebRequest.Result.ConnectionError)
                {
                    if (!string.IsNullOrWhiteSpace(requestError))
                    {
                        if (requestError.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return "The server did not respond before the request timed out. Please try again.";
                        }

                        if (requestError.IndexOf("certificate", StringComparison.OrdinalIgnoreCase) >= 0
                            || requestError.IndexOf("SSL", StringComparison.OrdinalIgnoreCase) >= 0
                            || requestError.IndexOf("TLS", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return "The secure connection to the server failed certificate verification.";
                        }

                        if (requestError.IndexOf("resolve host", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return "The server address could not be resolved. Check the selected API endpoint.";
                        }
                    }

                    return "Could not connect to the server. Check your connection and try again.";
                }

                if (responseCode > 0)
                {
                    return GetHttpFailureMessage(responseCode);
                }

                return "The request could not be completed. Check your connection and try again.";
            }

            if (body[0] == '{')
            {
                string validationMessage = ExtractValidationMessages(body);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    return validationMessage;
                }

                try
                {
                    ApiErrorPayload payload = JsonUtility.FromJson<ApiErrorPayload>(body);
                    if (payload != null)
                    {
                        if (!string.IsNullOrWhiteSpace(payload.detail))
                        {
                            return payload.detail.Trim();
                        }

                        if (!string.IsNullOrWhiteSpace(payload.message))
                        {
                            return payload.message.Trim();
                        }

                        if (!string.IsNullOrWhiteSpace(payload.title)
                            && !string.Equals(payload.title, "One or more validation errors occurred.", StringComparison.OrdinalIgnoreCase))
                        {
                            return payload.title.Trim();
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Fall through to a concise status-based message for malformed JSON.
                }

                return GetHttpFailureMessage(responseCode);
            }

            return body;
        }

        private static string ExtractValidationMessages(string json)
        {
            Match errorsMatch = ValidationErrorsPattern.Match(json);
            if (!errorsMatch.Success)
            {
                return string.Empty;
            }

            List<string> messages = new List<string>();
            MatchCollection fields = ValidationFieldPattern.Matches(errorsMatch.Groups["errors"].Value);
            for (int i = 0; i < fields.Count; i++)
            {
                MatchCollection values = JsonStringPattern.Matches(fields[i].Groups["messages"].Value);
                for (int j = 0; j < values.Count; j++)
                {
                    try
                    {
                        JsonStringPayload payload = JsonUtility.FromJson<JsonStringPayload>("{\"value\":" + values[j].Value + "}");
                        if (payload != null && !string.IsNullOrWhiteSpace(payload.value))
                        {
                            messages.Add(payload.value.Trim());
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Keep other validation messages if an individual value is malformed.
                    }
                }
            }

            return messages.Count == 0 ? string.Empty : string.Join(" ", messages);
        }

        private static string GetHttpFailureMessage(long responseCode)
        {
            if (responseCode == 400)
            {
                return "The server rejected the submitted information. Please check the login and password requirements.";
            }

            if (responseCode == 401)
            {
                return "The login or password is incorrect.";
            }

            if (responseCode == 409)
            {
                return "This login is already taken. Choose another login.";
            }

            if (responseCode == 429)
            {
                return "Too many attempts. Wait a moment and try again.";
            }

            if (responseCode >= 500)
            {
                return "The server encountered a problem. Please try again later.";
            }

            return responseCode > 0
                ? "The request failed (HTTP " + responseCode + "). Please try again."
                : "The server returned an unexpected response. Please try again.";
        }

        private static int GetRequestTimeoutSeconds(string apiBase)
        {
            if (HttpLink.IsLocalBase(apiBase))
            {
                return LocalRequestTimeoutSeconds;
            }

            return RemoteRequestTimeoutSeconds;
        }

        private static bool ShouldTryNextBase(UnityWebRequest request, string response)
        {
            if (HttpLink.IsLocal == false)
            {
                return false;
            }

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return true;
            }

            return request.responseCode == 0 && string.IsNullOrWhiteSpace(response);
        }
    }
}
