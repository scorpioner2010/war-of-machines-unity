using FishNet.Connection;
using Game.Scripts.API.Models;

namespace Game.Scripts.Networking.Sessions
{
    public class ServerPlayerSession
    {
        public int ClientId { get; }
        public NetworkConnection Connection { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public PlayerProfile Profile { get; private set; }

        public bool IsConnected => Connection != null;
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);
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

        public void SetToken(string token)
        {
            Token = token ?? string.Empty;
        }

        public void SetProfile(PlayerProfile profile)
        {
            Profile = profile;
        }

        public void ClearAuth()
        {
            Token = string.Empty;
            Profile = null;
        }
    }
}
