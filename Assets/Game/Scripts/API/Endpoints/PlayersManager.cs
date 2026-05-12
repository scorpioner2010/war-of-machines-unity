using System.Text;
using Cysharp.Threading.Tasks;
using Game.Scripts.API;
using Game.Scripts.API.Helpers;
using Game.Scripts.API.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts
{
    public abstract class PlayersManager
    {
        public static async UniTask<(bool isSuccess, string message, PlayerProfile profile)> GetMyProfile(string token)
        {
            string url = HttpLink.APIBase + "/players/me";

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
                PlayerProfile profile = JsonUtility.FromJson<PlayerProfile>(resp);
                return (true, resp, profile);
            }

            return (false, resp, default);
        }

        public static async UniTask<(bool isSuccess, string message)> SetActiveVehicle(int vehicleId, string token)
        {
            string url = HttpLink.APIBase + "/players/me/active/" + vehicleId;

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
    }
}

namespace Game.Scripts.API.Models
{
    [System.Serializable]
    public class PlayerProfile
    {
        public int id;
        public string username;
        public bool isAdmin;
        public int mmr;
        public int bolts;
        public int adamant;
        public int freeXp;

        public int activeVehicleId;
        public string activeVehicleCode;
        public string activeVehicleName;

        public OwnedVehicleDto[] ownedVehicles;
        public ResearchedVehicleDto[] researchedVehicles;

        public OwnedVehicleDto GetSelected()
        {
            if (ownedVehicles == null || ownedVehicles.Length == 0)
            {
                return null;
            }

            foreach (OwnedVehicleDto dto in ownedVehicles)
            {
                if (activeVehicleId == dto.vehicleId)
                {
                    return dto;
                }
            }

            foreach (OwnedVehicleDto dto in ownedVehicles)
            {
                if (dto != null && dto.isActive)
                {
                    return dto;
                }
            }

            return ownedVehicles[0];
        }

        public bool IsHave(int idVehicle)
        {
            if (ownedVehicles == null)
            {
                return false;
            }

            foreach (OwnedVehicleDto vehicleDto in ownedVehicles)
            {
                if (vehicleDto.vehicleId == idVehicle)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsResearched(int idVehicle)
        {
            if (IsHave(idVehicle))
            {
                return true;
            }

            if (researchedVehicles == null)
            {
                return false;
            }

            for (int i = 0; i < researchedVehicles.Length; i++)
            {
                ResearchedVehicleDto dto = researchedVehicles[i];
                if (dto != null && dto.vehicleId == idVehicle)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [System.Serializable]
    public class OwnedVehicleDto
    {
        public int vehicleId;
        public string code;
        public string name;
        public bool isActive;
        public int xp;
        public bool isResearched;
    }

    [System.Serializable]
    public class ResearchedVehicleDto
    {
        public int vehicleId;
        public string code;
        public string name;
    }
}
