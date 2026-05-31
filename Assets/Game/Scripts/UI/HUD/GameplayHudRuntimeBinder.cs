using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    [DisallowMultipleComponent]
    public class GameplayHudRuntimeBinder : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private GunCrosshair[] crosshairs = System.Array.Empty<GunCrosshair>();

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
                return;
            }

            if (crosshairs == null)
            {
                return;
            }

            for (int i = 0; i < crosshairs.Length; i++)
            {
                GunCrosshair crosshair = crosshairs[i];
                if (crosshair == null)
                {
                    continue;
                }

                crosshair.ResolveCanvasReference(canvas);
            }
        }
    }
}
