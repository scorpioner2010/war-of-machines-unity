using System.IO;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using UnityEngine;

namespace RAV.Scripts.Networking.Authentication
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SecureFishNetTransportConfiguration : MonoBehaviour
    {
        private void Awake()
        {
            ConfigureTransport();
        }

        private void ConfigureTransport()
        {
            NetworkManager networkManager = GetComponent<NetworkManager>();
            if (networkManager == null || networkManager.TransportManager == null)
            {
                Debug.LogError("Secure transport configuration requires a NetworkManager.");
                return;
            }

            UnityTransport transport = networkManager.TransportManager.Transport as UnityTransport;
            if (transport == null)
            {
                Debug.LogError("Secure transport configuration requires FishyUnityTransport.");
                return;
            }

            transport.UseEncryption = true;

            string serverCertificate = ReadSecret("RAV_UTP_SERVER_CERTIFICATE", "WOM_UTP_SERVER_CERTIFICATE");
            string serverPrivateKey = ReadSecret("RAV_UTP_SERVER_PRIVATE_KEY", "WOM_UTP_SERVER_PRIVATE_KEY");
            if (!string.IsNullOrWhiteSpace(serverCertificate) && !string.IsNullOrWhiteSpace(serverPrivateKey))
            {
                transport.SetServerSecrets(serverCertificate, serverPrivateKey);
            }

            string serverCommonName = GetEnvironmentValue("RAV_UTP_SERVER_COMMON_NAME", "WOM_UTP_SERVER_COMMON_NAME");
            string caCertificate = ReadSecret("RAV_UTP_CA_CERTIFICATE", "WOM_UTP_CA_CERTIFICATE");
            if (!string.IsNullOrWhiteSpace(serverCommonName))
            {
                transport.SetClientSecrets(serverCommonName, string.IsNullOrWhiteSpace(caCertificate) ? null : caCertificate);
            }

            if (string.IsNullOrWhiteSpace(serverCertificate) || string.IsNullOrWhiteSpace(serverPrivateKey))
            {
                Debug.LogWarning("DTLS server secrets are not configured. Dedicated Server startup will fail closed.");
            }

            if (string.IsNullOrWhiteSpace(serverCommonName))
            {
                Debug.LogWarning("DTLS server common name is not configured. Client startup will fail closed.");
            }
        }

        private static string ReadSecret(string environmentVariable, string legacyEnvironmentVariable)
        {
            string value = GetEnvironmentValue(environmentVariable, legacyEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            try
            {
                if (File.Exists(value))
                {
                    return File.ReadAllText(value);
                }
            }
            catch (IOException)
            {
                Debug.LogError("Unable to read configured transport secret file.");
                return string.Empty;
            }
            catch (System.ArgumentException)
            {
                Debug.LogError("Unable to read configured transport secret file.");
                return string.Empty;
            }

            return value;
        }

        private static string GetEnvironmentValue(string name, string legacyName)
        {
            string value = System.Environment.GetEnvironmentVariable(name) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                value = System.Environment.GetEnvironmentVariable(legacyName) ?? string.Empty;
            }
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return ReadLocalClientValue(name, legacyName);
#else
            return string.Empty;
#endif
        }

        private static string ReadLocalClientValue(string name, string legacyName)
        {
            string[] paths =
            {
                System.IO.Path.Combine(System.Environment.CurrentDirectory, "client-transport.env"),
                System.IO.Path.Combine(Application.dataPath, "..", "client-transport.env")
            };

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    string[] lines = File.ReadAllLines(path);
                    for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                    {
                        string line = lines[lineIndex].Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        {
                            continue;
                        }

                        int separator = line.IndexOf('=');
                        if (separator <= 0 || (!string.Equals(line.Substring(0, separator).Trim(), name, System.StringComparison.Ordinal) && !string.Equals(line.Substring(0, separator).Trim(), legacyName, System.StringComparison.Ordinal)))
                        {
                            continue;
                        }

                        return line.Substring(separator + 1).Trim();
                    }
                }
                catch (IOException)
                {
                    return string.Empty;
                }
            }

            return string.Empty;
        }
    }
}
