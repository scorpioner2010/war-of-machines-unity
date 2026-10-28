using Game.Scripts.Core.Services;
using Game.Scripts.Gameplay.Robots;
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
        [SerializeField] private TMP_Text autonomousControlText;
        [SerializeField] private float autonomousControlPulseSpeed = 2f;
        [SerializeField] private float autonomousControlMinAlpha = 0.35f;
        [SerializeField] private float autonomousControlMaxAlpha = 1f;

        private string _zoomLevelTextValue;
        private Color _autonomousControlBaseColor = Color.white;

        public void Awake()
        {
            In = this;
            SetZoomLevel(0);
            if (autonomousControlText != null)
            {
                _autonomousControlBaseColor = autonomousControlText.color;
                autonomousControlText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            VehicleRoot localVehicle = VehicleRoot.LocalPlayerVehicle;
            VehicleInputController inputController = localVehicle != null ? localVehicle.inputManager : null;
            bool isAutonomous = inputController != null && inputController.IsAutonomous;
            if (autonomousControlText == null)
            {
                return;
            }

            if (autonomousControlText.gameObject.activeSelf != isAutonomous)
            {
                autonomousControlText.gameObject.SetActive(isAutonomous);
            }

            if (!isAutonomous)
            {
                autonomousControlText.color = _autonomousControlBaseColor;
                return;
            }

            float pulse = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * Mathf.Max(0f, autonomousControlPulseSpeed)) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(
                Mathf.Clamp01(autonomousControlMinAlpha),
                Mathf.Clamp01(autonomousControlMaxAlpha),
                pulse);
            Color color = _autonomousControlBaseColor;
            color.a *= alpha;
            autonomousControlText.color = color;
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
