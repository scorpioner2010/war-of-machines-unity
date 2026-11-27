using System;
using System.Collections;
using RAV.Scripts.Core.Helpers;
using RAV.Scripts.MenuController;
using RAV.Scripts.Server;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RAV.Scripts.UI.Lobby
{
    public class MatchmakingStatusView : MonoBehaviour
    {
        private const float CompletionDuration = 1f;

        private static MatchmakingStatusView _instance;

        public static event Action OnGameTransitionReady;

        [SerializeField] private TMP_Text timer;
        [SerializeField] private TMP_Text players;
        [SerializeField] private Image fillLine;

        private Coroutine _completionRoutine;
        private int _presentationVersion;
        private bool _isCompleting;
        private bool _isSearchActive;
        private bool _presentationReady;
        private float _progress;

        public static bool IsPresentationPending =>
            _instance != null && _instance._isSearchActive && !_instance._presentationReady;
        
        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            StopCompletion();
        }

        private void OnDisable()
        {
            StopCompletion();
        }

        public static void BeginSearch()
        {
            _instance?.ResetSearchPresentation();
        }

        public static void CancelSearch()
        {
            _instance?.CancelSearchPresentation();
        }

        public static void UpdateInfo(float time, int players)
        {
            if (_instance == null)
            {
                return;
            }

            _instance.ApplyInfo(time, players);
        }

        public static void BeginGameTransition()
        {
            _instance?.StartGameTransition();
        }

        private void ResetSearchPresentation()
        {
            StopCompletion();
            _isSearchActive = true;
            _presentationReady = false;
            SetProgress(0f);
        }

        private void CancelSearchPresentation()
        {
            StopCompletion();
            _isSearchActive = false;
            _presentationReady = false;
        }

        private void ApplyInfo(float time, int playerCount)
        {
            if (_isCompleting)
            {
                return;
            }

            if (timer != null)
            {
                timer.text = GameplayAssistant.ConvertToTime(time);
            }

            int maxPlayers = Mathf.Max(1, RemoteServerSettings.MaxPlayersForFindRoom);
            int clampedPlayerCount = Mathf.Clamp(playerCount, 0, maxPlayers);

            if (players != null)
            {
                players.text = clampedPlayerCount + "/" + maxPlayers;
            }

            SetProgress((float)clampedPlayerCount / maxPlayers);
        }

        private void StartGameTransition()
        {
            if (!_isSearchActive || _presentationReady || _isCompleting)
            {
                return;
            }

            StopCompletion();

            if (fillLine == null || _progress >= 1f)
            {
                CompleteTransition();
                return;
            }

            _isCompleting = true;
            int version = _presentationVersion;
            _completionRoutine = StartCoroutine(CompleteProgress(version));
        }

        private IEnumerator CompleteProgress(int version)
        {
            float initialProgress = _progress;
            float startTime = Time.unscaledTime;

            while (true)
            {
                if (version != _presentationVersion || !isActiveAndEnabled)
                {
                    yield break;
                }

                float normalizedTime = Mathf.Clamp01((Time.unscaledTime - startTime) / CompletionDuration);
                SetProgress(Mathf.Lerp(initialProgress, 1f, normalizedTime));

                if (normalizedTime >= 1f)
                {
                    break;
                }

                yield return null;
            }

            CompleteTransition();
        }

        private void CompleteTransition()
        {
            SetProgress(1f);
            _completionRoutine = null;
            _isCompleting = false;
            _isSearchActive = false;
            _presentationReady = true;
            CloseMatchmakingMenu();
            NotifyGameTransitionReady();
        }

        private void SetProgress(float value)
        {
            _progress = Mathf.Clamp01(value);
            if (fillLine != null)
            {
                fillLine.fillAmount = _progress;
            }
        }

        private void CloseMatchmakingMenu()
        {
            MenuManager.CloseMenuImmediate(MenuType.Matchmaking);
        }

        private void NotifyGameTransitionReady()
        {
            OnGameTransitionReady?.Invoke();
        }

        private void StopCompletion()
        {
            _presentationVersion++;

            if (_completionRoutine != null)
            {
                StopCoroutine(_completionRoutine);
                _completionRoutine = null;
            }

            _isCompleting = false;
        }
    }
}
