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
        private const float InitialLoadingStageDuration = 1f;
        private const float InitialLoadingStageProgress = 0.5f;
        private const float MinimumSceneLoadingDuration = 2f;
        private const float LateSceneLoadingFinalFillDuration = 0.25f;

        [SerializeField] private MenuLoadingProgressBar loadingProgressBar;

        private bool _isSubscribed;
        private bool _sceneLoadCompleted;
        private LoadQueueData _activeSceneLoad;
        private float _sceneLoadingStartedAt;
        private int _sceneLoadingVersion;
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
            _sceneLoadingVersion++;
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
        
        private void OnGameplayLoadStart(SceneLoadStartEventArgs args)
        {
            _activeSceneLoad = args.QueueData;
            _sceneLoadCompleted = false;
            _sceneLoadingStartedAt = Time.unscaledTime;
            _sceneLoadingVersion++;
            loadingProgressBar?.SetProgress(0f);
            ShowLoading();
            RunSceneLoadingPresentationAsync(args.QueueData, _sceneLoadingVersion).Forget();
        }

        private void OnGameplayLoadEnd(SceneLoadEndEventArgs args)
        {
            if (_activeSceneLoad != args.QueueData)
            {
                return;
            }

            if (_sceneLoadCompleted)
            {
                return;
            }

            _sceneLoadCompleted = true;
            PrepareGameplayUnderLoadingOverlay();
        }

        private async UniTask RunSceneLoadingPresentationAsync(LoadQueueData sceneLoad, int loadingVersion)
        {
            if (!await AnimateProgressAsync(sceneLoad, loadingVersion, 0f, InitialLoadingStageProgress, InitialLoadingStageDuration))
            {
                return;
            }

            while (!_sceneLoadCompleted)
            {
                if (!IsCurrentSceneLoad(sceneLoad, loadingVersion))
                {
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!IsCurrentSceneLoad(sceneLoad, loadingVersion))
            {
                return;
            }

            float remainingMinimumDuration = Mathf.Max(0f, MinimumSceneLoadingDuration - (Time.unscaledTime - _sceneLoadingStartedAt));
            float finalFillDuration = remainingMinimumDuration > 0f
                ? remainingMinimumDuration
                : LateSceneLoadingFinalFillDuration;

            if (!await AnimateProgressAsync(sceneLoad, loadingVersion, InitialLoadingStageProgress, 1f, finalFillDuration))
            {
                return;
            }

            await UniTask.NextFrame();

            if (!IsCurrentSceneLoad(sceneLoad, loadingVersion))
            {
                return;
            }

            _activeSceneLoad = null;
            HideLoading();
        }

        private async UniTask<bool> AnimateProgressAsync(LoadQueueData sceneLoad, int loadingVersion, float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                loadingProgressBar?.SetProgress(to);
                return IsCurrentSceneLoad(sceneLoad, loadingVersion);
            }

            float stageStartedAt = Time.unscaledTime;
            while (true)
            {
                if (!IsCurrentSceneLoad(sceneLoad, loadingVersion))
                {
                    return false;
                }

                float normalizedProgress = Mathf.Clamp01((Time.unscaledTime - stageStartedAt) / duration);
                loadingProgressBar?.SetProgress(Mathf.Lerp(from, to, normalizedProgress));
                if (normalizedProgress >= 1f)
                {
                    return true;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        private void PrepareGameplayUnderLoadingOverlay()
        {
            RobotView.Despawn();

            if (MainMenu.MainMenu.In != null)
            {
                MainMenu.MainMenu.In.SetActive(false);
            }

            MenuManager.BringMenuToFront(MenuType.LoadScreen);
        }

        private bool IsCurrentSceneLoad(LoadQueueData sceneLoad, int loadingVersion)
        {
            return _activeSceneLoad == sceneLoad
                && _sceneLoadingVersion == loadingVersion
                && CurrentMode == MenuLoadingScreenMode.SceneLoading;
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
            MenuManager.CloseMenuImmediate(MenuType.LoadScreen);
        }
    }
}
