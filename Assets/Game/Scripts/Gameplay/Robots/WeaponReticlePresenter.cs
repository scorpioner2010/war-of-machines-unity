using Game.Scripts.Core.Services;
using Game.Scripts.Client;
using Game.Scripts.Diagnostics;
using Game.Scripts.Server;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.Settings;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DefaultExecutionOrder(100)]
    public class WeaponReticlePresenter : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        public VehicleRoot vehicleRoot;

        private RectTransform _serverCrosshair;
        private RectTransform _reticleRect;
        private Canvas _canvas;
        private RectTransform _canvasRect;
        private float _nextHudResolveTime;
        private bool _controlsLocalReticle;

        private Vector2 _curLocal;
        private Vector2 _tgtLocal;
        private bool _visible = true;
        private Vector3 _visualAimPoint;
        private bool _hasVisualAimPoint;
        private bool _wasSniperMode;

        private Vector2 _curLocalServer;
        private Vector2 _tgtLocalServer;
        private bool _visibleServer = true;
        private Vector3 _visualAimPointServer;
        private bool _hasVisualAimPointServer;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        public void OnVehicleInitialized(VehicleInitializationContext context)
        {
            _controlsLocalReticle = context.IsOwner && !context.IsMenu;
            if (_controlsLocalReticle)
            {
                Init();
            }
        }

        public void Init()
        {
            if (!_controlsLocalReticle && (vehicleRoot == null || !vehicleRoot.IsOwner || vehicleRoot.IsMenu))
            {
                return;
            }

            _controlsLocalReticle = true;
            TryResolveHudReferences(true);
        }

        private bool TryResolveHudReferences(bool force)
        {
            if (!force && Time.unscaledTime < _nextHudResolveTime)
            {
                return _canvasRect != null && _reticleRect != null;
            }

            _nextHudResolveTime = Time.unscaledTime + 0.25f;

            GunCrosshair gunCrosshair = Singleton<GunCrosshair>.CurrentOrNull;
            if (gunCrosshair == null)
            {
                return false;
            }

            _reticleRect = gunCrosshair.crosshair;
            _serverCrosshair = gunCrosshair.serverCrosshair;
            _canvas = gunCrosshair.ResolveCanvasReference();
            _canvasRect = gunCrosshair.canvasRect;

            if (_reticleRect != null)
            {
                _curLocal = _reticleRect.anchoredPosition;
                _visible = _reticleRect.gameObject.activeSelf;
            }

            if (_serverCrosshair != null)
            {
                _curLocalServer = _serverCrosshair.anchoredPosition;
                _visibleServer = _serverCrosshair.gameObject.activeSelf;
            }

            return _canvasRect != null && _reticleRect != null;
        }

        private void LateUpdate()
        {
            using (ProfileScope.Measure("Client.UI.WeaponReticle.LateUpdate", DiagnosticsCategories.Ui))
            {
                if (!_controlsLocalReticle)
                {
                    return;
                }

                if (_canvasRect == null || _reticleRect == null)
                {
                    if (!TryResolveHudReferences(false))
                    {
                        return;
                    }
                }

                if (vehicleRoot == null || vehicleRoot.weaponAimAtCamera == null)
                {
                    SetVisible(false);
                    SetVisibleServer(false);
                    ResetVisualAimPoints();
                    return;
                }

                bool sniperMode = IsSniperModeActive();
                if (sniperMode)
                {
                    vehicleRoot.cameraController.RefreshSniperCameraPose();
                }

                if (_wasSniperMode != sniperMode)
                {
                    _wasSniperMode = sniperMode;
                    ResetVisualAimPoints();
                }

                Camera cam = GetGameplayCamera();
                if (cam == null)
                {
                    return;
                }

                GameplayRuntimeSettings runtimeSettings = GameplayRuntimeSettingsProvider.Get();
                Vector3 gunFwd = vehicleRoot.weaponAimAtCamera.GetLogicalAimForwardWorld().normalized;
                float angle = Vector3.Angle(gunFwd, cam.transform.forward);
                if (angle > runtimeSettings.reticleHideWhenAngleGreaterThan)
                {
                    SetVisible(false);
                    SetVisibleServer(false);
                    ResetVisualAimPoints();
                    return;
                }

                Vector3 worldAim = ResolveVisualAimPoint(vehicleRoot.weaponAimAtCamera);
                if (worldAim == Vector3.zero)
                {
                    worldAim = vehicleRoot.weaponAimAtCamera.DesiredAimPoint;
                }

                GetReticleLerpSpeeds(sniperMode, out float horizontalLerpSpeed, out float verticalLerpSpeed);

                if (!UpdateReticle(
                        worldAim,
                        cam,
                        _reticleRect,
                        ref _curLocal,
                        ref _tgtLocal,
                        ref _visualAimPoint,
                        ref _hasVisualAimPoint,
                        horizontalLerpSpeed,
                        verticalLerpSpeed,
                        runtimeSettings))
                {
                    if (runtimeSettings.reticleHideWhenBehindCamera)
                    {
                        SetVisible(false);
                        _hasVisualAimPoint = false;
                    }
                    else
                    {
                        SetVisible(true);
                    }
                }
                else
                {
                    SetVisible(true);
                }

                if (runtimeSettings.reticleShowServerReticle && ClientGameplaySettings.ServerCrosshairEnabled && _serverCrosshair != null)
                {
                    Vector3 srvAim = vehicleRoot.weaponAimAtCamera.ServerAimPoint;
                    if (srvAim == Vector3.zero)
                    {
                        srvAim = worldAim;
                    }

                    if (!UpdateReticle(
                            srvAim,
                            cam,
                            _serverCrosshair,
                            ref _curLocalServer,
                            ref _tgtLocalServer,
                            ref _visualAimPointServer,
                            ref _hasVisualAimPointServer,
                            horizontalLerpSpeed,
                            verticalLerpSpeed,
                            runtimeSettings))
                    {
                        if (runtimeSettings.reticleHideWhenBehindCamera)
                        {
                            SetVisibleServer(false);
                            _hasVisualAimPointServer = false;
                        }
                        else
                        {
                            SetVisibleServer(true);
                        }
                    }
                    else
                    {
                        SetVisibleServer(true);
                    }
                }
                else
                {
                    SetVisibleServer(false);
                    _hasVisualAimPointServer = false;
                }
            }
        }

        private bool WorldToCanvasLocalPoint(Vector3 worldPoint, Camera cam, out Vector2 localPoint)
        {
            GameplayRuntimeSettings runtimeSettings = GameplayRuntimeSettingsProvider.Get();
            return WorldToCanvasLocalPoint(worldPoint, cam, out localPoint, out _, runtimeSettings);
        }

        private bool WorldToCanvasLocalPoint(
            Vector3 worldPoint,
            Camera cam,
            out Vector2 localPoint,
            out float screenDepth,
            GameplayRuntimeSettings runtimeSettings)
        {
            localPoint = default;
            screenDepth = 0f;
            Vector3 sp = cam.WorldToScreenPoint(worldPoint);
            screenDepth = sp.z;
            if (sp.z <= 0f)
            {
                if (!runtimeSettings.reticleHideWhenBehindCamera)
                {
                    sp *= -1f;
                }
                else
                {
                    return false;
                }
            }

            Camera canvasCam = _canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : (_canvas.worldCamera != null ? _canvas.worldCamera : cam);

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, sp, canvasCam, out localPoint);
        }

        private bool UpdateReticle(
            Vector3 targetWorldPoint,
            Camera cam,
            RectTransform rect,
            ref Vector2 cur,
            ref Vector2 tgt,
            ref Vector3 visualWorldPoint,
            ref bool hasVisualWorldPoint,
            float horizontalLerpSpeed,
            float verticalLerpSpeed,
            GameplayRuntimeSettings runtimeSettings)
        {
            if (rect == null)
            {
                return false;
            }

            if (!WorldToCanvasLocalPoint(targetWorldPoint, cam, out Vector2 targetLocal, out float targetDepth, runtimeSettings))
            {
                return false;
            }

            if (runtimeSettings.reticleClampToCanvas)
            {
                ClampToCanvas(ref targetLocal);
            }

            Vector2 currentLocal = cur;
            if (hasVisualWorldPoint
                && WorldToCanvasLocalPoint(visualWorldPoint, cam, out Vector2 projectedVisualLocal, out _, runtimeSettings))
            {
                currentLocal = projectedVisualLocal;
                if (runtimeSettings.reticleClampToCanvas)
                {
                    ClampToCanvas(ref currentLocal);
                }
            }
            else
            {
                currentLocal = rect.anchoredPosition;
                if (!hasVisualWorldPoint)
                {
                    currentLocal = targetLocal;
                }
            }

            tgt = targetLocal;
            LerpCanvasPoint(ref currentLocal, targetLocal, horizontalLerpSpeed, verticalLerpSpeed);

            if (runtimeSettings.reticleClampToCanvas)
            {
                ClampToCanvas(ref currentLocal);
            }

            cur = currentLocal;
            rect.anchoredPosition = cur;

            if (CanvasLocalPointToWorld(cur, cam, Mathf.Max(0.01f, targetDepth), out Vector3 newVisualWorldPoint))
            {
                visualWorldPoint = newVisualWorldPoint;
            }
            else
            {
                visualWorldPoint = targetWorldPoint;
            }

            hasVisualWorldPoint = true;
            return true;
        }

        private void LerpCanvasPoint(ref Vector2 cur, Vector2 tgt, float horizontalLerpSpeed, float verticalLerpSpeed)
        {
            if (horizontalLerpSpeed > 0f)
            {
                float horizontalT = 1f - Mathf.Exp(-horizontalLerpSpeed * Time.deltaTime);
                cur.x = Mathf.Lerp(cur.x, tgt.x, horizontalT);
            }
            else
            {
                cur.x = tgt.x;
            }

            if (verticalLerpSpeed > 0f)
            {
                float verticalT = 1f - Mathf.Exp(-verticalLerpSpeed * Time.deltaTime);
                cur.y = Mathf.Lerp(cur.y, tgt.y, verticalT);
            }
            else
            {
                cur.y = tgt.y;
            }

            if (Mathf.Abs(cur.x - tgt.x) <= 0.5f)
            {
                cur.x = tgt.x;
            }

            if (Mathf.Abs(cur.y - tgt.y) <= 0.5f)
            {
                cur.y = tgt.y;
            }
        }

        private void GetReticleLerpSpeeds(bool sniperMode, out float horizontalLerpSpeed, out float verticalLerpSpeed)
        {
            GunDispersionGlobalSettings settings = GetGlobalDispersionSettings();
            if (settings == null)
            {
                GameplayRuntimeSettings runtimeSettings = GameplayRuntimeSettingsProvider.Get();
                float fallback = Mathf.Max(0f, runtimeSettings.reticleFallbackSmoothSpeed);
                horizontalLerpSpeed = fallback;
                verticalLerpSpeed = fallback;
                return;
            }

            if (sniperMode)
            {
                horizontalLerpSpeed = Mathf.Max(0f, settings.uiSniperReticleHorizontalLerpSpeed);
                verticalLerpSpeed = Mathf.Max(0f, settings.uiSniperReticleVerticalLerpSpeed);
                return;
            }

            horizontalLerpSpeed = Mathf.Max(0f, settings.uiReticleHorizontalLerpSpeed);
            verticalLerpSpeed = Mathf.Max(0f, settings.uiReticleVerticalLerpSpeed);
        }

        private GunDispersionGlobalSettings GetGlobalDispersionSettings()
        {
            if (vehicleRoot != null && vehicleRoot.IsServerInitialized)
            {
                return ServerSettings.GetGunDispersion();
            }

            return RemoteServerSettings.GunDispersion;
        }

        private bool IsSniperModeActive()
        {
            return vehicleRoot != null
                   && vehicleRoot.cameraController != null
                   && vehicleRoot.cameraController.IsSniperModeActive;
        }

        private void ClampToCanvas(ref Vector2 localPoint)
        {
            if (_canvasRect == null)
            {
                return;
            }

            Vector2 half = _canvasRect.rect.size * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x, -half.x, half.x);
            localPoint.y = Mathf.Clamp(localPoint.y, -half.y, half.y);
        }

        private bool CanvasLocalPointToWorld(Vector2 localPoint, Camera cam, float depth, out Vector3 worldPoint)
        {
            worldPoint = default;
            if (_canvasRect == null)
            {
                return false;
            }

            Camera canvasCam = _canvas == null || _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : (_canvas.worldCamera != null ? _canvas.worldCamera : cam);
            Vector3 canvasWorldPoint = _canvasRect.TransformPoint(localPoint);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCam, canvasWorldPoint);
            worldPoint = cam.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
            return IsFinite(worldPoint);
        }

        private void ResetVisualAimPoints()
        {
            _hasVisualAimPoint = false;
            _hasVisualAimPointServer = false;
        }

        private static Vector3 ResolveVisualAimPoint(WeaponAimController weaponAim)
        {
            if (weaponAim == null)
            {
                return Vector3.zero;
            }

            Vector3 currentAimPoint = weaponAim.CurrentAimPoint;
            if (IsFinite(currentAimPoint) && currentAimPoint != Vector3.zero)
            {
                return currentAimPoint;
            }

            Transform gun = weaponAim.gun;
            if (gun != null)
            {
                Vector3 forward = weaponAim.GetLogicalAimForwardWorld();
                if (IsFinite(forward) && forward.sqrMagnitude > 0.000001f)
                {
                    forward.Normalize();
                    return gun.position + forward * Mathf.Max(0.25f, weaponAim.maxAimDistance);
                }
            }

            Vector3 desiredAimPoint = weaponAim.DesiredAimPoint;
            if (IsFinite(desiredAimPoint))
            {
                return desiredAimPoint;
            }

            return Vector3.zero;
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

        private static Camera GetGameplayCamera()
        {
            if (CameraSync.In != null && CameraSync.In.gameplayCamera != null)
            {
                return CameraSync.In.gameplayCamera;
            }

            return null;
        }

        private void SetVisible(bool v)
        {
            if (_reticleRect == null)
            {
                return;
            }
            if (_visible == v)
            {
                return;
            }
            _visible = v;
            _reticleRect.gameObject.SetActive(v);
        }

        private void SetVisibleServer(bool v)
        {
            if (_serverCrosshair == null)
            {
                return;
            }

            if (!ClientGameplaySettings.ServerCrosshairEnabled)
            {
                v = false;
            }

            if (_visibleServer == v)
            {
                return;
            }
            _visibleServer = v;
            _serverCrosshair.gameObject.SetActive(v);
        }
    }
}
