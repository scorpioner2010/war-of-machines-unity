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
        public const string LocalKestrelHttpAPIBase = "http://localhost:5220";
        public const string LocalIisHttpAPIBase = "http://localhost:43606";
        public const string RenderAPIBase = "https://war-of-machines-api.onrender.com";

#if UNITY_EDITOR
        public static string APIBase = LocalAPIBase;
#else
        public static string APIBase = RenderAPIBase;
#endif

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
            if (string.IsNullOrWhiteSpace(normalizedBase))
            {
                return;
            }

            APIBase = normalizedBase;
        }

        public static string[] GetBaseCandidates()
        {
            ApplyRuntimeConfig();

            if (IsLocalBase(APIBase) == false)
            {
                List<string> remoteCandidates = new List<string>(2);
                AddUnique(remoteCandidates, APIBase);
                AddUnique(remoteCandidates, RenderAPIBase);
                return remoteCandidates.ToArray();
            }

            string[] candidates =
            {
                APIBase,
                LocalAPIBase,
                LocalKestrelHttpAPIBase,
                LocalIisHttpsAPIBase,
                LocalIisHttpAPIBase,
                "https://127.0.0.1:7216",
                "http://127.0.0.1:5220",
                "https://127.0.0.1:44377",
                "http://127.0.0.1:43606",
                RenderAPIBase
            };

            List<string> uniqueCandidates = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                AddUnique(uniqueCandidates, candidates[i]);
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

        private static void AddUnique(List<string> values, string value)
        {
            string normalizedValue = NormalizeBase(value);
            if (string.IsNullOrWhiteSpace(normalizedValue))
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

            return apiBase.Trim().TrimEnd('/');
        }
    }
}
