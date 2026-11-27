using System;
using System.Diagnostics;
using System.Text;
using Cysharp.Threading.Tasks;
using RAV.Scripts.API.Helpers;
using RAV.Scripts.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;

namespace RAV.Scripts.API.Endpoints
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
            string requestId = Guid.NewGuid().ToString("N");
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
            req.SetRequestHeader("X-RAV-Request-Id", requestId);
            SetServerHeaders(req, serverId, credential);

            Stopwatch stopwatch = Stopwatch.StartNew();
            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }
            stopwatch.Stop();

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            RecordApiResult("dedicated_match_start_http_completed", requestId, req, stopwatch.ElapsedMilliseconds);

            if (req.result == UnityWebRequest.Result.Success)
            {
                var r = JsonUtility.FromJson<StartMatchResponse>(resp);
                return (true, resp, r.matchId);
            }

            return (false, HttpLink.FormatApiFailure(req), 0);
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
            string requestId = Guid.NewGuid().ToString("N");

            var body = new EndMatchRequest { participants = participants ?? Array.Empty<ParticipantInput>() };
            string json = JsonUtility.ToJson(body);

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-RAV-Request-Id", requestId);
            SetServerHeaders(req, serverId, credential);

            Stopwatch stopwatch = Stopwatch.StartNew();
            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }
            stopwatch.Stop();

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            RecordApiResult("dedicated_match_end_http_completed", requestId, req, stopwatch.ElapsedMilliseconds, "match_id", matchId);
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

            return (false, HttpLink.FormatApiFailure(req), null);
        }

        public static async UniTask<(bool isSuccess, string message, MatchParticipantView[] items)> GetParticipants(int matchId, string token)
        {
            string url = HttpLink.APIBase + "/matches/" + matchId + "/participants";
            string requestId = Guid.NewGuid().ToString("N");

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("X-RAV-Request-Id", requestId);

            Stopwatch stopwatch = Stopwatch.StartNew();
            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }
            stopwatch.Stop();

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
            RecordApiResult("match_participants_http_completed", requestId, req, stopwatch.ElapsedMilliseconds, "match_id", matchId);

            if (req.result == UnityWebRequest.Result.Success)
            {
                var arr = JsonHelper.FromJson<MatchParticipantView>(resp);
                return (true, resp, arr);
            }

            return (false, HttpLink.FormatApiFailure(req), Array.Empty<MatchParticipantView>());
        }

        private static string GetApiBase()
        {
            string[] candidates = HttpLink.GetBaseCandidates();
            return candidates.Length > 0 ? candidates[0] : HttpLink.APIBase;
        }

        private static void RecordApiResult(
            string eventName,
            string requestId,
            UnityWebRequest request,
            long durationMilliseconds,
            string additionalKey = null,
            object additionalValue = null)
        {
            if (string.IsNullOrWhiteSpace(additionalKey))
            {
                GameplayRuntimeDiagnostics.Record(
                    eventName,
                    "request_id", requestId,
                    "status_code", request.responseCode,
                    "result", request.result,
                    "duration_ms", durationMilliseconds,
                    "response_bytes", request.downloadHandler?.data?.Length ?? 0,
                    "error_category", request.result == UnityWebRequest.Result.Success ? "none" : GetErrorCategory(request));
                return;
            }

            GameplayRuntimeDiagnostics.Record(
                eventName,
                "request_id", requestId,
                "status_code", request.responseCode,
                "result", request.result,
                "duration_ms", durationMilliseconds,
                "response_bytes", request.downloadHandler?.data?.Length ?? 0,
                "error_category", request.result == UnityWebRequest.Result.Success ? "none" : GetErrorCategory(request),
                additionalKey, additionalValue);
        }

        private static string GetErrorCategory(UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                return !string.IsNullOrWhiteSpace(request.error)
                       && request.error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "timeout"
                    : "connection_error";
            }

            if (request.result == UnityWebRequest.Result.ProtocolError)
            {
                return "http_error";
            }

            if (request.result == UnityWebRequest.Result.DataProcessingError)
            {
                return "response_processing_error";
            }

            return "unknown";
        }

        private static string GetEnvironmentValue(string primaryName, string legacyName)
        {
            string value = Environment.GetEnvironmentVariable(primaryName);
            return string.IsNullOrWhiteSpace(value) ? Environment.GetEnvironmentVariable(legacyName) ?? string.Empty : value;
        }

        private static bool TryGetServerCredentials(out string serverId, out string credential)
        {
            serverId = GetEnvironmentValue("RAV_SERVER_ID", "WOM_SERVER_ID");
            credential = GetEnvironmentValue("RAV_SERVER_CREDENTIAL", "WOM_SERVER_CREDENTIAL");
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
