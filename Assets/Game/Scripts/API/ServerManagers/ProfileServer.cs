using System;
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
        private const float ProfileStatisticsTimeoutSeconds = 4f;
        private const float FreeXpConversionTimeoutSeconds = 10f;
        private const int MaxProfileRefreshAttempts = 120;

        private static ProfileServer _in;
        public DevelopmentTree developmentTree;

        private Coroutine _profileRefreshCoroutine;
        private bool _profileRefreshQueued;
        private bool _profileRequestInFlight;
        private StandardLoadingOverlay.LoadingRequest _profileLoadingRequest;
        private StandardLoadingOverlay.LoadingRequest _profileStatisticsLoadingRequest;
        private StandardLoadingOverlay.LoadingRequest _freeXpLoadingRequest;
        private int _profileStatisticsRequestVersion;
        private int _freeXpRequestVersion;
        private int _profileRefreshAttempt;
        private string _profileFlowId;

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
            _profileStatisticsRequestVersion++;
            EndProfileStatisticsLoadingScope();
            _freeXpRequestVersion++;
            EndFreeXpLoadingScope();
            base.OnStopClient();
        }

        private void OnDestroy()
        {
            StopQueuedProfileRefresh();
            _profileStatisticsRequestVersion++;
            EndProfileStatisticsLoadingScope();
            _freeXpRequestVersion++;
            EndFreeXpLoadingScope();
            if (_in == this)
            {
                _in = null;
            }
        }

        public static void UpdateProfile(string flowId = null)
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

            if (!ClientAuthenticationState.HasAuthenticationSession)
            {
                GameplayRuntimeDiagnostics.Record("profile_refresh_skipped", "reason", "https_session_missing");
                _in.EndProfileLoadingScope();
                return;
            }

            if (Guid.TryParse(flowId, out Guid parsedFlowId))
            {
                _in._profileFlowId = parsedFlowId.ToString("N");
            }
            else if (!_in._profileRequestInFlight && !_in._profileRefreshQueued)
            {
                _in._profileFlowId = Guid.NewGuid().ToString("N");
            }

            _in.BeginProfileLoadingScope();
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
            EndProfileLoadingScope();
        }

        private IEnumerator ProfileRefreshLoop()
        {
            try
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
                    string requestId = Guid.NewGuid().ToString("N");
                    string flowId = _profileFlowId;
                    GameplayRuntimeDiagnostics.Record(
                        "profile_request_started",
                        "request_id", requestId,
                        "flow_id", flowId,
                        "attempt", _profileRefreshAttempt,
                        "client_initialized", IsClientInitialized,
                        "client_started", IsClientStarted);

                    GetProfileServerRpc(requestId, flowId);

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
                            "request_id", requestId,
                            "flow_id", flowId,
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
                    EndProfileLoadingScope();
                }
            }
            finally
            {
                if (_profileRefreshQueued || _profileRequestInFlight)
                {
                    GameplayRuntimeDiagnostics.Record(
                        "profile_refresh_failed",
                        "reason", "coroutine_stopped",
                        "attempts", _profileRefreshAttempt);
                    _profileRefreshQueued = false;
                    _profileRequestInFlight = false;
                    EndProfileLoadingScope();
                }

                _profileRefreshCoroutine = null;
            }
        }

        private bool IsProfileRpcReady()
        {
            return ClientAuthenticationState.HasAuthenticationSession
                   && IsClientInitialized
                   && IsClientStarted;
        }

        private void BeginProfileLoadingScope()
        {
            if (_profileLoadingRequest != null && _profileLoadingRequest.IsActive)
            {
                return;
            }

            _profileLoadingRequest = StandardLoadingOverlay.BeginRequest();
        }

        private void EndProfileLoadingScope()
        {
            if (_profileLoadingRequest == null)
            {
                return;
            }

            _profileLoadingRequest.End();
            _profileLoadingRequest = null;
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

            if (_in._freeXpLoadingRequest != null && _in._freeXpLoadingRequest.IsActive)
            {
                return;
            }

            int requestVersion = _in.BeginFreeXpLoadingScope();
            try
            {
                _in.ConvertFreeXpServerRpc(vehicleId, amount, requestVersion);
                _in.WaitForFreeXpConversionTimeoutAsync(requestVersion).Forget();
            }
            catch (System.Exception exception)
            {
                _in.EndFreeXpLoadingScope(requestVersion);
                GameplayRuntimeDiagnostics.Record("freexp_conversion_failed", "reason", exception.Message);
                FreeXpConversionPopup.NotifyConversionFailed();
                Popup.ShowText("Unable to convert Free XP.", Color.red);
            }
        }

        private int BeginFreeXpLoadingScope()
        {
            if (_freeXpLoadingRequest != null && _freeXpLoadingRequest.IsActive)
            {
                return _freeXpRequestVersion;
            }

            _freeXpRequestVersion++;
            _freeXpLoadingRequest = StandardLoadingOverlay.BeginRequest();
            return _freeXpRequestVersion;
        }

        private void EndFreeXpLoadingScope()
        {
            if (_freeXpLoadingRequest == null)
            {
                return;
            }

            _freeXpLoadingRequest.End();
            _freeXpLoadingRequest = null;
        }

        private void EndFreeXpLoadingScope(int requestVersion)
        {
            if (requestVersion != _freeXpRequestVersion)
            {
                return;
            }

            EndFreeXpLoadingScope();
        }

        private async UniTask WaitForFreeXpConversionTimeoutAsync(int requestVersion)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(FreeXpConversionTimeoutSeconds), ignoreTimeScale: true);
            if (this == null
                || requestVersion != _freeXpRequestVersion
                || _freeXpLoadingRequest == null)
            {
                return;
            }

            GameplayRuntimeDiagnostics.Record(
                "freexp_conversion_failed",
                "reason", "timeout");
            FreeXpConversionPopup.NotifyConversionFailed();
            Popup.ShowText("Free XP conversion timed out.", Color.red);
            EndFreeXpLoadingScope(requestVersion);
        }

        public static void RequestProfileStatistics()
        {
            if (ClientMenuTestMode.TryCreateProfileStatistics(out PlayerStatisticsDto localTestStatistics))
            {
                _in?.EndProfileStatisticsLoadingScope();
                MainMenu.In?.UpdateProfileStatistics(localTestStatistics);
                return;
            }

            if (ClientMenuTestMode.IsEnabled)
            {
                _in?.EndProfileStatisticsLoadingScope();
                ProfileStatisticsPopup.In?.ShowUnavailable("Server statistics are unavailable in offline menu test mode.");
                return;
            }

            if (_in == null)
            {
                ProfileStatisticsPopup.In?.ShowError("Profile service is not available.");
                return;
            }

            if (_in._profileStatisticsLoadingRequest != null
                && _in._profileStatisticsLoadingRequest.IsActive)
            {
                ProfileStatisticsPopup.In?.ShowLoading();
                return;
            }

            ProfileStatisticsPopup.In?.ShowLoading();
            int requestVersion = _in.BeginProfileStatisticsLoadingScope();
            try
            {
                _in.GetProfileStatisticsServerRpc();
                _in.WaitForProfileStatisticsTimeoutAsync(requestVersion).Forget();
            }
            catch (Exception exception)
            {
                _in.EndProfileStatisticsLoadingScope(requestVersion);
                GameplayRuntimeDiagnostics.Record("profile_statistics_failed", "reason", exception.Message);
                ProfileStatisticsPopup.In?.ShowError("Unable to load profile statistics.");
            }
        }

        private int BeginProfileStatisticsLoadingScope()
        {
            if (_profileStatisticsLoadingRequest != null && _profileStatisticsLoadingRequest.IsActive)
            {
                return _profileStatisticsRequestVersion;
            }

            _profileStatisticsRequestVersion++;
            _profileStatisticsLoadingRequest = StandardLoadingOverlay.BeginRequest();
            return _profileStatisticsRequestVersion;
        }

        private void EndProfileStatisticsLoadingScope()
        {
            if (_profileStatisticsLoadingRequest == null)
            {
                return;
            }

            _profileStatisticsLoadingRequest.End();
            _profileStatisticsLoadingRequest = null;
        }

        private void EndProfileStatisticsLoadingScope(int requestVersion)
        {
            if (requestVersion != _profileStatisticsRequestVersion)
            {
                return;
            }

            EndProfileStatisticsLoadingScope();
        }

        private async UniTask WaitForProfileStatisticsTimeoutAsync(int requestVersion)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(ProfileStatisticsTimeoutSeconds), ignoreTimeScale: true);
            if (this == null
                || requestVersion != _profileStatisticsRequestVersion
                || _profileStatisticsLoadingRequest == null)
            {
                return;
            }

            GameplayRuntimeDiagnostics.Record("profile_statistics_failed", "reason", "timeout");
            EndProfileStatisticsLoadingScope(requestVersion);
            MainMenu.In?.ShowProfileStatisticsError("Profile statistics request timed out.");
        }

        [ServerRpc(RequireOwnership = false)]
        private void GetProfileServerRpc(string requestId, string flowId, NetworkConnection sender = null)
        {
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out _))
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_rejected",
                    "reason", "server_session_not_authenticated",
                    "request_id", requestId,
                    "flow_id", flowId,
                    "client_id", sender != null ? sender.ClientId : -1);
                return;
            }

            RequestGetProfileAsync(sender, requestId, flowId).Forget();
        }

        private async UniTask RequestGetProfileAsync(NetworkConnection sender, string requestId, string flowId)
        {
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out ServerPlayerSession session))
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_failed",
                    "reason", "server_session_missing",
                    "request_id", requestId,
                    "flow_id", flowId,
                    "client_id", sender != null ? sender.ClientId : -1);
                TargetRpcUpdateProfile(sender, requestId, flowId, false, "Not logged in.", null);
                return;
            }

            string token = session.Token;
            if (string.IsNullOrEmpty(token))
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_failed",
                    "reason", "delegated_token_missing",
                    "request_id", requestId,
                    "flow_id", flowId,
                    "client_id", sender.ClientId);
                TargetRpcUpdateProfile(sender, requestId, flowId, false, "Not logged in.", null);
                return;
            }

            (bool isSuccess, string message, PlayerProfile profile) data = await PlayersManager.GetMyProfile(token, requestId, flowId);

            if (data.isSuccess)
            {
                ServerPlayerSessions.SetProfile(sender, data.profile);
                GameplayRuntimeDiagnostics.Record(
                    "profile_loaded_server",
                    "request_id", requestId,
                    "flow_id", flowId,
                    "client_id", sender.ClientId,
                    "user_id", data.profile != null ? data.profile.id : 0,
                    "owned_count", data.profile?.ownedVehicles?.Length ?? 0,
                    "active_vehicle_code", data.profile != null ? data.profile.activeVehicleCode : string.Empty);
            }
            else
            {
                GameplayRuntimeDiagnostics.Record(
                    "profile_request_failed",
                    "reason_category", string.IsNullOrWhiteSpace(data.message) ? "empty_api_error" : "api_rejected_request",
                    "request_id", requestId,
                    "flow_id", flowId,
                    "client_id", sender.ClientId);
            }

            TargetRpcUpdateProfile(sender, requestId, flowId, data.isSuccess, data.message, data.profile);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ConvertFreeXpServerRpc(int vehicleId, int amount, int requestVersion, NetworkConnection sender = null)
        {
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out _))
            {
                TargetRpcConvertFreeXp(sender, requestVersion, false, "Not logged in.");
                return;
            }

            ConvertFreeXpAsync(sender, vehicleId, amount, requestVersion).Forget();
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

        private async UniTask ConvertFreeXpAsync(NetworkConnection sender, int vehicleId, int amount, int requestVersion)
        {
            try
            {
                if (vehicleId <= 0 || amount <= 0)
                {
                    TargetRpcConvertFreeXp(sender, requestVersion, false, "Choose a vehicle and XP amount.");
                    return;
                }

                if (!ServerPlayerSessions.TryGetAuthenticated(sender, out ServerPlayerSession session))
                {
                    TargetRpcConvertFreeXp(sender, requestVersion, false, "Not logged in.");
                    return;
                }

                string token = session.Token;
                if (string.IsNullOrEmpty(token))
                {
                    TargetRpcConvertFreeXp(sender, requestVersion, false, "Not logged in.");
                    return;
                }

                (bool ok, string message) result = await UserVehiclesManager.ConvertFreeXp(vehicleId, amount, token);
                TargetRpcConvertFreeXp(sender, requestVersion, result.ok, result.message);
            }
            catch (System.Exception exception)
            {
                GameplayRuntimeDiagnostics.Record(
                    "freexp_conversion_failed",
                    "reason", exception.Message,
                    "vehicle_id", vehicleId,
                    "amount", amount);
                TargetRpcConvertFreeXp(sender, requestVersion, false, "Unable to convert Free XP.");
            }
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
        private void TargetRpcUpdateProfile(
            NetworkConnection target,
            string requestId,
            string flowId,
            bool success,
            string errorMessage,
            PlayerProfile profile)
        {
            _profileRequestInFlight = false;
            GameplayRuntimeDiagnostics.Record(
                success ? "profile_response_received" : "profile_response_failed",
                "request_id", requestId,
                "flow_id", flowId,
                "success", success,
                "reason_category", success ? "none" : string.IsNullOrWhiteSpace(errorMessage) ? "empty_server_error" : "server_api_rejected_request",
                "owned_count", profile?.ownedVehicles?.Length ?? 0,
                "active_vehicle_code", profile != null ? profile.activeVehicleCode : string.Empty);
            TargetRpcUpdateProfileAsync(target, success, errorMessage, profile).Forget();
        }

        [TargetRpc]
        private void TargetRpcConvertFreeXp(NetworkConnection target, int requestVersion, bool success, string errorMessage)
        {
            if (requestVersion != _freeXpRequestVersion)
            {
                if (success)
                {
                    UpdateProfile();
                }

                return;
            }

            if (success)
            {
                try
                {
                    FreeXpConversionPopup.NotifyConversionSucceeded();
                    UpdateProfile();
                }
                finally
                {
                    EndFreeXpLoadingScope(requestVersion);
                }

                return;
            }

            FreeXpConversionPopup.NotifyConversionFailed();
            Popup.ShowText(errorMessage, Color.red);
            EndFreeXpLoadingScope(requestVersion);
        }

        [TargetRpc]
        private void TargetRpcUpdateProfileStatistics(NetworkConnection target, bool success, string errorMessage, PlayerStatisticsDto statistics)
        {
            EndProfileStatisticsLoadingScope();
            if (success && statistics != null)
            {
                MainMenu.In?.UpdateProfileStatistics(statistics);
                return;
            }

            MainMenu.In?.ShowProfileStatisticsError(errorMessage);
        }

        private async UniTask TargetRpcUpdateProfileAsync(NetworkConnection target, bool success, string errorMessage, PlayerProfile profile)
        {
            bool keepLoadingScope = false;
            try
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
                    keepLoadingScope = true;
                    GameplayRuntimeDiagnostics.Record("profile_response_retrying", "reason", "server_session_not_ready");
                }
                else
                {
                    _profileRefreshQueued = false;
                    Popup.ShowText(errorMessage, Color.red);
                }
            }
            finally
            {
                if (!keepLoadingScope)
                {
                    EndProfileLoadingScope();
                }
            }
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
