using FishNet.Connection;
using Game.Scripts.API.Models;

namespace Game.Scripts.Networking.Sessions
{
    public class ServerPlayerSession
    {
        public int ClientId { get; }
        public NetworkConnection Connection { get; private set; }
        public int UserId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public PlayerProfile Profile { get; private set; }

        public bool IsConnected => Connection != null;
        public bool IsAuthenticated => UserId > 0;
        public bool IsLoggedIn => IsAuthenticated && !string.IsNullOrEmpty(Token);
        public bool HasProfile => Profile != null;

        public ServerPlayerSession(NetworkConnection connection)
        {
            ClientId = connection.ClientId;
            Connection = connection;
        }

        public void UpdateConnection(NetworkConnection connection)
        {
            Connection = connection;
        }

        public void SetAuthenticated(int userId, string delegatedToken)
        {
            if (userId <= 0)
            {
                ClearAuth();
                return;
            }

            UserId = userId;
            Token = delegatedToken ?? string.Empty;
        }

        public void SetProfile(PlayerProfile profile)
        {
            Profile = profile;
        }

        public void ClearAuth()
        {
            UserId = 0;
            Token = string.Empty;
            Profile = null;
        }
    }
}
