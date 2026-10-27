using System;
using System.Globalization;
using Game.Scripts.API.Models;
using Game.Scripts.Core.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class ProfileVehicleRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image vehicleIcon;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.29f, 0.17f, 0.95f);
        [SerializeField] private Color selectedColor = new Color(0.78f, 0.62f, 0.22f, 0.98f);

        private VehicleStatisticsDto _vehicle;
        private Action<VehicleStatisticsDto> _onSelected;

        public int VehicleId => _vehicle != null ? _vehicle.vehicleId : 0;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClick);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClick);
            }
        }

        public void Setup(VehicleStatisticsDto vehicle, Action<VehicleStatisticsDto> onSelected, bool selected)
        {
            _vehicle = vehicle;
            _onSelected = onSelected;

            if (_vehicle == null)
            {
                SetLabel(string.Empty);
                SetVehicleIcon(null);
                SetSelected(false);
                return;
            }

            string tier = _vehicle.tier > 0
                ? "T" + _vehicle.tier.ToString(CultureInfo.InvariantCulture)
                : "T-";
            string rowText = _vehicle.name + "  " + tier + "   "
                + _vehicle.battles.ToString(CultureInfo.InvariantCulture) + " B";
            SetLabel(rowText);
            SetVehicleIcon(_vehicle.code);
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
            {
                background.color = selected ? selectedColor : normalColor;
            }
        }

        private void HandleClick()
        {
            if (_vehicle != null)
            {
                _onSelected?.Invoke(_vehicle);
            }
        }

        private void SetLabel(string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private void SetVehicleIcon(string vehicleCode)
        {
            if (vehicleIcon == null)
            {
                return;
            }

            Sprite icon = string.IsNullOrWhiteSpace(vehicleCode)
                ? null
                : GameResourceManager.GetIcon(vehicleCode);
            vehicleIcon.sprite = icon;
            vehicleIcon.enabled = icon != null;
            vehicleIcon.gameObject.SetActive(icon != null);
        }

    }
}
