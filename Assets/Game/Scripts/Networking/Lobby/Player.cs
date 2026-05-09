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
        public int userId;
        public int mmr;
        public MatchTeam team;
        public bool isBot;
        public bool randomPlayerConnected; //for random game
    }
}
