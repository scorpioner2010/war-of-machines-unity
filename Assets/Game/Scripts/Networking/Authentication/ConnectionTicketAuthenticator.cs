using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using FishNet.Authenticating;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using RAV.Scripts.API;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.Networking.Sessions;
using UnityEngine;
using UnityEngine.Networking;

using FishNetChannel = FishNet.Transporting.Channel;

namespace RAV.Scripts.Networking.Authentication
{
    public sealed class ConnectionTicketAuthenticator : Authenticator
    {
        private const float AuthenticationTimeoutSeconds = 10f;
        private const string RedeemEndpoint = "/connection-tickets/redeem";
        private const string RenewEndpoint = "/connection-tickets/renew";
        private const float TokenRenewalCheckIntervalSeconds = 30f;
        private const int TokenRenewalLeadMinutes = 5;
        private readonly HashSet<int> _pendingConnections = new HashSet<int>();
        private float _nextTokenRenewalCheckTime;

        public override event Action<NetworkConnection, bool> OnAuthenticationResult;

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);
            networkManager.ServerManager.RegisterBroadcast<ConnectionTicketBroadcast>(OnTicketBroadcast, false);
            networkManager.ClientManager.RegisterBroadcast<ConnectionTicketAuthenticationResponse>(OnAuthenticationResponse);
            networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }

        public override void OnRemoteConnection(NetworkConnection connection)
        {
            if (connection == null)
            {
                return;
            }

            StartCoroutine(AuthenticationTimeout(connection));
        }

        private void Update()
        {
            if (NetworkManager == null
                || !NetworkManager.IsServerStarted
                || Time.realtimeSinceStartup < _nextTokenRenewalCheckTime)
            {
                return;
            }

            _nextTokenRenewalCheckTime = Time.realtimeSinceStartup + TokenRenewalCheckIntervalSeconds;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset renewBefore = now.AddMinutes(TokenRenewalLeadMinutes);
            ServerPlayerSession[] sessions = ServerPlayerSessions.GetAuthenticatedSnapshot();
            for (int i = 0; i < sessions.Length; i++)
            {
                ServerPlayerSession session = sessions[i];
                if (session != null
                    && session.IsConnected
                    && session.TryBeginTokenRefresh(renewBefore, now))
                {
                    RefreshDelegatedTokenAsync(session).Forget();
                }
            }
        }

        private void OnClientConnectionState(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            if (NetworkManager.IsHostStarted)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                OnAuthenticationResult?.Invoke(NetworkManager.ClientManager.Connection, true);
                return;
#else
                NetworkManager.ClientManager.StopConnection();
                return;
#endif
            }

            if (ClientAuthenticationState.IsAuthenticated == false
                || string.IsNullOrWhiteSpace(ClientAuthenticationState.ConnectionTicket))
            {
                NetworkManager.ClientManager.StopConnection();
                return;
            }

            NetworkManager.ClientManager.Broadcast(new ConnectionTicketBroadcast
            {
                Ticket = ClientAuthenticationState.ConnectionTicket
            });
        }

        private void OnAuthenticationResponse(ConnectionTicketAuthenticationResponse response, FishNetChannel channel)
        {
            if (!response.Passed)
            {
                NetworkManager.ClientManager.StopConnection();
            }
        }

        private void OnTicketBroadcast(NetworkConnection connection, ConnectionTicketBroadcast broadcast, FishNetChannel channel)
        {
            if (connection == null || connection.IsAuthenticated || !_pendingConnections.Add(connection.ClientId))
            {
                connection?.Disconnect(true);
                return;
            }

            RedeemTicket(connection, broadcast.Ticket).Forget();
        }

        private async UniTaskVoid RedeemTicket(NetworkConnection connection, string ticket)
        {
            bool accepted = false;
            int userId = 0;
            Guid userSessionId = Guid.Empty;
            string delegatedAccessToken = string.Empty;
            DateTimeOffset delegatedTokenExpiresAtUtc = DateTimeOffset.MinValue;

            if (!string.IsNullOrWhiteSpace(ticket)
                && TryGetServerCredentials(out string serverId, out string credential))
            {
                var payload = new RedeemTicketRequest { ticket = ticket };
                byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));

                foreach (string apiBase in HttpLink.GetBaseCandidates())
                {
                    using (UnityWebRequest request = new UnityWebRequest(apiBase + RedeemEndpoint, UnityWebRequest.kHttpVerbPOST))
                    {
                        request.uploadHandler = new UploadHandlerRaw(body);
                        request.downloadHandler = new DownloadHandlerBuffer();
                        request.timeout = 10;
                        request.SetRequestHeader("Content-Type", "application/json");
                        request.SetRequestHeader("X-Server-Id", serverId);
                        request.SetRequestHeader("X-Server-Credential", credential);

                        try
                        {
                            await request.SendWebRequest();
                        }
                        catch (UnityWebRequestException)
                        {
                            continue;
                        }

                        if (request.result != UnityWebRequest.Result.Success)
                        {
                            continue;
                        }

                        ConnectionTicketRedemptionResponse response = JsonUtility.FromJson<ConnectionTicketRedemptionResponse>(request.downloadHandler.text);
                        if (response != null
                            && response.accepted
                            && response.userId > 0
                            && Guid.TryParse(response.sessionId, out userSessionId)
                            && string.Equals(response.serverId, serverId, StringComparison.Ordinal)
                            && string.IsNullOrWhiteSpace(response.accessToken) == false
                            && DateTimeOffset.TryParse(response.accessTokenExpiresAtUtc, out delegatedTokenExpiresAtUtc)
                            && delegatedTokenExpiresAtUtc > DateTimeOffset.UtcNow)
                        {
                            accepted = true;
                            userId = response.userId;
                            delegatedAccessToken = response.accessToken;
                            HttpLink.SetResolvedBase(apiBase);
                        }

                    }

                    if (accepted)
                    {
                        break;
                    }
                }
            }

            _pendingConnections.Remove(connection.ClientId);
            if (accepted)
            {
                ServerPlayerSessions.SetAuthenticated(connection, userId, userSessionId, delegatedAccessToken, delegatedTokenExpiresAtUtc);
            }

            NetworkManager.ServerManager.Broadcast(connection, new ConnectionTicketAuthenticationResponse
            {
                Passed = accepted
            }, false);
            OnAuthenticationResult?.Invoke(connection, accepted);

            if (!accepted)
            {
                connection.Disconnect(true);
            }
        }

        private async UniTaskVoid RefreshDelegatedTokenAsync(ServerPlayerSession session)
        {
            if (!TryGetServerCredentials(out string serverId, out string credential))
            {
                session.EndTokenRefresh(DateTimeOffset.UtcNow.AddMinutes(1));
                return;
            }

            string payload = JsonUtility.ToJson(new RenewDelegatedTokenRequest { sessionId = session.SessionId.ToString("D") });
            byte[] body = Encoding.UTF8.GetBytes(payload);
            string[] apiBases = HttpLink.GetBaseCandidates();
            long lastStatusCode = 0;
            UnityWebRequest.Result lastResult = UnityWebRequest.Result.InProgress;

            for (int i = 0; i < apiBases.Length; i++)
            {
                string apiBase = apiBases[i];
                using (UnityWebRequest request = new UnityWebRequest(apiBase + RenewEndpoint, UnityWebRequest.kHttpVerbPOST))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.timeout = 10;
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("X-Server-Id", serverId);
                    request.SetRequestHeader("X-Server-Credential", credential);

                    try
                    {
                        await request.SendWebRequest();
                    }
                    catch (UnityWebRequestException)
                    {
                        lastStatusCode = request.responseCode;
                        lastResult = request.result;
                        continue;
                    }

                    lastStatusCode = request.responseCode;
                    lastResult = request.result;
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        continue;
                    }

                    DelegatedTokenRenewalResponse response = JsonUtility.FromJson<DelegatedTokenRenewalResponse>(request.downloadHandler.text);
                    if (response == null
                        || string.IsNullOrWhiteSpace(response.accessToken)
                        || !DateTimeOffset.TryParse(response.accessTokenExpiresAtUtc, out DateTimeOffset expiresAtUtc)
                        || expiresAtUtc <= DateTimeOffset.UtcNow)
                    {
                        lastResult = UnityWebRequest.Result.DataProcessingError;
                        continue;
                    }

                    bool updated = ServerPlayerSessions.UpdateDelegatedToken(
                        session.ClientId,
                        session.SessionId,
                        response.accessToken,
                        expiresAtUtc);
                    if (updated)
                    {
                        HttpLink.SetResolvedBase(apiBase);
                        GameplayRuntimeDiagnostics.Record(
                            "server_delegated_token_renewed",
                            "user_id", session.UserId,
                            "expires_at", expiresAtUtc.ToString("O"));
                        return;
                    }

                    lastResult = UnityWebRequest.Result.DataProcessingError;
                    break;
                }
            }

            session.EndTokenRefresh(DateTimeOffset.UtcNow.AddMinutes(1));
            GameplayRuntimeDiagnostics.Record(
                "server_delegated_token_renewal_failed",
                "user_id", session.UserId,
                "status_code", lastStatusCode,
                "result", lastResult);
        }

        private IEnumerator AuthenticationTimeout(NetworkConnection connection)
        {
            yield return new WaitForSeconds(AuthenticationTimeoutSeconds);
            if (connection.IsValid && !connection.IsAuthenticated)
            {
                connection.Disconnect(true);
            }
        }

        private static string GetEnvironmentValue(string primaryName, string legacyName)
        {
            string value = Environment.GetEnvironmentVariable(primaryName);
            return string.IsNullOrWhiteSpace(value) ? Environment.GetEnvironmentVariable(legacyName) ?? string.Empty : value;
        }

        private static bool TryGetServerCredentials(out string serverId, out string credential)
        {
            serverId = GetEnvironmentValue("RAV_SERVER_ID", "WOM_SERVER_ID");
            credential = GetEnvironmentValue("RAV_SERVER_CREDENTIAL", "WOM_SERVER_CREDENTIAL");
            return string.IsNullOrWhiteSpace(serverId) == false
                   && string.IsNullOrWhiteSpace(credential) == false;
        }
    }

    public struct ConnectionTicketBroadcast : IBroadcast
    {
        public string Ticket;
    }

    public struct ConnectionTicketAuthenticationResponse : IBroadcast
    {
        public bool Passed;
    }

    [Serializable]
    public sealed class RedeemTicketRequest
    {
        public string ticket;
    }

    [Serializable]
    public sealed class ConnectionTicketRedemptionResponse
    {
        public bool accepted;
        public int userId;
        public string sessionId;
        public string serverId;
        public string accessToken;
        public string accessTokenExpiresAtUtc;
    }

    [Serializable]
    public sealed class RenewDelegatedTokenRequest
    {
        public string sessionId;
    }

    [Serializable]
    public sealed class DelegatedTokenRenewalResponse
    {
        public string accessToken;
        public string accessTokenExpiresAtUtc;
    }
}
