using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
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

            RequestGetProfile(sender);
        }

        private async void RequestGetProfile(NetworkConnection sender)
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
            Loading.Hide();

            if (success)
            {
                IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
                clientInfo.SetPlayerData(profile);
                clientInfo.SetClientId(target.ClientId);
                RobotView.GenerateIcons();
                MainMenu.In.UpdatePlayerInfo(clientInfo.Profile);
                developmentTree.Init();
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
            }
        }
    }
}
