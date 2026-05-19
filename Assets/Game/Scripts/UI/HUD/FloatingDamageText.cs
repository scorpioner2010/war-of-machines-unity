using DG.Tweening;
using Game.Scripts.Client;
using Game.Scripts.Gameplay.Robots;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class FloatingDamageText : MonoBehaviour
    {
        public TMP_Text text;
        private Camera _camera;
        private Sequence _sequence;

        public void SetText(string value)
        {
            _camera = CameraSync.In != null ? CameraSync.In.gameplayCamera : null;

            text.text = value;
            Color color = text.color;
            color.a = 1f;
            text.color = color;
            transform.localScale = Vector3.one;
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float duration = settings.floatingDamageTextDuration;
            KillSequence();

            _sequence = DOTween.Sequence();
            _sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            _sequence.Join(transform.DOMoveY(transform.position.y + settings.floatingDamageTextMoveUp, duration));
            _sequence.Join(text.DOFade(0f, duration));
            _sequence.Join(transform.DOScale(settings.floatingDamageTextEndScale, duration));
            _sequence.SetEase(Ease.OutQuad).OnComplete(() => Destroy(gameObject));
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                transform.forward = _camera.transform.forward;
            }
        }

        private void OnDisable()
        {
            KillSequence();
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        private void KillSequence()
        {
            if (_sequence == null)
            {
                return;
            }

            _sequence.Kill();
            _sequence = null;
        }
    }
}
