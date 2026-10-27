using Game.Scripts.Client;
using Game.Scripts.UI.Robots;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class VehicleHudInitializer : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        public VehicleRoot vehicleRoot;
        public bool isActive;

        private Vector3 _prevPos;
        private float _sampleDistance;
        private float _sampleTime;
        private float _targetDisplaySpeed;
        private float _smoothedDisplaySpeed;
        private int _lastShownSpeed = int.MinValue;
        private CameraController _spectatorCameraController;
        private VehicleRoot _displayTarget;
        private Transform _displayTransform;

        public void SetVehicleRoot(VehicleRoot root)
        {
            if (vehicleRoot != root)
            {
                UnsubscribeFromSpectatorTarget();
            }

            vehicleRoot = root;
            SubscribeToSpectatorTarget();
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (context.IsOwner && !context.IsMenu)
            {
                vehicleRoot = context.Root;
                Init();
            }
        }

        public void Init()
        {
            if (vehicleRoot == null || vehicleRoot.objectMover == null)
            {
                return;
            }

            isActive = true;
            _prevPos = vehicleRoot.objectMover.transform.position;
            _sampleDistance = 0f;
            _sampleTime = 0f;
            _targetDisplaySpeed = 0f;
            _smoothedDisplaySpeed = 0f;
            _lastShownSpeed = int.MinValue;
            SubscribeToSpectatorTarget();
            BindDisplayTarget(ResolveDisplayTarget());
        }

        private void Update()
        {
            if (!isActive || vehicleRoot == null || !vehicleRoot.IsOwner)
            {
                return;
            }

            SubscribeToSpectatorTarget();

            VehicleRoot displayTarget = ResolveDisplayTarget();
            Transform displayTransform = ResolveDisplayTransform(displayTarget);
            if (_displayTarget != displayTarget || _displayTransform != displayTransform)
            {
                BindDisplayTarget(displayTarget);
            }

            if (_displayTransform == null)
            {
                return;
            }

            Transform t = _displayTransform;
            Vector3 delta = t.position - _prevPos;
            _prevPos = t.position;

            delta.y = 0f;
            _sampleDistance += delta.magnitude;
            _sampleTime += Time.deltaTime;

            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            float sampleInterval = Mathf.Max(0.02f, settings.speedHudSampleInterval);
            if (_sampleTime >= sampleInterval)
            {
                float speed = _sampleDistance / Mathf.Max(_sampleTime, 0.0001f);
                _targetDisplaySpeed = speed * Mathf.Max(0f, settings.speedHudDisplaySpeedMultiplier);

                if (_targetDisplaySpeed < settings.speedHudStopSnapThreshold)
                {
                    _targetDisplaySpeed = 0f;
                }

                _sampleDistance = 0f;
                _sampleTime = 0f;
            }

            float smoothRate = Mathf.Max(0.01f, settings.speedHudSmoothRate);
            float tSmooth = 1f - Mathf.Exp(-smoothRate * Time.deltaTime);
            _smoothedDisplaySpeed = Mathf.Lerp(_smoothedDisplaySpeed, _targetDisplaySpeed, tSmooth);

            if (_targetDisplaySpeed <= 0f && _smoothedDisplaySpeed < settings.speedHudStopSnapThreshold)
            {
                _smoothedDisplaySpeed = 0f;
            }

            int shownSpeed = Mathf.RoundToInt(_smoothedDisplaySpeed);
            if (shownSpeed != _lastShownSpeed)
            {
                _lastShownSpeed = shownSpeed;
                SpeedHud.SetText(shownSpeed);
            }
        }

        private VehicleRoot ResolveDisplayTarget()
        {
            if (vehicleRoot == null || vehicleRoot.cameraController == null)
            {
                return vehicleRoot;
            }

            if (!vehicleRoot.cameraController.IsDeathSpectatorActive)
            {
                return vehicleRoot;
            }

            return vehicleRoot.cameraController.SpectatorTarget ?? vehicleRoot;
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

        private void BindDisplayTarget(VehicleRoot target)
        {
            _displayTarget = target != null ? target : vehicleRoot;
            _displayTransform = ResolveDisplayTransform(_displayTarget);
            _prevPos = _displayTransform != null ? _displayTransform.position : Vector3.zero;
            _sampleDistance = 0f;
            _sampleTime = 0f;
            _targetDisplaySpeed = 0f;
            _smoothedDisplaySpeed = 0f;
            _lastShownSpeed = int.MinValue;
            SpeedHud.SetText(0);
        }

        private void SubscribeToSpectatorTarget()
        {
            CameraController nextController = vehicleRoot != null ? vehicleRoot.cameraController : null;
            if (_spectatorCameraController == nextController)
            {
                return;
            }

            UnsubscribeFromSpectatorTarget();
            _spectatorCameraController = nextController;
            if (_spectatorCameraController != null)
            {
                _spectatorCameraController.SpectatorTargetChanged += OnSpectatorTargetChanged;
            }
        }

        private void UnsubscribeFromSpectatorTarget()
        {
            if (_spectatorCameraController != null)
            {
                _spectatorCameraController.SpectatorTargetChanged -= OnSpectatorTargetChanged;
                _spectatorCameraController = null;
            }
        }

        private void OnSpectatorTargetChanged(VehicleRoot target)
        {
            if (!isActive)
            {
                return;
            }

            BindDisplayTarget(target != null ? target : ResolveDisplayTarget());
        }

        private void OnDestroy()
        {
            UnsubscribeFromSpectatorTarget();
        }
    }
}
