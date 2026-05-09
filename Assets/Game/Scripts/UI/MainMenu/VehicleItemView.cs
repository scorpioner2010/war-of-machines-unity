using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class VehicleItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject selectedMarker;

        public void Apply(VehicleSlotVehicleData data)
        {
            if (data == null)
            {
                return;
            }

            name = string.IsNullOrEmpty(data.Name) ? "Vehicle" : data.Name;

            if (iconImage != null)
            {
                iconImage.sprite = data.Icon;
                iconImage.enabled = data.Icon != null;
            }

            if (selectedMarker != null)
            {
                selectedMarker.SetActive(data.IsSelected);
            }
        }
    }
}
