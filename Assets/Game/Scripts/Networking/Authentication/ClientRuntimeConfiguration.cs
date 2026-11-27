using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RAV.Scripts.Networking.Authentication
{
    public static class ClientRuntimeConfiguration
    {
        public const string FileName = "rav-client.env";

        private static readonly object Sync = new object();
        private static readonly HashSet<string> AllowedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RAV_CLIENT_TARGET",
            "RAV_API_BASE",
            "RAV_UTP_SERVER_COMMON_NAME",
            "RAV_UTP_CA_CERTIFICATE"
        };

        private static Dictionary<string, string> _values;
        private static string _configurationPath;
        private static bool _loaded;

        public static bool TryGetBundledValue(string name, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(name) || !AllowedKeys.Contains(name))
            {
                return false;
            }

            EnsureLoaded();
            if (_values == null || !_values.TryGetValue(name, out value) || string.IsNullOrWhiteSpace(value))
            {
                value = string.Empty;
                return false;
            }

            if (string.Equals(name, "RAV_UTP_CA_CERTIFICATE", StringComparison.OrdinalIgnoreCase)
                && !Path.IsPathRooted(value))
            {
                string directory = Path.GetDirectoryName(_configurationPath);
                value = Path.GetFullPath(Path.Combine(directory ?? string.Empty, value));
            }

            return true;
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            lock (Sync)
            {
                if (_loaded)
                {
                    return;
                }

                _loaded = true;
                try
                {
                    _configurationPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FileName));
                    if (!File.Exists(_configurationPath))
                    {
                        return;
                    }

                    _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string[] lines = File.ReadAllLines(_configurationPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        int separator = line.IndexOf('=');
                        if (separator <= 0)
                        {
                            continue;
                        }

                        string name = line.Substring(0, separator).Trim();
                        if (!AllowedKeys.Contains(name))
                        {
                            continue;
                        }

                        _values[name] = line.Substring(separator + 1).Trim().Trim('"');
                    }
                }
                catch (IOException)
                {
                    _values = null;
                }
                catch (UnauthorizedAccessException)
                {
                    _values = null;
                }
                catch (ArgumentException)
                {
                    _values = null;
                }
                catch (NotSupportedException)
                {
                    _values = null;
                }
                catch (System.Security.SecurityException)
                {
                    _values = null;
                }
            }
        }
    }
}
