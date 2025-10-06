using System;
using System.Collections.Generic;

namespace Game.Scripts.Networking.Lobby
{
    [Serializable]
    public class ClientRoom
    {
        public string roomId;
        public string roomName;
        public int maxPlayers;
        public string selectedLocation;
        public bool isInGame;
        public DateTime CreatedTime;
        public string loadedSceneName;
        public int handle;
        public List<Player> players = new();
        public bool isAutoRoom;
        
        public string GetPlayerCountText()
        {
            return $"{players.Count}/{maxPlayers}";
        }
    }
}