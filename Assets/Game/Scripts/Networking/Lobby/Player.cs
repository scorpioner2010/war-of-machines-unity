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
        public TankRoot playerRoot;
        public bool isBot;
        public bool randomPlayerConnected; //for random game
    }
}