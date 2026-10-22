using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class VehicleItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject selectedMarker;
        [SerializeField] private VehicleBlink vehicleBlink;

        public void Apply(VehicleSlotVehicleData data)
        {
            if (data == null)
            {
                return;
            }

            string displayName = string.IsNullOrEmpty(data.Name) ? "Vehicle" : data.Name;
            name = displayName;

            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (iconImage != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
            }

            if (selectedMarker != null)
            {
                selectedMarker.SetActive(data.IsSelected);
            }

            if (vehicleBlink != null)
            {
                vehicleBlink.SetBlinking(data.IsSelected);
            }
        }
    }
}
