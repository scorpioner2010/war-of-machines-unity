using System.Collections;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using Cysharp.Threading.Tasks;
using RAV.Scripts.API.Endpoints;
using RAV.Scripts.API.Models;
using RAV.Scripts.Core.Services;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.MenuController;
using RAV.Scripts.Networking.Lobby;
using RAV.Scripts.Networking.Sessions;
using RAV.Scripts.Player.Data;
using RAV.Scripts.Testing;
using RAV.Scripts.UI.Helpers;
using RAV.Scripts.UI.MainMenu;
using RAV.Scripts.UI.Tree;
using UnityEngine;

namespace RAV.Scripts.API.ServerManagers
{
    public class ProfileServer : NetworkBehaviour
    {
        private const float ProfileRetryDelaySeconds = 0.5f;
        private const float ProfileRequestTimeoutSeconds = 4f;
        private const int MaxProfileRefreshAttempts = 120;

        private static ProfileServer _in;
        public DevelopmentTree developmentTree;

        private Coroutine _profileRefreshCoroutine;
        private bool _profileRefreshQueued;
        private bool _profileRequestInFlight;
        private int _profileRefreshAttempt;

        private void Awake() => _in = this;

        private void OnEnable()
        {
            StartQueuedProfileRefresh();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            GameplayRuntimeDiagnostics.Record("profile_service_client_started", "object", name);
            StartQueuedProfileRefresh();
        }

        public override void OnStopClient()
        {
            StopQueuedProfileRefresh();
            base.OnStopClient();
        }

        private void OnDestroy()
        {
            StopQueuedProfileRefresh();
            if (_in == this)
            {
                _in = null;
            }
        }

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
                GameplayRuntimeDiagnostics.Record("profile_refresh_skipped", "reason", "service_missing");
                return;
            }

            if (!ClientAuthenticationState.IsAuthenticated)
            {
                GameplayRuntimeDiagnostics.Record("profile_refresh_skipped", "reason", "https_session_missing");
                StandardLoadingOverlay.Hide();
                return;
            }

            StandardLoadingOverlay.Show();
            _in.QueueProfileRefresh();
        }

        private void QueueProfileRefresh()
        {
            if (!_profileRefreshQueued)
            {
                _profileRefreshAttempt = 0;
            }

            _profileRefreshQueued = true;
            StartQueuedProfileRefresh();
        }

        private void StartQueuedProfileRefresh()
        {
            if (_profileRefreshCoroutine != null || !_profileRefreshQueued)
            {
                return;
            }

            if (!isActiveAndEnabled)
            {
                GameplayRuntimeDiagnostics.Record("profile_refresh_waiting", "reason", "service_inactive");
                return;
            }

            _profileRefreshCoroutine = StartCoroutine(ProfileRefreshLoop());
        }

        private void StopQueuedProfileRefresh()
        {
            if (_profileRefreshCoroutine != null)
            {
                StopCoroutine(_profileRefreshCoroutine);
                _profileRefreshCoroutine = null;
            }

            _profileRefreshQueued = false;
            _profileRequestInFlight = false;
        }

        private IEnumerator ProfileRefreshLoop()
        {
            while (_profileRefreshQueued && _profileRefreshAttempt < MaxProfileRefreshAttempts)
            {
                if (!IsProfileRpcReady())
                {
                    yield return new WaitForSecondsRealtime(ProfileRetryDelaySeconds);
                    continue;
                }

                _profileRefreshQueued = false;
                _profileRequestInFlight = true;
                _profileRefreshAttempt++;
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_started",
                    "attempt", _profileRefreshAttempt,
                    "client_initialized", IsClientInitialized,
                    "client_started", IsClientStarted);

                GetProfileServerRpc();

                float timeoutAt = Time.unscaledTime + ProfileRequestTimeoutSeconds;
                while (_profileRequestInFlight && Time.unscaledTime < timeoutAt)
                {
                    yield return null;
                }

                if (_profileRequestInFlight)
                {
                    _profileRequestInFlight = false;
                    _profileRefreshQueued = true;
                    GameplayRuntimeDiagnostics.Record(
                        "profile_request_timeout",
                        "attempt", _profileRefreshAttempt);
                }

                if (_profileRefreshQueued)
                {
                    yield return new WaitForSecondsRealtime(ProfileRetryDelaySeconds);
                }
            }

            if (_profileRefreshQueued)
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_refresh_failed",
                    "reason", "attempt_limit",
                    "attempts", _profileRefreshAttempt);
                _profileRefreshQueued = false;
                _profileRequestInFlight = false;
                StandardLoadingOverlay.Hide();
            }

            _profileRefreshCoroutine = null;
        }

        private bool IsProfileRpcReady()
        {
            return ClientAuthenticationState.IsAuthenticated
                   && IsClientInitialized
                   && IsClientStarted;
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
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out _))
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_rejected",
                    "reason", "server_session_not_authenticated",
                    "client_id", sender != null ? sender.ClientId : -1);
                return;
            }

            RequestGetProfileAsync(sender).Forget();
        }

        private async UniTask RequestGetProfileAsync(NetworkConnection sender)
        {
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out ServerPlayerSession session))
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_failed",
                    "reason", "server_session_missing",
                    "client_id", sender != null ? sender.ClientId : -1);
                TargetRpcUpdateProfile(sender, false, "Not logged in.", null);
                return;
            }

            string token = session.Token;
            if (string.IsNullOrEmpty(token))
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_failed",
                    "reason", "delegated_token_missing",
                    "client_id", sender.ClientId);
                TargetRpcUpdateProfile(sender, false, "Not logged in.", null);
                return;
            }

            (bool isSuccess, string message, PlayerProfile profile) data = await PlayersManager.GetMyProfile(token);

            if (data.isSuccess)
            {
                ServerPlayerSessions.SetProfile(sender, data.profile);
                GameplayRuntimeDiagnostics.Record(
                    "profile_loaded_server",
                    "client_id", sender.ClientId,
                    "user_id", data.profile != null ? data.profile.id : 0,
                    "username", data.profile != null ? data.profile.username : string.Empty,
                    "owned_count", data.profile?.ownedVehicles?.Length ?? 0,
                    "active_vehicle_code", data.profile != null ? data.profile.activeVehicleCode : string.Empty);
            }
            else
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_failed",
                    "reason", data.message,
                    "client_id", sender.ClientId);
            }

            TargetRpcUpdateProfile(sender, data.isSuccess, data.message, data.profile);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ConvertFreeXpServerRpc(int vehicleId, int amount, NetworkConnection sender = null)
        {
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out _))
            {
                return;
            }

            ConvertFreeXpAsync(sender, vehicleId, amount).Forget();
        }

        [ServerRpc(RequireOwnership = false)]
        private void GetProfileStatisticsServerRpc(NetworkConnection sender = null)
        {
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out _))
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

            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out ServerPlayerSession session))
            {
                TargetRpcConvertFreeXp(sender, false, "Not logged in.");
                return;
            }

            string token = session.Token;
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
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out ServerPlayerSession session))
            {
                TargetRpcUpdateProfileStatistics(sender, false, "Not logged in.", null);
                return;
            }

            string token = session.Token;
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
            _profileRequestInFlight = false;
            GameplayRuntimeDiagnostics.Record(
                success ? "profile_response_received" : "profile_response_failed",
                "success", success,
                "reason", success ? string.Empty : errorMessage,
                "owned_count", profile?.ownedVehicles?.Length ?? 0,
                "active_vehicle_code", profile != null ? profile.activeVehicleCode : string.Empty);
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
            if (success && profile != null)
            {
                _profileRefreshQueued = false;
                IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
                clientInfo.SetPlayerData(profile);
                clientInfo.SetClientId(target.ClientId);

                await RobotView.GenerateIconsAsync();

                MainMenu.In?.UpdatePlayerInfo(clientInfo.Profile);
                developmentTree?.RefreshProgressionStates();

                GameplayRuntimeDiagnostics.Record(
                    "profile_applied_client",
                    "client_id", target.ClientId,
                    "user_id", profile.id,
                    "username", profile.username,
                    "owned_count", profile.ownedVehicles?.Length ?? 0,
                    "active_vehicle_code", profile.activeVehicleCode);

                if (GameplaySpawner.In != null)
                {
                    GameplaySpawner.In.RequestPendingGameResultsServerRpc();
                }
            }
            else if (string.Equals(errorMessage, "Not logged in.", System.StringComparison.OrdinalIgnoreCase))
            {
                _profileRefreshQueued = true;
                GameplayRuntimeDiagnostics.Record("profile_response_retrying", "reason", "server_session_not_ready");
                return;
            }
            else
            {
                _profileRefreshQueued = false;
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
