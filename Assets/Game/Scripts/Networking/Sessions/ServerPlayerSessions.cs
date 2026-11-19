using System.Collections.Generic;
using FishNet.Connection;
using Game.Scripts.API.Models;

namespace Game.Scripts.Networking.Sessions
{
    public static class ServerPlayerSessions
    {
        private static readonly Dictionary<int, ServerPlayerSession> SessionsByClientId = new();

        public static ServerPlayerSession UpsertConnection(NetworkConnection connection)
        {
            if (connection == null)
            {
                return null;
            }

            int clientId = connection.ClientId;
            if (SessionsByClientId.TryGetValue(clientId, out ServerPlayerSession session))
            {
                session.UpdateConnection(connection);
                return session;
            }

            session = new ServerPlayerSession(connection);
            SessionsByClientId.Add(clientId, session);
            return session;
        }

        public static bool TryGet(int clientId, out ServerPlayerSession session)
        {
            return SessionsByClientId.TryGetValue(clientId, out session);
        }

        public static bool TryGet(NetworkConnection connection, out ServerPlayerSession session)
        {
            session = null;
            if (connection == null)
            {
                return false;
            }

            return TryGet(connection.ClientId, out session);
        }

        public static bool TryGetAuthenticated(NetworkConnection connection, out ServerPlayerSession session)
        {
            if (connection == null || !connection.IsAuthenticated || !TryGet(connection, out session))
            {
                session = null;
                return false;
            }

            return session.IsAuthenticated;
        }

        public static void SetAuthenticated(NetworkConnection connection, int userId, string delegatedToken)
        {
            ServerPlayerSession session = UpsertConnection(connection);
            if (session != null)
            {
                session.SetAuthenticated(userId, delegatedToken);
            }
        }

        public static void SetProfile(NetworkConnection connection, PlayerProfile profile)
        {
            ServerPlayerSession session = UpsertConnection(connection);
            if (session != null)
            {
                session.SetProfile(profile);
            }
        }

        public static PlayerProfile GetProfile(int clientId)
        {
            return TryGet(clientId, out ServerPlayerSession session) ? session.Profile : null;
        }

        public static NetworkConnection GetConnectionByUserId(int userId)
        {
            foreach (ServerPlayerSession session in SessionsByClientId.Values)
            {
                if (session != null
                    && session.Connection != null
                    && session.Profile != null
                    && session.Profile.id == userId)
                {
                    return session.Connection;
                }
            }

            return null;
        }

        public static void ClearAuth(int clientId)
        {
            if (TryGet(clientId, out ServerPlayerSession session))
            {
                session.ClearAuth();
            }
        }

        public static void Remove(int clientId)
        {
            SessionsByClientId.Remove(clientId);
        }

        public static void Clear()
        {
            SessionsByClientId.Clear();
        }
    }
}
