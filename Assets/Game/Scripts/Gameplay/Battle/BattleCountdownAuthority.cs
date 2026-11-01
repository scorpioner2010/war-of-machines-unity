using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.Networking.Lobby;
using UnityEngine;
using LobbyPlayer = Game.Scripts.Networking.Lobby.Player;

namespace Game.Scripts.Gameplay.Battle
{
    public class BattleCountdownAuthority : NetworkBehaviour
    {
        private static BattleCountdownAuthority _currentClient;

        private readonly HashSet<int> _finishedClientIds = new HashSet<int>();
        private ServerRoom _serverRoom;
        private bool _serverLockStarted;

        public static BattleCountdownAuthority CurrentClient => _currentClient;

        public override void OnStartServer()
        {
            base.OnStartServer();

            NetworkGameplayTimer gameplayTimer = GetComponent<NetworkGameplayTimer>();
            _serverRoom = gameplayTimer != null ? gameplayTimer.serverRoom : null;
            if (_serverRoom == null)
            {
                return;
            }

            _finishedClientIds.Clear();
            BattleCountdownGate.BeginServer(_serverRoom.roomId);
            _serverLockStarted = true;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _currentClient = this;
        }

        public override void OnStopServer()
        {
            StopServerLock();
            base.OnStopServer();
        }

        public override void OnStopClient()
        {
            if (_currentClient == this)
            {
                _currentClient = null;
            }

            base.OnStopClient();
        }

        private void Update()
        {
            if (!_serverLockStarted || _serverRoom == null)
            {
                return;
            }

            if (_serverRoom.isGameFinished || AllRealPlayersFinished())
            {
                StopServerLock();
            }
        }

        public void NotifyClientCountdownFinished()
        {
            if (!IsClientInitialized)
            {
                return;
            }

            NotifyClientCountdownFinishedServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void NotifyClientCountdownFinishedServerRpc(NetworkConnection sender = null)
        {
            if (!_serverLockStarted || _serverRoom == null || sender == null)
            {
                return;
            }

            LobbyPlayer player = _serverRoom.GetPlayerByConnection(sender);
            if (player == null || player.isBot)
            {
                return;
            }

            _finishedClientIds.Add(sender.ClientId);
            if (AllRealPlayersFinished())
            {
                StopServerLock();
            }
        }

        private bool AllRealPlayersFinished()
        {
            List<LobbyPlayer> players = _serverRoom.GetPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                LobbyPlayer player = players[i];
                if (player == null || player.isBot || player.Connection == null)
                {
                    continue;
                }

                if (!_finishedClientIds.Contains(player.Connection.ClientId))
                {
                    return false;
                }
            }

            return true;
        }

        private void StopServerLock()
        {
            if (!_serverLockStarted)
            {
                return;
            }

            BattleCountdownGate.EndServer(_serverRoom != null ? _serverRoom.roomId : null);
            _serverLockStarted = false;
        }
    }
}
