using Game.Scripts.Core.Services;
using Game.Scripts.Client;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Gameplay.Robots
{
    public class HealthBarUI : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;

        private float _display01;
        private HealthBar _healthBar;
        private Image _fillImage;
        private bool _active;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void Start()
        {
            if (vehicleRoot == null)
            {
                enabled = false;
                return;
            }

            if (!vehicleRoot.IsOwner)
            {
                enabled = false;
                return;
            }

            _healthBar = Singleton<HealthBar>.CurrentOrNull;
            if (_healthBar == null)
            {
                enabled = false;
                return;
            }

            float cur01 = Mathf.Clamp01(vehicleRoot.health.Current / Mathf.Max(1f, vehicleRoot.health.maxHealth));
            _display01 = cur01;
            _healthBar.slider.value = _display01;
            CacheFillImage();
            ApplyHealthColor();
            RefreshLabel();

            _active = true;
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            float target01 = Mathf.Clamp01(vehicleRoot.health.Current / Mathf.Max(1f, vehicleRoot.health.maxHealth));
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float smoothSpeed = settings.ownerHealthBarSmoothSpeed;

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
            ApplyHealthColor();
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            int cur = Mathf.RoundToInt(vehicleRoot.health.Current);
            int max = Mathf.RoundToInt(vehicleRoot.health.maxHealth);
            _healthBar.label.text = $"{cur} / {max}";
        }

        private void ApplyHealthColor()
        {
            if (_fillImage == null)
            {
                return;
            }

            _fillImage.color = GameplayRuntimeSettingsProvider.Get().alliedHpColor;
        }

        private void CacheFillImage()
        {
            if (_healthBar == null || _healthBar.slider == null || _healthBar.slider.fillRect == null)
            {
                return;
            }

            _fillImage = _healthBar.slider.fillRect.GetComponent<Image>();
        }
    }
}
