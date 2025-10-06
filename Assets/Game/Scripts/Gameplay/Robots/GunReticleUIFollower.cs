using Game.Scripts.Core.Services;
using Game.Scripts.UI.HUD;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public class GunReticleUIFollower : MonoBehaviour
    {
        public TankRoot tankRoot;

        public float smoothSpeed = 20f;
        public bool hideWhenBehindCamera = true;
        public bool clampToCanvas = true;
        public float hideWhenAngleGreaterThan = 90f;

        public bool showServerReticle = true;
        
        private RectTransform _serverCrosshair; // окремий UI-елемент для серверного прицілу
        private RectTransform _reticleRect;
        private Canvas _canvas;

        private Vector2 _curLocal;
        private Vector2 _tgtLocal;
        private bool _visible = true;

        private Vector2 _curLocalServer;
        private Vector2 _tgtLocalServer;
        private bool _visibleServer = true;

        public void Init()
        {
            GunCrosshair gunCrosshair = Singleton<GunCrosshair>.Instance;
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

            Camera cam = Camera.main;
            if (cam == null)
            {
                return;
            }

            // 1) Кут між пушкою і камерою: якщо занадто різняться — ховаємо
            Vector3 gunFwd = GetGunForwardWorld(tankRoot.weaponAimAtCamera.gun, tankRoot.weaponAimAtCamera.localForwardAxis).normalized;
            float angle = Vector3.Angle(gunFwd, cam.transform.forward);
            if (angle > hideWhenAngleGreaterThan)
            {
                SetVisible(false);
                SetVisibleServer(false);
                return;
            }

            // 2) ЛОКАЛЬНИЙ приціл (швидкий)
            Vector3 worldAim = tankRoot.weaponAimAtCamera.CurrentAimPoint;
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

            // 3) СЕРВЕРНИЙ приціл (повільніший, авторитетний)
            if (showServerReticle && _serverCrosshair != null)
            {
                Vector3 srvAim = tankRoot.weaponAimAtCamera.ServerAimPoint;
                // якщо ще не оновлювався SyncVar (Vector3.zero), підстрахуємось локальним
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

            rect.anchoredPosition = cur;
        }

        private void ClampToCanvas(ref Vector2 localPoint)
        {
            RectTransform canvasRect = _canvas.GetComponent<RectTransform>();
            Vector2 half = canvasRect.rect.size * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x, -half.x, half.x);
            localPoint.y = Mathf.Clamp(localPoint.y, -half.y, half.y);
        }

        private static Vector3 GetGunForwardWorld(Transform gun, WeaponAimAtCamera.Axis forwardAxis)
        {
            if (gun == null)
            {
                return Vector3.forward;
            }
            if (forwardAxis == WeaponAimAtCamera.Axis.X)
            {
                return gun.right;
            }
            if (forwardAxis == WeaponAimAtCamera.Axis.Y)
            {
                return gun.up;
            }
            return gun.forward;
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
            if (_visibleServer == v)
            {
                return;
            }
            _visibleServer = v;
            _serverCrosshair.gameObject.SetActive(v);
        }
    }
}
