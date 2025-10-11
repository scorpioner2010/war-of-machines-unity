using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Player.Data;
using NaughtyAttributes;
using NewDropDude.Script.API.ServerManagers;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using Game.Scripts.API;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Player.Data;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Screens;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.MenuController;
using Game.Scripts.Server;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.MainMenu;
using NewDropDude.Script.API.ServerManagers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class RobotView : NetworkBehaviour
    {
        public Transform spawnPosition;
        public GameObject rootSpawnPlace;
        public Transform vehicleContainer;
        public VehicleButton vehicleButtonPrefab;

        private TankRoot _tankRoot;
        private List<Button> _buttons = new();
        
        private static RobotView _in;
        
        private void Awake() => _in = this;

        public static void GenerateIcons()
        {
            Despawn();
            
            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            OwnedVehicleDto selected = clientInfo.Profile.GetSelected();

            List<RobotList> list = new();
            
            foreach (OwnedVehicleDto vehicle in clientInfo.Profile.ownedVehicles)
            {
                RobotList robotList = new RobotList();
                robotList.icon = ResourceManager.GetIcon(vehicle.code);
                robotList.id = vehicle.vehicleId;
                robotList.isSelected = selected.vehicleId == vehicle.vehicleId;
                robotList.name = vehicle.name;
                list.Add(robotList);
            }
            
            list = list.OrderBy(x => x.name).ToList();

            foreach (RobotList robotList in list)
            {
                MakeIcons(robotList.icon, robotList.isSelected, robotList.id);
            }
            
            TankRoot vehicleRoot = ResourceManager.GetPrefab(selected.code);
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
                
                if (id == selected.vehicleId)
                {
                    return;
                }
                
                Helpers.Loading.Show();
                _in.SelectRPC(_in.ClientManager.Connection.ClientId, id);
            });
            
            _in._buttons.Add(button.button);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SelectRPC(int clientID, int code)
        {
            Select(clientID, code);
        }
            
        private async void Select(int clientID, int id)
        {
            string token = RegisterServer.GetToken(clientID);
            (bool ok, string msg) result =  await UserVehiclesManager.SetActive(id, token);
            NetworkConnection senderConn = ServerManager.Clients[clientID];
            TargetRpcSelect(senderConn, result.ok, result.msg);
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
        public static async void UpdateUI()
        {
            await UniTask.DelayFrame(1);
            List<Transform> l = new();
            l.Add(_in.vehicleContainer);
            GameplayAssistant.RebuildAllLayouts(l).Forget();
        }
        
        public static async void Spawn(TankRoot  tankRoot)
        {
            _in.rootSpawnPlace.SetActive(true);
            _in._tankRoot = Instantiate(tankRoot, _in.spawnPosition.transform, true);
            _in._tankRoot.gameObject.SetActive(false);
            
            // Прибираємо FishNet-компоненти (лишаємо твої скрипти).
            _in.StripFishNetRuntime(_in._tankRoot.gameObject);

            _in._tankRoot.transform.position = Vector3.zero;
            _in._tankRoot.transform.rotation = Quaternion.identity;

            await _in.ActivateAndInitNextFrame(_in._tankRoot);
            
            _in._tankRoot.transform.position = _in.spawnPosition.position;
            _in._tankRoot.transform.rotation = _in.spawnPosition.rotation;
        }
        
        public static void Despawn()
        {
            if (_in._tankRoot != null)
            {
                Destroy(_in._tankRoot.gameObject);
            }
            
            foreach (Button button in _in._buttons)
            {
                Destroy(button.gameObject);
            }
            
            _in._buttons.Clear();
            _in.rootSpawnPlace.SetActive(false);
        }
        
        private async UniTask ActivateAndInitNextFrame(TankRoot go)
        {
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            
            go.gameObject.SetActive(true);
            go.Init(true);
        }

        private void StripFishNetRuntime(GameObject root)
        {
            // 1) Залежні від NetworkObject
            GameplayAssistant.DestroyAll<NetworkObserver>(root);
            GameplayAssistant.DestroyAll<NetworkTransform>(root);

            // 2) NetworkObject — останнім
            GameplayAssistant.DestroyAll<NetworkObject>(root);
        }
    }
    
    [Serializable]
    public class RobotList
    {
        public string name;
        public int id;
        public Sprite icon;
        public bool isSelected;
    }
}
