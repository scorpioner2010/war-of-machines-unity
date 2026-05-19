using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.Networking.Lobby
{
    public class ServerRoom : MonoBehaviour
    {
        private const int DefaultMmr = 1000;
        public const int NoSceneSlot = -1;

        public string roomId;
        public string roomName;
        public int maxPlayers;
        public string selectedLocation;
        public bool isInGame;
        public DateTime CreatedTime;
        public string loadedSceneName;
        public int handle;
        public int sceneSlotIndex = NoSceneSlot;
        public int sceneOffsetX;
        public List<Player> players = new List<Player>();
        public bool isAutoRoom;
        public event Action<ServerRoom> OnTimeIsUp;
        public NetworkGameplayTimer gameplayTimer;
        public bool isGameFinished;
        public bool spawnStarted;
        public bool matchEndSubmitted;
        public bool matchRewardsSent;
        public int matchId;

        private MatchVisibilityService _visibility;

        public bool IsEmpty => players.Count == 0;
        public bool IsActiveMatch => isInGame && !matchRewardsSent;
        public bool HasSceneSlot => sceneSlotIndex != NoSceneSlot;
        public bool HasLoadedScene => handle != 0;
        public MatchVisibilityService Visibility
        {
            get
            {
                if (_visibility == null)
                {
                    _visibility = new MatchVisibilityService();
                }

                return _visibility;
            }
        }

        private void OnDestroy()
        {
            if (_visibility != null)
            {
                _visibility.Stop();
            }

            if (gameplayTimer != null)
            {
                Destroy(gameplayTimer.gameObject);
            }
        }

        public void AssignSceneSlot(int slotIndex, int offsetX)
        {
            sceneSlotIndex = slotIndex;
            sceneOffsetX = offsetX;
        }

        public void AssignLoadedScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            loadedSceneName = scene.name;
            handle = scene.handle;
        }

        public Scene GetLoadedScene()
        {
            if (!HasLoadedScene)
            {
                return default;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.handle == handle)
                {
                    return scene;
                }
            }

            return default;
        }

        public void ClearSceneSlot()
        {
            sceneSlotIndex = NoSceneSlot;
            sceneOffsetX = 0;
        }

        public void ClearLoadedScene()
        {
            loadedSceneName = string.Empty;
            handle = 0;
        }

        private void SyncedTimeOnChange(float newTime)
        {
            RemoveNullPlayers();
            RoomController.UpdateTimer(newTime, players);
        }

        public void StartMatchmakingTimer()
        {
            RunMatchmakingTimer().Forget();
        }

        private async UniTask RunMatchmakingTimer()
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
            if (player == null)
            {
                return;
            }

            players.Add(player);
        }

        public void RemovePlayer(Player player)
        {
            RemoveNullPlayers();
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

            return GetPlayerByConnection(connection) != null;
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
            if (connection == null)
            {
                return null;
            }

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player != null && player.Connection == connection)
                {
                    return player;
                }
            }

            return null;
        }
        
        public Player GetPlayerByName(string name)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player != null && player.loginName == name)
                {
                    return player;
                }
            }

            return null;
        }

        public Player GetPlayerByUserId(int userId)
        {
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player != null && player.userId == userId)
                {
                    return player;
                }
            }

            return null;
        }

        public Player GetPlayerByVehicle(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null)
            {
                return null;
            }

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player != null && player.playerRoot == vehicleRoot)
                {
                    return player;
                }
            }

            return null;
        }

        public List<Player> GetPlayers()
        {
            return players;
        }
        
        public List<Player> GetRealPlayers()
        {
            List<Player> realPlayers = new List<Player>();

            foreach (Player player in players)
            {
                if (player != null && player.isBot == false)
                {
                    realPlayers.Add(player);
                }
            }

            return realPlayers;
        }

        public bool AreAllRealPlayersLoaded()
        {
            bool hasRealPlayer = false;

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null || player.isBot)
                {
                    continue;
                }

                hasRealPlayer = true;
                if (!player.randomPlayerConnected)
                {
                    return false;
                }
            }

            return hasRealPlayer;
        }

        public void AssignTeams()
        {
            List<Player> realPlayers = new List<Player>();
            List<Player> botPlayers = new List<Player>();

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player != null)
                {
                    player.team = MatchTeam.None;
                    if (player.isBot)
                    {
                        botPlayers.Add(player);
                    }
                    else
                    {
                        realPlayers.Add(player);
                    }
                }
            }

            int teamACount = 0;
            int teamBCount = 0;
            int teamAMmr = 0;
            int teamBMmr = 0;
            bool preferTeamA = true;

            realPlayers.Sort(CompareForTeamBalance);
            AssignPlayersToBalancedTeams(realPlayers, ref teamACount, ref teamBCount, ref teamAMmr, ref teamBMmr, ref preferTeamA);

            botPlayers.Sort(CompareForTeamBalance);
            AssignPlayersToBalancedTeams(botPlayers, ref teamACount, ref teamBCount, ref teamAMmr, ref teamBMmr, ref preferTeamA);
        }

        private static void AssignPlayersToBalancedTeams(
            List<Player> playersForBalance,
            ref int teamACount,
            ref int teamBCount,
            ref int teamAMmr,
            ref int teamBMmr,
            ref bool preferTeamA)
        {
            for (int i = 0; i < playersForBalance.Count; i++)
            {
                Player player = playersForBalance[i];
                if (player == null)
                {
                    continue;
                }

                MatchTeam team = ChooseTeam(teamACount, teamBCount, teamAMmr, teamBMmr, preferTeamA);

                player.team = team;
                if (team == MatchTeam.TeamA)
                {
                    teamACount++;
                    teamAMmr += GetMmr(player);
                }
                else
                {
                    teamBCount++;
                    teamBMmr += GetMmr(player);
                }

                preferTeamA = !preferTeamA;
            }
        }

        private static MatchTeam ChooseTeam(int teamACount, int teamBCount, int teamAMmr, int teamBMmr, bool preferTeamA)
        {
            if (teamACount < teamBCount)
            {
                return MatchTeam.TeamA;
            }

            if (teamBCount < teamACount)
            {
                return MatchTeam.TeamB;
            }

            if (teamAMmr < teamBMmr)
            {
                return MatchTeam.TeamA;
            }

            if (teamBMmr < teamAMmr)
            {
                return MatchTeam.TeamB;
            }

            return preferTeamA ? MatchTeam.TeamA : MatchTeam.TeamB;
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

        public string GetApiToken()
        {
            foreach (Player player in players)
            {
                if (player != null && !string.IsNullOrEmpty(player.token))
                {
                    return player.token;
                }
            }

            return string.Empty;
        }

        private void RemoveNullPlayers()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i] == null)
                {
                    players.RemoveAt(i);
                }
            }
        }
    }
}
