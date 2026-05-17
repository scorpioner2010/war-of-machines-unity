using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    [DisallowMultipleComponent]
    public class GameplayHudRuntimeBinder : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;

        private GunCrosshair[] _crosshairs;
        private bool _crosshairsCached;

        private void Awake()
        {
            Bind();
        }

        private void OnEnable()
        {
            Bind();
        }

        private void Start()
        {
            Bind();
        }

        public void Bind(Canvas preferredCanvas = null)
        {
            if (preferredCanvas != null)
            {
                canvas = preferredCanvas;
            }

            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }

            if (canvas == null)
            {
                return;
            }

            EnsureCrosshairs();
            if (_crosshairs == null)
            {
                return;
            }

            for (int i = 0; i < _crosshairs.Length; i++)
            {
                GunCrosshair crosshair = _crosshairs[i];
                if (crosshair == null)
                {
                    continue;
                }

                crosshair.ResolveCanvasReference(canvas);
            }
        }

        private void EnsureCrosshairs()
        {
            if (_crosshairsCached)
            {
                return;
            }

            _crosshairs = GetComponentsInChildren<GunCrosshair>(true);
            _crosshairsCached = true;
        }
    }
}
