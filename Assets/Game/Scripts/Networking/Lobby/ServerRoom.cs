using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
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

        public bool IsEmpty => players.Count == 0;

        private void OnDestroy()
        {
            if (gameplayTimer != null)
            {
                Destroy(gameplayTimer.gameObject);
            }
        }

        private void SyncedTimeOnChange(float newTime)
        {
            players.RemoveAll(player => player == null);
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

            if (bots > 0)
            {
                OnTimeIsUp?.Invoke(this);
            }
        }

        public ClientRoom GetInfo()
        {
            return new ClientRoom
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
        }
        
        public void AddPlayer(Player player)
        {
            players.Add(player);
        }

        public void RemovePlayer(Player player)
        {
            players.RemoveAll(item => item == null);
            if (player != null)
            {
                players.Remove(player);
            }
        }
        
        public int PlayersCount()
        {
            return players.Count;
        }

        public bool HasPlayer(NetworkConnection connection)
        {
            if (connection == null)
            {
                return false;
            }

            return players.Find(p => p.Connection == connection) != null;
        }
        
        public bool HasPlayer(int clientId)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player != null && player.Connection != null && player.Connection.ClientId == clientId)
                {
                    return true;
                }
            }

            return false;
        }

        public Player GetPlayerByConnection(NetworkConnection connection)
        {
            return players.Find(p => p.Connection == connection);
        }
        
        public Player GetPlayerByName(string name)
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
