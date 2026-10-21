using Game.Scripts.Core.Services;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.HUD
{
    public class GameplayGUI : MonoBehaviour
    {
        public static GameplayGUI In;
        public Transform ShotResultTextTransform
        {
            get
            {
                GunCrosshair activeCrosshair = Singleton<GunCrosshair>.CurrentOrNull;
                if (activeCrosshair != null && activeCrosshair.PenetrationStatusTextTransform != null)
                {
                    return activeCrosshair.PenetrationStatusTextTransform;
                }

                return isPenetrationText != null ? isPenetrationText.transform : null;
            }
        }
        
        [SerializeField] private TMP_Text isPenetrationText;
        [SerializeField] private TMP_Text[] zoomLevelTexts = System.Array.Empty<TMP_Text>();

        private string _zoomLevelTextValue;

        public void Awake()
        {
            In = this;
            SetZoomLevel(0);
        }

        public void SetZoomLevel(int zoomLevel)
        {
            string value = zoomLevel.ToString();
            if (_zoomLevelTextValue == value)
            {
                return;
            }

            _zoomLevelTextValue = value;
            if (zoomLevelTexts == null)
            {
                return;
            }

            for (int i = 0; i < zoomLevelTexts.Length; i++)
            {
                TMP_Text zoomLevelText = zoomLevelTexts[i];
                if (zoomLevelText != null)
                {
                    zoomLevelText.text = value;
                }
            }
        }
        
        public void UpdateHealth(float healthPercentage)
        {
           
        }

        public void ShowShotResult(string message)
        {
            GunCrosshair activeCrosshair = Singleton<GunCrosshair>.CurrentOrNull;
            if (activeCrosshair != null)
            {
                activeCrosshair.SetPenetrationStatus(message);
                return;
            }

            if (isPenetrationText != null)
            {
                isPenetrationText.text = string.IsNullOrWhiteSpace(message)
                    ? "PENETRATION STATUS"
                    : message;
            }
        }

        public void OnDestroy()
        {
            In = null;
        }
    }
}
