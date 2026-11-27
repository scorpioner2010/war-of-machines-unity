using System;
using System.Collections;
using System.Text;
using RAV.Scripts.API.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace RAV.Scripts.API
{
    public static class ClientAuthenticationState
    {
        private const int RefreshBeforeExpirySeconds = 60;
        private static bool _refreshInProgress;
        private static bool _lastRefreshSucceeded;
        private static string _lastRefreshError = string.Empty;

        public static string AccessToken { get; private set; } = string.Empty;
        public static string RefreshToken { get; private set; } = string.Empty;
        public static string ConnectionTicket { get; private set; } = string.Empty;
        public static string ConnectionServerId { get; private set; } = string.Empty;

        public static System.DateTimeOffset? AccessTokenExpiresAtUtc { get; private set; }

        public static bool IsAuthenticated => string.IsNullOrWhiteSpace(AccessToken) == false
                                              && (!AccessTokenExpiresAtUtc.HasValue
                                                  || AccessTokenExpiresAtUtc.Value > System.DateTimeOffset.UtcNow);
        public static bool HasAuthenticationSession => IsAuthenticated || !string.IsNullOrWhiteSpace(RefreshToken);

        public static void SetSession(string accessToken, string refreshToken, string expiresAtUtc)
        {
            AccessToken = accessToken ?? string.Empty;
            RefreshToken = refreshToken ?? string.Empty;
            AccessTokenExpiresAtUtc = System.DateTimeOffset.TryParse(expiresAtUtc, out System.DateTimeOffset expires)
                ? expires
                : null;
            ClearConnectionTicket();
        }

        public static IEnumerator EnsureValidAccessToken(Action<bool, string> onComplete, bool forceRefresh = false)
        {
            if (!forceRefresh
                && !string.IsNullOrWhiteSpace(AccessToken)
                && (!AccessTokenExpiresAtUtc.HasValue
                    || AccessTokenExpiresAtUtc.Value > DateTimeOffset.UtcNow.AddSeconds(RefreshBeforeExpirySeconds)))
            {
                onComplete?.Invoke(true, string.Empty);
                yield break;
            }

            if (_refreshInProgress)
            {
                while (_refreshInProgress)
                {
                    yield return null;
                }

                bool refreshedTokenUsable = !string.IsNullOrWhiteSpace(AccessToken)
                                            && AccessTokenExpiresAtUtc.HasValue
                                            && AccessTokenExpiresAtUtc.Value > DateTimeOffset.UtcNow.AddSeconds(RefreshBeforeExpirySeconds);
                if (refreshedTokenUsable && _lastRefreshSucceeded)
                {
                    onComplete?.Invoke(true, string.Empty);
                    yield break;
                }

                if (!forceRefresh && !string.IsNullOrWhiteSpace(AccessToken)
                                   && AccessTokenExpiresAtUtc.HasValue
                                   && AccessTokenExpiresAtUtc.Value > DateTimeOffset.UtcNow)
                {
                    onComplete?.Invoke(true, string.Empty);
                    yield break;
                }

                onComplete?.Invoke(false, string.IsNullOrWhiteSpace(_lastRefreshError)
                    ? "The sign-in session could not be refreshed."
                    : _lastRefreshError);
                yield break;
            }

            if (string.IsNullOrWhiteSpace(RefreshToken))
            {
                onComplete?.Invoke(false, "Your sign-in session has expired. Log in again to continue.");
                yield break;
            }

            string[] apiBases = HttpLink.GetBaseCandidates();
            if (apiBases == null || apiBases.Length == 0)
            {
                onComplete?.Invoke(false, "No valid API endpoint is configured for this environment.");
                yield break;
            }

            _refreshInProgress = true;
            _lastRefreshSucceeded = false;
            _lastRefreshError = string.Empty;
            string requestBody = JsonUtility.ToJson(new RefreshRequest { refreshToken = RefreshToken });
            string lastError = "The sign-in session could not be refreshed.";
            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                using (UnityWebRequest request = new UnityWebRequest(apiBase + "/auth/refresh", UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(requestBody));
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = 10;
                    request.SetRequestHeader("Content-Type", "application/json");

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        TokenResponse response = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                        if (response == null
                            || string.IsNullOrWhiteSpace(response.token)
                            || string.IsNullOrWhiteSpace(response.refreshToken)
                            || !DateTimeOffset.TryParse(response.expiresAtUtc, out DateTimeOffset expiresAtUtc)
                            || expiresAtUtc <= DateTimeOffset.UtcNow)
                        {
                            lastError = "The API returned an invalid refreshed sign-in session.";
                            continue;
                        }

                        SetSession(response.token, response.refreshToken, response.expiresAtUtc);
                        HttpLink.SetResolvedBase(apiBase);
                        _lastRefreshSucceeded = true;
                        _refreshInProgress = false;
                        onComplete?.Invoke(true, string.Empty);
                        yield break;
                    }

                    if (request.responseCode == 401)
                    {
                        Clear();
                        _lastRefreshError = "Your sign-in session has expired. Log in again to continue.";
                        _refreshInProgress = false;
                        onComplete?.Invoke(false, "Your sign-in session has expired. Log in again to continue.");
                        yield break;
                    }

                    lastError = HttpLink.FormatApiFailure(request);
                }
            }

            bool currentTokenStillValid = !string.IsNullOrWhiteSpace(AccessToken)
                                          && AccessTokenExpiresAtUtc.HasValue
                                          && AccessTokenExpiresAtUtc.Value > DateTimeOffset.UtcNow;
            _lastRefreshError = lastError;
            _refreshInProgress = false;
            onComplete?.Invoke(currentTokenStillValid, currentTokenStillValid ? string.Empty : lastError);
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
            _lastRefreshSucceeded = false;
            _lastRefreshError = string.Empty;
        }

        [Serializable]
        private sealed class RefreshRequest
        {
            public string refreshToken;
        }
    }
}
