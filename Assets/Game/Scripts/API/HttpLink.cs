using System;
using System.Collections.Generic;

namespace Game.Scripts.API
{
    public abstract class HttpLink
    {
        private const string ApiBaseEnvironmentVariable = "WOM_API_BASE";
        private const string ApiBaseFallbackEnvironmentVariable = "API_BASE";
        public const string LocalAPIBase = "https://localhost:7216";
        public const string LocalIisHttpsAPIBase = "https://localhost:44377";
        public const string RenderAPIBase = "https://war-of-machines-api.onrender.com";

        public static string APIBase = GetDefaultApiBase();

        private static bool _runtimeConfigApplied;

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

        public static string[] GetBaseCandidates()
        {
            ApplyRuntimeConfig();

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
                apiBase = Environment.GetEnvironmentVariable(ApiBaseFallbackEnvironmentVariable);
            }

            SetResolvedBase(apiBase);
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

            return string.Equals(uri.Host, "war-of-machines-api.onrender.com", StringComparison.OrdinalIgnoreCase);
        }
    }
}
