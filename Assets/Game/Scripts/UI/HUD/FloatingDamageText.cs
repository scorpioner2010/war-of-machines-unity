using DG.Tweening;
using Game.Scripts.Gameplay.Robots;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class FloatingDamageText : MonoBehaviour
    {
        public TMP_Text text;
        public float duration = 0.8f;
        public float moveUp = 1.5f;
        public float endScale = 1.3f;
        private Camera _camera;

        public void SetText(string value)
        {
            _camera = CameraSync.In != null ? CameraSync.In.gameplayCamera : null;

            text.text = value;
            Color color = text.color;
            color.a = 1f;
            text.color = color;
            transform.localScale = Vector3.one;
            Sequence sequence = DOTween.Sequence();
            sequence.Join(transform.DOMoveY(transform.position.y + moveUp, duration));
            sequence.Join(text.DOFade(0f, duration));
            sequence.Join(transform.DOScale(endScale, duration));
            sequence.SetEase(Ease.OutQuad).OnComplete(() => Destroy(gameObject));
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                transform.forward = _camera.transform.forward;
            }
        }
    }
}
