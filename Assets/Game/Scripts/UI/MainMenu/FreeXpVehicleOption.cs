using System;
using Game.Scripts.API.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class FreeXpVehicleOption : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        public void Configure(OwnedVehicleDto vehicle, Action<OwnedVehicleDto> onSelected)
        {
            if (vehicle == null)
            {
                return;
            }

            if (label != null)
            {
                label.text = string.IsNullOrWhiteSpace(vehicle.name) ? vehicle.code : vehicle.name;
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelected?.Invoke(vehicle));
            }
        }

        public void ConfigureReferences(Button optionButton, TMP_Text optionLabel)
        {
            button = optionButton;
            label = optionLabel;
        }
    }
}
