using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(100)]
    public class VehicleGunRecoilController : MonoBehaviour, IVehicleRootAware
    {
        private const float MinimumSmoothTime = 0.01f;
        private const float OffsetEpsilon = 0.0001f;

        [Header("References")]
        [SerializeField] private Transform gun;

        [Header("Recoil")]
        [Min(0f)] [SerializeField] private float recoilDistance = 0.18f;
        [Min(0f)] [SerializeField] private float recoilSpeed = 3f;
        [Min(0f)] [SerializeField] private float returnDelay = 0.04f;
        [Min(0f)] [SerializeField] private float returnSpeed = 0.75f;

        private VehicleRoot _vehicleRoot;
        private Vector3 _initialLocalPosition;
        private float _currentRecoil;
        private float _returnDelayRemaining;
        private float _returnVelocity;
        private bool _hasInitialLocalPosition;
        private bool _isRecoiling;

        public void SetVehicleRoot(VehicleRoot root)
        {
            _vehicleRoot = root;
        }

        private void Awake()
        {
            CaptureInitialLocalPosition();
        }

        private void OnDisable()
        {
            _isRecoiling = false;
            _returnDelayRemaining = 0f;
            _returnVelocity = 0f;

            if (gun != null && _hasInitialLocalPosition && !IsVehicleDead())
            {
                _currentRecoil = 0f;
                gun.localPosition = _initialLocalPosition;
            }
        }

        public void PlayRecoil()
        {
            if (gun == null
                || IsVehicleDead()
                || recoilDistance <= OffsetEpsilon
                || recoilSpeed <= 0f)
            {
                return;
            }

            CaptureInitialLocalPosition();
            _isRecoiling = true;
            _returnDelayRemaining = returnDelay;
            _returnVelocity = 0f;
        }

        public void StopForDeath()
        {
            _isRecoiling = false;
            _returnDelayRemaining = 0f;
            _returnVelocity = 0f;
            enabled = false;
        }

        private void LateUpdate()
        {
            if (gun == null || IsVehicleDead())
            {
                return;
            }

            CaptureInitialLocalPosition();
            if (!_hasInitialLocalPosition)
            {
                return;
            }

            if (!_isRecoiling
                && _returnDelayRemaining <= 0f
                && _currentRecoil <= OffsetEpsilon)
            {
                return;
            }

            float maximumRecoil = Mathf.Max(0f, recoilDistance);
            if (_isRecoiling)
            {
                if (recoilSpeed > 0f)
                {
                    _currentRecoil = Mathf.MoveTowards(
                        _currentRecoil,
                        maximumRecoil,
                        recoilSpeed * Time.deltaTime
                    );
                }

                if (maximumRecoil - _currentRecoil <= OffsetEpsilon)
                {
                    _currentRecoil = maximumRecoil;
                    _isRecoiling = false;
                    _returnDelayRemaining = returnDelay;
                }
            }
            else if (_returnDelayRemaining > 0f)
            {
                _returnDelayRemaining = Mathf.Max(0f, _returnDelayRemaining - Time.deltaTime);
            }
            else if (_currentRecoil > OffsetEpsilon && returnSpeed > 0f)
            {
                float smoothTime = Mathf.Max(
                    MinimumSmoothTime,
                    maximumRecoil / returnSpeed
                );
                _currentRecoil = Mathf.SmoothDamp(
                    _currentRecoil,
                    0f,
                    ref _returnVelocity,
                    smoothTime,
                    returnSpeed,
                    Time.deltaTime
                );

                if (_currentRecoil <= OffsetEpsilon)
                {
                    _currentRecoil = 0f;
                    _returnVelocity = 0f;
                }
            }

            Vector3 recoilDirection = gun.localRotation * Vector3.back;
            gun.localPosition = _initialLocalPosition + recoilDirection * _currentRecoil;
        }

        private void CaptureInitialLocalPosition()
        {
            if (gun == null || _hasInitialLocalPosition)
            {
                return;
            }

            _initialLocalPosition = gun.localPosition;
            _hasInitialLocalPosition = true;
        }

        private bool IsVehicleDead()
        {
            return _vehicleRoot != null
                   && _vehicleRoot.health != null
                   && _vehicleRoot.health.IsDead;
        }
    }
}
