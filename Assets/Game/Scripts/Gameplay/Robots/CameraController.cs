using UnityEngine;
using Game.Scripts.Core.Services;
using Game.Scripts.Client;
using Game.Scripts.Diagnostics;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Settings;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(-100)]
    public class CameraController : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        private const int DefaultZoomStep = 0;

        public VehicleRoot vehicleRoot;
        public Transform rig;
        public CameraCollisionController collisionController;

        private float _X;
        private float _Y;
        private float _targetDistance;
        private float _currentDistance;
        private int _currentZoomStep = DefaultZoomStep;
        private int _lastNonSniperZoomStep = DefaultZoomStep;
        private bool _sniperUiApplied;
        private bool _initialized;
        private GameplayRuntimeSettings _runtimeSettings = GameplayRuntimeSettings.Default;

        public bool IsSniperModeActive => IsSniperStep(_currentZoomStep);
        public CameraZoomProfile CurrentZoomProfile => GetZoomProfile(_currentZoomStep);

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

            Vector3 angles = transform.eulerAngles;
            _X = angles.y;
            _Y = angles.x;

            if (CameraSync.In != null)
            {
                CameraSync.In.target = transform;

            }

            _currentDistance = GetZoomProfile(DefaultZoomStep).cameraDistance;
            _currentZoomStep = DefaultZoomStep;
            _lastNonSniperZoomStep = DefaultZoomStep;
            ApplyZoomStep(DefaultZoomStep, true);
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

        private void Update()
        {
            using (ProfileScope.Measure("Camera.Controller.Update", DiagnosticsCategories.Render))
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

            _targetDistance = GetDistanceForZoomStep(_currentZoomStep);
            Quaternion rotation = Quaternion.Euler(_Y, _X, 0);
            if (IsSniperModeActive && TryGetSniperCameraPosition(rotation, out Vector3 sniperPosition))
            {
                _currentDistance = _targetDistance;
                transform.rotation = rotation;
                transform.position = sniperPosition;
                ResolveCameraCollision();
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
            ResolveCameraCollision();
        }

        private void ResolveCameraCollision()
        {
            if (collisionController == null || rig == null)
            {
                return;
            }

            collisionController.Resolve(rig.position);
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
            float profileDistance = GetDistanceForZoomStep(_currentZoomStep);
            position = anchor.position
                       + forward * (Mathf.Max(0f, _runtimeSettings.cameraSniperForwardOffset) + profileDistance)
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

            if (_currentZoomStep > -GetMaxBackwardZoomStep())
            {
                ApplyZoomStep(_currentZoomStep - 1, false);
                return;
            }

            ApplyZoomStep(-GetMaxBackwardZoomStep(), false);
        }

        private void EnterSniper()
        {
            if (!IsSniperStep(_currentZoomStep))
            {
                _lastNonSniperZoomStep = _currentZoomStep;
                AlignSniperCameraToCurrentGunAim();
            }

            ApplyZoomStep(GetSniperZoomStep(), false);
        }

        private void AlignSniperCameraToCurrentGunAim()
        {
            if (!TryGetCurrentGunAimPoint(out Vector3 aimPoint))
            {
                return;
            }

            Transform anchor = GetSniperCameraAnchor();
            if (anchor == null)
            {
                return;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            Vector3 origin = anchor.position;
            float sniperProfileDistance = GetDistanceForZoomStep(GetSniperZoomStep());
            float yaw = _X;
            float pitch = _Y;

            for (int i = 0; i < 2; i++)
            {
                Vector3 direction = aimPoint - origin;
                if (!TryGetCameraAngles(direction, out yaw, out pitch))
                {
                    return;
                }

                Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 forward = rotation * Vector3.forward;
                Vector3 up = rotation * Vector3.up;
                origin = anchor.position
                         + forward * (Mathf.Max(0f, _runtimeSettings.cameraSniperForwardOffset) + sniperProfileDistance)
                         + up * _runtimeSettings.cameraSniperVerticalOffset;
            }

            _X = yaw;
            _Y = pitch;
        }

        private bool TryGetCurrentGunAimPoint(out Vector3 aimPoint)
        {
            aimPoint = default;

            WeaponAimController weaponAim = vehicleRoot != null ? vehicleRoot.weaponAimAtCamera : null;
            if (weaponAim == null)
            {
                return false;
            }

            Vector3 origin = weaponAim.gun != null ? weaponAim.gun.position : transform.position;
            if (IsUsableAimPoint(weaponAim.CurrentAimPoint, origin))
            {
                aimPoint = weaponAim.CurrentAimPoint;
                return true;
            }

            if (IsUsableAimPoint(weaponAim.DesiredAimPoint, origin))
            {
                aimPoint = weaponAim.DesiredAimPoint;
                return true;
            }

            if (weaponAim.gun == null)
            {
                return false;
            }

            Vector3 forward = weaponAim.GetLogicalAimForwardWorld();
            if (!IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            forward.Normalize();
            aimPoint = weaponAim.gun.position + forward * Mathf.Max(0.25f, weaponAim.maxAimDistance);
            return IsFinite(aimPoint);
        }

        private static bool IsUsableAimPoint(Vector3 aimPoint, Vector3 origin)
        {
            return IsFinite(aimPoint) && (aimPoint - origin).sqrMagnitude > 0.000001f;
        }

        private bool TryGetCameraAngles(Vector3 direction, out float yaw, out float pitch)
        {
            yaw = _X;
            pitch = _Y;

            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            direction.Normalize();
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 euler = rotation.eulerAngles;

            yaw = euler.y;
            pitch = Mathf.Clamp(NormalizeSignedAngle(euler.x), _runtimeSettings.cameraMinPitch, _runtimeSettings.cameraMaxPitch);
            return true;
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        private void ExitSniper()
        {
            int returnStep = _lastNonSniperZoomStep;
            if (returnStep >= GetSniperZoomStep())
            {
                returnStep = DefaultZoomStep;
            }

            AlignOrbitCameraToCurrentGunAim();
            ApplyZoomStep(returnStep, false);
        }

        private void AlignOrbitCameraToCurrentGunAim()
        {
            if (rig == null || !TryGetCurrentGunAimPoint(out Vector3 aimPoint))
            {
                return;
            }

            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            if (TryGetCameraAngles(aimPoint - rig.position, out float yaw, out float pitch))
            {
                _X = yaw;
                _Y = pitch;
            }
        }

        private void ApplyZoomStep(int zoomStep, bool immediate)
        {
            int sniperStep = GetSniperZoomStep();
            if (zoomStep >= sniperStep)
            {
                zoomStep = sniperStep;
            }
            else if (zoomStep < -GetMaxBackwardZoomStep())
            {
                zoomStep = -GetMaxBackwardZoomStep();
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
            return _runtimeSettings.cameraZoomForwardSteps != null
                ? _runtimeSettings.cameraZoomForwardSteps.Length
                : 0;
        }

        private int GetMaxBackwardZoomStep()
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            return _runtimeSettings.cameraZoomBackwardSteps != null
                ? _runtimeSettings.cameraZoomBackwardSteps.Length
                : 0;
        }

        private int GetSniperZoomStep()
        {
            return GetMaxNonSniperZoomStep() + 1;
        }

        private bool IsSniperStep(int zoomStep)
        {
            return zoomStep >= GetSniperZoomStep();
        }

        private CameraZoomProfile GetZoomProfile(int zoomStep)
        {
            _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            if (IsSniperStep(zoomStep))
            {
                return _runtimeSettings.cameraZoomSniper ?? GameplayRuntimeSettings.Default.cameraZoomSniper;
            }

            if (zoomStep > DefaultZoomStep
                && _runtimeSettings.cameraZoomForwardSteps != null
                && zoomStep - 1 < _runtimeSettings.cameraZoomForwardSteps.Length)
            {
                return _runtimeSettings.cameraZoomForwardSteps[zoomStep - 1]
                       ?? GameplayRuntimeSettings.Default.cameraZoomDefault;
            }

            if (zoomStep < DefaultZoomStep
                && _runtimeSettings.cameraZoomBackwardSteps != null
                && -zoomStep - 1 < _runtimeSettings.cameraZoomBackwardSteps.Length)
            {
                return _runtimeSettings.cameraZoomBackwardSteps[-zoomStep - 1]
                       ?? GameplayRuntimeSettings.Default.cameraZoomDefault;
            }

            return _runtimeSettings.cameraZoomDefault ?? GameplayRuntimeSettings.Default.cameraZoomDefault;
        }

        private float GetDistanceForZoomStep(int zoomStep)
        {
            return Mathf.Max(0.01f, GetZoomProfile(zoomStep).cameraDistance);
        }

        private float GetFovForZoomStep(int zoomStep)
        {
            return Mathf.Clamp(GetZoomProfile(zoomStep).fieldOfView, 1f, 179f);
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
