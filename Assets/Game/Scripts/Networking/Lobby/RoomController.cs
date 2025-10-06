using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Player.Data;
using Game.Scripts.Server;
using Game.Scripts.UI.Lobby;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Networking.Lobby
{
    public class RoomController : NetworkBehaviour
    {
        [SerializeField] private Button play;
        [SerializeField] private Button cancel;
        
        [SerializeField] private LobbyManager lobbyManager;

        private static RoomController _in;
        public GameMaps currentMap;
        
        private void Awake()
        {
            _in = this;
            play.onClick.AddListener(CreateRoomOrJoin);
            cancel.onClick.AddListener(Cancel);
        }
        
        public static void UpdateTimer(float time, List<Player> players)
        {
            foreach (Player player in players)
            {
                if (player.isBot == false)
                {
                    _in.UpdateTimerTargetRpc(player.Connection, time, players.Count);
                }
            }
        }
        
        [TargetRpc]
        private void UpdateTimerTargetRpc(NetworkConnection target, float time, int players)
        {
            FindGame.UpdateInfo(time, players);
        }

        public void CreateRoomOrJoin()
        {
            IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
            lobbyManager.CreateRoomOrJoinServerRpc(ServerSettings.In.maxPlayersForFindRoom, currentMap.ToString(), info.Profile.username, info.ClientId);
            MenuManager.OpenMenu(MenuType.FindGame);
        }

        public void Cancel()
        {
            IPlayerClientInfo playerClientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            lobbyManager.CancelFindRoomServerRpc(playerClientInfo.ClientId);
        }
    }
}