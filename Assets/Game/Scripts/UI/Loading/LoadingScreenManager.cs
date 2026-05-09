using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Managing.Scened;
using Game.Scripts.MenuController;
using Game.Scripts.UI.MainMenu;
using System.Collections;
using UnityEngine;

namespace Game.Scripts.UI.Loading 
{
    public class LoadingScreenManager : MonoBehaviour
    {
        private bool _isSubscribed;

        private IEnumerator Start()
        {
            while (InstanceFinder.SceneManager == null)
            {
                yield return null;
            }

            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            InstanceFinder.SceneManager.OnLoadEnd += OnGameplayLoadEnd;
            InstanceFinder.SceneManager.OnLoadStart += OnGameplayLoadStart;
            _isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_isSubscribed || InstanceFinder.SceneManager == null)
            {
                return;
            }

            InstanceFinder.SceneManager.OnLoadEnd -= OnGameplayLoadEnd;
            InstanceFinder.SceneManager.OnLoadStart -= OnGameplayLoadStart;
            _isSubscribed = false;
        }
        
        private void OnGameplayLoadStart(SceneLoadStartEventArgs obj)
        {
            ShowLoading();
        }

        private void OnGameplayLoadEnd(SceneLoadEndEventArgs obj)
        {
            HandleGameplayLoadEndAsync().Forget();
        }

        private async UniTask HandleGameplayLoadEndAsync()
        {
            RobotView.Despawn();
            HideLoading();
            await UniTask.Delay(1000);

            if (MainMenu.MainMenu.In != null)
            {
                MainMenu.MainMenu.In.SetActive(false);
            }
        }

        public static void ShowLoading()
        {
            MenuManager.OpenMenu(MenuType.LoadScreen);
        }

        public static void HideLoading()
        {
            HideLoadingAsync().Forget();
        }

        private static async UniTask HideLoadingAsync()
        {
            await UniTask.Delay(1000);
            MenuManager.CloseMenu(MenuType.LoadScreen);
        }
    }
}
