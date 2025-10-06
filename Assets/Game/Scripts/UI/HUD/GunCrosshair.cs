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
    }
}
