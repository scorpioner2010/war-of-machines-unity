using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.Networking.Lobby
{
    public class ServerRoom : MonoBehaviour
    {
        public string roomId;
        public string roomName;
        public int maxPlayers;
        public string selectedLocation;
        public bool isInGame;
        public DateTime CreatedTime;
        public string loadedSceneName;
        public int handle;
        public List<Player> players = new ();
        public bool isAutoRoom;
        public event Action<ServerRoom> OnTimeIsUp;
        public GameplayTimer gameplayTimer;

        private void OnDestroy()
        {
            if (gameplayTimer != null)
            {
                Destroy(gameplayTimer.gameObject);
            }
        }

        private void SyncedTimeOnChange(float newTime)
        {
            players.RemoveAllNull();
            RoomController.UpdateTimer(newTime, players);
        }

        public async void RunTimerAsync()
        {
            float currentTime = ServerSettings.In.findRoomSeconds;
            
            while (currentTime > 0)
            {
                await UniTask.Delay(1000);
                currentTime--;
                SyncedTimeOnChange(currentTime);
            }

            int bots = maxPlayers - players.Count;

            for (int i = 0; i < bots; i++)
            {
                continue;
                Player bot = new Player();
                int lengthName = GameplayAssistant.GetRandomInt(3, 5);
                bot.loginName = GameplayAssistant.GenerateName(lengthName);
                bot.isBot = true;
                AddPlayer(bot);
            }

            if (bots > 0)
            {
                OnTimeIsUp?.Invoke(this);
            }
        }

        public ClientRoom GetInfo()
        {
            ClientRoom @new = new ClientRoom()
            {
                CreatedTime = CreatedTime,
                selectedLocation = selectedLocation,
                isAutoRoom = isAutoRoom,
                roomName = roomName,
                maxPlayers = maxPlayers,
                roomId = roomId,
                loadedSceneName = loadedSceneName,
                players = players,
                isInGame = isInGame,
                handle = handle,
            };

            return @new;
        }
        
        public void AddPlayer(Player player)
        {
            players.Add(player);
        }

        public void RemovePlayer(Player player)
        {
            players.RemoveAllNull();
            players.Remove(player);
        }
        
        public int PlayersCount()
        {
            return players.Count;
        }

        public bool HasPlayer(NetworkConnection connection)
        {
            if (players.Find(p => p.Connection == connection) != null)
            {
                return true;
            }

            return false;
        }
        
        public bool HasPlayer(int clientId)
        {
            if (players.Find(p => p.Connection.ClientId == clientId) != null)
            {
                return true;
            }

            return false;
        }

        public Player GetPlayerBuyConnection(NetworkConnection connection)
        {
            return players.Find(p => p.Connection == connection);
        }
        
        public Player GetPlayerBuyName(string name)
        {
            return players.Find(p => p.loginName == name);
        }

        public List<Player> GetPlayers()
        {
            return players;
        }
        
        public List<Player> GetRealPlayers()
        {
            List<Player> realPlayers = new();

            foreach (Player player in players)
            {
                if (player.isBot == false)
                {
                    realPlayers.Add(player);
                }
            }
            
            return realPlayers;
        }

        public bool IsFull()
        {
            return players.Count >= maxPlayers;
        }
    }
}