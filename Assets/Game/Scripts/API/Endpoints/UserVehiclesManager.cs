using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public abstract class UserVehiclesManager
    {
        // GET /user-vehicles/me
        public static async UniTask<(bool ok, string msg, UserVehicleResponse response)> GetMyVehicles(string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/me";

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler?.text ?? string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                var obj = JsonUtility.FromJson<UserVehicleResponseWrapper>("{\"data\":" + resp + "}");
                return (true, resp, obj.data);
            }

            return (false, resp, null);
        }

        // PUT /user-vehicles/me/active/{vehicleId}
        public static async UniTask<(bool ok, string msg)> SetActive(int vehicleId, string token)
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

            string resp = req.downloadHandler?.text ?? string.Empty;
            return (req.result == UnityWebRequest.Result.Success, resp);
        }

        // DELETE /user-vehicles/me/{vehicleId}
        public static async UniTask<(bool ok, string msg)> Remove(int vehicleId, string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/me/" + vehicleId;

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbDELETE)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler?.text ?? string.Empty;
            return (req.result == UnityWebRequest.Result.Success, resp);
        }

        // POST /user-vehicles/{vehicleId}/convert-freexp
        public static async UniTask<(bool ok, string msg)> ConvertFreeXp(int vehicleId, int amount, string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/" + vehicleId + "/convert-freexp";
            string json = "{\"amount\":" + amount + "}";

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler?.text ?? string.Empty;
            return (req.result == UnityWebRequest.Result.Success, resp);
        }

        // GET /user-vehicles/xp
        public static async UniTask<(bool ok, string msg, VehicleXpResponse data)> GetXpInfo(string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/xp";

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler?.text ?? string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<VehicleXpResponse>(resp);
                return (true, resp, data);
            }

            return (false, resp, null);
        }

        // POST /user-vehicles/me/buy/{code}
        // Купівля техніки за кодом: списує Bolts на бекенді та додає машину гравцеві
        public static async UniTask<(bool ok, string msg, BuyVehicleResult data)> Buy(string code, string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/me/buy/" + code;

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(new byte[0]),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler?.text ?? string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                var data = JsonUtility.FromJson<BuyVehicleResult>(resp);
                return (true, resp, data);
            }

            return (false, resp, null);
        }

        // POST /user-vehicles/me/sell/{vehicleId}
        // Продаж техніки за 50% від ціни
        public static async UniTask<(bool ok, string msg, SellVehicleResult data)> Sell(int vehicleId, string token)
        {
            string url = HttpLink.APIBase + "/user-vehicles/me/sell/" + vehicleId;

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(new byte[0]),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler?.text ?? string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                // очікуємо JSON: { ok, soldVehicleId, refundBolts, newBolts }
                var data = JsonUtility.FromJson<SellVehicleResult>(resp);
                return (true, resp, data);
            }

            return (false, resp, null);
        }
    }

    [Serializable]
    public class UserVehicleResponse
    {
        public int freeXp;
        public UserVehicleEntry[] vehicles;
    }

    [Serializable]
    public class UserVehicleResponseWrapper
    {
        public UserVehicleResponse data;
    }

    [Serializable]
    public class UserVehicleEntry
    {
        public int id;
        public int vehicleId;
        public string vehicleCode;
        public string vehicleName;
        public int xp;
        public bool isActive;
    }

    [Serializable]
    public class VehicleXpResponse
    {
        public int freeXp;
        public VehicleXpItem[] vehicles;
    }

    [Serializable]
    public class VehicleXpItem
    {
        public int vehicleId;
        public string vehicleName;
        public int xp;
        public bool isActive;
    }

    [Serializable]
    public class BuyVehicleResult
    {
        public bool ok;
        public int userVehicleId;
        public int vehicleId;
        public int newBolts;
    }

    [Serializable]
    public class SellVehicleResult
    {
        public bool ok;
        public int soldVehicleId;
        public int refundBolts;
        public int newBolts;
    }
}
