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
using Game.Scripts.API;
using Game.Scripts.Networking.Sessions;
using UnityEngine;
using UnityEngine.Networking;

using FishNetChannel = FishNet.Transporting.Channel;

namespace Game.Scripts.Networking.Authentication
{
    public sealed class ConnectionTicketAuthenticator : Authenticator
    {
        private const float AuthenticationTimeoutSeconds = 10f;
        private const string RedeemEndpoint = "/connection-tickets/redeem";
        private readonly HashSet<int> _pendingConnections = new HashSet<int>();

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
            string delegatedAccessToken = string.Empty;

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
                            && string.Equals(response.serverId, serverId, StringComparison.Ordinal)
                            && string.IsNullOrWhiteSpace(response.accessToken) == false)
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
                ServerPlayerSessions.SetAuthenticated(connection, userId, delegatedAccessToken);
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

        private IEnumerator AuthenticationTimeout(NetworkConnection connection)
        {
            yield return new WaitForSeconds(AuthenticationTimeoutSeconds);
            if (connection.IsValid && !connection.IsAuthenticated)
            {
                connection.Disconnect(true);
            }
        }

        private static bool TryGetServerCredentials(out string serverId, out string credential)
        {
            serverId = Environment.GetEnvironmentVariable("WOM_SERVER_ID") ?? string.Empty;
            credential = Environment.GetEnvironmentVariable("WOM_SERVER_CREDENTIAL") ?? string.Empty;
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
    }
}
