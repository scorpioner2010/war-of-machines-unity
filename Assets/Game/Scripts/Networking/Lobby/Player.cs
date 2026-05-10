using System;
using FishNet.Connection;
using Game.Scripts.Gameplay.Robots;

namespace Game.Scripts.Networking.Lobby
{
    [Serializable]
    public class Player
    {
        public string loginName;
        public NetworkConnection Connection;
        public VehicleRoot playerRoot;
        public string token;
        public int userId;
        public int mmr;
        public int activeVehicleId;
        public string activeVehicleCode;
        public MatchTeam team;
        public bool isBot;
        public bool randomPlayerConnected; //for random game
        public bool leftBattle;
        public string battleResult;
        public int kills;
        public int damage;
    }
}
