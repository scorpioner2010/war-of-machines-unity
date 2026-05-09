using FishNet.Connection;
using FishNet.Object;
using Cysharp.Threading.Tasks;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.Player.Data;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.MainMenu;
using Game.Scripts.UI.Tree;
using UnityEngine;

namespace Game.Scripts.API.ServerManagers
{
    public class ProfileServer : NetworkBehaviour
    {
        private static ProfileServer _in;
        public DevelopmentTree developmentTree;

        private void Awake() => _in = this;

        public static void UpdateProfile()
        {
            if (_in == null)
            {
                return;
            }

            Loading.Show();
            _in.GetProfileServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void GetProfileServerRpc(NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            RequestGetProfileAsync(sender).Forget();
        }

        private async UniTask RequestGetProfileAsync(NetworkConnection sender)
        {
            string token = ServerPlayerSessions.GetToken(sender.ClientId);
            if (string.IsNullOrEmpty(token))
            {
                TargetRpcUpdateProfile(sender, false, "Not logged in.", null);
                return;
            }

            (bool isSuccess, string message, PlayerProfile profile) data = await PlayersManager.GetMyProfile(token);

            if (data.isSuccess)
            {
                ServerPlayerSessions.SetProfile(sender, data.profile);
            }

            TargetRpcUpdateProfile(sender, data.isSuccess, data.message, data.profile);
        }

        [TargetRpc]
        private void TargetRpcUpdateProfile(NetworkConnection target, bool success, string errorMessage, PlayerProfile profile)
        {
            TargetRpcUpdateProfileAsync(target, success, errorMessage, profile).Forget();
        }

        private async UniTask TargetRpcUpdateProfileAsync(NetworkConnection target, bool success, string errorMessage, PlayerProfile profile)
        {
            if (success)
            {
                IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
                clientInfo.SetPlayerData(profile);
                clientInfo.SetClientId(target.ClientId);

                await RobotView.GenerateIconsAsync();

                MainMenu.In.UpdatePlayerInfo(clientInfo.Profile);
                developmentTree.Init();

                if (GameplaySpawner.In != null)
                {
                    GameplaySpawner.In.RequestPendingGameResultsServerRpc();
                }
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
            }

            Loading.Hide();
        }
    }
}
