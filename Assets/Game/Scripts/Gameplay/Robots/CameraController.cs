using UnityEngine;
using Game.Scripts.Core.Services;
using Game.Scripts.Client;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Settings;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(-100)]
    public class CameraController : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        private const int NormalZoomStep = -1;

        public VehicleRoot vehicleRoot;
        public Transform rig;
        public float distance = 10f;

        private float _X;
        private float _Y;
        private float _normalDistance;
        private float _targetDistance;
        private float _currentDistance;
        private int _currentZoomStep = NormalZoomStep;
        private int _lastNonSniperZoomStep = NormalZoomStep;
        private bool _sniperUiApplied;
        private bool _initialized;
        private GameplayRuntimeSettings _runtimeSettings = GameplayRuntimeSettings.Default;

        public bool IsSniperModeActive => IsSniperStep(_currentZoomStep);

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            vehicleRoot = context.Root;
            if (context.IsOwner && !context.IsMenu)
            {
                Init();
            }
        }

        public void Init()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();

            if (vehicleRoot == null)
            {
                vehicleRoot = GetComponentInParent<VehicleRoot>();
            }

            Vector3 angles = transform.eulerAngles;
            _X = angles.y;
            _Y = angles.x;

            if (CameraSync.In != null)
            {
                CameraSync.In.target = transform;

                if (CameraSync.In.gameplayCamera != null)
                {
                    CameraSync.In.gameplayCamera.fieldOfView = _runtimeSettings.cameraNormalFov;
                }
            }

            _normalDistance = Mathf.Max(0.1f, distance);
            _currentDistance = _normalDistance;
            _currentZoomStep = NormalZoomStep;
            _lastNonSniperZoomStep = NormalZoomStep;
            ApplyZoomStep(NormalZoomStep, true);
            _initialized = true;
            ApplyCameraTransform(true);
            SyncGameplayCameraTransform();
        }

        public void RefreshSniperCameraPose()
        {
            if (!_initialized || !IsSniperModeActive)
            {
                return;
            }

            ApplyCameraTransform(true);
            SyncGameplayCameraTransform();
        }

        public float GetAimUiZoom01()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();

            float normalDistance = _normalDistance > 0.01f
                ? _normalDistance
                : Mathf.Max(0.01f, distance);
            float sniperCameraDistance = Mathf.Max(0.01f, _runtimeSettings.cameraSniperDistance);
            float minDistance = Mathf.Min(normalDistance, sniperCameraDistance);
            float maxDistance = Mathf.Max(normalDistance, sniperCameraDistance);
            if (maxDistance <= minDistance + 0.0001f)
            {
                return IsSniperStep(_currentZoomStep) ? 1f : 0f;
            }

            float clampedDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
            float distance01 = Mathf.InverseLerp(minDistance, maxDistance, clampedDistance);
            return Mathf.Clamp01(1f - distance01);
        }

        private void Update()
        {
            if (!_initialized || rig == null)
            {
                return;
            }

            bool inputBlocked = VehicleInputController.IsGameplayInputBlockedByUi;
            if (!inputBlocked)
            {
                UpdateAimInputs();

                float mouseSensitivity = GetMouseSensitivity();
                _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
                _X += Input.GetAxis("Mouse X") * _runtimeSettings.cameraHorizontalSpeed * mouseSensitivity * 0.02f;
                _Y -= Input.GetAxis("Mouse Y") * _runtimeSettings.cameraVerticalSpeed * mouseSensitivity * 0.02f;

                _Y = Mathf.Clamp(_Y, _runtimeSettings.cameraMinPitch, _runtimeSettings.cameraMaxPitch);
            }

            ApplyCameraTransform(false);
            SyncGameplayCameraTransform();
            UpdateFov();
        }

        private void SyncGameplayCameraTransform()
        {
            if (CameraSync.In != null && CameraSync.In.target == transform)
            {
                CameraSync.In.SyncToTarget();
            }
        }

        private void ApplyCameraTransform(bool immediate)
        {
            if (rig == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(_Y, _X, 0);
            if (IsSniperModeActive && TryGetSniperCameraPosition(rotation, out Vector3 sniperPosition))
            {
                _currentDistance = _targetDistance;
                transform.rotation = rotation;
                transform.position = sniperPosition;
                return;
            }

            if (immediate)
            {
                _currentDistance = _targetDistance;
            }
            else
            {
                _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
                float cameraT = 1f - Mathf.Exp(-Mathf.Max(0.01f, _runtimeSettings.cameraDistanceLerpSpeed) * Time.deltaTime);
                _currentDistance = Mathf.Lerp(_currentDistance, _targetDistance, cameraT);
            }

            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -_currentDistance) + rig.position;

            transform.rotation = rotation;
            transform.position = position;
        }

        private bool TryGetSniperCameraPosition(Quaternion cameraRotation, out Vector3 position)
        {
            position = default;

            Transform anchor = GetSniperCameraAnchor();
            if (anchor == null)
            {
                return false;
            }

            Vector3 forward = cameraRotation * Vector3.forward;
            if (!IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            forward.Normalize();

            Vector3 up = cameraRotation * Vector3.up;
            if (!IsFinite(up) || up.sqrMagnitude <= 0.000001f)
            {
                up = Vector3.up;
            }
            else
            {
                up.Normalize();
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            position = anchor.position
                       + forward * Mathf.Max(0f, _runtimeSettings.cameraSniperForwardOffset)
                       + up * _runtimeSettings.cameraSniperVerticalOffset;
            return IsFinite(position);
        }

        private Transform GetSniperCameraAnchor()
        {
            if (vehicleRoot == null)
            {
                return rig;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();

            if (_runtimeSettings.cameraSniperFromMuzzle
                && vehicleRoot.shooterNet != null
                && vehicleRoot.shooterNet.muzzleTransform != null)
            {
                return vehicleRoot.shooterNet.muzzleTransform;
            }

            if (vehicleRoot.weaponAimAtCamera != null && vehicleRoot.weaponAimAtCamera.gun != null)
            {
                return vehicleRoot.weaponAimAtCamera.gun;
            }

            return rig;
        }

        private void UpdateAimInputs()
        {
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                ToggleSniper();
            }

            int scrollDirection = GetScrollDirection();
            if (scrollDirection > 0)
            {
                ZoomIn();
            }
            else if (scrollDirection < 0)
            {
                ZoomOut();
            }
        }

        private float GetMouseSensitivity()
        {
            if (IsSniperStep(_currentZoomStep))
            {
                return ClientGameplaySettings.SniperMouseSensitivity;
            }

            return ClientGameplaySettings.GameplayMouseSensitivity;
        }

        private int GetScrollDirection()
        {
            float scroll = Input.mouseScrollDelta.y;
            float legacyScroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(legacyScroll) > Mathf.Abs(scroll))
            {
                scroll = legacyScroll;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            if (Mathf.Abs(scroll) <= Mathf.Max(0.0001f, _runtimeSettings.cameraScrollDeadZone))
            {
                return 0;
            }

            if (scroll > 0f)
            {
                return 1;
            }

            return -1;
        }

        private void ToggleSniper()
        {
            if (IsSniperStep(_currentZoomStep))
            {
                ExitSniper();
                return;
            }

            EnterSniper();
        }

        private void ZoomIn()
        {
            if (IsSniperStep(_currentZoomStep))
            {
                return;
            }

            int maxNonSniperStep = GetMaxNonSniperZoomStep();
            if (_currentZoomStep < maxNonSniperStep)
            {
                ApplyZoomStep(_currentZoomStep + 1, false);
                return;
            }

            EnterSniper();
        }

        private void ZoomOut()
        {
            if (IsSniperStep(_currentZoomStep))
            {
                ExitSniper();
                return;
            }

            if (_currentZoomStep > NormalZoomStep)
            {
                ApplyZoomStep(_currentZoomStep - 1, false);
                return;
            }

            ApplyZoomStep(NormalZoomStep, false);
        }

        private void EnterSniper()
        {
            if (!IsSniperStep(_currentZoomStep))
            {
                _lastNonSniperZoomStep = _currentZoomStep;
            }

            ApplyZoomStep(GetSniperZoomStep(), false);
        }

        private void ExitSniper()
        {
            int returnStep = _lastNonSniperZoomStep;
            if (returnStep >= GetSniperZoomStep())
            {
                returnStep = NormalZoomStep;
            }

            ApplyZoomStep(returnStep, false);
        }

        private void ApplyZoomStep(int zoomStep, bool immediate)
        {
            int sniperStep = GetSniperZoomStep();
            if (zoomStep >= sniperStep)
            {
                zoomStep = sniperStep;
            }
            else if (zoomStep < NormalZoomStep)
            {
                zoomStep = NormalZoomStep;
            }
            else
            {
                int maxNonSniperStep = GetMaxNonSniperZoomStep();
                if (zoomStep > maxNonSniperStep)
                {
                    zoomStep = maxNonSniperStep;
                }
            }

            _currentZoomStep = zoomStep;
            if (!IsSniperStep(zoomStep))
            {
                _lastNonSniperZoomStep = zoomStep;
            }

            _targetDistance = GetDistanceForZoomStep(zoomStep);
            if (immediate)
            {
                _currentDistance = _targetDistance;
                ApplyCameraFov(GetFovForZoomStep(zoomStep), true);
            }

            ApplySniperUi(IsSniperStep(zoomStep), immediate);
        }

        private int GetMaxNonSniperZoomStep()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            int count = _runtimeSettings.cameraAimZoomDistances != null ? _runtimeSettings.cameraAimZoomDistances.Length : 0;
            if (count > 0)
            {
                return count - 1;
            }

            return 0;
        }

        private int GetSniperZoomStep()
        {
            return GetMaxNonSniperZoomStep() + 1;
        }

        private bool IsSniperStep(int zoomStep)
        {
            return zoomStep >= GetSniperZoomStep();
        }

        private float GetDistanceForZoomStep(int zoomStep)
        {
            if (zoomStep == NormalZoomStep)
            {
                return _normalDistance;
            }

            if (IsSniperStep(zoomStep))
            {
                _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
                return Mathf.Max(0.01f, _runtimeSettings.cameraSniperDistance);
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            if (_runtimeSettings.cameraAimZoomDistances != null
                && zoomStep >= 0
                && zoomStep < _runtimeSettings.cameraAimZoomDistances.Length)
            {
                return ClampGameplayDistance(_runtimeSettings.cameraAimZoomDistances[zoomStep]);
            }

            return ClampGameplayDistance(_runtimeSettings.cameraAimDistance);
        }

        private float GetFovForZoomStep(int zoomStep)
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();

            if (zoomStep == NormalZoomStep)
            {
                return _runtimeSettings.cameraNormalFov;
            }

            if (IsSniperStep(zoomStep))
            {
                return _runtimeSettings.cameraSniperFov;
            }

            return _runtimeSettings.cameraAimFov;
        }

        private float ClampGameplayDistance(float value)
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            float min = Mathf.Max(0.01f, Mathf.Min(_runtimeSettings.cameraSniperDistance, _normalDistance));
            float max = Mathf.Max(min, _normalDistance);
            return Mathf.Clamp(Mathf.Max(0.01f, value), min, max);
        }

        private void UpdateFov()
        {
            if (CameraSync.In == null || CameraSync.In.gameplayCamera == null)
            {
                return;
            }

            ApplyCameraFov(GetFovForZoomStep(_currentZoomStep), false);
        }

        private void ApplyCameraFov(float targetFov, bool immediate)
        {
            if (CameraSync.In == null || CameraSync.In.gameplayCamera == null)
            {
                return;
            }

            Camera cam = CameraSync.In.gameplayCamera;
            if (immediate)
            {
                cam.fieldOfView = targetFov;
                return;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, _runtimeSettings.cameraFovLerpSpeed) * Time.deltaTime);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, t);
        }

        private void ApplySniperUi(bool enabled, bool force)
        {
            if (!force && _sniperUiApplied == enabled)
            {
                return;
            }

            _sniperUiApplied = enabled;

            GunCrosshair crosshair = Singleton<GunCrosshair>.CurrentOrNull;
            if (crosshair != null)
            {
                crosshair.SetSniperMode(enabled);
            }

            SniperScopeOverlay.SetActiveScreen(enabled);
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                   && !float.IsNaN(value.y)
                   && !float.IsNaN(value.z)
                   && !float.IsInfinity(value.x)
                   && !float.IsInfinity(value.y)
                   && !float.IsInfinity(value.z);
        }
    }
}
