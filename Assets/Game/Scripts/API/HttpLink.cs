using System;
using System.Collections.Generic;

namespace Game.Scripts.API
{
    public abstract class HttpLink
    {
        public const string LocalAPIBase = "https://localhost:7216";
        public const string LocalIisHttpsAPIBase = "https://localhost:44377";
        public const string LocalKestrelHttpAPIBase = "http://localhost:5220";
        public const string LocalIisHttpAPIBase = "http://localhost:43606";
        public const string RenderAPIBase = "https://war-of-machines-api.onrender.com";
        public static string APIBase = LocalAPIBase;

        public static bool IsLocal => IsLocalBase(APIBase);

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
            if (IsLocalBase(APIBase) == false)
            {
                return new[] { NormalizeBase(APIBase) };
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
                "http://127.0.0.1:43606"
            };

            List<string> uniqueCandidates = new List<string>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = NormalizeBase(candidates[i]);
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                bool alreadyAdded = false;
                for (int j = 0; j < uniqueCandidates.Count; j++)
                {
                    if (string.Equals(uniqueCandidates[j], candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (alreadyAdded == false)
                {
                    uniqueCandidates.Add(candidate);
                }
            }

            return uniqueCandidates.ToArray();
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
