using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using RAV.Scripts.Audio;
using RAV.Scripts.MenuController;
using RAV.Scripts.Testing;
using RAV.Scripts.UI.Lobby;
using UnityEngine;
using UnityEngine.UI;

namespace RAV.Scripts.Networking.Lobby
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

        private void OnEnable()
        {
            MatchmakingStatusView.OnGameTransitionReady += HandleGameTransitionReady;
        }

        private void OnDisable()
        {
            MatchmakingStatusView.OnGameTransitionReady -= HandleGameTransitionReady;
        }

        private void HandleGameTransitionReady()
        {
            lobbyManager?.MatchmakingPresentationReadyServerRpc();
        }
        
        public static void UpdateTimer(float time, List<Player> players)
        {
            if (_in == null)
            {
                return;
            }

            int realPlayerCount = 0;

            if (players != null)
            {
                foreach (Player player in players)
                {
                    if (player != null && !player.isBot)
                    {
                        realPlayerCount++;
                    }
                }
            }

            if (realPlayerCount == 0)
            {
                return;
            }

            foreach (Player player in players)
            {
                if (player != null && !player.isBot && player.Connection != null)
                {
                    _in.UpdateTimerTargetRpc(player.Connection, time, realPlayerCount);
                }
            }
        }
        
        [TargetRpc]
        private void UpdateTimerTargetRpc(NetworkConnection target, float time, int players)
        {
            MatchmakingStatusView.UpdateInfo(time, players);
        }

        public void FindMatch()
        {
            if (ClientMenuTestMode.IsEnabled)
            {
                SoundCaller.PlayUi(AudioEventNames.UiSearchStarted);
                MatchmakingStatusView.BeginSearch();
                MatchmakingStatusView.UpdateInfo(0f, 1);
                MenuManager.OpenMenu(MenuType.Matchmaking);
                return;
            }

            if (lobbyManager == null)
            {
                return;
            }

            MatchmakingStatusView.BeginSearch();
            lobbyManager.FindMatchServerRpc(currentMap.ToString());
            SoundCaller.PlayUi(AudioEventNames.UiSearchStarted);
            MenuManager.OpenMenu(MenuType.Matchmaking);
        }

        public void Cancel()
        {
            if (ClientMenuTestMode.IsEnabled)
            {
                SoundCaller.PlayUi(AudioEventNames.UiSearchCancelled);
                MatchmakingStatusView.CancelSearch();
                MenuManager.OpenMenu(MenuType.MainMenu);
                return;
            }

            if (lobbyManager == null)
            {
                return;
            }

            lobbyManager.CancelFindRoomServerRpc();
            MatchmakingStatusView.CancelSearch();
            SoundCaller.PlayUi(AudioEventNames.UiSearchCancelled);
        }
    }
}
