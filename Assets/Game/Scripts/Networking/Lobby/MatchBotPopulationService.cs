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

            string vehicleCode = ResolveVehicleCode(defaultVehicleCode);
            if (string.IsNullOrEmpty(vehicleCode))
            {
                Debug.LogWarning("Cannot add match bots: default bot vehicle code is empty and registry fallback was not found.");
                return;
            }

            for (int i = 0; i < botsToAdd; i++)
            {
                Player bot = new Player
                {
                    loginName = BuildBotName(room),
                    Connection = null,
                    token = string.Empty,
                    userId = 0,
                    mmr = BotMmr,
                    activeVehicleId = 0,
                    activeVehicleCode = vehicleCode,
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

        private static string ResolveVehicleCode(string defaultVehicleCode)
        {
            if (!string.IsNullOrEmpty(defaultVehicleCode))
            {
                return defaultVehicleCode;
            }

            return GameResourceManager.GetFirstVehicleCode();
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
