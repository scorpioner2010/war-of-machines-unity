using Game.Scripts.API.Models;

namespace Game.Scripts.Player.Data
{
    public interface IPlayerClientInfo
    {
        public PlayerProfile Profile { get;}
        public int ClientId { get; }
        
        public void SetPlayerData(PlayerProfile profile);
        public void SetClientId(int clientId);
    }
}