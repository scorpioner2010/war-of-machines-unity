using Game.Scripts.Core.Services;
using Game.Scripts.Client;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class HealthBarUI : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        public VehicleRoot vehicleRoot;

        private float _display01;
        private HealthBar _healthBar;
        private bool _active;
        private VehicleRoot _displayTarget;
        private VehicleHealth _displayHealth;
        private int _lastLabelCurrent = int.MinValue;
        private int _lastLabelMax = int.MinValue;
        private string _lastVehicleName;

        public void SetVehicleRoot(VehicleRoot root)
        {
            if (vehicleRoot != root)
            {
                UnsubscribeFromDisplayHealth();
            }

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

        private void OnDisable()
        {
            UnsubscribeFromDisplayHealth();
            _active = false;
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

            if (vehicleRoot.health == null || (_healthBar.slider == null && _healthBar.fillImage == null))
            {
                return;
            }

            float cur01 = Mathf.Clamp01(vehicleRoot.health.Current / vehicleRoot.health.MaxHealth);
            _display01 = cur01;
            _healthBar.SetNormalizedValue(_display01);
            RefreshLabel();
            RefreshVehicleName();

            _active = true;
            BindDisplayTarget(vehicleRoot);
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

            if (_displayTarget == null || _displayHealth == null)
            {
                BindDisplayTarget(vehicleRoot);
            }

            if (_displayTarget == null || _displayHealth == null)
            {
                return;
            }

            float target01 = Mathf.Clamp01(_displayHealth.Current / _displayHealth.MaxHealth);
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

            _healthBar.SetNormalizedValue(_display01);
            RefreshLabel();
            RefreshVehicleName();
        }

        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            if (!_active || _healthBar == null || (_healthBar.slider == null && _healthBar.fillImage == null))
            {
                return;
            }

            _display01 = Mathf.Clamp01(currentHealth / Mathf.Max(1f, maxHealth));
            _healthBar.SetNormalizedValue(_display01);
            RefreshLabel();
            RefreshVehicleName();
        }

        private void OnDamaged(float damageAmount, float currentHealth, float maxHealth)
        {
            OnHealthChanged(currentHealth, maxHealth);
        }

        private void RefreshLabel()
        {
            if (_healthBar == null || _healthBar.label == null || _displayHealth == null)
            {
                return;
            }

            int cur = Mathf.RoundToInt(_displayHealth.Current);
            int max = Mathf.RoundToInt(_displayHealth.MaxHealth);
            if (_lastLabelCurrent == cur && _lastLabelMax == max)
            {
                return;
            }

            _lastLabelCurrent = cur;
            _lastLabelMax = max;
            _healthBar.label.text = $"{cur} / {max}";
        }

        private void RefreshVehicleName()
        {
            if (_healthBar == null || _healthBar.vehicleNameLabel == null || _displayTarget == null)
            {
                return;
            }

            string vehicleName = ResolveVehicleName();
            if (_lastVehicleName == vehicleName)
            {
                return;
            }

            _lastVehicleName = vehicleName;
            _healthBar.vehicleNameLabel.text = vehicleName;
        }

        private string ResolveVehicleName()
        {
            VehicleRuntimeStats stats = _displayTarget.RuntimeStats;
            if (stats != null)
            {
                if (!string.IsNullOrEmpty(stats.Name))
                {
                    return stats.Name;
                }

                if (!string.IsNullOrEmpty(stats.Code))
                {
                    return stats.Code;
                }
            }

            return _displayTarget.name;
        }

        private void BindDisplayTarget(VehicleRoot target)
        {
            if (target == null || target.health == null)
            {
                target = vehicleRoot;
            }

            VehicleHealth nextHealth = target != null ? target.health : null;
            if (_displayTarget == target && _displayHealth == nextHealth)
            {
                RefreshLabel();
                RefreshVehicleName();
                return;
            }

            UnsubscribeFromDisplayHealth();
            _displayTarget = target;
            _displayHealth = nextHealth;
            _lastLabelCurrent = int.MinValue;
            _lastLabelMax = int.MinValue;
            _lastVehicleName = null;
            if (_displayHealth == null)
            {
                return;
            }

            _displayHealth.OnHealthChanged += OnHealthChanged;
            _displayHealth.OnDamaged += OnDamaged;
            _displayHealth.OnClientDeath += OnClientDeath;
            _display01 = Mathf.Clamp01(_displayHealth.Current / _displayHealth.MaxHealth);
            _healthBar.SetNormalizedValue(_display01);
            RefreshLabel();
            RefreshVehicleName();
        }

        private void UnsubscribeFromDisplayHealth()
        {
            if (_displayHealth == null)
            {
                _displayTarget = null;
                return;
            }

            _displayHealth.OnHealthChanged -= OnHealthChanged;
            _displayHealth.OnDamaged -= OnDamaged;
            _displayHealth.OnClientDeath -= OnClientDeath;
            _displayHealth = null;
            _displayTarget = null;
        }

        private void OnClientDeath(int killerObjectId)
        {
            if (_displayHealth != null && _displayHealth.IsDead)
            {
                OnHealthChanged(0f, _displayHealth.MaxHealth);
            }
        }

    }
}
