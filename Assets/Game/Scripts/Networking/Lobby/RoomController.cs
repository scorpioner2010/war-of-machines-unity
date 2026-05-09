using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.MenuController;
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

            if (play != null)
            {
                play.onClick.AddListener(FindMatch);
            }

            if (cancel != null)
            {
                cancel.onClick.AddListener(Cancel);
            }
        }
        
        public static void UpdateTimer(float time, List<Player> players)
        {
            if (_in == null)
            {
                return;
            }

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

        public void FindMatch()
        {
            if (lobbyManager == null)
            {
                return;
            }

            lobbyManager.FindMatchServerRpc(currentMap.ToString());
            MenuManager.OpenMenu(MenuType.FindGame);
        }

        public void Cancel()
        {
            if (lobbyManager == null)
            {
                return;
            }

            lobbyManager.CancelFindRoomServerRpc();
        }
    }
}
