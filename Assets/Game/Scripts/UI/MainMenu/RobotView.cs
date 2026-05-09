using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Helpers;
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

        private static RobotView _in;

        private void Awake() => _in = this;

        public static void GenerateIcons()
        {
            GenerateIconsAsync().Forget();
        }

        public static async UniTask GenerateIconsAsync()
        {
            if (_in == null)
            {
                return;
            }

            Despawn();

            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            if (clientInfo?.Profile == null)
            {
                return;
            }

            OwnedVehicleDto selected = clientInfo.Profile.GetSelected();
            List<VehicleSlotVehicleData> vehicles = new List<VehicleSlotVehicleData>();

            if (clientInfo.Profile.ownedVehicles != null)
            {
                foreach (OwnedVehicleDto vehicle in clientInfo.Profile.ownedVehicles)
                {
                    if (vehicle == null)
                    {
                        continue;
                    }

                    VehicleSlotVehicleData vehicleData = new VehicleSlotVehicleData();
                    vehicleData.Icon = ResourceManager.GetIcon(vehicle.code);
                    vehicleData.VehicleId = vehicle.vehicleId;
                    vehicleData.IsSelected = selected != null && selected.vehicleId == vehicle.vehicleId;
                    vehicleData.Name = vehicle.name;
                    vehicles.Add(vehicleData);
                }
            }

            vehicles.Sort(CompareByName);

            BuildVehicleSlots(vehicles);

            if (selected == null)
            {
                await UpdateUIAsync();
                return;
            }

            VehicleRoot vehicleRoot = ResourceManager.GetPrefab(selected.code);
            if (vehicleRoot == null)
            {
                return;
            }

            await SpawnAsync(vehicleRoot);
            await UpdateUIAsync();
        }

        private static void BuildVehicleSlots(List<VehicleSlotVehicleData> vehicles)
        {
            if (_in.vehicleSlotPrefab == null)
            {
                Debug.LogWarning("Cannot build vehicle slots because VehicleSlot prefab is not assigned.");
                return;
            }

            int slotCount = _in.GetVisibleSlotCount(vehicles.Count);
            for (int i = 0; i < slotCount; i++)
            {
                VehicleSlotView slot = Instantiate(_in.vehicleSlotPrefab, _in.vehicleContainer.transform);
                slot.InitEmpty(i);
                _in._slots.Add(slot);

                if (i >= vehicles.Count)
                {
                    continue;
                }

                if (_in.vehiclePrefab == null)
                {
                    Debug.LogWarning("Cannot place vehicle in slot because Vehicle prefab is not assigned.");
                    continue;
                }

                VehicleItemView vehicle = Instantiate(_in.vehiclePrefab);
                slot.PlaceVehicle(vehicle, vehicles[i], _in.OnVehicleSelected);
            }
        }

        private int GetVisibleSlotCount(int vehicleCount)
        {
            int configuredSlots = Mathf.Max(0, unlockedVehicleSlots);
            return Mathf.Max(configuredSlots, vehicleCount);
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

        private static async UniTask UpdateUIAsync()
        {
            if (_in == null || _in.vehicleContainer == null)
            {
                return;
            }

            await UniTask.DelayFrame(1);
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

            _in.rootSpawnPlace.SetActive(true);
            _in._vehicleRoot = Instantiate(vehicleRoot, _in.spawnPosition.transform, true);
            _in._vehicleRoot.gameObject.SetActive(false);

            // Remove FishNet components and keep only local scripts.
            _in.StripFishNetRuntime(_in._vehicleRoot.gameObject);

            _in._vehicleRoot.transform.position = Vector3.zero;
            _in._vehicleRoot.transform.rotation = Quaternion.identity;

            await _in.ActivateAndInitNextFrame(_in._vehicleRoot);

            _in._vehicleRoot.transform.position = _in.spawnPosition.position;
            _in._vehicleRoot.transform.rotation = _in.spawnPosition.rotation;
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

            if (_in._vehicleRoot != null)
            {
                Destroy(_in._vehicleRoot.gameObject);
            }

            foreach (VehicleSlotView slot in _in._slots)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }

            _in._slots.Clear();
            _in.rootSpawnPlace.SetActive(false);
        }

        private async UniTask ActivateAndInitNextFrame(VehicleRoot go)
        {
            await UniTask.NextFrame();
            await UniTask.NextFrame();

            go.gameObject.SetActive(true);
            go.Init(true);
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
