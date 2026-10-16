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

            StandardLoadingOverlay.Show();
            _in.GetProfileServerRpc();
        }

        public static void ConvertFreeXp(int vehicleId, int amount)
        {
            if (_in == null)
            {
                Popup.ShowText("Profile service is not available.", Color.red);
                return;
            }

            if (vehicleId <= 0 || amount <= 0)
            {
                Popup.ShowText("Choose a vehicle and XP amount.", Color.red);
                return;
            }

            StandardLoadingOverlay.Show();
            _in.ConvertFreeXpServerRpc(vehicleId, amount);
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

        [ServerRpc(RequireOwnership = false)]
        private void ConvertFreeXpServerRpc(int vehicleId, int amount, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            ConvertFreeXpAsync(sender, vehicleId, amount).Forget();
        }

        private async UniTask ConvertFreeXpAsync(NetworkConnection sender, int vehicleId, int amount)
        {
            if (vehicleId <= 0 || amount <= 0)
            {
                TargetRpcConvertFreeXp(sender, false, "Choose a vehicle and XP amount.");
                return;
            }

            string token = ServerPlayerSessions.GetToken(sender.ClientId);
            if (string.IsNullOrEmpty(token))
            {
                TargetRpcConvertFreeXp(sender, false, "Not logged in.");
                return;
            }

            (bool ok, string message) result = await UserVehiclesManager.ConvertFreeXp(vehicleId, amount, token);
            TargetRpcConvertFreeXp(sender, result.ok, result.message);
        }

        [TargetRpc]
        private void TargetRpcUpdateProfile(NetworkConnection target, bool success, string errorMessage, PlayerProfile profile)
        {
            TargetRpcUpdateProfileAsync(target, success, errorMessage, profile).Forget();
        }

        [TargetRpc]
        private void TargetRpcConvertFreeXp(NetworkConnection target, bool success, string errorMessage)
        {
            if (success)
            {
                FreeXpConversionPopup.NotifyConversionSucceeded();
                UpdateProfile();
                return;
            }

            FreeXpConversionPopup.NotifyConversionFailed();
            Popup.ShowText(errorMessage, Color.red);
            StandardLoadingOverlay.Hide();
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

            StandardLoadingOverlay.Hide();
        }
    }
}
