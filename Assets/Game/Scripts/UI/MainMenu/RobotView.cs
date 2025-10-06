using System;
using Cysharp.Threading.Tasks;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Observing;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Gameplay.Robots;
using NaughtyAttributes;
using UnityEngine;

namespace Game.Scripts.UI.MainMenu
{
    public class RobotView : MonoBehaviour
    {
        public TankRoot robot;
        public Transform spawnPosition;
        public GameObject rootSpawnPlace;

        private TankRoot _tankRoot;
        private static RobotView _in;
        private void Awake() => _in = this;

        [Button]
        public static async void Spawn()
        {
            _in.rootSpawnPlace.SetActive(true);
            _in._tankRoot = Instantiate(_in.robot, _in.spawnPosition.transform, true);
            _in._tankRoot.gameObject.SetActive(false);
            //_in._tankRoot.SetMode(RunMode.Local);
            
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
