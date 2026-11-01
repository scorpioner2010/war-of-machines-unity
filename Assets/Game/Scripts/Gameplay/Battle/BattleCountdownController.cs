using System.Collections;
using Game.Scripts.Audio;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.Loading;
using UnityEngine;

namespace Game.Scripts.Gameplay.Battle
{
    public class BattleCountdownController : MonoBehaviour
    {
        [SerializeField] private BattleCountdownView view;
        [SerializeField, Min(0f)] private float startDelay = 0.5f;
        [SerializeField, Min(1)] private int countdownSeconds = 10;
        [SerializeField, Min(0.1f)] private float tickInterval = 1f;

        private Coroutine _countdownRoutine;
        private bool _sceneCountdownStarted;

        private void OnEnable()
        {
            MenuLoadingScreenManager.SceneLoadingHidden += HandleSceneLoadingHidden;
            BattleCountdownGate.EndClient();
            view?.Hide();
        }

        private void OnDisable()
        {
            MenuLoadingScreenManager.SceneLoadingHidden -= HandleSceneLoadingHidden;
            StopCountdown();
        }

        private void HandleSceneLoadingHidden()
        {
            if (_sceneCountdownStarted || !CanStartCountdown())
            {
                return;
            }

            _sceneCountdownStarted = true;
            _countdownRoutine = StartCoroutine(RunCountdown());
        }

        private bool CanStartCountdown()
        {
            return isActiveAndEnabled
                && MenuManager.IsReady
                && MenuManager.CurrentType == MenuType.GameplayHUD
                && GameplaySpawner.In != null
                && GameplaySpawner.In.IsClientInitialized;
        }

        private IEnumerator RunCountdown()
        {
            BattleCountdownGate.BeginClient();
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, startDelay));

            int firstNumber = Mathf.Max(1, countdownSeconds);
            for (int number = firstNumber; number >= 1; number--)
            {
                if (!isActiveAndEnabled)
                {
                    yield break;
                }

                view?.ShowNumber(number);
                SoundCaller.PlayUi(AudioEventNames.UiBattleCountdown);
                yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, tickInterval));
            }

            BattleCountdownGate.EndClient();
            view?.Hide();
            _countdownRoutine = null;
            StartCoroutine(NotifyServerCountdownFinished());
        }

        private IEnumerator NotifyServerCountdownFinished()
        {
            while (isActiveAndEnabled)
            {
                BattleCountdownAuthority authority = BattleCountdownAuthority.CurrentClient;
                if (authority != null)
                {
                    authority.NotifyClientCountdownFinished();
                    yield break;
                }

                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        private void StopCountdown()
        {
            if (_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                _countdownRoutine = null;
            }

            BattleCountdownGate.EndClient();
            view?.Hide();
            _sceneCountdownStarted = false;
        }
    }
}
