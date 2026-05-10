using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.Server
{
    public class ServerSettings : MonoBehaviour
    {
        public static ServerSettings In;
        
        public int maxPlayersForFindRoom = 1;
        public int findRoomSeconds = 60;
        public GunDispersionGlobalSettings gunDispersion = new GunDispersionGlobalSettings();
        
        private void Awake()
        {
            In = this;
        }

        public static int GetMaxPlayersForFindRoom()
        {
            if (In == null || In.maxPlayersForFindRoom <= 0)
            {
                return 1;
            }

            return In.maxPlayersForFindRoom;
        }

        public static int GetFindRoomSeconds()
        {
            if (In == null || In.findRoomSeconds <= 0)
            {
                return 60;
            }

            return In.findRoomSeconds;
        }

        public static GunDispersionGlobalSettings GetGunDispersion()
        {
            if (In == null || In.gunDispersion == null)
            {
                return GunDispersionGlobalSettings.Default;
            }

            return In.gunDispersion;
        }
    }
}
