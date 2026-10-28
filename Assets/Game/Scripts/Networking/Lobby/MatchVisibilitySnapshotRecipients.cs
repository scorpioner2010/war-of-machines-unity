using System;
using FishNet.Connection;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public struct MatchVisibilitySnapshot
    {
        public int Version;
        public int Count;
        public int[] ObjectIds;
        public byte[] Relations;
        public Vector3[] Positions;
        public float[] Yaws;
        public float[] RemainingVisibilitySeconds;
    }

    public sealed class MatchVisibilitySnapshotRecipients
    {
        public void SendSnapshots(
            ServerRoom room,
            IMatchVisibilityUpdateSink updateSink,
            MatchVisibilitySnapshot teamASnapshot,
            MatchVisibilitySnapshot teamBSnapshot)
        {
            if (room == null || updateSink == null)
            {
                return;
            }

            for (int i = 0; i < room.players.Count; i++)
            {
                Player player = room.players[i];
                if (!CanReceive(player))
                {
                    continue;
                }

                if (!TryGetSnapshot(player, teamASnapshot, teamBSnapshot, out MatchVisibilitySnapshot snapshot))
                {
                    continue;
                }

                Send(updateSink, player.Connection, snapshot);
            }
        }

        public void SendEmptySnapshots(
            ServerRoom room,
            IMatchVisibilityUpdateSink updateSink,
            int version)
        {
            if (room == null || updateSink == null)
            {
                return;
            }

            MatchVisibilitySnapshot emptySnapshot = new MatchVisibilitySnapshot
            {
                Version = version,
                Count = 0,
                ObjectIds = Array.Empty<int>(),
                Relations = Array.Empty<byte>(),
                Positions = Array.Empty<Vector3>(),
                Yaws = Array.Empty<float>(),
                RemainingVisibilitySeconds = Array.Empty<float>()
            };

            for (int i = 0; i < room.players.Count; i++)
            {
                Player player = room.players[i];
                if (!CanReceive(player))
                {
                    continue;
                }

                Send(updateSink, player.Connection, emptySnapshot);
            }
        }

        public bool TryGetSnapshot(
            Player player,
            MatchVisibilitySnapshot teamASnapshot,
            MatchVisibilitySnapshot teamBSnapshot,
            out MatchVisibilitySnapshot snapshot)
        {
            snapshot = default;
            if (player == null || player.leftBattle || player.isBot)
            {
                return false;
            }

            MatchTeam team = player.team;
            if (!MatchTeamUtility.IsAssigned(team)
                && player.playerRoot != null
                && player.playerRoot.characterInit != null)
            {
                team = player.playerRoot.characterInit.Team.Value;
            }

            if (team == MatchTeam.TeamA)
            {
                snapshot = teamASnapshot;
                return true;
            }

            if (team == MatchTeam.TeamB)
            {
                snapshot = teamBSnapshot;
                return true;
            }

            return false;
        }

        private static bool CanReceive(Player player)
        {
            return player != null
                   && !player.isBot
                   && !player.leftBattle
                   && player.Connection != null
                   && player.Connection.IsActive;
        }

        private static void Send(
            IMatchVisibilityUpdateSink updateSink,
            NetworkConnection connection,
            MatchVisibilitySnapshot snapshot)
        {
            updateSink.SendMapVisibility(
                connection,
                snapshot.Version,
                snapshot.Count,
                snapshot.ObjectIds,
                snapshot.Relations,
                snapshot.Positions,
                snapshot.Yaws,
                snapshot.RemainingVisibilitySeconds);
        }
    }
}
