using FishNet.Connection;

namespace Game.Scripts.Networking.Lobby
{
    public struct PlayerBattleResult
    {
        public string RoomId;
        public string Result;
        public int Kills;
        public int Damage;
        public int XpEarned;
        public int Bolts;
        public int FreeXp;
        public int MmrDelta;
        public bool RewardsAvailable;
    }

    public struct PlayerBattleResultDelivery
    {
        public int UserId;
        public NetworkConnection Connection;
        public PlayerBattleResult Result;
    }
}
