using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Managing.Scened;
using Game.Scripts.MenuController;
using Game.Scripts.UI.MainMenu;
using System.Collections;
using UnityEngine;
using StandardLoading = Game.Scripts.UI.Helpers.StandardLoadingOverlay;

namespace Game.Scripts.UI.Loading
{
    public enum MenuLoadingScreenMode
    {
        SceneLoading = 0,
        Connection = 1
    }

    public class MenuLoadingScreenManager : MonoBehaviour
    {
        private const string DefaultConnectionStatus = "Trying to connect to battle server";

        private bool _isSubscribed;
        private static string _connectionStatusText = DefaultConnectionStatus;
        private static int _connectionLoadingVersion;

        public static MenuLoadingScreenMode CurrentMode { get; private set; } = MenuLoadingScreenMode.SceneLoading;

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
            CurrentMode = MenuLoadingScreenMode.SceneLoading;
            StandardLoading.Hide();
            MenuManager.OpenMenu(MenuType.LoadScreen);
        }

        public static void ShowConnectionLoading(string statusText = null)
        {
            _connectionLoadingVersion++;
            CurrentMode = MenuLoadingScreenMode.Connection;
            SetConnectionStatus(string.IsNullOrWhiteSpace(statusText) ? DefaultConnectionStatus : statusText);
            StandardLoading.Show();
        }

        public static void SetConnectionStatus(string statusText)
        {
            if (string.IsNullOrWhiteSpace(statusText))
            {
                _connectionStatusText = DefaultConnectionStatus;
                return;
            }

            _connectionStatusText = statusText;
        }

        public static bool TryGetConnectionStatus(out string statusText)
        {
            statusText = _connectionStatusText;
            return CurrentMode == MenuLoadingScreenMode.Connection && !string.IsNullOrWhiteSpace(statusText);
        }

        public static void HideConnectionLoading()
        {
            if (CurrentMode != MenuLoadingScreenMode.Connection)
            {
                return;
            }

            _connectionLoadingVersion++;
            HideConnectionLoadingNow();
        }

        private static void HideConnectionLoadingNow()
        {
            StandardLoading.Hide();
            CurrentMode = MenuLoadingScreenMode.SceneLoading;
            _connectionStatusText = DefaultConnectionStatus;
        }

        public static void HideLoading()
        {
            MenuManager.CloseMenu(MenuType.LoadScreen);
        }
    }
}
