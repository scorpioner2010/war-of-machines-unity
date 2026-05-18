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

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (context.IsOwner && !context.IsMenu)
            {
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
            SpeedHud.SetText("0");
        }

        private void Update()
        {
            if (!isActive || !vehicleRoot.IsOwner)
            {
                return;
            }

            Transform t = vehicleRoot.objectMover.transform;
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
                SpeedHud.SetText(shownSpeed.ToString());
            }
        }
    }
}
