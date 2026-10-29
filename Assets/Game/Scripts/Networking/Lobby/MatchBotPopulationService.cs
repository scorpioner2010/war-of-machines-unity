using System.Collections.Generic;
using Game.Scripts.Core.Resources;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public sealed class MatchBotPopulationService
    {
        public int FillToTarget(ServerRoom room, int targetPlayerCount, string defaultVehicleCode)
        {
            if (room == null || targetPlayerCount <= 0)
            {
                return 0;
            }

            int desiredBotCount = Mathf.Max(0, targetPlayerCount - room.RealPlayersCount());
            int existingBots = CountBots(room);
            int botsToAdd = Mathf.Max(0, desiredBotCount - existingBots);
            if (botsToAdd <= 0)
            {
                return 0;
            }

            List<string> vehicleCodes = BuildVehicleCodePool(defaultVehicleCode);
            if (vehicleCodes.Count == 0)
            {
                Debug.LogWarning("Cannot add match bots: no bot vehicle codes were found in registry or default settings.");
                return 0;
            }

            Shuffle(vehicleCodes);
            int botMmr = ServerSettings.GetBotMmr();
            string botNamePrefix = ServerSettings.GetBotNamePrefix();

            for (int i = 0; i < botsToAdd; i++)
            {
                if (i > 0 && i % vehicleCodes.Count == 0)
                {
                    Shuffle(vehicleCodes);
                }

                Player bot = new Player
                {
                    loginName = BuildBotName(room, botNamePrefix),
                    Connection = null,
                    token = string.Empty,
                    userId = 0,
                    mmr = botMmr,
                    activeVehicleId = 0,
                    activeVehicleCode = vehicleCodes[i % vehicleCodes.Count],
                    team = MatchTeam.None,
                    isBot = true,
                    randomPlayerConnected = true
                };

                room.AddPlayer(bot);
            }

            return botsToAdd;
        }

        private static int CountBots(ServerRoom room)
        {
            int count = 0;
            for (int i = 0; i < room.players.Count; i++)
            {
                Player player = room.players[i];
                if (player != null && player.isBot)
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string> BuildVehicleCodePool(string defaultVehicleCode)
        {
            List<string> vehicleCodes = new List<string>(16);
            GameResourceManager.FillVehicleCodes(vehicleCodes);

            if (vehicleCodes.Count == 0 && !string.IsNullOrEmpty(defaultVehicleCode))
            {
                vehicleCodes.Add(defaultVehicleCode);
            }

            if (vehicleCodes.Count == 0)
            {
                string fallbackCode = GameResourceManager.GetFirstVehicleCode();
                if (!string.IsNullOrEmpty(fallbackCode))
                {
                    vehicleCodes.Add(fallbackCode);
                }
            }

            return vehicleCodes;
        }

        private static void Shuffle(List<string> values)
        {
            if (values == null)
            {
                return;
            }

            for (int i = values.Count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                string temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }

        private static string BuildBotName(ServerRoom room, string botNamePrefix)
        {
            if (string.IsNullOrEmpty(botNamePrefix))
            {
                botNamePrefix = "Bot ";
            }

            int index = 1;
            while (index < 10000)
            {
                string candidate = botNamePrefix + index;
                if (room.GetPlayerByName(candidate) == null)
                {
                    return candidate;
                }

                index++;
            }

            return botNamePrefix + Random.Range(10000, 99999);
        }
    }
}
