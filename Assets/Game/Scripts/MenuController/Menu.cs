using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Game.Scripts.MenuController
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] private RectTransform menuRect;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float fadeDuration = 0.5f;

        private MenuState _state = MenuState.Idle;
        private Tween _fadeTween;

        public Transform MenuParent => menuRect != null ? menuRect.parent : null;

        public void SetActive(bool isActive)
        {
            if (menuRect != null)
            {
                menuRect.gameObject.SetActive(isActive);
            }
        }

        private void Awake()
        {
            if (!IsConfigured())
            {
                _state = MenuState.Idle;
                return;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _state = MenuState.Idle;
        }

        public void Open()
        {
            if (!IsConfigured())
            {
                return;
            }

            if (_state == MenuState.Opened || _state == MenuState.Opening)
            {
                return;
            }

            SetActive(true);
            BringToFront();
            _state = MenuState.Opening;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            _fadeTween?.Kill();
            _fadeTween = canvasGroup.DOFade(1f, fadeDuration);
            _fadeTween.OnComplete(() =>
            {
                _state = MenuState.Opened;
            });
        }

        public async UniTask CloseAsync()
        {
            if (!IsConfigured())
            {
                return;
            }

            if (_state == MenuState.Idle || _state == MenuState.Closing)
            {
                return;
            }

            _state = MenuState.Closing;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            _fadeTween?.Kill();
            _fadeTween = canvasGroup.DOFade(0f, fadeDuration);

            Tween closeTween = _fadeTween;
            await closeTween.AsyncWaitForCompletion();

            if (_state != MenuState.Closing || _fadeTween != closeTween)
            {
                return;
            }

            canvasGroup.alpha = 0f;
            _state = MenuState.Idle;
            canvasGroup.blocksRaycasts = false;
            SetActive(false);
        }

        public void CloseImmediate()
        {
            if (!IsConfigured())
            {
                return;
            }

            _fadeTween?.Kill();
            _state = MenuState.Idle;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            SetActive(false);
        }

        private bool IsConfigured()
        {
            return menuRect != null && canvasGroup != null;
        }

        public void BringToFront()
        {
            menuRect.SetAsLastSibling();
        }
    }
}
