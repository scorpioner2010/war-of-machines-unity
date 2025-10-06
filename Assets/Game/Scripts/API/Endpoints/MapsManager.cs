using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine.Networking;

namespace Game.Scripts.API.Endpoints
{
    public abstract class MapsManager
    {
        // GET /maps
        public static async UniTask<(bool isSuccess, string message, MapView[] items)> GetAll()
        {
            string url = HttpLink.APIBase + "/maps";

            var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            try { await req.SendWebRequest(); } catch (UnityWebRequestException) { }

            string resp = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;

            if (req.result == UnityWebRequest.Result.Success)
            {
                var arr = JsonHelper.FromJson<MapView>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<MapView>());
        }
    }

    [Serializable]
    public class MapView
    {
        public int id;
        public string code;
        public string name;
        public string description;
    }
}