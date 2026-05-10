using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class GunCrosshair : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform crosshair;
        public RectTransform serverCrosshair;
        public Image  fillImage;
        public TMP_Text reloadText; 
        public TMP_Text ammoLeftText;

        private Vector2 _crosshairBaseSize;
        private Vector2 _serverCrosshairBaseSize;
        private bool _crosshairBaseSizeCached;
        private bool _serverCrosshairBaseSizeCached;

        public void SetAimingDiameters(float localDiameter, float serverDiameter)
        {
            ApplyAimingDiameter(crosshair, localDiameter, ref _crosshairBaseSize, ref _crosshairBaseSizeCached);
            ApplyAimingDiameter(serverCrosshair, serverDiameter, ref _serverCrosshairBaseSize, ref _serverCrosshairBaseSizeCached);
        }

        private static void ApplyAimingDiameter(RectTransform target, float diameter, ref Vector2 baseSize, ref bool baseSizeCached)
        {
            if (target == null)
            {
                return;
            }

            if (!baseSizeCached)
            {
                baseSize = target.sizeDelta;
                baseSizeCached = true;
            }

            float safeDiameter = Mathf.Max(1f, diameter);
            if (baseSize.x > 0f && baseSize.y > 0f)
            {
                target.sizeDelta = new Vector2(safeDiameter, safeDiameter);
                return;
            }

            float baseDiameter = Mathf.Max(1f, Mathf.Max(Mathf.Abs(baseSize.x), Mathf.Abs(baseSize.y)));
            float scale = safeDiameter / baseDiameter;
            target.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
