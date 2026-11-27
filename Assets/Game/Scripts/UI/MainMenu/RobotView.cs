using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using RAV.Scripts.API;
using RAV.Scripts.API.Endpoints;
using RAV.Scripts.API.Models;
using RAV.Scripts.API.ServerManagers;
using RAV.Scripts.Core.Helpers;
using RAV.Scripts.Core.Resources;
using RAV.Scripts.Core.Services;
using RAV.Scripts.Diagnostics;
using RAV.Scripts.Gameplay.Robots;
using RAV.Scripts.MenuController;
using RAV.Scripts.Networking.Sessions;
using RAV.Scripts.Player.Data;
using RAV.Scripts.Testing;
using RAV.Scripts.UI.Helpers;
using RAV.Scripts.UI.Screens;
using NaughtyAttributes;
using UnityEngine;

namespace RAV.Scripts.UI.MainMenu
{
    public class RobotView : NetworkBehaviour
    {
        private const float VehicleSelectionTimeoutSeconds = 10f;

        public Transform spawnPosition;
        public GameObject rootSpawnPlace;
        public Transform vehicleContainer;
        public VehicleSlotView vehicleSlotPrefab;
        public VehicleItemView vehiclePrefab;
        [SerializeField] private int unlockedVehicleSlots = 3;

        private VehicleRoot _vehicleRoot;
        private readonly List<VehicleSlotView> _slots = new List<VehicleSlotView>();
        private int _buildVersion;
        private int _spawnVersion;
        private int _vehicleSelectionRequestVersion;
        private StandardLoadingOverlay.LoadingRequest _vehicleSelectionLoadingRequest;
        private bool _hiddenForGameplay;
        private bool _hiddenForMatchmaking;
        private bool _wasActiveBeforeMatchmaking;

        private GameObject MatchmakingVisibilityTarget => rootSpawnPlace != null ? rootSpawnPlace : gameObject;

        private static RobotView _in;

        private void Awake()
        {
            _in = this;
            MenuManager.OnEnable += HandleMenuEnabled;
            MenuManager.OnDisable += HandleMenuDisabled;
        }

        private void OnDestroy()
        {
            MenuManager.OnEnable -= HandleMenuEnabled;
            MenuManager.OnDisable -= HandleMenuDisabled;
            _vehicleSelectionRequestVersion++;
            EndVehicleSelectionLoading();
            if (_in == this)
            {
                _buildVersion++;
                _spawnVersion++;
                _in = null;
            }
        }

        private void HandleMenuEnabled(MenuType type)
        {
            if (type == MenuType.Matchmaking)
            {
                HideForMatchmaking();
                return;
            }

            RestoreAfterMatchmaking();
        }

        private void HandleMenuDisabled(MenuType type)
        {
            if (type == MenuType.Matchmaking)
            {
                RestoreAfterMatchmaking();
            }
        }

        private void HideForMatchmaking()
        {
            if (_hiddenForMatchmaking)
            {
                return;
            }

            GameObject visibilityTarget = MatchmakingVisibilityTarget;
            _wasActiveBeforeMatchmaking = visibilityTarget.activeSelf;
            _hiddenForMatchmaking = true;
            visibilityTarget.SetActive(false);
        }

        private void RestoreAfterMatchmaking()
        {
            if (!_hiddenForMatchmaking)
            {
                return;
            }

            bool shouldBeActive = _wasActiveBeforeMatchmaking;
            _hiddenForMatchmaking = false;
            GameObject visibilityTarget = MatchmakingVisibilityTarget;
            if (visibilityTarget.activeSelf != shouldBeActive)
            {
                visibilityTarget.SetActive(shouldBeActive);
            }
        }

        public static void GenerateIcons()
        {
            GenerateIconsAsync().Forget();
        }

        public static async UniTask GenerateIconsAsync()
        {
            RobotView view = _in;
            if (view == null || view._hiddenForGameplay)
            {
                GameplayRuntimeDiagnostics.Record("robot_menu_refresh_skipped", "reason", "view_missing");
                return;
            }

            StandardLoadingOverlay.LoadingRequest loadingRequest = StandardLoadingOverlay.BeginRequest();
            try
            {
                await GenerateIconsInternalAsync(view);
            }
            finally
            {
                loadingRequest.End();
            }
        }

        public override void OnStopClient()
        {
            _vehicleSelectionRequestVersion++;
            EndVehicleSelectionLoading();
            base.OnStopClient();
        }

        private static async UniTask GenerateIconsInternalAsync(RobotView view)
        {

            int buildVersion = ++view._buildVersion;
            view.DespawnPreviewRobot();

            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            if (clientInfo?.Profile == null)
            {
                GameplayRuntimeDiagnostics.Record("robot_menu_refresh_failed", "reason", "profile_missing");
                view.ClearVehicleSlots();
                return;
            }

            GameplayRuntimeDiagnostics.Record(
                "robot_menu_refresh_started",
                "user_id", clientInfo.Profile.id,
                "username", clientInfo.Profile.username,
                "owned_count", clientInfo.Profile.ownedVehicles?.Length ?? 0,
                "active_vehicle_code", clientInfo.Profile.activeVehicleCode);

            if (ClientMenuTestMode.TryCreateVehicles(out VehicleLite[] localTestVehicles))
            {
                await BuildLocalTestMenuAsync(view, clientInfo.Profile, localTestVehicles, buildVersion);
                return;
            }

            OwnedVehicleDto selected = clientInfo.Profile.GetSelected();
            List<VehicleSlotVehicleData> vehicles = new List<VehicleSlotVehicleData>();
            (bool isSuccess, string message, VehicleLite[] items) vehicleStatsResult = await VehiclesManager.GetAll();
            if (!IsBuildCurrent(view, buildVersion))
            {
                return;
            }

            VehicleLite[] vehicleStats = vehicleStatsResult.isSuccess && vehicleStatsResult.items != null
                ? vehicleStatsResult.items
                : Array.Empty<VehicleLite>();
            GameplayRuntimeDiagnostics.Record(
                vehicleStatsResult.isSuccess ? "robot_catalog_loaded" : "robot_catalog_failed",
                "success", vehicleStatsResult.isSuccess,
                "count", vehicleStats.Length,
                "message", vehicleStatsResult.isSuccess ? "ok" : vehicleStatsResult.message);

            if (clientInfo.Profile.ownedVehicles != null)
            {
                foreach (OwnedVehicleDto vehicle in clientInfo.Profile.ownedVehicles)
                {
                    if (vehicle == null)
                    {
                        continue;
                    }

                    VehicleSlotVehicleData vehicleData = new VehicleSlotVehicleData();
                    vehicleData.Icon = GameResourceManager.GetIcon(vehicle.code);
                    vehicleData.VehicleId = vehicle.vehicleId;
                    vehicleData.IsSelected = selected != null && selected.vehicleId == vehicle.vehicleId;
                    vehicleData.Name = vehicle.name;
                    vehicleData.ViewRange = VehicleRuntimeStats.ResolveViewRange(vehicle.viewRange);
                    vehicleData.EnginePower = vehicle.enginePower;
                    vehicleData.ApplyVehicleLite(FindVehicleStats(vehicleStats, vehicle.code, vehicle.vehicleId));
                    vehicles.Add(vehicleData);
                }
            }

            vehicles.Sort(CompareByName);

            BuildVehicleSlots(view, vehicles);
            GameplayRuntimeDiagnostics.Record(
                "robot_menu_slots_built",
                "count", vehicles.Count,
                "visible_slots", view.GetVisibleSlotCount(vehicles.Count));
            if (!IsBuildCurrent(view, buildVersion))
            {
                return;
            }

            if (selected == null)
            {
                GameplayRuntimeDiagnostics.Record("robot_menu_preview_skipped", "reason", "selected_vehicle_missing");
                await UpdateUIAsync(view, buildVersion);
                return;
            }

            VehicleRoot vehicleRoot = GameResourceManager.GetPrefab(selected.code);
            if (vehicleRoot == null || !IsBuildCurrent(view, buildVersion))
            {
                GameplayRuntimeDiagnostics.Record(
                    "robot_menu_preview_failed",
                    "reason", "selected_prefab_missing",
                    "vehicle_code", selected.code);
                return;
            }

            await SpawnAsync(vehicleRoot);
            GameplayRuntimeDiagnostics.Record("robot_menu_preview_spawned", "vehicle_code", selected.code);
            await UpdateUIAsync(view, buildVersion);
        }

        private static async UniTask BuildLocalTestMenuAsync(
            RobotView view,
            PlayerProfile profile,
            VehicleLite[] localTestVehicles,
            int buildVersion)
        {
            if (view == null || profile == null || localTestVehicles == null || localTestVehicles.Length == 0)
            {
                return;
            }

            OwnedVehicleDto selected = profile.GetSelected();
            List<VehicleSlotVehicleData> vehicles = new List<VehicleSlotVehicleData>();
            if (profile.ownedVehicles != null)
            {
                for (int i = 0; i < profile.ownedVehicles.Length; i++)
                {
                    OwnedVehicleDto vehicle = profile.ownedVehicles[i];
                    if (vehicle == null)
                    {
                        continue;
                    }

                    VehicleSlotVehicleData vehicleData = new VehicleSlotVehicleData();
                    vehicleData.Icon = GameResourceManager.GetIcon(vehicle.code);
                    vehicleData.VehicleId = vehicle.vehicleId;
                    vehicleData.IsSelected = selected != null && selected.vehicleId == vehicle.vehicleId;
                    vehicleData.Name = vehicle.name;
                    vehicleData.ViewRange = VehicleRuntimeStats.ResolveViewRange(vehicle.viewRange);
                    vehicleData.EnginePower = vehicle.enginePower;
                    vehicleData.ApplyVehicleLite(FindVehicleStats(localTestVehicles, vehicle.code, vehicle.vehicleId));
                    vehicles.Add(vehicleData);
                }
            }

            vehicles.Sort(CompareByName);
            BuildVehicleSlots(view, vehicles);
            if (!IsBuildCurrent(view, buildVersion))
            {
                return;
            }

            if (selected == null)
            {
                await UpdateUIAsync(view, buildVersion);
                return;
            }

            VehicleRoot vehicleRoot = GameResourceManager.GetPrefab(selected.code);
            if (vehicleRoot != null && IsBuildCurrent(view, buildVersion))
            {
                await SpawnAsync(vehicleRoot);
            }

            await UpdateUIAsync(view, buildVersion);
        }

        private static bool IsBuildCurrent(RobotView view, int buildVersion)
        {
            return view != null
                && _in == view
                && !view._hiddenForGameplay
                && view._buildVersion == buildVersion;
        }

        private static void BuildVehicleSlots(RobotView view, List<VehicleSlotVehicleData> vehicles)
        {
            if (view == null || view.vehicleContainer == null)
            {
                return;
            }

            if (view.vehicleSlotPrefab == null)
            {
                Debug.LogWarning("Cannot build vehicle slots because VehicleSlot prefab is not assigned.");
                return;
            }

            int slotCount = view.GetVisibleSlotCount(vehicles.Count);
            List<VehicleSlotView> availableSlots = new List<VehicleSlotView>(view._slots);
            List<VehicleSlotView> updatedSlots = new List<VehicleSlotView>(slotCount);

            for (int i = 0; i < vehicles.Count; i++)
            {
                VehicleSlotVehicleData vehicleData = vehicles[i];
                VehicleSlotView slot = TakeSlotForVehicle(availableSlots, vehicleData.VehicleId);
                if (slot == null)
                {
                    slot = TakeAnySlot(availableSlots);
                }

                if (slot == null)
                {
                    slot = view.CreateVehicleSlot();
                }

                view.UpdateVehicleSlot(slot, i, vehicleData);
                slot.transform.SetSiblingIndex(i);
                updatedSlots.Add(slot);
            }

            for (int i = vehicles.Count; i < slotCount; i++)
            {
                VehicleSlotView slot = TakeAnySlot(availableSlots);
                if (slot == null)
                {
                    slot = view.CreateVehicleSlot();
                }

                slot.InitEmpty(i);
                slot.transform.SetSiblingIndex(i);
                updatedSlots.Add(slot);
            }

            for (int i = 0; i < availableSlots.Count; i++)
            {
                if (availableSlots[i] != null)
                {
                    Destroy(availableSlots[i].gameObject);
                }
            }

            view._slots.Clear();
            view._slots.AddRange(updatedSlots);

            VehicleSlotVehicleData selectedVehicle = null;
            for (int i = 0; i < vehicles.Count; i++)
            {
                if (vehicles[i].IsSelected)
                {
                    selectedVehicle = vehicles[i];
                    break;
                }
            }

            MainMenu.In?.UpdateVehicleInfo(selectedVehicle);
        }

        private static VehicleSlotView TakeSlotForVehicle(List<VehicleSlotView> slots, int vehicleId)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                VehicleSlotView slot = slots[i];
                if (slot == null || !slot.HasVehicle || slot.VehicleId != vehicleId)
                {
                    continue;
                }

                slots.RemoveAt(i);
                return slot;
            }

            return null;
        }

        private static VehicleSlotView TakeAnySlot(List<VehicleSlotView> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                VehicleSlotView slot = slots[i];
                slots.RemoveAt(i);
                if (slot != null)
                {
                    return slot;
                }

                i--;
            }

            return null;
        }

        private int GetVisibleSlotCount(int vehicleCount)
        {
            int configuredSlots = Mathf.Max(0, unlockedVehicleSlots);
            return Mathf.Max(configuredSlots, vehicleCount);
        }

        private VehicleSlotView CreateVehicleSlot()
        {
            VehicleSlotView slot = Instantiate(vehicleSlotPrefab, vehicleContainer);
            slot.InitEmpty(_slots.Count);
            return slot;
        }

        private void UpdateVehicleSlot(VehicleSlotView slot, int slotIndex, VehicleSlotVehicleData vehicleData)
        {
            slot.SetSlotIndex(slotIndex);
            if (slot.TryUpdateVehicle(vehicleData, OnVehicleSelected))
            {
                return;
            }

            slot.InitEmpty(slotIndex);
            if (vehiclePrefab == null)
            {
                Debug.LogWarning("Cannot place vehicle in slot because Vehicle prefab is not assigned.");
                return;
            }

            VehicleItemView vehicle = Instantiate(vehiclePrefab);
            slot.PlaceVehicle(vehicle, vehicleData, OnVehicleSelected);
        }

        private static VehicleLite FindVehicleStats(VehicleLite[] vehicles, string code, int vehicleId)
        {
            if (vehicles == null)
            {
                return null;
            }

            for (int i = 0; i < vehicles.Length; i++)
            {
                VehicleLite vehicle = vehicles[i];
                if (vehicle != null && vehicle.id == vehicleId)
                {
                    return vehicle;
                }
            }

            if (!string.IsNullOrEmpty(code))
            {
                for (int i = 0; i < vehicles.Length; i++)
                {
                    VehicleLite vehicle = vehicles[i];
                    if (vehicle != null && vehicle.code == code)
                    {
                        return vehicle;
                    }
                }
            }

            return null;
        }

        private void OnVehicleSelected(int id)
        {
            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            OwnedVehicleDto selected = clientInfo?.Profile?.GetSelected();

            if (selected == null || id == selected.vehicleId)
            {
                return;
            }

            if (ClientMenuTestMode.TrySelectVehicle(id))
            {
                ProfileServer.UpdateProfile();
                return;
            }

            int requestVersion = BeginVehicleSelectionLoading();
            try
            {
                SelectVehicleServerRpc(id, requestVersion);
                WaitForVehicleSelectionTimeoutAsync(requestVersion).Forget();
            }
            catch (Exception exception)
            {
                GameplayRuntimeDiagnostics.Record(
                    "robot_selection_failed",
                    "reason", exception.Message,
                    "vehicle_id", id);
                Popup.ShowText("Unable to switch robot.", Color.red);
                EndVehicleSelectionLoading(requestVersion);
            }
        }

        private int BeginVehicleSelectionLoading()
        {
            if (_vehicleSelectionLoadingRequest != null && _vehicleSelectionLoadingRequest.IsActive)
            {
                return _vehicleSelectionRequestVersion;
            }

            _vehicleSelectionRequestVersion++;
            _vehicleSelectionLoadingRequest = StandardLoadingOverlay.BeginRequest();
            return _vehicleSelectionRequestVersion;
        }

        private void EndVehicleSelectionLoading()
        {
            if (_vehicleSelectionLoadingRequest == null)
            {
                return;
            }

            _vehicleSelectionLoadingRequest.End();
            _vehicleSelectionLoadingRequest = null;
        }

        private void EndVehicleSelectionLoading(int requestVersion)
        {
            if (requestVersion != _vehicleSelectionRequestVersion)
            {
                return;
            }

            EndVehicleSelectionLoading();
        }

        private async UniTask WaitForVehicleSelectionTimeoutAsync(int requestVersion)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(VehicleSelectionTimeoutSeconds), ignoreTimeScale: true);
            if (this == null
                || requestVersion != _vehicleSelectionRequestVersion
                || _vehicleSelectionLoadingRequest == null)
            {
                return;
            }

            GameplayRuntimeDiagnostics.Record(
                "robot_selection_failed",
                "reason", "timeout");
            Popup.ShowText("Robot switch timed out.", Color.red);
            EndVehicleSelectionLoading(requestVersion);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SelectVehicleServerRpc(int code, int requestVersion, NetworkConnection sender = null)
        {
            if (!ServerPlayerSessions.TryGetAuthenticated(sender, out _))
            {
                TargetRpcSelect(sender, requestVersion, false, "Not logged in.");
                return;
            }

            SelectVehicleAsync(sender, code, requestVersion).Forget();
        }

        private async UniTask SelectVehicleAsync(NetworkConnection sender, int id, int requestVersion)
        {
            try
            {
                if (!ServerPlayerSessions.TryGetAuthenticated(sender, out ServerPlayerSession session))
                {
                    TargetRpcSelect(sender, requestVersion, false, "Not logged in.");
                    return;
                }

                string token = session.Token;
                if (string.IsNullOrEmpty(token))
                {
                    TargetRpcSelect(sender, requestVersion, false, "Not logged in.");
                    return;
                }

                (bool ok, string msg) result = await UserVehiclesManager.SetActive(id, token);
                TargetRpcSelect(sender, requestVersion, result.ok, result.msg);
            }
            catch (Exception exception)
            {
                GameplayRuntimeDiagnostics.Record(
                    "robot_selection_failed",
                    "reason", exception.Message,
                    "vehicle_id", id);
                TargetRpcSelect(sender, requestVersion, false, "Unable to switch robot.");
            }
        }

        [TargetRpc]
        private void TargetRpcSelect(NetworkConnection target, int requestVersion, bool success, string errorMessage)
        {
            if (requestVersion != _vehicleSelectionRequestVersion)
            {
                if (success)
                {
                    ProfileServer.UpdateProfile();
                }

                return;
            }

            if (success)
            {
                try
                {
                    ProfileServer.UpdateProfile();
                }
                finally
                {
                    EndVehicleSelectionLoading(requestVersion);
                }

                return;
            }

            Popup.ShowText(errorMessage, Color.red);
            EndVehicleSelectionLoading(requestVersion);
        }

        [Button]
        public static void UpdateUI()
        {
            UpdateUIAsync().Forget();
        }

        private static async UniTask UpdateUIAsync(RobotView expectedView = null, int expectedBuildVersion = 0)
        {
            if (_in == null || _in.vehicleContainer == null)
            {
                return;
            }

            await UniTask.DelayFrame(1);
            if (expectedView != null && !IsBuildCurrent(expectedView, expectedBuildVersion))
            {
                return;
            }

            List<Transform> l = new List<Transform>();
            l.Add(_in.vehicleContainer);
            await GameplayAssistant.RebuildAllLayouts(l);
        }

        public static void Spawn(VehicleRoot vehicleRoot)
        {
            SpawnAsync(vehicleRoot).Forget();
        }

        private static async UniTask SpawnAsync(VehicleRoot vehicleRoot)
        {
            if (_in == null || _in._hiddenForGameplay || vehicleRoot == null)
            {
                return;
            }

            RobotView view = _in;
            int spawnVersion = ++view._spawnVersion;

            view.rootSpawnPlace.SetActive(true);
            VehicleRoot spawnedVehicleRoot = Instantiate(vehicleRoot, view.spawnPosition.transform, true);
            view._vehicleRoot = spawnedVehicleRoot;
            spawnedVehicleRoot.gameObject.SetActive(false);

            // Remove FishNet components and keep only local scripts.
            view.StripFishNetRuntime(spawnedVehicleRoot.gameObject);

            spawnedVehicleRoot.transform.position = Vector3.zero;
            spawnedVehicleRoot.transform.rotation = Quaternion.identity;

            bool activated = await view.ActivateAndInitNextFrame(spawnedVehicleRoot, spawnVersion);
            if (!activated || view == null || spawnedVehicleRoot == null || view._vehicleRoot != spawnedVehicleRoot)
            {
                return;
            }

            spawnedVehicleRoot.transform.position = view.spawnPosition.position;
            spawnedVehicleRoot.transform.rotation = view.spawnPosition.rotation;
        }

        private static int CompareByName(VehicleSlotVehicleData left, VehicleSlotVehicleData right)
        {
            string leftName = left != null ? left.Name : string.Empty;
            string rightName = right != null ? right.Name : string.Empty;
            return string.Compare(leftName, rightName, StringComparison.Ordinal);
        }

        public static void Despawn()
        {
            if (_in == null)
            {
                return;
            }

            _in._buildVersion++;
            _in.DespawnContent(hidePreviewPlatform: true);
        }

        public static void HideForGameplay()
        {
            if (_in == null)
            {
                return;
            }

            _in._hiddenForGameplay = true;
            Despawn();
        }

        public static void ShowForMainMenu()
        {
            if (_in != null)
            {
                _in._hiddenForGameplay = false;
            }
        }

        private void DespawnContent(bool hidePreviewPlatform)
        {
            DespawnPreviewRobot();
            ClearVehicleSlots();

            if (hidePreviewPlatform && rootSpawnPlace != null)
            {
                rootSpawnPlace.SetActive(false);
            }
        }

        private void ClearVehicleSlots()
        {
            if (vehicleContainer != null)
            {
                for (int i = vehicleContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(vehicleContainer.GetChild(i).gameObject);
                }
            }

            _slots.Clear();
        }

        private void DespawnPreviewRobot()
        {
            _spawnVersion++;

            if (_vehicleRoot != null)
            {
                Destroy(_vehicleRoot.gameObject);
                _vehicleRoot = null;
            }
        }

        private async UniTask<bool> ActivateAndInitNextFrame(VehicleRoot go, int spawnVersion)
        {
            await UniTask.NextFrame();
            if (this == null || go == null || _spawnVersion != spawnVersion)
            {
                return false;
            }

            await UniTask.NextFrame();
            if (this == null || go == null || _spawnVersion != spawnVersion)
            {
                return false;
            }

            go.gameObject.SetActive(true);
            go.Init(true);
            return true;
        }

        private void StripFishNetRuntime(GameObject root)
        {
            if (_vehicleRoot != null)
            {
                _vehicleRoot.DestroyConfiguredMenuStripComponents();
            }
        }
    }

}
