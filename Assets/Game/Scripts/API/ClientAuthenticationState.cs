namespace Game.Scripts.API
{
    public static class ClientAuthenticationState
    {
        public static string AccessToken { get; private set; } = string.Empty;
        public static string RefreshToken { get; private set; } = string.Empty;
        public static string ConnectionTicket { get; private set; } = string.Empty;
        public static string ConnectionServerId { get; private set; } = string.Empty;

        public static System.DateTimeOffset? AccessTokenExpiresAtUtc { get; private set; }

        public static bool IsAuthenticated => string.IsNullOrWhiteSpace(AccessToken) == false
                                              && (!AccessTokenExpiresAtUtc.HasValue
                                                  || AccessTokenExpiresAtUtc.Value > System.DateTimeOffset.UtcNow);

        public static void SetSession(string accessToken, string refreshToken, string expiresAtUtc)
        {
            AccessToken = accessToken ?? string.Empty;
            RefreshToken = refreshToken ?? string.Empty;
            AccessTokenExpiresAtUtc = System.DateTimeOffset.TryParse(expiresAtUtc, out System.DateTimeOffset expires)
                ? expires
                : null;
            ClearConnectionTicket();
        }

        public static void SetConnectionTicket(string serverId, string ticket)
        {
            ConnectionServerId = serverId ?? string.Empty;
            ConnectionTicket = ticket ?? string.Empty;
        }

        public static void ClearConnectionTicket()
        {
            ConnectionServerId = string.Empty;
            ConnectionTicket = string.Empty;
        }

        public static void Clear()
        {
            AccessToken = string.Empty;
            RefreshToken = string.Empty;
            AccessTokenExpiresAtUtc = null;
            ClearConnectionTicket();
        }
    }
}
