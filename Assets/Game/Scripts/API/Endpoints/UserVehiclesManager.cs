using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    /// <summary>
    /// Робота з інвентарем гравця:
    ///   GET    /user-vehicles/me
    ///   PUT    /user-vehicles/me/active/{vehicleId}
    ///   POST   /user-vehicles/me/add-by-code/{code}
    ///   DELETE /user-vehicles/me/{vehicleId}
    /// </summary>
    public abstract class UserVehiclesManager
    {
        // GET /user-vehicles/me
        public static async UniTask<(bool isSuccess, string message, UserVehicleEntry[] items)> GetMyVehicles(string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/me";

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                var arr = JsonHelper.FromJson<UserVehicleEntry>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<UserVehicleEntry>());
        }

        // PUT /user-vehicles/me/active/{vehicleId}
        public static async UniTask<(bool isSuccess, string message)> SetActive(int vehicleId, string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/me/active/" + vehicleId;

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            return (req.result == UnityWebRequest.Result.Success, resp);
        }

        // POST /user-vehicles/me/add-by-code/{code}
        public static async UniTask<(bool isSuccess, string message)> AddByCode(string code, string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/me/add-by-code/" + UnityWebRequest.EscapeURL(code);

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            return (req.result == UnityWebRequest.Result.Success, resp);
        }

        // DELETE /user-vehicles/me/{vehicleId}
        public static async UniTask<(bool isSuccess, string message)> Remove(int vehicleId, string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/me/" + vehicleId;

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            return (req.result == UnityWebRequest.Result.Success, resp);
        }
    }

    // ---- Models ----
    [Serializable]
    public class UserVehicleEntry
    {
        public int id;
        public int vehicleId;
        public string vehicleCode;
        public string vehicleName;
        public bool isActive;
    }
}
