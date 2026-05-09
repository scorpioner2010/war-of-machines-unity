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
        private const int DefaultMmr = 1000;

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
        public bool isGameFinished;

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
            float currentTime = ServerSettings.GetFindRoomSeconds();
            
            while (currentTime > 0 && !isInGame)
            {
                await UniTask.Delay(1000);
                currentTime--;
                SyncedTimeOnChange(currentTime);
            }

            if (!isInGame && players.Count > 0)
            {
                OnTimeIsUp?.Invoke(this);
            }
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

        public void AssignTeams()
        {
            List<Player> playersForBalance = new List<Player>();

            foreach (Player player in players)
            {
                if (player != null)
                {
                    player.team = MatchTeam.None;
                    playersForBalance.Add(player);
                }
            }

            playersForBalance.Sort(CompareForTeamBalance);

            int redCount = 0;
            int blueCount = 0;
            int redMmr = 0;
            int blueMmr = 0;
            bool preferRed = true;

            for (int i = 0; i < playersForBalance.Count; i++)
            {
                Player player = playersForBalance[i];
                MatchTeam team = ChooseTeam(redCount, blueCount, redMmr, blueMmr, preferRed);

                player.team = team;
                if (team == MatchTeam.Red)
                {
                    redCount++;
                    redMmr += GetMmr(player);
                }
                else
                {
                    blueCount++;
                    blueMmr += GetMmr(player);
                }

                preferRed = !preferRed;
            }
        }

        private static MatchTeam ChooseTeam(int redCount, int blueCount, int redMmr, int blueMmr, bool preferRed)
        {
            if (redCount < blueCount)
            {
                return MatchTeam.Red;
            }

            if (blueCount < redCount)
            {
                return MatchTeam.Blue;
            }

            if (redMmr < blueMmr)
            {
                return MatchTeam.Red;
            }

            if (blueMmr < redMmr)
            {
                return MatchTeam.Blue;
            }

            return preferRed ? MatchTeam.Red : MatchTeam.Blue;
        }

        private static int CompareForTeamBalance(Player left, Player right)
        {
            int leftMmr = GetMmr(left);
            int rightMmr = GetMmr(right);

            if (leftMmr != rightMmr)
            {
                return rightMmr.CompareTo(leftMmr);
            }

            int leftClientId = GetClientId(left);
            int rightClientId = GetClientId(right);
            if (leftClientId != rightClientId)
            {
                return leftClientId.CompareTo(rightClientId);
            }

            string leftName = left != null ? left.loginName : string.Empty;
            string rightName = right != null ? right.loginName : string.Empty;
            return string.Compare(leftName, rightName, StringComparison.Ordinal);
        }

        private static int GetMmr(Player player)
        {
            if (player == null || player.mmr <= 0)
            {
                return DefaultMmr;
            }

            return player.mmr;
        }

        private static int GetClientId(Player player)
        {
            if (player == null || player.Connection == null)
            {
                return int.MaxValue;
            }

            return player.Connection.ClientId;
        }

        public bool IsFull()
        {
            return players.Count >= maxPlayers;
        }
    }
}
