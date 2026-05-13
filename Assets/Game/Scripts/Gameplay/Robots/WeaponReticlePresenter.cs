using Game.Scripts.Core.Services;
using Game.Scripts.UI.HUD;
using Game.Scripts.UI.Settings;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class WeaponReticlePresenter : MonoBehaviour, IVehicleRootAware, IVehicleInitializable
    {
        public VehicleRoot vehicleRoot;

        public float smoothSpeed = 20f;
        public bool hideWhenBehindCamera = true;
        public bool clampToCanvas = true;
        public float hideWhenAngleGreaterThan = 90f;

        public bool showServerReticle = true;
        
        private RectTransform _serverCrosshair;
        private RectTransform _reticleRect;
        private Canvas _canvas;

        private Vector2 _curLocal;
        private Vector2 _tgtLocal;
        private bool _visible = true;

        private Vector2 _curLocalServer;
        private Vector2 _tgtLocalServer;
        private bool _visibleServer = true;

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
            GunCrosshair gunCrosshair = Singleton<GunCrosshair>.CurrentOrNull;
            if (gunCrosshair == null)
            {
                return;
            }

            _reticleRect = gunCrosshair.crosshair;
            _serverCrosshair = gunCrosshair.serverCrosshair;
            _canvas = gunCrosshair.canvas;

            if (_reticleRect != null)
            {
                _curLocal = _reticleRect.anchoredPosition;
            }
            if (_serverCrosshair != null)
            {
                _curLocalServer = _serverCrosshair.anchoredPosition;
            }
        }

        private void LateUpdate()
        {
            if (_canvas == null)
            {
                return;
            }

            if (vehicleRoot == null || vehicleRoot.weaponAimAtCamera == null)
            {
                SetVisible(false);
                SetVisibleServer(false);
                return;
            }

            Camera cam = GetGameplayCamera();
            if (cam == null)
            {
                return;
            }

            Vector3 gunFwd = GetGunForwardWorld(vehicleRoot.weaponAimAtCamera.gun, vehicleRoot.weaponAimAtCamera.localForwardAxis).normalized;
            float angle = Vector3.Angle(gunFwd, cam.transform.forward);
            if (angle > hideWhenAngleGreaterThan)
            {
                SetVisible(false);
                SetVisibleServer(false);
                return;
            }

            Vector3 worldAim = vehicleRoot.weaponAimAtCamera.CurrentAimPoint;
            if (worldAim == Vector3.zero)
            {
                worldAim = vehicleRoot.weaponAimAtCamera.DesiredAimPoint;
            }

            bool ok = WorldToCanvasLocalPoint(worldAim, cam, out Vector2 localPoint);
            if (!ok)
            {
                if (hideWhenBehindCamera)
                {
                    SetVisible(false);
                }
                else
                {
                    SetVisible(true);
                }
            }
            else
            {
                SetVisible(true);
                if (clampToCanvas)
                {
                    ClampToCanvas(ref localPoint);
                }
                _tgtLocal = localPoint;
                LerpReticle(ref _curLocal, _tgtLocal, _reticleRect);
            }

            if (showServerReticle && ClientGameplaySettings.ServerCrosshairEnabled && _serverCrosshair != null)
            {
                Vector3 srvAim = vehicleRoot.weaponAimAtCamera.ServerAimPoint;
                if (srvAim == Vector3.zero)
                {
                    srvAim = worldAim;
                }

                bool okSrv = WorldToCanvasLocalPoint(srvAim, cam, out Vector2 localSrv);
                if (!okSrv)
                {
                    if (hideWhenBehindCamera)
                    {
                        SetVisibleServer(false);
                    }
                    else
                    {
                        SetVisibleServer(true);
                    }
                }
                else
                {
                    SetVisibleServer(true);
                    if (clampToCanvas)
                    {
                        ClampToCanvas(ref localSrv);
                    }
                    _tgtLocalServer = localSrv;
                    LerpReticle(ref _curLocalServer, _tgtLocalServer, _serverCrosshair);
                }
            }
            else
            {
                SetVisibleServer(false);
            }
        }

        private bool WorldToCanvasLocalPoint(Vector3 worldPoint, Camera cam, out Vector2 localPoint)
        {
            localPoint = default;
            Vector3 sp = cam.WorldToScreenPoint(worldPoint);
            if (sp.z <= 0f)
            {
                if (!hideWhenBehindCamera)
                {
                    sp *= -1f;
                }
                else
                {
                    return false;
                }
            }

            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Camera canvasCam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : (_canvas.worldCamera != null ? _canvas.worldCamera : cam);

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, canvasCam, out localPoint);
        }

        private void LerpReticle(ref Vector2 cur, Vector2 tgt, RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            if (smoothSpeed > 0f)
            {
                float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                cur = Vector2.Lerp(cur, tgt, t);
            }
            else
            {
                cur = tgt;
            }

            if ((cur - tgt).sqrMagnitude <= 0.25f)
            {
                cur = tgt;
            }

            rect.anchoredPosition = cur;
        }

        private void ClampToCanvas(ref Vector2 localPoint)
        {
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Vector2 half = canvasRect.rect.size * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x, -half.x, half.x);
            localPoint.y = Mathf.Clamp(localPoint.y, -half.y, half.y);
        }

        private static Vector3 GetGunForwardWorld(Transform gun, WeaponAimController.Axis forwardAxis)
        {
            if (gun == null)
            {
                return Vector3.forward;
            }
            if (forwardAxis == WeaponAimController.Axis.X)
            {
                return gun.right;
            }
            if (forwardAxis == WeaponAimController.Axis.Y)
            {
                return gun.up;
            }
            return gun.forward;
        }

        private static Camera GetGameplayCamera()
        {
            if (CameraSync.In != null && CameraSync.In.gameplayCamera != null)
            {
                return CameraSync.In.gameplayCamera;
            }

            return Camera.main;
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
