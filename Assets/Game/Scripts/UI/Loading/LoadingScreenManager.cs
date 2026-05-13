using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Managing.Scened;
using Game.Scripts.MenuController;
using Game.Scripts.UI.MainMenu;
using System.Collections;
using UnityEngine;
using StandardLoading = Game.Scripts.UI.Helpers.Loading;

namespace Game.Scripts.UI.Loading 
{
    public enum LoadingScreenMode
    {
        SceneLoading = 0,
        Connection = 1
    }

    public class LoadingScreenManager : MonoBehaviour
    {
        private const string DefaultConnectionStatus = "Trying to connect to battle server";

        private bool _isSubscribed;
        private static string _connectionStatusText = DefaultConnectionStatus;
        private static int _connectionLoadingVersion;

        public static LoadingScreenMode CurrentMode { get; private set; } = LoadingScreenMode.SceneLoading;

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
            CurrentMode = LoadingScreenMode.SceneLoading;
            StandardLoading.Hide();
            MenuManager.OpenMenu(MenuType.LoadScreen);
        }

        public static void ShowConnectionLoading(string statusText = null)
        {
            _connectionLoadingVersion++;
            CurrentMode = LoadingScreenMode.Connection;
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
            return CurrentMode == LoadingScreenMode.Connection && !string.IsNullOrWhiteSpace(statusText);
        }

        public static void HideConnectionLoading(float delaySeconds = 0f)
        {
            if (CurrentMode != LoadingScreenMode.Connection)
            {
                return;
            }

            if (delaySeconds > 0f)
            {
                int version = ++_connectionLoadingVersion;
                HideConnectionLoadingDelayed(version, delaySeconds).Forget();
                return;
            }

            HideConnectionLoadingNow();
        }

        private static async UniTask HideConnectionLoadingDelayed(int version, float delaySeconds)
        {
            int delayMilliseconds = Mathf.RoundToInt(Mathf.Max(0f, delaySeconds) * 1000f);
            if (delayMilliseconds > 0)
            {
                await UniTask.Delay(delayMilliseconds, ignoreTimeScale: true);
            }

            if (version != _connectionLoadingVersion || CurrentMode != LoadingScreenMode.Connection)
            {
                return;
            }

            HideConnectionLoadingNow();
        }

        private static void HideConnectionLoadingNow()
        {
            StandardLoading.Hide();
            CurrentMode = LoadingScreenMode.SceneLoading;
            _connectionStatusText = DefaultConnectionStatus;
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
