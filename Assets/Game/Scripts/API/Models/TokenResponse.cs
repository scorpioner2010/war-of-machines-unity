namespace Game.Scripts.API.Models
{
    [System.Serializable]
    public class TokenResponse
    {
        public string token;
        public string refreshToken;
        public string expiresAtUtc;
    }
}
