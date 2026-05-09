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
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class RobotView : NetworkBehaviour
    {
        public Transform spawnPosition;
        public GameObject rootSpawnPlace;
        public Transform vehicleContainer;
        public VehicleButton vehicleButtonPrefab;

        private VehicleRoot _vehicleRoot;
        private readonly List<Button> _buttons = new List<Button>();

        private static RobotView _in;

        private void Awake() => _in = this;

        public static void GenerateIcons()
        {
            if (_in == null)
            {
                return;
            }

            Despawn();

            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            if (clientInfo?.Profile == null || clientInfo.Profile.ownedVehicles == null || clientInfo.Profile.ownedVehicles.Length == 0)
            {
                return;
            }

            OwnedVehicleDto selected = clientInfo.Profile.GetSelected();
            if (selected == null)
            {
                return;
            }

            List<RobotButtonData> list = new List<RobotButtonData>();

            foreach (OwnedVehicleDto vehicle in clientInfo.Profile.ownedVehicles)
            {
                if (vehicle == null)
                {
                    continue;
                }

                RobotButtonData robotList = new RobotButtonData();
                robotList.icon = ResourceManager.GetIcon(vehicle.code);
                robotList.id = vehicle.vehicleId;
                robotList.isSelected = selected.vehicleId == vehicle.vehicleId;
                robotList.name = vehicle.name;
                list.Add(robotList);
            }

            list.Sort(CompareByName);

            foreach (RobotButtonData robotList in list)
            {
                MakeIcons(robotList.icon, robotList.isSelected, robotList.id);
            }

            VehicleRoot vehicleRoot = ResourceManager.GetPrefab(selected.code);
            if (vehicleRoot == null)
            {
                return;
            }

            Spawn(vehicleRoot);

            UpdateUI();
        }

        private static void MakeIcons(Sprite sprite, bool isSelect, int id)
        {
            VehicleButton button = Instantiate(_in.vehicleButtonPrefab, _in.vehicleContainer.transform);
            button.vehicleImage.sprite = sprite;
            button.isSelect.gameObject.SetActive(isSelect);

            button.button.onClick.AddListener(() =>
            {
                IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
                OwnedVehicleDto selected = clientInfo.Profile.GetSelected();

                if (selected == null || id == selected.vehicleId)
                {
                    return;
                }

                Helpers.Loading.Show();
                _in.SelectVehicleServerRpc(id);
            });

            _in._buttons.Add(button.button);
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
            }

            UpdateUI();
            Helpers.Loading.Hide();
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

        private static int CompareByName(RobotButtonData left, RobotButtonData right)
        {
            string leftName = left != null ? left.name : string.Empty;
            string rightName = right != null ? right.name : string.Empty;
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

            foreach (Button button in _in._buttons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _in._buttons.Clear();
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

    [Serializable]
    public class RobotButtonData
    {
        public string name;
        public int id;
        public Sprite icon;
        public bool isSelected;
    }
}
