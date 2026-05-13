using UnityEngine;
using Game.Scripts.Core.Services;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Settings;

namespace Game.Scripts.Gameplay.Robots
{
    public class CameraController : MonoBehaviour, IVehicleInitializable
    {
        private const int NormalZoomStep = -1;

        public Transform rig;
        public float distance = 10f;
        public float aimDistance = 6f;
        public float sniperDistance = 0.25f;
        public float xSpeed = 120.0f;
        public float ySpeed = 120.0f;
        public float normalFov = 60f;
        public float aimFov = 45f;
        public float sniperFov = 24f;
        public float fovLerpSpeed = 10f;
        public float cameraLerpSpeed = 12f;
        public float scrollDeadZone = 0.001f;
        public float[] aimZoomDistances = { 6f, 4f, 2.5f };

        private float _X;
        private float _Y;
        private float _normalDistance;
        private float _targetDistance;
        private int _currentZoomStep = NormalZoomStep;
        private int _lastNonSniperZoomStep = NormalZoomStep;
        private bool _sniperUiApplied;
        private bool _initialized;

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            if (context.IsOwner && !context.IsMenu)
            {
                Init();
            }
        }

        public void Init()
        {
            Vector3 angles = transform.eulerAngles;
            _X = angles.y;
            _Y = angles.x;

            if (CameraSync.In != null)
            {
                CameraSync.In.target = transform;

                if (CameraSync.In.gameplayCamera != null)
                {
                    CameraSync.In.gameplayCamera.fieldOfView = normalFov;
                }
            }

            _normalDistance = Mathf.Max(0.1f, distance);
            _currentZoomStep = NormalZoomStep;
            _lastNonSniperZoomStep = NormalZoomStep;
            ApplyZoomStep(NormalZoomStep, true);
            _initialized = true;
            ApplyCameraTransform(true);
        }

        private void Update()
        {
            if (!_initialized || rig == null)
            {
                return;
            }

            bool inputBlocked = InputManager.IsGameplayInputBlockedByUi;
            if (!inputBlocked)
            {
                UpdateAimInputs();

                float mouseSensitivity = GetMouseSensitivity();
                _X += Input.GetAxis("Mouse X") * xSpeed * mouseSensitivity * 0.02f;
                _Y -= Input.GetAxis("Mouse Y") * ySpeed * mouseSensitivity * 0.02f;

                _Y = Mathf.Clamp(_Y, -20, 80);
            }

            ApplyCameraTransform(false);
            UpdateFov();
        }

        private void ApplyCameraTransform(bool immediate)
        {
            if (rig == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.Euler(_Y, _X, 0);
            if (immediate)
            {
                distance = _targetDistance;
            }
            else
            {
                float cameraT = 1f - Mathf.Exp(-Mathf.Max(0.01f, cameraLerpSpeed) * Time.deltaTime);
                distance = Mathf.Lerp(distance, _targetDistance, cameraT);
            }

            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + rig.position;

            transform.rotation = rotation;
            transform.position = position;
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

            if (Mathf.Abs(scroll) <= Mathf.Max(0.0001f, scrollDeadZone))
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
                distance = _targetDistance;
                ApplyCameraFov(GetFovForZoomStep(zoomStep), true);
            }

            ApplySniperUi(IsSniperStep(zoomStep), immediate);
        }

        private int GetMaxNonSniperZoomStep()
        {
            int count = aimZoomDistances != null ? aimZoomDistances.Length : 0;
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
                return Mathf.Max(0.01f, sniperDistance);
            }

            if (aimZoomDistances != null && zoomStep >= 0 && zoomStep < aimZoomDistances.Length)
            {
                return ClampGameplayDistance(aimZoomDistances[zoomStep]);
            }

            return ClampGameplayDistance(aimDistance);
        }

        private float GetFovForZoomStep(int zoomStep)
        {
            if (zoomStep == NormalZoomStep)
            {
                return normalFov;
            }

            if (IsSniperStep(zoomStep))
            {
                return sniperFov;
            }

            return aimFov;
        }

        private float ClampGameplayDistance(float value)
        {
            float min = Mathf.Max(0.01f, Mathf.Min(sniperDistance, _normalDistance));
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

            float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, fovLerpSpeed) * Time.deltaTime);
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
    }
}
