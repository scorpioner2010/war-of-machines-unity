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
using Game.Scripts.Testing;
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
            if (ClientMenuTestMode.TryCreateProfile(out PlayerProfile localTestProfile))
            {
                ApplyLocalTestProfile(localTestProfile);
                _in?.developmentTree?.RefreshProgressionStates();
                return;
            }

            if (_in == null)
            {
                return;
            }

            StandardLoadingOverlay.Show();
            _in.GetProfileServerRpc();
        }

        public static void ConvertFreeXp(int vehicleId, int amount)
        {
            if (ClientMenuTestMode.IsEnabled)
            {
                FreeXpConversionPopup.NotifyConversionFailed();
                Popup.ShowText("Free XP conversion is disabled in offline menu test mode.", Color.yellow);
                return;
            }

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

        public static void RequestProfileStatistics()
        {
            if (ClientMenuTestMode.TryCreateProfileStatistics(out PlayerStatisticsDto localTestStatistics))
            {
                MainMenu.In?.UpdateProfileStatistics(localTestStatistics);
                return;
            }

            if (ClientMenuTestMode.IsEnabled)
            {
                ProfileStatisticsPopup.In?.ShowUnavailable("Server statistics are unavailable in offline menu test mode.");
                return;
            }

            if (_in == null)
            {
                ProfileStatisticsPopup.In?.ShowError("Profile service is not available.");
                return;
            }

            ProfileStatisticsPopup.In?.ShowLoading();
            _in.GetProfileStatisticsServerRpc();
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

        [ServerRpc(RequireOwnership = false)]
        private void GetProfileStatisticsServerRpc(NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            RequestProfileStatisticsAsync(sender).Forget();
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

        private async UniTask RequestProfileStatisticsAsync(NetworkConnection sender)
        {
            string token = ServerPlayerSessions.GetToken(sender.ClientId);
            if (string.IsNullOrEmpty(token))
            {
                TargetRpcUpdateProfileStatistics(sender, false, "Not logged in.", null);
                return;
            }

            (bool isSuccess, string message, PlayerStatisticsDto statistics) data = await PlayersManager.GetMyStatistics(token);
            TargetRpcUpdateProfileStatistics(sender, data.isSuccess, data.message, data.statistics);
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

        [TargetRpc]
        private void TargetRpcUpdateProfileStatistics(NetworkConnection target, bool success, string errorMessage, PlayerStatisticsDto statistics)
        {
            if (success && statistics != null)
            {
                MainMenu.In?.UpdateProfileStatistics(statistics);
                return;
            }

            MainMenu.In?.ShowProfileStatisticsError(errorMessage);
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
                developmentTree?.RefreshProgressionStates();

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

        private static void ApplyLocalTestProfile(PlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (ServiceLocator.IsRegistered<IPlayerClientInfo>() == false)
            {
                ServiceLocator.TryRegister<IPlayerClientInfo>(new PlayerClientInfo());
            }

            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            clientInfo.SetPlayerData(profile);
            clientInfo.SetClientId(0);

            MainMenu.In?.UpdatePlayerInfo(profile);
            RobotView.GenerateIcons();
        }
    }
}
