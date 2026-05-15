using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using Game.Scripts.API;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Resources;
using Game.Scripts.Core.Services;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.Player.Data;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Screens;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Scripts.UI.MainMenu
{
    public class RobotView : NetworkBehaviour
    {
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

        private static RobotView _in;

        private void Awake()
        {
            _in = this;
        }

        private void OnDestroy()
        {
            if (_in == this)
            {
                _buildVersion++;
                _spawnVersion++;
                _in = null;
            }
        }

        public static void GenerateIcons()
        {
            GenerateIconsAsync().Forget();
        }

        public static async UniTask GenerateIconsAsync()
        {
            RobotView view = _in;
            if (view == null)
            {
                return;
            }

            int buildVersion = ++view._buildVersion;
            view.DespawnContent();

            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            if (clientInfo?.Profile == null)
            {
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
                    vehicleData.ApplyVehicleLite(FindVehicleStats(vehicleStats, vehicle.code, vehicle.vehicleId));
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
            if (vehicleRoot == null || !IsBuildCurrent(view, buildVersion))
            {
                return;
            }

            await SpawnAsync(vehicleRoot);
            await UpdateUIAsync(view, buildVersion);
        }

        private static bool IsBuildCurrent(RobotView view, int buildVersion)
        {
            return view != null && _in == view && view._buildVersion == buildVersion;
        }

        private static void BuildVehicleSlots(RobotView view, List<VehicleSlotVehicleData> vehicles)
        {
            if (view == null || view.vehicleContainer == null)
            {
                return;
            }

            view.ClearVehicleSlots();

            if (view.vehicleSlotPrefab == null)
            {
                Debug.LogWarning("Cannot build vehicle slots because VehicleSlot prefab is not assigned.");
                return;
            }

            int slotCount = view.GetVisibleSlotCount(vehicles.Count);
            for (int i = 0; i < slotCount; i++)
            {
                VehicleSlotView slot = Instantiate(view.vehicleSlotPrefab, view.vehicleContainer.transform);
                slot.InitEmpty(i);
                view._slots.Add(slot);

                if (i >= vehicles.Count)
                {
                    continue;
                }

                if (view.vehiclePrefab == null)
                {
                    Debug.LogWarning("Cannot place vehicle in slot because Vehicle prefab is not assigned.");
                    continue;
                }

                VehicleItemView vehicle = Instantiate(view.vehiclePrefab);
                slot.PlaceVehicle(vehicle, vehicles[i], view.OnVehicleSelected);
            }
        }

        private int GetVisibleSlotCount(int vehicleCount)
        {
            int configuredSlots = Mathf.Max(0, unlockedVehicleSlots);
            return Mathf.Max(configuredSlots, vehicleCount);
        }

        private static VehicleLite FindVehicleStats(VehicleLite[] vehicles, string code, int vehicleId)
        {
            if (vehicles == null)
            {
                return null;
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

            for (int i = 0; i < vehicles.Length; i++)
            {
                VehicleLite vehicle = vehicles[i];
                if (vehicle != null && vehicle.id == vehicleId)
                {
                    return vehicle;
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

            Helpers.Loading.Show();
            SelectVehicleServerRpc(id);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SelectVehicleServerRpc(int code, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            SelectVehicleAsync(sender, code).Forget();
        }

        private async UniTask SelectVehicleAsync(NetworkConnection sender, int id)
        {
            string token = ServerPlayerSessions.GetToken(sender.ClientId);
            if (string.IsNullOrEmpty(token))
            {
                TargetRpcSelect(sender, false, "Not logged in.");
                return;
            }

            (bool ok, string msg) result = await UserVehiclesManager.SetActive(id, token);
            TargetRpcSelect(sender, result.ok, result.msg);
        }

        [TargetRpc]
        private void TargetRpcSelect(NetworkConnection target, bool success, string errorMessage)
        {
            if (success)
            {
                ProfileServer.UpdateProfile();
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
                Helpers.Loading.Hide();
            }
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
            GameplayAssistant.RebuildAllLayouts(l).Forget();
        }

        public static void Spawn(VehicleRoot vehicleRoot)
        {
            SpawnAsync(vehicleRoot).Forget();
        }

        private static async UniTask SpawnAsync(VehicleRoot vehicleRoot)
        {
            if (_in == null || vehicleRoot == null)
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
            _in.DespawnContent();
        }

        private void DespawnContent()
        {
            _spawnVersion++;

            if (_vehicleRoot != null)
            {
                Destroy(_vehicleRoot.gameObject);
                _vehicleRoot = null;
            }

            ClearVehicleSlots();

            if (rootSpawnPlace != null)
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
                    Transform child = vehicleContainer.GetChild(i);
                    if (child == null || !child.gameObject.activeSelf)
                    {
                        continue;
                    }

                    VehicleSlotView slot = child.GetComponent<VehicleSlotView>();
                    if (slot != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                VehicleSlotView slot = _slots[i];
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _slots.Clear();
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
            // 1) Components that depend on NetworkObject.
            GameplayAssistant.DestroyAll<NetworkObserver>(root);
            GameplayAssistant.DestroyAll<NetworkTransform>(root);

            // 2) Remove NetworkObject last.
            GameplayAssistant.DestroyAll<NetworkObject>(root);
        }
    }

}
