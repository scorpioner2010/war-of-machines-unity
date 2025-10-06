using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API;
using Game.Scripts.API.Helpers;
using Game.Scripts.API.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts
{
    /// <summary>
    /// Робота з ендпоїнтами PlayersController:
    /// GET /players/me
    /// PUT /players/me/active/{vehicleId}
    /// </summary>
    public abstract class PlayersManager
    {
        /// <summary>
        /// Отримати профіль поточного гравця за JWT.
        /// </summary>
        public static async UniTask<(bool isSuccess, string message, PlayerProfile profile)> GetMyProfile(string token)
        {
            string url = HttpLink.APIBase + "/players/me";

            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            request.SetRequestHeader("Authorization", "Bearer " + token);

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException) { }

            string resp = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(resp);
                return (true, resp, profile);
            }

            return (false, resp, default(PlayerProfile));
        }

        /// <summary>
        /// Виставити активну техніку (короткий шлях через PlayersController).
        /// </summary>
        public static async UniTask<(bool isSuccess, string message)> SetActiveVehicle(int vehicleId, string token)
        {
            string url = HttpLink.APIBase + "/players/me/active/" + vehicleId;

            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}")),
                downloadHandler = new DownloadHandlerBuffer(),
                certificateHandler = new AcceptAllCertificates()
            };

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            try
            {
                await request.SendWebRequest();
            }
            catch (UnityWebRequestException) { }

            string resp = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result == UnityWebRequest.Result.Success)
            {
                return (true, resp);
            }

            return (false, resp);
        }
    }
}

namespace Game.Scripts.API.Models
{
    /// <summary>
    /// DTO під відповіді /players/me (має збігатися з серверним PlayerProfileDto).
    /// Примітка: JsonUtility не підтримує nullable, тож якщо на сервері null — тут буде 0/порожньо.
    /// </summary>
    [System.Serializable]
    public class PlayerProfile
    {
        public int id;
        public string username;
        public bool isAdmin;
        public int xpTotal;
        public int mmr;
        public int bolts;
        public int adamant;

        public int activeVehicleId;        // якщо на сервері null, тут буде 0
        public string activeVehicleCode;   // може бути null/empty
        public string activeVehicleName;   // може бути null/empty

        public OwnedVehicleDto[] ownedVehicles;
    }

    [System.Serializable]
    public class OwnedVehicleDto
    {
        public int vehicleId;
        public string code;
        public string name;
        public bool isActive;
    }
}
