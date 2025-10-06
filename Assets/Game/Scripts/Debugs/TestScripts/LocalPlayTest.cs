using System.Collections;
using Cysharp.Threading.Tasks;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Observing;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.HUD;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Debugs.TestScripts
{
    public class LocalPlayTest : MonoBehaviour
    {
        public TankRoot[] robots;
        public Button play;

        public TMP_InputField robotNumber;

        private void Start()
        {
            robotNumber.text = "1";
            play.onClick.AddListener(OnPlayClicked);
        }
        
        private async void OnPlayClicked()
        {
            int number = int.Parse(robotNumber.text);
        
            if (number <= 0 || number > robots.Length)
            {
                Debug.LogError("error");
                robotNumber.text = "1";
                return;
            }
        
            play.gameObject.SetActive(false);
            robotNumber.gameObject.SetActive(false);
        
            TankRoot tankRoot = Instantiate(robots[number-1]);
        
            tankRoot.gameObject.SetActive(false);
            //tankRoot.SetMode(RunMode.Local);

            // Прибираємо FishNet-компоненти (лишаємо твої скрипти).
            StripFishNetRuntime(tankRoot.gameObject);

            tankRoot.transform.position = Vector3.zero;
            tankRoot.transform.rotation = Quaternion.identity;

            await ActivateAndInitNextFrame(tankRoot);
        }

        private async UniTask ActivateAndInitNextFrame(TankRoot go)
        {
            await UniTask.NextFrame();
            await UniTask.NextFrame();
            
            go.gameObject.SetActive(true);
            go.Init();
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
