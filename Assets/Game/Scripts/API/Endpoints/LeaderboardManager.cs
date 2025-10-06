using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    /// <summary>
    /// Лідерборди:
    ///   GET /leaderboard/xp?top=10
    ///   GET /leaderboard/mmr?top=10
    /// </summary>
    public abstract class LeaderboardManager
    {
        // GET /leaderboard/xp?top=10
        public static async UniTask<(bool isSuccess, string message, LeaderboardEntry[] items)> GetTopXp(int top, string token)
        {
            string url = HttpLink.APIBase + "/leaderboard/xp?top=" + Mathf.Max(1, top);

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
                var arr = JsonHelper.FromJson<LeaderboardEntry>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<LeaderboardEntry>());
        }

        // GET /leaderboard/mmr?top=10
        public static async UniTask<(bool isSuccess, string message, LeaderboardEntry[] items)> GetTopMmr(int top, string token)
        {
            string url = HttpLink.APIBase + "/leaderboard/mmr?top=" + Mathf.Max(1, top);

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
                var arr = JsonHelper.FromJson<LeaderboardEntry>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<LeaderboardEntry>());
        }
    }

    // ----- Models -----
    [Serializable]
    public class LeaderboardEntry
    {
        public int userId;
        public string username;
        public int value; // XP або MMR залежно від ендпоїнта
    }
}
