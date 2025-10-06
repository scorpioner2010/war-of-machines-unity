using System;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Helpers;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.API
{
    /// <summary>
    /// Каталог техніки із фільтрами:
    /// GET /vehicles?faction=...&branch=...
    /// GET /vehicles/{id}
    /// GET /vehicles/by-code/{code}
    /// </summary>
    public abstract class VehiclesManager
    {
        /// <summary>
        /// Отримати список техніки. Можна фільтрувати за фракцією (код) і гілкою ("tracked"|"biped").
        /// Приклад: GetAll("iron_alliance", "tracked")
        /// </summary>
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
                hasQuery = true;
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
                // Сервер віддає масив JSON (camelCase), сумісно з нижче визначеною моделлю
                VehicleLite[] arr = JsonHelper.FromJson<VehicleLite>(resp);
                return (true, resp, arr);
            }

            return (false, resp, Array.Empty<VehicleLite>());
        }

        /// <summary>
        /// GET /vehicles/{id}
        /// </summary>
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
                // Сервер віддає одиничний об’єкт JSON (camelCase)
                VehicleLite item = JsonUtility.FromJson<VehicleLite>(resp);
                return (true, resp, item);
            }

            return (false, resp, default(VehicleLite));
        }

        /// <summary>
        /// GET /vehicles/by-code/{code}
        /// </summary>
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
    }

    // ---- Models ----
    [Serializable]
    public class VehicleLite
    {
        // Ідентифікація/категорія
        public int id;
        public string code;
        public string name;

        public string branch;       // "tracked" | "biped"
        public string factionCode;  // напр. "iron_alliance"
        public string factionName;  // англ. назва (локалізацію робимо на клієнті)

        // Бойові параметри
        public int hp;
        public int damage;
        public int penetration;

        // Гарматні/точність/кадри
        public float reloadTime;         // сек
        public float accuracy;           // 0..1 (або інша шкала — на клієнті вирішимо формат UI)
        public float aimTime;            // сек

        // Мобільність
        public float speed;              // макс. швидкість
        public float acceleration;       // прискорення
        public float traverseSpeed;      // град/с (корпус)
        public float turretTraverseSpeed;// град/с (башта)

        // Броня (рядком "Front/Side/Rear", напр. "100/50/30")
        public string turretArmor;
        public string hullArmor;

        // --- Утиліти (клієнтські), щоб не парсити у кожному місці UI ---
        /// <summary>
        /// Розбити рядок броні "F/S/R" у три числа. Повертає (0,0,0) якщо парсинг не вдався.
        /// </summary>
        public static (int front, int side, int rear) ParseArmor(string armor)
        {
            if (string.IsNullOrWhiteSpace(armor)) return (0, 0, 0);
            var parts = armor.Split('/');
            if (parts.Length != 3) return (0, 0, 0);
            int f = 0, s = 0, r = 0;
            int.TryParse(parts[0], out f);
            int.TryParse(parts[1], out s);
            int.TryParse(parts[2], out r);
            return (f, s, r);
        }
    }
}
