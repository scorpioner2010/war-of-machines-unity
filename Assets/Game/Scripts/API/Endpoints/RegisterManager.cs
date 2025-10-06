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
        public static async UniTask<(bool isSuccess, string message)> SendRegisterRequest(string username, string password)
        {
            string json = JsonUtility.ToJson(new AuthRequest
            {
                username = username,
                password = password
            });

            UnityWebRequest request = new UnityWebRequest(HttpLink.APIBase+"/auth/register", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException) { }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                return (true, request.downloadHandler.text);
            }
            
            return (false, request.downloadHandler.text);
        }

        public static async UniTask<(bool isSuccess, string message, string token)> SendLoginRequest(string username, string password)
        {
            string json = JsonUtility.ToJson(new AuthRequest
            {
                username = username,
                password = password
            });

            UnityWebRequest request = new UnityWebRequest(HttpLink.APIBase+"/auth/login", "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            request.SetRequestHeader("Content-Type", "application/json");

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException) { }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                TokenResponse tokenResponse = JsonUtility.FromJson<TokenResponse>(response);
                return (true, request.downloadHandler.text, tokenResponse.token);
            }
            
            return (false, request.downloadHandler.text, string.Empty);
        }
    }
}
