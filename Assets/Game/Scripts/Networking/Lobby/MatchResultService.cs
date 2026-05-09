using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using Game.Scripts.API.Endpoints;
using Game.Scripts.Networking.Sessions;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public static class MatchResultService
    {
        public static async UniTask<List<PlayerBattleResultDelivery>> EndMatchAndBuildDeliveries(ServerRoom serverRoom)
        {
            List<PlayerBattleResultDelivery> deliveries = new List<PlayerBattleResultDelivery>();
            if (serverRoom == null || serverRoom.matchEndSubmitted)
            {
                return deliveries;
            }

            serverRoom.matchEndSubmitted = true;

            ParticipantInput[] participants = BuildParticipantInputs(serverRoom);
            string token = serverRoom.GetApiToken();
            MatchParticipantView[] rewardItems = Array.Empty<MatchParticipantView>();

            if (serverRoom.matchId <= 0 && !string.IsNullOrEmpty(token))
            {
                (bool started, _, int matchId) = await MatchesManager.StartMatch(serverRoom.selectedLocation, token);
                if (started)
                {
                    serverRoom.matchId = matchId;
                }
            }

            if (serverRoom.matchId > 0 && !string.IsNullOrEmpty(token) && participants.Length > 0)
            {
                rewardItems = await LoadRewards(serverRoom.matchId, participants, token);
            }

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player == null || player.isBot)
                {
                    continue;
                }

                PlayerBattleResult result = BuildPlayerResult(serverRoom.roomId, player, rewardItems);
                deliveries.Add(new PlayerBattleResultDelivery
                {
                    UserId = player.userId,
                    Connection = ResolveResultConnection(player),
                    Result = result
                });
            }

            return deliveries;
        }

        private static async UniTask<MatchParticipantView[]> LoadRewards(int matchId, ParticipantInput[] participants, string token)
        {
            (bool isSuccess, _, EndMatchResponse response) = await MatchesManager.EndMatch(matchId, participants, token);

            if (isSuccess && response != null && response.participants != null)
            {
                return response.participants;
            }

            if (isSuccess)
            {
                (bool loaded, _, MatchParticipantView[] items) = await MatchesManager.GetParticipants(matchId, token);
                if (loaded && items != null)
                {
                    return items;
                }
            }

            return Array.Empty<MatchParticipantView>();
        }

        private static PlayerBattleResult BuildPlayerResult(string roomId, Player player, MatchParticipantView[] rewardItems)
        {
            MatchParticipantView reward = FindReward(player.userId, rewardItems);

            return new PlayerBattleResult
            {
                RoomId = roomId,
                Result = player.battleResult,
                Kills = player.kills,
                Damage = player.damage,
                XpEarned = reward != null ? reward.xpEarned : 0,
                Bolts = reward != null ? reward.bolts : 0,
                FreeXp = reward != null ? reward.freeXp : 0,
                MmrDelta = reward != null ? reward.mmrDelta : 0
            };
        }

        private static NetworkConnection ResolveResultConnection(Player player)
        {
            if (player.Connection != null)
            {
                return player.Connection;
            }

            return ServerPlayerSessions.GetConnectionByUserId(player.userId);
        }

        private static ParticipantInput[] BuildParticipantInputs(ServerRoom serverRoom)
        {
            List<ParticipantInput> participants = new List<ParticipantInput>();

            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player == null || player.isBot || player.userId <= 0)
                {
                    continue;
                }

                participants.Add(new ParticipantInput
                {
                    userId = player.userId,
                    vehicleId = player.activeVehicleId,
                    team = (int)player.team,
                    result = string.IsNullOrEmpty(player.battleResult) ? BattleRules.ApiResultLose : player.battleResult,
                    kills = Mathf.Clamp(player.kills, 0, 20),
                    damage = Mathf.Clamp(player.damage, 0, 20000)
                });
            }

            return participants.ToArray();
        }

        private static MatchParticipantView FindReward(int userId, MatchParticipantView[] rewards)
        {
            if (rewards == null)
            {
                return null;
            }

            for (int i = 0; i < rewards.Length; i++)
            {
                MatchParticipantView reward = rewards[i];
                if (reward != null && reward.userId == userId)
                {
                    return reward;
                }
            }

            return null;
        }
    }
}
