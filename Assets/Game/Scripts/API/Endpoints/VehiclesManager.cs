using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API
{
    public abstract class VehiclesManager
    {
        private const float DefaultViewRange = 100f;

        public static async UniTask<(bool isSuccess, string message, VehicleLite[] items)> GetAll(string faction = null, string branch = null)
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
                NormalizeVehicleLites(arr);
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
                NormalizeVehicleLite(item);
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
                NormalizeVehicleLite(item);
                return (true, resp, item);
            }

            return (false, resp, default(VehicleLite));
        }

        public static async UniTask<(bool isSuccess, string message, ResearchFromLink[] items)> GetResearchFrom(int vehicleId)
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

            var graph = JsonUtility.FromJson<VehicleGraph>(resp);
            NormalizeVehicleGraph(graph);
            return (true, resp, graph);
        }

        private static void NormalizeVehicleLites(VehicleLite[] vehicles)
        {
            if (vehicles == null)
            {
                return;
            }

            for (int i = 0; i < vehicles.Length; i++)
            {
                NormalizeVehicleLite(vehicles[i]);
            }
        }

        private static void NormalizeVehicleLite(VehicleLite vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            vehicle.viewRange = ResolveViewRange(vehicle.viewRange);
        }

        private static void NormalizeVehicleGraph(VehicleGraph graph)
        {
            if (graph == null || graph.nodes == null)
            {
                return;
            }

            for (int i = 0; i < graph.nodes.Length; i++)
            {
                VehicleNode node = graph.nodes[i];
                if (node != null)
                {
                    node.viewRange = ResolveViewRange(node.viewRange);
                }
            }
        }

        private static float ResolveViewRange(float value)
        {
            if (value > 0f)
            {
                return value;
            }

            return DefaultViewRange;
        }
    }

    [Serializable]
    public class VehicleLite
    {
        public int id;
        public string code;
        public string name;

        public string branch;
        public string factionCode;
        public string factionName;

        public string @class;
        public int level;
        public int purchaseCost;
        public bool isVisible;

        public int hp;
        public int damage;
        public int penetration;
        public float shellSpeed;
        public int shellsCount;
        public float damageMin;
        public float damageMax;

        public float reloadTime;
        public float accuracy;
        public float aimTime;
        public float viewRange;

        public float speed;
        public float acceleration;
        public float traverseSpeed;
        public float turretTraverseSpeed;

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

    [Serializable]
    public class ResearchFromLink
    {
        public int predecessorId;
        public int requiredXp;
    }

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
        public string @class;
        public int level;
        public string branch;
        public string factionCode;
        public bool isVisible;
        public float shellSpeed;
        public int shellsCount;
        public float damageMin;
        public float damageMax;
        public float viewRange;
    }

    [Serializable]
    public class VehicleEdge
    {
        public int fromId;
        public int toId;
        public int requiredXp;
    }
}
