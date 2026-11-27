using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RAV.Scripts.Networking.Authentication;
using UnityEngine;
using UnityEngine.Networking;

namespace RAV.Scripts.API
{
    public abstract class HttpLink
    {
        private static readonly Regex ValidationErrorsPattern = new Regex("\\\"errors\\\"\\s*:\\s*\\{(?<errors>[^}]*)\\}", RegexOptions.Singleline);
        private static readonly Regex ValidationFieldPattern = new Regex("\\\"(?:\\\\.|[^\\\"\\\\])*\\\"\\s*:\\s*\\[(?<messages>[^\\]]*)\\]", RegexOptions.Singleline);
        private static readonly Regex JsonStringPattern = new Regex("\\\"(?:\\\\.|[^\\\"\\\\])*\\\"", RegexOptions.Singleline);

        private const string ApiBaseEnvironmentVariable = "RAV_API_BASE";
        private const string LegacyApiBaseEnvironmentVariable = "WOM_API_BASE";
        private const string ApiBaseFallbackEnvironmentVariable = "API_BASE";
        public const string LocalAPIBase = "https://localhost:7216";
        public const string LocalIisHttpsAPIBase = "https://localhost:44377";
        public const string RenderAPIBase = "https://war-of-machines-api.onrender.com";

        public static string APIBase = GetDefaultApiBase();

        private static bool _runtimeConfigApplied;
        private static bool _hasExplicitApiBaseOverride;
        private static string _explicitApiBaseOverride = string.Empty;
        private static HashSet<string> _productionApiBases;

        static HttpLink()
        {
            // Resolve packaged Local/Netcup configuration before any API endpoint
            // reads APIBase directly during its startup sequence.
            ApplyRuntimeConfig();
        }

        public static bool IsLocal
        {
            get
            {
                ApplyRuntimeConfig();
                return IsLocalBase(APIBase);
            }
        }

        public static void SetResolvedBase(string apiBase)
        {
            string normalizedBase = NormalizeBase(apiBase);
            if (string.IsNullOrWhiteSpace(normalizedBase) || IsAllowedBase(normalizedBase) == false)
            {
                return;
            }

            APIBase = normalizedBase;
        }

#if UNITY_EDITOR
        public static bool TrySetEditorApiBase(string apiBase, out string error)
        {
            error = string.Empty;
            string normalizedBase = NormalizeBase(apiBase);
            if (string.IsNullOrWhiteSpace(normalizedBase)
                || !Uri.TryCreate(normalizedBase, UriKind.Absolute, out Uri uri)
                || uri.Scheme != Uri.UriSchemeHttps)
            {
                error = "The selected Editor API endpoint must be a valid HTTPS base URL.";
                return false;
            }

            string previousApiBase = Environment.GetEnvironmentVariable(ApiBaseEnvironmentVariable);
            Environment.SetEnvironmentVariable(ApiBaseEnvironmentVariable, normalizedBase);
            _runtimeConfigApplied = false;
            _hasExplicitApiBaseOverride = false;
            _explicitApiBaseOverride = string.Empty;
            APIBase = GetDefaultApiBase();
            ApplyRuntimeConfig();

            string[] candidates = GetBaseCandidates();
            if (candidates.Length == 1
                && string.Equals(candidates[0], normalizedBase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            Environment.SetEnvironmentVariable(ApiBaseEnvironmentVariable, previousApiBase);
            _runtimeConfigApplied = false;
            _hasExplicitApiBaseOverride = false;
            _explicitApiBaseOverride = string.Empty;
            APIBase = GetDefaultApiBase();
            ApplyRuntimeConfig();
            error = "The selected API endpoint is not in the Editor allow-list. No fallback will be used.";
            return false;
        }
#endif

        public static string[] GetBaseCandidates()
        {
            ApplyRuntimeConfig();

            // An explicitly configured endpoint defines the current environment. Do not
            // silently fall back from staging to a production API if that endpoint fails.
            if (_hasExplicitApiBaseOverride)
            {
                if (string.IsNullOrWhiteSpace(_explicitApiBaseOverride)
                    || IsAllowedBase(_explicitApiBaseOverride) == false)
                {
                    return Array.Empty<string>();
                }

                return new[] { _explicitApiBaseOverride };
            }

            if (IsDevelopmentConfiguration == false)
            {
                List<string> remoteCandidates = new List<string>(1);
                AddAllowedUnique(remoteCandidates, APIBase);
                if (remoteCandidates.Count == 0)
                {
                    AddAllowedUnique(remoteCandidates, RenderAPIBase);
                }

                return remoteCandidates.ToArray();
            }

            string[] candidates =
            {
                APIBase,
                LocalAPIBase,
                LocalIisHttpsAPIBase,
                "https://127.0.0.1:7216",
                RenderAPIBase
            };

            List<string> uniqueCandidates = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                AddAllowedUnique(uniqueCandidates, candidates[i]);
            }

            return uniqueCandidates.ToArray();
        }

        public static string[] GetLocalBaseCandidates()
        {
            string[] candidates =
            {
                LocalAPIBase,
                LocalIisHttpsAPIBase,
                "https://127.0.0.1:7216"
            };

            List<string> uniqueCandidates = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                AddAllowedUnique(uniqueCandidates, candidates[i]);
            }

            return uniqueCandidates.ToArray();
        }

        private static void ApplyRuntimeConfig()
        {
            if (_runtimeConfigApplied)
            {
                return;
            }

            _runtimeConfigApplied = true;

#if !UNITY_EDITOR
            // A player's build target is selected in the Editor and packaged beside
            // the executable. Keep that choice authoritative even when the machine
            // has stale API environment variables from a different run.
            if (ClientRuntimeConfiguration.TryGetBundledValue("RAV_CLIENT_TARGET", out string bundledTarget))
            {
                if ((!string.Equals(bundledTarget, "Local", StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(bundledTarget, "Netcup", StringComparison.OrdinalIgnoreCase))
                    || !ClientRuntimeConfiguration.TryGetBundledValue(ApiBaseEnvironmentVariable, out string bundledApiBase))
                {
                    _hasExplicitApiBaseOverride = true;
                    _explicitApiBaseOverride = string.Empty;
                    APIBase = string.Empty;
                    return;
                }

                _hasExplicitApiBaseOverride = true;
                _explicitApiBaseOverride = NormalizeBase(bundledApiBase);
                if (string.IsNullOrWhiteSpace(_explicitApiBaseOverride)
                    || !IsAllowedBase(_explicitApiBaseOverride))
                {
                    _explicitApiBaseOverride = string.Empty;
                    APIBase = string.Empty;
                    return;
                }

                APIBase = _explicitApiBaseOverride;
                return;
            }
#endif

            string apiBase = GetCommandLineValue("-apiBase");
            if (string.IsNullOrWhiteSpace(apiBase))
            {
                apiBase = GetCommandLineValue("--api-base");
            }

            if (string.IsNullOrWhiteSpace(apiBase))
            {
                apiBase = Environment.GetEnvironmentVariable(ApiBaseEnvironmentVariable);
            }

            if (string.IsNullOrWhiteSpace(apiBase))
            {
                apiBase = Environment.GetEnvironmentVariable(LegacyApiBaseEnvironmentVariable);
            }

            if (string.IsNullOrWhiteSpace(apiBase))
            {
                apiBase = Environment.GetEnvironmentVariable(ApiBaseFallbackEnvironmentVariable);
            }

            string clientTarget = Environment.GetEnvironmentVariable("RAV_CLIENT_TARGET");
            if (string.IsNullOrWhiteSpace(clientTarget))
            {
                ClientRuntimeConfiguration.TryGetBundledValue("RAV_CLIENT_TARGET", out clientTarget);
            }

            if (string.IsNullOrWhiteSpace(apiBase))
            {
                ClientRuntimeConfiguration.TryGetBundledValue(ApiBaseEnvironmentVariable, out apiBase);
            }

            // A client packaged for the Netcup staging environment must never fall
            // through to the production API if its explicit staging config is missing.
            if (string.Equals(clientTarget, "Netcup", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(apiBase))
            {
                _hasExplicitApiBaseOverride = true;
                _explicitApiBaseOverride = string.Empty;
                APIBase = string.Empty;
                return;
            }

            if (string.IsNullOrWhiteSpace(apiBase) == false)
            {
                _hasExplicitApiBaseOverride = true;
                _explicitApiBaseOverride = NormalizeBase(apiBase);
                if (string.IsNullOrWhiteSpace(_explicitApiBaseOverride)
                    || IsAllowedBase(_explicitApiBaseOverride) == false)
                {
                    APIBase = string.Empty;
                    return;
                }

                APIBase = _explicitApiBaseOverride;
                return;
            }

            if (string.Equals(clientTarget, "Netcup", StringComparison.OrdinalIgnoreCase))
            {
                _hasExplicitApiBaseOverride = true;
                _explicitApiBaseOverride = string.Empty;
                APIBase = string.Empty;
            }
        }

        private static string GetCommandLineValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            string[] args = Environment.GetCommandLineArgs();
            string prefix = key + "=";

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                if (string.Equals(arg, key, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        return args[i + 1];
                    }

                    return string.Empty;
                }

                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(prefix.Length);
                }
            }

            return string.Empty;
        }

        private static void AddAllowedUnique(List<string> values, string value)
        {
            string normalizedValue = NormalizeBase(value);
            if (string.IsNullOrWhiteSpace(normalizedValue) || IsAllowedBase(normalizedValue) == false)
            {
                return;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], normalizedValue, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            values.Add(normalizedValue);
        }

        public static bool IsLocalBase(string apiBase)
        {
            if (string.IsNullOrWhiteSpace(apiBase))
            {
                return false;
            }

            string value = apiBase.Trim();
            return value.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0
                   || value.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0
                   || value.IndexOf("::1", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string NormalizeBase(string apiBase)
        {
            if (string.IsNullOrWhiteSpace(apiBase))
            {
                return string.Empty;
            }

            string value = apiBase.Trim().TrimEnd('/');
            if (Uri.TryCreate(value, UriKind.Absolute, out Uri uri) == false)
            {
                return string.Empty;
            }

            if ((uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                || string.IsNullOrWhiteSpace(uri.Host)
                || !string.IsNullOrWhiteSpace(uri.AbsolutePath.Trim('/'))
                || string.IsNullOrWhiteSpace(uri.Query) == false
                || string.IsNullOrWhiteSpace(uri.Fragment) == false)
            {
                return string.Empty;
            }

            return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        public static string FormatApiFailure(UnityWebRequest request)
        {
            if (request == null)
            {
                return "The API request could not be created.";
            }

            string endpoint = "API";
            if (Uri.TryCreate(request.url, UriKind.Absolute, out Uri requestUri))
            {
                endpoint = requestUri.AbsolutePath;
            }

            long statusCode = request.responseCode;
            string reason;
            if (statusCode == 401)
            {
                reason = GetSafeClientErrorBody(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = "The game session has expired or is no longer valid. Sign in again.";
                }
            }
            else if (statusCode == 403)
            {
                reason = GetSafeClientErrorBody(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = "The account is not allowed to perform this action.";
                }
            }
            else if (statusCode == 404)
            {
                reason = GetSafeClientErrorBody(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = "The requested API resource was not found or is unavailable.";
                }
            }
            else if (statusCode == 409)
            {
                reason = GetSafeClientErrorBody(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = "The request conflicts with the current account or game state.";
                }
            }
            else if (statusCode == 429)
            {
                reason = GetSafeClientErrorBody(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = "The API is rate-limiting requests. Wait briefly and try again.";
                }
            }
            else if (statusCode >= 500)
            {
                reason = "The API encountered an internal error.";
            }
            else if (statusCode > 0)
            {
                reason = GetSafeClientErrorBody(request.downloadHandler != null ? request.downloadHandler.text : string.Empty);
                if (string.IsNullOrWhiteSpace(reason))
                {
                    reason = statusCode == 400
                        ? "The API rejected the request because its data was invalid."
                        : "The API rejected the request.";
                }

                return endpoint + " failed: " + reason + " (HTTP " + statusCode + ").";
            }
            else if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                string requestError = request.error ?? string.Empty;
                if (requestError.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    reason = "The API did not respond before the request timed out.";
                }
                else if (requestError.IndexOf("certificate", StringComparison.OrdinalIgnoreCase) >= 0
                         || requestError.IndexOf("SSL", StringComparison.OrdinalIgnoreCase) >= 0
                         || requestError.IndexOf("TLS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    reason = "The secure API connection failed certificate verification.";
                }
                else if (requestError.IndexOf("resolve host", StringComparison.OrdinalIgnoreCase) >= 0
                         || requestError.IndexOf("DNS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    reason = "The API host could not be resolved.";
                }
                else
                {
                    reason = "A network connection to the API could not be established.";
                }
            }
            else if (request.result == UnityWebRequest.Result.DataProcessingError)
            {
                reason = "The API response could not be read.";
            }
            else
            {
                reason = "The API request failed before a response was received.";
            }

            return statusCode > 0
                ? endpoint + " failed: " + reason + " (HTTP " + statusCode + ")."
                : endpoint + " failed: " + reason;
        }

        private static string GetSafeClientErrorBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            string trimmed = body.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                string validationMessages = ExtractValidationMessages(trimmed);
                if (!string.IsNullOrWhiteSpace(validationMessages))
                {
                    return validationMessages;
                }

                try
                {
                    ApiErrorMessage payload = JsonUtility.FromJson<ApiErrorMessage>(trimmed);
                    if (payload != null)
                    {
                        string message = !string.IsNullOrWhiteSpace(payload.detail) ? payload.detail
                            : !string.IsNullOrWhiteSpace(payload.message) ? payload.message
                            : !string.IsNullOrWhiteSpace(payload.error) ? payload.error
                            : payload.title;

                        if (!string.IsNullOrWhiteSpace(message)
                            && !string.Equals(message, "One or more validation errors occurred.", StringComparison.OrdinalIgnoreCase))
                        {
                            return SanitizeDisplayMessage(message);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Do not display malformed JSON or server implementation details.
                }

                return string.Empty;
            }

            return SanitizeDisplayMessage(trimmed);
        }

        private static string SanitizeDisplayMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            string trimmed = message.Trim();
            if (trimmed.Length > 240)
            {
                trimmed = trimmed.Substring(0, 237) + "...";
            }

            char[] characters = trimmed.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
            {
                if (char.IsControl(characters[i]))
                {
                    characters[i] = ' ';
                }
            }

            return new string(characters);
        }

        [Serializable]
        private sealed class ApiErrorMessage
        {
            public string title;
            public string detail;
            public string message;
            public string error;
        }

        [Serializable]
        private sealed class ApiErrorString
        {
            public string value;
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
                        ApiErrorString payload = JsonUtility.FromJson<ApiErrorString>("{\"value\":" + values[j].Value + "}");
                        if (payload != null && !string.IsNullOrWhiteSpace(payload.value))
                        {
                            messages.Add(payload.value.Trim());
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Ignore malformed individual validation strings; keep other useful messages.
                    }
                }
            }

            if (messages.Count == 0)
            {
                return string.Empty;
            }

            string combined = string.Join(" ", messages);
            return combined.Length <= 240 ? combined : combined.Substring(0, 237) + "...";
        }

        public static bool IsDevelopmentConfiguration
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return true;
#else
                return false;
#endif
            }
        }

        private static string GetDefaultApiBase()
        {
            return IsDevelopmentConfiguration ? LocalAPIBase : RenderAPIBase;
        }

        private static bool IsAllowedBase(string apiBase)
        {
            if (Uri.TryCreate(apiBase, UriKind.Absolute, out Uri uri) == false
                || uri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            if (IsDevelopmentConfiguration && IsLocalBase(apiBase))
            {
                return true;
            }

            string normalizedBase = NormalizeBase(apiBase);
            if (GetProductionApiBases().Contains(normalizedBase))
            {
                return true;
            }

            return IsExplicitStagingBase(normalizedBase);
        }

        private static bool IsExplicitStagingBase(string normalizedBase)
        {
            string configuredStagingBase = NormalizeBase(Environment.GetEnvironmentVariable(ApiBaseEnvironmentVariable));
            if (string.IsNullOrWhiteSpace(configuredStagingBase))
            {
                ClientRuntimeConfiguration.TryGetBundledValue(ApiBaseEnvironmentVariable, out string bundledApiBase);
                configuredStagingBase = NormalizeBase(bundledApiBase);
            }

            if (string.IsNullOrWhiteSpace(normalizedBase)
                || !string.Equals(normalizedBase, configuredStagingBase, StringComparison.OrdinalIgnoreCase)
                || !Uri.TryCreate(normalizedBase, UriKind.Absolute, out Uri uri))
            {
                return false;
            }

            string[] labels = uri.IdnHost.Split('.');
            for (int i = 0; i < labels.Length; i++)
            {
                if (string.Equals(labels[i], "staging", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static HashSet<string> GetProductionApiBases()
        {
            if (_productionApiBases != null)
            {
                return _productionApiBases;
            }

            _productionApiBases = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                RenderAPIBase
            };

            TextAsset allowlist = Resources.Load<TextAsset>("production-api-hosts");
            if (allowlist == null || string.IsNullOrWhiteSpace(allowlist.text))
            {
                return _productionApiBases;
            }

            string[] entries = allowlist.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string entry in entries)
            {
                string candidate = entry.Trim();
                if (candidate.Length == 0 || candidate.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                string normalized = NormalizeBase(candidate);
                if (!string.IsNullOrWhiteSpace(normalized)
                    && Uri.TryCreate(normalized, UriKind.Absolute, out Uri configuredUri)
                    && configuredUri.Scheme == Uri.UriSchemeHttps
                    && !IsLocalBase(normalized))
                {
                    _productionApiBases.Add(normalized);
                }
            }

            return _productionApiBases;
        }
    }
}
