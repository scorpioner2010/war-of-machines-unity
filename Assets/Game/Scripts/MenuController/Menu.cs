using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Game.Scripts.MenuController
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] private RectTransform menuRect;
        [SerializeField] private RectTransform animationPanel;
        
        [SerializeField] private CanvasGroup canvasGroup;
        
        private float _duration = 0.3f;

        private MenuState _state = MenuState.Idle;
        private Sequence _sequence;
        
        [SerializeField] private Vector2 hiddenPosition = new (0, -800);
        [SerializeField] private Vector2 shownPosition = new (0, 0);

        public void SetActive(bool isActive)
        {
            menuRect.gameObject.SetActive(isActive);
        }

        private void Awake()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _state = MenuState.Idle;
            _sequence.Join(animationPanel.DOAnchorPos(hiddenPosition, _duration).SetEase(Ease.InBack));
        }

        public void Open()
        {
            if (_state != MenuState.Idle)
            {
                return;
            }

            SetActive(true);
            _state = MenuState.Opening;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            _sequence?.Kill();
            _sequence = DOTween.Sequence();
            
            _sequence.Append(canvasGroup.DOFade(1f, _duration));
            _sequence.Join(animationPanel.DOAnchorPos(shownPosition, _duration).SetEase(Ease.OutBack));
            
            _sequence.OnComplete(() =>
            {
                _state = MenuState.Opened;
            });
        }

        public async UniTask CloseAsync()
        {
            if (_state != MenuState.Opened)
            {
                return;
            }

            _state = MenuState.Closing;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;

            _sequence?.Kill();
            _sequence = DOTween.Sequence();
            
            _sequence.Append(canvasGroup.DOFade(0f, _duration));
            _sequence.Join(animationPanel.DOAnchorPos(hiddenPosition, _duration).SetEase(Ease.InBack));
            
            await _sequence.AsyncWaitForCompletion();
            canvasGroup.alpha = 0f;
            _state = MenuState.Idle;
            canvasGroup.blocksRaycasts = false;
            SetActive(false);
        }
    }
}
