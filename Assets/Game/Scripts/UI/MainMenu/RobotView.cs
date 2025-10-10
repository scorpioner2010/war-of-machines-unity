using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Observing;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Player.Data;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class RobotView : MonoBehaviour
    {
        public Transform spawnPosition;
        public GameObject rootSpawnPlace;
        public Transform vehicleContainer;
        public VehicleButton vehicleButtonPrefab;

        private TankRoot _tankRoot;
        private List<Button> _buttons = new();
        
        private static RobotView _in;
        
        private void Awake() => _in = this;

        public static void GenerateIcons(IPlayerClientInfo clientInfo)
        {
            OwnedVehicleDto selected = clientInfo.Profile.GetSelected();
            
            foreach (OwnedVehicleDto vehicle in clientInfo.Profile.ownedVehicles)
            {
                Sprite sprite = ResourceManager.GetIcon(vehicle.vehicleId);
                    
                if (selected.vehicleId == vehicle.vehicleId)
                {
                    MakeIcons(sprite, true);
                }
                else
                {
                    MakeIcons(sprite, false);
                }
            }
            
            TankRoot vehicleRoot = ResourceManager.GetPrefab(selected.vehicleId);
            Spawn(vehicleRoot);
        }
        
        private static async void MakeIcons(Sprite sprite, bool isSelect)
        {
            VehicleButton button = Instantiate(_in.vehicleButtonPrefab, _in.vehicleContainer.transform);
            button.vehicleImage.sprite = sprite;
            button.isSelect.gameObject.SetActive(isSelect);
            
            button.button.onClick.AddListener(() =>
            {
                Debug.LogError("select");
                //select
            });
            
            _in._buttons.Add(button.button);
            List<Transform> l = new();
            l.Add(_in.vehicleContainer);
            await GameplayAssistant.RebuildAllLayouts(l);
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

        [Button]
        public static void Despawn()
        {
            Destroy(_in._tankRoot.gameObject);

            foreach (Button button in _in._buttons)
            {
                Destroy(button);
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
}
