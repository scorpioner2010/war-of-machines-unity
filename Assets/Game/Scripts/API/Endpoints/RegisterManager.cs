using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using Game.Scripts.API.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public abstract class RegisterManager
    {
        private const int LocalRequestTimeoutSeconds = 2;
        private const int RemoteRequestTimeoutSeconds = 10;

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

        public static async UniTask<(bool isSuccess, string message, string token)> SendLoginRequest(string username, string password)
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
                return (true, result.response, tokenResponse.token);
            }

            return (false, result.response, string.Empty);
        }

        private static async UniTask<(bool completed, bool success, string response)> SendAuthPostRequest(string endpoint, string json)
        {
            string[] apiBases = HttpLink.GetBaseCandidates();
            string lastResponse = string.Empty;

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                string url = apiBase + endpoint;

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.certificateHandler = new AcceptAllCertificates();
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
                        lastResponse = response;
                        continue;
                    }

                    HttpLink.SetResolvedBase(apiBase);
                    return (true, false, response);
                }
            }

            return (false, false, lastResponse);
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
