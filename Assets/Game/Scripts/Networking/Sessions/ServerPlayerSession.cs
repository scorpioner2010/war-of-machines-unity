using System;
using FishNet.Connection;
using RAV.Scripts.API.Models;

namespace RAV.Scripts.Networking.Sessions
{
    public class ServerPlayerSession
    {
        public int ClientId { get; }
        public NetworkConnection Connection { get; private set; }
        public int UserId { get; private set; }
        public Guid SessionId { get; private set; }
        public string Token { get; private set; } = string.Empty;
        public DateTimeOffset TokenExpiresAtUtc { get; private set; }
        public bool TokenRefreshInProgress { get; private set; }
        private DateTimeOffset _nextTokenRefreshAttemptUtc;
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

        public void SetAuthenticated(int userId, Guid sessionId, string delegatedToken, DateTimeOffset tokenExpiresAtUtc)
        {
            if (userId <= 0 || sessionId == Guid.Empty)
            {
                ClearAuth();
                return;
            }

            UserId = userId;
            SessionId = sessionId;
            Token = delegatedToken ?? string.Empty;
            TokenExpiresAtUtc = tokenExpiresAtUtc;
            TokenRefreshInProgress = false;
            _nextTokenRefreshAttemptUtc = DateTimeOffset.MinValue;
        }

        public bool TryBeginTokenRefresh(DateTimeOffset renewBeforeUtc, DateTimeOffset nowUtc)
        {
            if (!IsLoggedIn
                || TokenExpiresAtUtc > renewBeforeUtc
                || TokenRefreshInProgress
                || _nextTokenRefreshAttemptUtc > nowUtc)
            {
                return false;
            }

            TokenRefreshInProgress = true;
            return true;
        }

        public void EndTokenRefresh(DateTimeOffset retryAfterUtc)
        {
            TokenRefreshInProgress = false;
            _nextTokenRefreshAttemptUtc = retryAfterUtc;
        }

        public void SetDelegatedToken(string delegatedToken, DateTimeOffset tokenExpiresAtUtc)
        {
            Token = delegatedToken ?? string.Empty;
            TokenExpiresAtUtc = tokenExpiresAtUtc;
            TokenRefreshInProgress = false;
            _nextTokenRefreshAttemptUtc = DateTimeOffset.MinValue;
        }

        public void SetProfile(PlayerProfile profile)
        {
            Profile = profile;
        }

        public void ClearAuth()
        {
            UserId = 0;
            SessionId = Guid.Empty;
            Token = string.Empty;
            TokenExpiresAtUtc = DateTimeOffset.MinValue;
            TokenRefreshInProgress = false;
            _nextTokenRefreshAttemptUtc = DateTimeOffset.MinValue;
            Profile = null;
        }
    }
}
