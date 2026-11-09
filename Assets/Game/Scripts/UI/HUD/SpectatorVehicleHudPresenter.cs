using Game.Scripts.Client;
using Game.Scripts.Core.Services;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.UI.Robots;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    [DisallowMultipleComponent]
    public sealed class SpectatorVehicleHudPresenter : MonoBehaviour
    {
        private CameraController _cameraController;
        private VehicleRoot _displayTarget;
        private VehicleHealth _displayHealth;
        private Transform _displayTransform;
        private HealthBar _healthBar;
        private float _displayHealth01;
        private Vector3 _previousPosition;
        private float _sampleDistance;
        private float _sampleElapsed;
        private float _targetDisplaySpeed;
        private float _smoothedDisplaySpeed;
        private int _lastShownSpeed = int.MinValue;
        private TMP_Text _lastHealthLabel;
        private TMP_Text _lastVehicleNameLabel;
        private int _lastHealthLabelCurrent = int.MinValue;
        private int _lastHealthLabelMax = int.MinValue;
        private string _lastVehicleName;

        private void OnEnable()
        {
            CameraController.DeathSpectatorCameraChanged += OnDeathSpectatorCameraChanged;
            BindCamera(CameraController.ActiveDeathSpectator);
        }

        private void OnDisable()
        {
            CameraController.DeathSpectatorCameraChanged -= OnDeathSpectatorCameraChanged;
            UnsubscribeFromSpectatorTarget();
            _cameraController = null;
            _displayTarget = null;
            _displayHealth = null;
            _displayTransform = null;
        }

        private void LateUpdate()
        {
            CameraController activeCamera = CameraController.ActiveDeathSpectator;
            if (_cameraController != activeCamera)
            {
                BindCamera(activeCamera);
            }

            if (_cameraController == null || !_cameraController.IsDeathSpectatorActive)
            {
                return;
            }

            UpdateHealthDisplay();
            UpdateSpeedDisplay();
        }

        private void OnDeathSpectatorCameraChanged(CameraController cameraController)
        {
            BindCamera(cameraController);
        }

        private void BindCamera(CameraController cameraController)
        {
            if (_cameraController == cameraController)
            {
                return;
            }

            UnsubscribeFromSpectatorTarget();
            _cameraController = cameraController;
            if (_cameraController == null)
            {
                _displayTarget = null;
                _displayHealth = null;
                _displayTransform = null;
                return;
            }

            _cameraController.SpectatorTargetChanged += OnSpectatorTargetChanged;
            BindDisplayTarget(_cameraController.SpectatorTarget);
        }

        private void UnsubscribeFromSpectatorTarget()
        {
            if (_cameraController != null)
            {
                _cameraController.SpectatorTargetChanged -= OnSpectatorTargetChanged;
            }
        }

        private void OnSpectatorTargetChanged(VehicleRoot target)
        {
            BindDisplayTarget(target);
        }

        private void BindDisplayTarget(VehicleRoot target)
        {
            _displayTarget = target;
            _displayHealth = target != null ? target.health : null;
            _displayTransform = ResolveDisplayTransform(target);
            _displayHealth01 = GetHealth01();
            _previousPosition = _displayTransform != null ? _displayTransform.position : Vector3.zero;
            _sampleDistance = 0f;
            _sampleElapsed = 0f;
            _targetDisplaySpeed = 0f;
            _smoothedDisplaySpeed = 0f;
            _lastShownSpeed = int.MinValue;
            _lastHealthLabel = null;
            _lastVehicleNameLabel = null;
            _lastHealthLabelCurrent = int.MinValue;
            _lastHealthLabelMax = int.MinValue;
            _lastVehicleName = null;
            SpeedHud.SetText(0);
            RefreshHealthDisplay();
        }

        private void UpdateHealthDisplay()
        {
            _healthBar = _healthBar != null ? _healthBar : Singleton<HealthBar>.CurrentOrNull;
            if (_healthBar == null)
            {
                return;
            }

            float targetHealth01 = GetHealth01();
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float smoothSpeed = settings.ownerHealthBarSmoothSpeed;
            if (smoothSpeed > 0f)
            {
                float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                _displayHealth01 = Mathf.Lerp(_displayHealth01, targetHealth01, t);
            }
            else
            {
                _displayHealth01 = targetHealth01;
            }

            RefreshHealthDisplay();
        }

        private void RefreshHealthDisplay()
        {
            if (_healthBar == null)
            {
                return;
            }

            _healthBar.SetNormalizedValue(_displayHealth01);
            if (_healthBar.label != null)
            {
                int current = _displayHealth != null ? Mathf.RoundToInt(_displayHealth.Current) : 0;
                int max = _displayHealth != null ? Mathf.RoundToInt(_displayHealth.MaxHealth) : 0;
                if (_lastHealthLabel != _healthBar.label
                    || _lastHealthLabelCurrent != current
                    || _lastHealthLabelMax != max)
                {
                    _healthBar.label.text = $"{current} / {max}";
                    _lastHealthLabel = _healthBar.label;
                    _lastHealthLabelCurrent = current;
                    _lastHealthLabelMax = max;
                }
            }

            if (_healthBar.vehicleNameLabel != null)
            {
                string vehicleName = ResolveVehicleName();
                if (_lastVehicleNameLabel != _healthBar.vehicleNameLabel
                    || _lastVehicleName != vehicleName)
                {
                    _healthBar.vehicleNameLabel.text = vehicleName;
                    _lastVehicleNameLabel = _healthBar.vehicleNameLabel;
                    _lastVehicleName = vehicleName;
                }
            }
        }

        private float GetHealth01()
        {
            if (_displayHealth == null)
            {
                return 0f;
            }

            return Mathf.Clamp01(_displayHealth.Current / _displayHealth.MaxHealth);
        }

        private string ResolveVehicleName()
        {
            if (_displayTarget == null)
            {
                return string.Empty;
            }

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

        private void UpdateSpeedDisplay()
        {
            if (_displayTransform == null)
            {
                SetSpeedDisplay(0);
                return;
            }

            Vector3 currentPosition = _displayTransform.position;
            Vector3 delta = currentPosition - _previousPosition;
            _previousPosition = currentPosition;
            delta.y = 0f;
            _sampleDistance += delta.magnitude;
            _sampleElapsed += Time.deltaTime;

            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float sampleInterval = Mathf.Max(0.02f, settings.speedHudSampleInterval);
            if (_sampleElapsed >= sampleInterval)
            {
                float speed = _sampleDistance / Mathf.Max(_sampleElapsed, 0.0001f);
                _targetDisplaySpeed = speed * Mathf.Max(0f, settings.speedHudDisplaySpeedMultiplier);
                if (_targetDisplaySpeed < settings.speedHudStopSnapThreshold)
                {
                    _targetDisplaySpeed = 0f;
                }

                _sampleDistance = 0f;
                _sampleElapsed = 0f;
            }

            float smoothRate = Mathf.Max(0.01f, settings.speedHudSmoothRate);
            float tSmooth = 1f - Mathf.Exp(-smoothRate * Time.deltaTime);
            _smoothedDisplaySpeed = Mathf.Lerp(_smoothedDisplaySpeed, _targetDisplaySpeed, tSmooth);
            if (_targetDisplaySpeed <= 0f && _smoothedDisplaySpeed < settings.speedHudStopSnapThreshold)
            {
                _smoothedDisplaySpeed = 0f;
            }

            SetSpeedDisplay(Mathf.RoundToInt(_smoothedDisplaySpeed));
        }

        private void SetSpeedDisplay(int speed)
        {
            if (_lastShownSpeed == speed)
            {
                return;
            }

            _lastShownSpeed = speed;
            SpeedHud.SetText(speed);
        }

        private static Transform ResolveDisplayTransform(VehicleRoot target)
        {
            if (target == null)
            {
                return null;
            }

            if (target.objectMover != null)
            {
                return target.objectMover.transform;
            }

            return target.transform;
        }
    }
}
