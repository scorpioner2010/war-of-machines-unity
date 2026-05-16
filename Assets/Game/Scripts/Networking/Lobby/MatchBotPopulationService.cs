using System.Collections.Generic;
using Game.Scripts.Core.Resources;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public sealed class MatchBotPopulationService
    {
        private const int BotMmr = 1000;
        private const string BotNamePrefix = "Bot ";

        public void AddBots(ServerRoom room, int requestedBotCount, string defaultVehicleCode)
        {
            if (room == null || requestedBotCount <= 0)
            {
                return;
            }

            int existingBots = CountBots(room);
            int botsToAdd = Mathf.Max(0, requestedBotCount - existingBots);
            if (botsToAdd <= 0)
            {
                return;
            }

            List<string> vehicleCodes = BuildVehicleCodePool(defaultVehicleCode);
            if (vehicleCodes.Count == 0)
            {
                Debug.LogWarning("Cannot add match bots: no bot vehicle codes were found in registry or default settings.");
                return;
            }

            Shuffle(vehicleCodes);

            for (int i = 0; i < botsToAdd; i++)
            {
                if (i > 0 && i % vehicleCodes.Count == 0)
                {
                    Shuffle(vehicleCodes);
                }

                Player bot = new Player
                {
                    loginName = BuildBotName(room),
                    Connection = null,
                    token = string.Empty,
                    userId = 0,
                    mmr = BotMmr,
                    activeVehicleId = 0,
                    activeVehicleCode = vehicleCodes[i % vehicleCodes.Count],
                    team = MatchTeam.None,
                    isBot = true,
                    randomPlayerConnected = true
                };

                room.AddPlayer(bot);
            }

            room.maxPlayers = room.PlayersCount();
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

        private static string BuildBotName(ServerRoom room)
        {
            int index = 1;
            while (index < 10000)
            {
                string candidate = BotNamePrefix + index;
                if (room.GetPlayerByName(candidate) == null)
                {
                    return candidate;
                }

                index++;
            }

            return BotNamePrefix + Random.Range(10000, 99999);
        }
    }
}
