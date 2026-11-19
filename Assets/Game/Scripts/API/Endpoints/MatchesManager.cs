using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public abstract class MatchesManager
    {
        public static async UniTask<(bool isSuccess, string message, int matchId)> StartMatchAsDedicatedServer(
            string map,
            MatchRosterInput[] participants)
        {
            if (!TryGetServerCredentials(out string serverId, out string credential))
            {
                return (false, "Dedicated server credentials are not configured.", 0);
            }

            string url = GetApiBase() + "/matches/start";
            var payload = new StartMatchRequest
            {
                map = string.IsNullOrWhiteSpace(map) ? "default_map" : map,
                participants = participants ?? Array.Empty<MatchRosterInput>()
            };
            string json = JsonUtility.ToJson(payload);

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            SetServerHeaders(req, serverId, credential);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                var r = JsonUtility.FromJson<StartMatchResponse>(resp);
                return (true, resp, r.matchId);
            }

            return (false, resp, 0);
        }

        public static async UniTask<(bool isSuccess, string message, EndMatchResponse response)> EndMatchAsDedicatedServer(
            int matchId,
            ParticipantInput[] participants)
        {
            if (!TryGetServerCredentials(out string serverId, out string credential))
            {
                return (false, "Dedicated server credentials are not configured.", null);
            }

            string url = GetApiBase() + "/matches/" + matchId + "/end";

            var body = new EndMatchRequest { participants = participants ?? Array.Empty<ParticipantInput>() };
            string json = JsonUtility.ToJson(body);

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            SetServerHeaders(req, serverId, credential);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            if (req.result == UnityWebRequest.Result.Success)
            {
                EndMatchResponse response = null;
                string trimmed = string.IsNullOrWhiteSpace(resp) ? string.Empty : resp.TrimStart();
                if (trimmed.StartsWith("{"))
                {
                    response = JsonUtility.FromJson<EndMatchResponse>(trimmed);
                }

                return (true, resp, response);
            }

            return (false, resp, null);
        }

        public static async UniTask<(bool isSuccess, string message, MatchParticipantView[] items)> GetParticipants(int matchId, string token)
        {
            string url = HttpLink.APIBase + "/matches/" + matchId + "/participants";

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                var arr = JsonHelper.FromJson<MatchParticipantView>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<MatchParticipantView>());
        }

        private static string GetApiBase()
        {
            string[] candidates = HttpLink.GetBaseCandidates();
            return candidates.Length > 0 ? candidates[0] : HttpLink.APIBase;
        }

        private static bool TryGetServerCredentials(out string serverId, out string credential)
        {
            serverId = Environment.GetEnvironmentVariable("WOM_SERVER_ID") ?? string.Empty;
            credential = Environment.GetEnvironmentVariable("WOM_SERVER_CREDENTIAL") ?? string.Empty;
            return string.IsNullOrWhiteSpace(serverId) == false
                   && string.IsNullOrWhiteSpace(credential) == false;
        }

        private static void SetServerHeaders(UnityWebRequest request, string serverId, string credential)
        {
            request.SetRequestHeader("X-Server-Id", serverId);
            request.SetRequestHeader("X-Server-Credential", credential);
        }
    }

    [Serializable]
    public class StartMatchRequest
    {
        public string map;
        public MatchRosterInput[] participants;
    }

    [Serializable]
    public class MatchRosterInput
    {
        public int userId;
        public int vehicleId;
        public int team;
    }

    [Serializable]
    public class StartMatchResponse
    {
        public int matchId;
    }

    [Serializable]
    public class EndMatchRequest
    {
        public ParticipantInput[] participants;
    }

    [Serializable]
    public class EndMatchResponse
    {
        public MatchParticipantView[] participants;
    }

    [Serializable]
    public class ParticipantInput
    {
        public int userId;
        public int vehicleId;
        public int team;
        public string result;
        public int kills;
        public int damage;
    }

    [Serializable]
    public class MatchParticipantView
    {
        public int userId;
        public string username;
        public int vehicleId;
        public string vehicleName;
        public int team;
        public string result;
        public int kills;
        public int damage;
        public int xpEarned;
        public int bolts;
        public int freeXp;
        public int mmrDelta;
        public bool rewardsCalculated;
    }
}
