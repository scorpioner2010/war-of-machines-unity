using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using RAV.Scripts.API.Endpoints;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.Networking.Sessions;
using UnityEngine;

namespace RAV.Scripts.Networking.Lobby
{
    public static class MatchResultService
    {
        public static async UniTask<List<PlayerBattleResultDelivery>> EndMatchAndBuildDeliveries(ServerRoom serverRoom)
        {
            List<PlayerBattleResultDelivery> deliveries = new List<PlayerBattleResultDelivery>();
            if (serverRoom == null || serverRoom.matchEndSubmitted)
            {
                GameplayRuntimeDiagnostics.Record(
                    "match_end_pipeline_skipped",
                    "reason", serverRoom == null ? "room_missing" : "already_submitted",
                    "room_id", serverRoom != null ? serverRoom.roomId : string.Empty,
                    "match_id", serverRoom != null ? serverRoom.matchId : 0);
                return deliveries;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            serverRoom.matchEndSubmitted = true;

            ParticipantInput[] participants = BuildParticipantInputs(serverRoom);
            MatchParticipantView[] rewardItems = Array.Empty<MatchParticipantView>();
            GameplayRuntimeDiagnostics.Record(
                "match_end_pipeline_started",
                "room_id", serverRoom.roomId,
                "match_id", serverRoom.matchId,
                "participant_count", participants.Length);

            if (serverRoom.matchId <= 0)
            {
                (bool started, _, int matchId) = await MatchesManager.StartMatchAsDedicatedServer(
                    serverRoom.selectedLocation,
                    BuildMatchRoster(serverRoom));
                if (started)
                {
                    serverRoom.matchId = matchId;
                    GameplayRuntimeDiagnostics.Record(
                        "match_api_record_created",
                        "room_id", serverRoom.roomId,
                        "match_id", matchId);
                }
                else
                {
                    GameplayRuntimeDiagnostics.Record(
                        "match_api_record_create_failed",
                        "room_id", serverRoom.roomId,
                        "participant_count", participants.Length);
                }
            }

            if (serverRoom.matchId > 0 && participants.Length > 0)
            {
                rewardItems = await LoadRewards(serverRoom.matchId, participants, serverRoom.GetApiToken());
            }
            else
            {
                GameplayRuntimeDiagnostics.Record(
                    "match_end_api_submission_skipped",
                    "room_id", serverRoom.roomId,
                    "match_id", serverRoom.matchId,
                    "reason", serverRoom.matchId <= 0 ? "api_match_id_missing" : "no_participants");
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

            stopwatch.Stop();
            GameplayRuntimeDiagnostics.Record(
                "match_end_pipeline_completed",
                "room_id", serverRoom.roomId,
                "match_id", serverRoom.matchId,
                "participant_count", participants.Length,
                "reward_count", rewardItems.Length,
                "delivery_count", deliveries.Count,
                "duration_ms", stopwatch.ElapsedMilliseconds);
            return deliveries;
        }

        private static async UniTask<MatchParticipantView[]> LoadRewards(int matchId, ParticipantInput[] participants, string token)
        {
            (bool isSuccess, _, EndMatchResponse response) = await MatchesManager.EndMatchAsDedicatedServer(matchId, participants);

            if (isSuccess && response != null && HasAuthoritativeRewards(response.participants, participants))
            {
                GameplayRuntimeDiagnostics.Record(
                    "match_rewards_received_from_end_api",
                    "match_id", matchId,
                    "participant_count", response.participants.Length);
                return response.participants;
            }

            GameplayRuntimeDiagnostics.Record(
                "match_rewards_fallback_started",
                "match_id", matchId,
                "reason", !isSuccess ? "end_api_failed" : response == null ? "empty_end_response" : "rewards_not_authoritative");
            (bool loaded, _, MatchParticipantView[] items) = await MatchesManager.GetParticipants(matchId, token);
            if (loaded && HasAuthoritativeRewards(items, participants))
            {
                GameplayRuntimeDiagnostics.Record(
                    "match_rewards_fallback_completed",
                    "match_id", matchId,
                    "participant_count", items.Length);
                return items;
            }

            GameplayRuntimeDiagnostics.Record(
                "match_rewards_unavailable",
                "match_id", matchId,
                "fallback_loaded", loaded,
                "fallback_count", items?.Length ?? 0);
            return Array.Empty<MatchParticipantView>();
        }

        private static MatchRosterInput[] BuildMatchRoster(ServerRoom serverRoom)
        {
            List<MatchRosterInput> roster = new List<MatchRosterInput>();
            foreach (Player player in serverRoom.GetPlayers())
            {
                if (player == null || player.isBot || player.userId <= 0 || player.activeVehicleId <= 0 || player.team == MatchTeam.None)
                {
                    continue;
                }

                roster.Add(new MatchRosterInput
                {
                    userId = player.userId,
                    vehicleId = player.activeVehicleId,
                    team = (int)player.team
                });
            }

            return roster.ToArray();
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
                MmrDelta = reward != null ? reward.mmrDelta : 0,
                RewardsAvailable = reward != null && reward.rewardsCalculated
            };
        }

        private static bool HasAuthoritativeRewards(MatchParticipantView[] rewards, ParticipantInput[] participants)
        {
            if (rewards == null || participants == null || participants.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < participants.Length; i++)
            {
                MatchParticipantView reward = FindReward(participants[i].userId, rewards);
                if (reward == null || !reward.rewardsCalculated)
                {
                    return false;
                }
            }

            return true;
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
