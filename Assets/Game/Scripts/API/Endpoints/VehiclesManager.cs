using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API
{
    /// <summary>
    /// Робота з технікою:
    ///   GET /vehicles?faction=&branch=
    ///   GET /vehicles/{id}
    ///   GET /vehicles/by-code/{code}
    ///   GET /vehicles/{id}/research-from
    ///   GET /vehicles/graph?faction=
    /// </summary>
    public abstract class VehiclesManager
    {
        public static async UniTask<(bool isSuccess, string message, VehicleLite[] items)>
            GetAll(string faction = null, string branch = null)
        {
            string url = HttpLink.APIBase + "/vehicles";

            bool hasQuery = false;
            if (!string.IsNullOrEmpty(faction))
            {
                url += hasQuery ? "&" : "?";
                url += "faction=" + UnityWebRequest.EscapeURL(faction);
                hasQuery = true;
            }
            if (!string.IsNullOrEmpty(branch))
            {
                url += hasQuery ? "&" : "?";
                url += "branch=" + UnityWebRequest.EscapeURL(branch);
            }

            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            try { await request.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                VehicleLite[] arr = JsonHelper.FromJson<VehicleLite>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<VehicleLite>());
        }

        public static async UniTask<(bool isSuccess, string message, VehicleLite item)> GetById(int id)
        {
            string url = HttpLink.APIBase + "/vehicles/" + id;

            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            try { await request.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                VehicleLite item = JsonUtility.FromJson<VehicleLite>(resp);
                return (true, resp, item);
            }

            return (false, resp, default(VehicleLite));
        }

        public static async UniTask<(bool isSuccess, string message, VehicleLite item)> GetByCode(string code)
        {
            string url = HttpLink.APIBase + "/vehicles/by-code/" + UnityWebRequest.EscapeURL(code);

            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            try { await request.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                VehicleLite item = JsonUtility.FromJson<VehicleLite>(resp);
                return (true, resp, item);
            }

            return (false, resp, default(VehicleLite));
        }

        /// <summary>
        /// Хто може відкрити цей танк (і скільки XP потрібно на предку).
        /// GET /vehicles/{id}/research-from
        /// </summary>
        public static async UniTask<(bool isSuccess, string message, ResearchFromLink[] items)>
            GetResearchFrom(int vehicleId)
        {
            string url = HttpLink.APIBase + "/vehicles/" + vehicleId + "/research-from";

            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            try { await request.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                var arr = JsonHelper.FromJson<ResearchFromLink>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<ResearchFromLink>());
        }

        /// <summary>
        /// Повний граф для побудови дерева (вершини + ребра).
        /// GET /vehicles/graph?faction=
        /// </summary>
        public static async UniTask<(bool ok, string msg, VehicleGraph graph)>
            GetGraph(string faction = null)
        {
            string url = HttpLink.APIBase + "/vehicles/graph";
            if (!string.IsNullOrEmpty(faction))
                url += "?faction=" + UnityWebRequest.EscapeURL(faction);

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            if (req.result != UnityWebRequest.Result.Success)
                return (false, resp, default);

            // Кореневий об'єкт -> годиться для JsonUtility
            var graph = JsonUtility.FromJson<VehicleGraph>(resp);
            return (true, resp, graph);
        }
    }

    // -------- Models --------

    [Serializable]
    public class VehicleLite
    {
        // Ідентифікація/категорія
        public int id;
        public string code;
        public string name;

        public string branch;       // "tracked" | "biped"
        public string factionCode;  // напр. "iron_alliance"
        public string factionName;  // текстове ім'я

        // НОВЕ з сервера
        public string @class;       // "Scout" | "Guardian" | "Colossus"  (зарезервоване слово -> @class)
        public int level;           // 1..4
        public int purchaseCost;    // валюта гри

        // Бойові параметри
        public int hp;
        public int damage;
        public int penetration;

        // Гарматні/точність/кадри
        public float reloadTime;
        public float accuracy;
        public float aimTime;

        // Мобільність
        public float speed;
        public float acceleration;
        public float traverseSpeed;
        public float turretTraverseSpeed;

        // Броня (рядком "Front/Side/Rear", напр. "100/50/30")
        public string turretArmor;
        public string hullArmor;

        public static (int front, int side, int rear) ParseArmor(string armor)
        {
            if (string.IsNullOrWhiteSpace(armor)) return (0, 0, 0);
            var parts = armor.Split('/');
            if (parts.Length != 3) return (0, 0, 0);
            int.TryParse(parts[0], out var f);
            int.TryParse(parts[1], out var s);
            int.TryParse(parts[2], out var r);
            return (f, s, r);
        }
    }

    /// <summary>DTO для /vehicles/{id}/research-from</summary>
    [Serializable]
    public class ResearchFromLink
    {
        public int predecessorId;
        public int requiredXp;
    }

    /// <summary>Граф для побудови дерева на клієнті</summary>
    [Serializable]
    public class VehicleGraph
    {
        public VehicleNode[] nodes;
        public VehicleEdge[] edges;
    }

    [Serializable]
    public class VehicleNode
    {
        public int id;
        public string code;
        public string name;
        public string @class;    // "Scout" | "Guardian" | "Colossus"
        public int level;        // 1..4
        public string branch;    // "tracked" | "biped"
        public string factionCode;
    }

    [Serializable]
    public class VehicleEdge
    {
        public int fromId;       // predecessor
        public int toId;         // successor
        public int requiredXp;   // XP на предку
    }
}
