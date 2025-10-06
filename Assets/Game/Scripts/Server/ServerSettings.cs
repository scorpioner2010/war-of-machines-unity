using UnityEngine;

namespace Game.Scripts.Server
{
    public class ServerSettings : MonoBehaviour
    {
        public static ServerSettings In;
        
        public bool isTestMode;
        public int maxPlayersForFindRoom = 1;
        public int findRoomSeconds = 60;
        
        private void Awake() => In = this;
    }
}