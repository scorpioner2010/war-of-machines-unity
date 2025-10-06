using Game.Scripts.API.Models;

namespace Game.Scripts.Player.Data
{
    public class PlayerClientInfo : IPlayerClientInfo
    {
        public PlayerProfile Profile { get; private set; } = new ();
        public int ClientId { get; private set; }
        public string RoomId { get; private set; }

        public void SetPlayerData(PlayerProfile playerData)
        {
            Profile = playerData;
        }

        public void SetClientId(int clientId)
        {
            ClientId = clientId;
        }

        public void SetRoomId(string roomId)
        {
            RoomId = roomId;
        }
    }
}