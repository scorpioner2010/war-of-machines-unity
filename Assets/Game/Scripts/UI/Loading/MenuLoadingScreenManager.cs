using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Managing.Scened;
using RAV.Scripts.MenuController;
using RAV.Scripts.UI.Helpers;
using RAV.Scripts.UI.Lobby;
using RAV.Scripts.UI.MainMenu;
using System.Collections;
using System;
using UnityEngine;

namespace RAV.Scripts.UI.Loading
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
        private bool _sceneLoadingPresentationStarted;
        private bool _waitingForMatchmakingPresentation;
        private LoadQueueData _activeSceneLoad;
        private float _sceneLoadingStartedAt;
        private int _sceneLoadingVersion;
        private static string _connectionStatusText = DefaultConnectionStatus;
        private static int _connectionLoadingVersion;
        private static StandardLoadingOverlay.LoadingRequest _connectionLoadingRequest;

        public static MenuLoadingScreenMode CurrentMode { get; private set; } = MenuLoadingScreenMode.SceneLoading;
        public static event Action SceneLoadingHidden;

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
            EndConnectionLoadingRequest();
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
            MatchmakingStatusView.OnGameTransitionReady += OnMatchmakingPresentationReady;
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
            MatchmakingStatusView.OnGameTransitionReady -= OnMatchmakingPresentationReady;
            _isSubscribed = false;
        }
        
        private void OnGameplayLoadStart(SceneLoadStartEventArgs args)
        {
            _activeSceneLoad = args.QueueData;
            _sceneLoadCompleted = false;
            _sceneLoadingPresentationStarted = false;
            _waitingForMatchmakingPresentation = false;
            _sceneLoadingStartedAt = Time.unscaledTime;
            _sceneLoadingVersion++;
            loadingProgressBar?.SetProgress(0f);
            MatchmakingStatusView.BeginGameTransition();

            if (MatchmakingStatusView.IsPresentationPending)
            {
                _waitingForMatchmakingPresentation = true;
                return;
            }

            StartSceneLoadingPresentation(args.QueueData, _sceneLoadingVersion);
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

            if (!_sceneLoadingPresentationStarted)
            {
                return;
            }

            PrepareGameplayUnderLoadingOverlay();
        }

        private void OnMatchmakingPresentationReady()
        {
            if (!_waitingForMatchmakingPresentation || _activeSceneLoad == null)
            {
                return;
            }

            _waitingForMatchmakingPresentation = false;
            StartSceneLoadingPresentation(_activeSceneLoad, _sceneLoadingVersion);

            if (_sceneLoadCompleted)
            {
                PrepareGameplayUnderLoadingOverlay();
            }
        }

        private void StartSceneLoadingPresentation(LoadQueueData sceneLoad, int loadingVersion)
        {
            if (_sceneLoadingPresentationStarted)
            {
                return;
            }

            _sceneLoadingPresentationStarted = true;
            ShowLoading();
            RunSceneLoadingPresentationAsync(sceneLoad, loadingVersion).Forget();
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
            SceneLoadingHidden?.Invoke();
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
            MenuManager.CloseMenuImmediate(MenuType.MainMenu);
            RobotView.HideForGameplay();

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
            EndConnectionLoadingRequest();
            StandardLoadingOverlay.SetSceneLoadingSuppressed(true);
            MenuManager.OpenMenu(MenuType.LoadScreen);
        }

        public static void ShowConnectionLoading(string statusText = null)
        {
            _connectionLoadingVersion++;
            CurrentMode = MenuLoadingScreenMode.Connection;
            StandardLoadingOverlay.SetSceneLoadingSuppressed(false);
            SetConnectionStatus(string.IsNullOrWhiteSpace(statusText) ? DefaultConnectionStatus : statusText);
            if (_connectionLoadingRequest == null || !_connectionLoadingRequest.IsActive)
            {
                _connectionLoadingRequest = StandardLoadingOverlay.BeginRequest();
            }
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
            EndConnectionLoadingRequest();
            CurrentMode = MenuLoadingScreenMode.SceneLoading;
            _connectionStatusText = DefaultConnectionStatus;
        }

        private static void EndConnectionLoadingRequest()
        {
            if (_connectionLoadingRequest == null)
            {
                return;
            }

            _connectionLoadingRequest.End();
            _connectionLoadingRequest = null;
        }

        public static void HideLoading()
        {
            MenuManager.CloseMenuImmediate(MenuType.LoadScreen);
        }
    }
}
