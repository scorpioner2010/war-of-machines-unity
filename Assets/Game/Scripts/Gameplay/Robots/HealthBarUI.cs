using Game.Scripts.Core.Services;
using Game.Scripts.Client;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Gameplay.Robots
{
    public class HealthBarUI : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        public VehicleRoot vehicleRoot;

        private float _display01;
        private HealthBar _healthBar;
        private Image _fillImage;
        private bool _active;
        private int _lastLabelCurrent = int.MinValue;
        private int _lastLabelMax = int.MinValue;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (!context.IsOwner || context.IsMenu)
            {
                return;
            }

            vehicleRoot = context.Root;
            TryActivate();
        }

        private void Start()
        {
            TryActivate();
        }

        private void OnEnable()
        {
            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnHealthChanged += OnHealthChanged;
                vehicleRoot.health.OnDamaged += OnDamaged;
            }
        }

        private void OnDisable()
        {
            if (vehicleRoot != null && vehicleRoot.health != null)
            {
                vehicleRoot.health.OnHealthChanged -= OnHealthChanged;
                vehicleRoot.health.OnDamaged -= OnDamaged;
            }
        }

        private void TryActivate()
        {
            if (vehicleRoot == null)
            {
                return;
            }

            if (!vehicleRoot.IsOwner)
            {
                return;
            }

            _healthBar = Singleton<HealthBar>.CurrentOrNull;
            if (_healthBar == null)
            {
                return;
            }

            if (vehicleRoot.health == null)
            {
                return;
            }

            float cur01 = Mathf.Clamp01(vehicleRoot.health.Current / vehicleRoot.health.MaxHealth);
            _display01 = cur01;
            _healthBar.slider.value = _display01;
            _fillImage = _healthBar.fillImage;
            ApplyHealthColor();
            RefreshLabel();

            _active = true;
            vehicleRoot.health.OnHealthChanged -= OnHealthChanged;
            vehicleRoot.health.OnDamaged -= OnDamaged;
            vehicleRoot.health.OnHealthChanged += OnHealthChanged;
            vehicleRoot.health.OnDamaged += OnDamaged;
        }

        private void Update()
        {
            if (!_active)
            {
                TryActivate();
            }

            if (!_active)
            {
                return;
            }

            float target01 = Mathf.Clamp01(vehicleRoot.health.Current / vehicleRoot.health.MaxHealth);
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

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            if (!_active || _healthBar == null || _healthBar.slider == null)
            {
                return;
            }

            _display01 = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
            _healthBar.slider.value = _display01;
            RefreshLabel();
        }

        private void OnDamaged(float damageAmount, float currentHealth, float maxHealth)
        {
            OnHealthChanged(currentHealth, maxHealth);
        }

        private void RefreshLabel()
        {
            if (_healthBar == null || _healthBar.label == null || vehicleRoot == null || vehicleRoot.health == null)
            {
                return;
            }

            int cur = Mathf.RoundToInt(vehicleRoot.health.Current);
            int max = Mathf.RoundToInt(vehicleRoot.health.MaxHealth);
            if (_lastLabelCurrent == cur && _lastLabelMax == max)
            {
                return;
            }

            _lastLabelCurrent = cur;
            _lastLabelMax = max;
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
    }
}
