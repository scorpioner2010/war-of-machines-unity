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
        private const int LocalRequestTimeoutSeconds = 2;
        private const int RemoteRequestTimeoutSeconds = 10;

        public static async UniTask<(bool isSuccess, string message, VehicleLite[] items)> GetAll(string faction = null, string branch = null)
        {
            string endpoint = "/vehicles";

            bool hasQuery = false;
            if (!string.IsNullOrEmpty(faction))
            {
                endpoint += hasQuery ? "&" : "?";
                endpoint += "faction=" + UnityWebRequest.EscapeURL(faction);
                hasQuery = true;
            }

            if (!string.IsNullOrEmpty(branch))
            {
                endpoint += hasQuery ? "&" : "?";
                endpoint += "branch=" + UnityWebRequest.EscapeURL(branch);
            }

            (bool isSuccess, string response) result = await SendGetRequest(endpoint);
            if (result.isSuccess)
            {
                VehicleLite[] arr = JsonHelper.FromJson<VehicleLite>(result.response);
                NormalizeVehicleLites(arr);
                return (true, result.response, arr);
            }

            return (false, result.response, Array.Empty<VehicleLite>());
        }

        public static async UniTask<(bool isSuccess, string message, VehicleLite item)> GetById(int id)
        {
            (bool isSuccess, string response) result = await SendGetRequest("/vehicles/" + id);
            if (result.isSuccess)
            {
                VehicleLite item = JsonUtility.FromJson<VehicleLite>(result.response);
                NormalizeVehicleLite(item);
                return (true, result.response, item);
            }

            return (false, result.response, default(VehicleLite));
        }

        public static async UniTask<(bool isSuccess, string message, VehicleLite item)> GetByCode(string code)
        {
            (bool isSuccess, string response) result = await SendGetRequest("/vehicles/by-code/" + UnityWebRequest.EscapeURL(code));
            if (result.isSuccess)
            {
                VehicleLite item = JsonUtility.FromJson<VehicleLite>(result.response);
                NormalizeVehicleLite(item);
                return (true, result.response, item);
            }

            return (false, result.response, default(VehicleLite));
        }

        public static async UniTask<(bool isSuccess, string message, ResearchFromLink[] items)> GetResearchFrom(int vehicleId)
        {
            (bool isSuccess, string response) result = await SendGetRequest("/vehicles/" + vehicleId + "/research-from");
            if (result.isSuccess)
            {
                ResearchFromLink[] arr = JsonHelper.FromJson<ResearchFromLink>(result.response);
                return (true, result.response, arr);
            }

            return (false, result.response, Array.Empty<ResearchFromLink>());
        }

        public static async UniTask<(bool ok, string msg, VehicleGraph graph)>
            GetGraph(string faction = null)
        {
            string endpoint = "/vehicles/graph";
            if (!string.IsNullOrEmpty(faction))
            {
                endpoint += "?faction=" + UnityWebRequest.EscapeURL(faction);
            }

            (bool isSuccess, string response) result = await SendGetRequest(endpoint);
            if (!result.isSuccess)
            {
                return (false, result.response, default);
            }

            VehicleGraph graph = JsonUtility.FromJson<VehicleGraph>(result.response);
            NormalizeVehicleGraph(graph);
            return (true, result.response, graph);
        }

        private static async UniTask<(bool isSuccess, string response)> SendGetRequest(string endpoint)
        {
            string[] apiBases = HttpLink.GetBaseCandidates();
            string lastResponse = string.Empty;

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                string url = apiBase + endpoint;

                using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
                {
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.certificateHandler = new AcceptAllCertificates();
                    request.timeout = GetRequestTimeoutSeconds(apiBase);

                    try
                    {
                        await request.SendWebRequest();
                    }
                    catch (UnityWebRequestException) { }

                    string response = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        HttpLink.SetResolvedBase(apiBase);
                        return (true, response);
                    }

                    lastResponse = string.IsNullOrWhiteSpace(response) == false
                        ? response
                        : FormatRequestError(apiBase, request);

                    if (ShouldTryNextBase(request, response))
                    {
                        continue;
                    }

                    HttpLink.SetResolvedBase(apiBase);
                    return (false, lastResponse);
                }
            }

            return (false, lastResponse);
        }

        private static int GetRequestTimeoutSeconds(string apiBase)
        {
            if (HttpLink.IsLocalBase(apiBase))
            {
                return LocalRequestTimeoutSeconds;
            }

            return RemoteRequestTimeoutSeconds;
        }

        private static string FormatRequestError(string apiBase, UnityWebRequest request)
        {
            return "Vehicles API is not available at " + apiBase + ": " + request.responseCode + " " + request.error;
        }

        private static bool ShouldTryNextBase(UnityWebRequest request, string response)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return true;
            }

            return request.responseCode == 0 && string.IsNullOrWhiteSpace(response);
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
