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

        public void Awake()
        {
            In = this;
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
