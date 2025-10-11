using Game.Scripts.Core.Services;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class HealthBarUI : MonoBehaviour
    {
        public TankRoot tankRoot;
        public float smoothSpeed = 10f;

        private float _display01;
        private HealthBar _healthBar;
        private bool _active;

        private void Start()
        {
            // Працюємо тільки для локального власника
            if (!tankRoot.IsOwner)
            {
                enabled = false;
                return;
            }

            _healthBar = Singleton<HealthBar>.Instance;
            if (_healthBar == null)
            {
                enabled = false;
                return;
            }

            float cur01 = Mathf.Clamp01(tankRoot.health.Current / Mathf.Max(1f, tankRoot.health.maxHealth));
            _display01 = cur01;
            _healthBar.slider.value = _display01;
            RefreshLabel();

            _active = true;
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            float target01 = Mathf.Clamp01(tankRoot.health.Current / Mathf.Max(1f, tankRoot.health.maxHealth));

            if (smoothSpeed > 0f)
            {
                float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                _display01 = Mathf.Lerp(_display01, target01, t);
            }
            else
            {
                _display01 = target01;
            }

            _healthBar.slider.value = _display01;
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            int cur = Mathf.RoundToInt(tankRoot.health.Current);
            int max = Mathf.RoundToInt(tankRoot.health.maxHealth);
            _healthBar.label.text = $"{cur} / {max}";
        }
    }
}