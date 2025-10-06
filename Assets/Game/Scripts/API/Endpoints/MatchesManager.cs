using System;
using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    /// <summary>
    /// Робота з матчами:
    ///   POST /matches/start
    ///   POST /matches/{matchId}/end
    ///   GET  /matches/{matchId}/participants
    /// </summary>
    public abstract class MatchesManager
    {
        // POST /matches/start
        public static async UniTask<(bool isSuccess, string message, int matchId)> StartMatch(string map, string token)
        {
            string url = HttpLink.APIBase + "/matches/start";
            var payload = new StartMatchRequest { map = string.IsNullOrWhiteSpace(map) ? "default_map" : map };
            string json = JsonUtility.ToJson(payload);

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                var r = JsonUtility.FromJson<StartMatchResponse>(resp);
                return (true, resp, r.matchId);
            }

            return (false, resp, 0);
        }

        // POST /matches/{matchId}/end
        // Клієнт відправляє тільки "сирі" дані. XP/MMR/Bolts рахує сервер.
        public static async UniTask<(bool isSuccess, string message)> EndMatch(int matchId, ParticipantInput[] participants, string token)
        {
            string url = HttpLink.APIBase + "/matches/" + matchId + "/end";

            var body = new EndMatchRequest { participants = participants ?? Array.Empty<ParticipantInput>() };
            string json = JsonUtility.ToJson(body);

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + token);

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            return (req.result == UnityWebRequest.Result.Success, resp);
        }

        // GET /matches/{matchId}/participants
        public static async UniTask<(bool isSuccess, string message, MatchParticipantView[] items)> GetParticipants(int matchId, string token)
        {
            string url = HttpLink.APIBase + "/matches/" + matchId + "/participants";

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
                var arr = JsonHelper.FromJson<MatchParticipantView>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<MatchParticipantView>());
        }
    }

    // ===== Models (мають відповідати серверним контрактам) =====

    [Serializable]
    public class StartMatchRequest
    {
        public string map;
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

    /// <summary>
    /// "win" | "lose" | "draw"
    /// Значення поза цими трьома сервер трактує як "lose".
    /// </summary>
    [Serializable]
    public class ParticipantInput
    {
        public int userId;
        public int vehicleId;
        public int team;
        public string result; // "win"/"lose"/"draw"
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
        public int mmrDelta;
    }
}
