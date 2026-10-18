using System.Collections.Generic;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Services;
using Game.Scripts.Player.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class FreeXpConversionPopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text availableFreeXpText;
        [SerializeField] private TMP_Dropdown vehicleDropdown;
        [SerializeField] private Slider conversionSlider;
        [SerializeField] private TMP_Text conversionAmountText;
        [SerializeField] private Button convertButton;
        [SerializeField] private Button closeButton;

        private static FreeXpConversionPopup _in;

        private readonly List<OwnedVehicleDto> _vehicles = new();
        private OwnedVehicleDto _selectedVehicle;
        private int _availableFreeXp;
        private bool _isInitialized;
        private bool _isSubmitting;

        private void Awake()
        {
            _in = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (_in == this)
            {
                _in = null;
            }
        }

        public void Show()
        {
            Initialize();

            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            PlayerProfile profile = clientInfo != null ? clientInfo.Profile : null;
            if (profile == null)
            {
                return;
            }

            _availableFreeXp = Mathf.Max(0, profile.freeXp);
            _selectedVehicle = null;
            _isSubmitting = false;

            if (availableFreeXpText != null)
            {
                availableFreeXpText.text = "Available Free XP: " + _availableFreeXp;
            }

            PopulateVehicleOptions(profile.ownedVehicles);

            if (conversionSlider != null)
            {
                conversionSlider.minValue = 0f;
                conversionSlider.maxValue = _availableFreeXp;
                conversionSlider.wholeNumbers = true;
                conversionSlider.SetValueWithoutNotify(0f);
                conversionSlider.interactable = _availableFreeXp > 0;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            UpdateAmountDisplay();
        }

        public static void NotifyConversionSucceeded()
        {
            if (_in == null)
            {
                return;
            }

            _in._isSubmitting = false;
            _in.Hide();
        }

        public static void NotifyConversionFailed()
        {
            if (_in == null)
            {
                return;
            }

            _in._isSubmitting = false;
            _in.UpdateButtons();
        }

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            if (vehicleDropdown != null)
            {
                vehicleDropdown.onValueChanged.AddListener(OnVehicleDropdownChanged);
            }

            if (conversionSlider != null)
            {
                conversionSlider.onValueChanged.AddListener(_ => UpdateAmountDisplay());
            }

            if (convertButton != null)
            {
                convertButton.onClick.AddListener(Convert);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Hide);
            }

        }

        private void PopulateVehicleOptions(OwnedVehicleDto[] vehicles)
        {
            _vehicles.Clear();

            if (vehicleDropdown == null)
            {
                UpdateButtons();
                return;
            }

            List<string> options = new List<string> { "Choose vehicle" };
            if (vehicles != null)
            {
                for (int i = 0; i < vehicles.Length; i++)
                {
                    OwnedVehicleDto vehicle = vehicles[i];
                    if (vehicle == null || vehicle.vehicleId <= 0)
                    {
                        continue;
                    }

                    _vehicles.Add(vehicle);
                    options.Add(string.IsNullOrWhiteSpace(vehicle.name) ? vehicle.code : vehicle.name);
                }
            }

            vehicleDropdown.ClearOptions();
            vehicleDropdown.AddOptions(options);
            vehicleDropdown.SetValueWithoutNotify(0);
            vehicleDropdown.RefreshShownValue();
            UpdateButtons();
        }

        private void OnVehicleDropdownChanged(int index)
        {
            if (index <= 0 || index > _vehicles.Count)
            {
                _selectedVehicle = null;
                UpdateButtons();
                return;
            }

            _selectedVehicle = _vehicles[index - 1];
            UpdateButtons();
        }

        private void UpdateAmountDisplay()
        {
            int amount = conversionSlider != null ? Mathf.RoundToInt(conversionSlider.value) : 0;
            if (conversionAmountText != null)
            {
                conversionAmountText.text = "Convert: " + amount + " XP";
            }

            UpdateButtons();
        }

        private void UpdateButtons()
        {
            int amount = conversionSlider != null ? Mathf.RoundToInt(conversionSlider.value) : 0;

            if (vehicleDropdown != null)
            {
                vehicleDropdown.interactable = !_isSubmitting && _vehicles.Count > 0;
            }

            if (convertButton != null)
            {
                convertButton.interactable = !_isSubmitting && _selectedVehicle != null && amount > 0;
            }
        }

        private void Convert()
        {
            int amount = conversionSlider != null ? Mathf.RoundToInt(conversionSlider.value) : 0;
            if (_selectedVehicle == null || _selectedVehicle.vehicleId <= 0 || amount <= 0 || amount > _availableFreeXp)
            {
                UpdateButtons();
                return;
            }

            _isSubmitting = true;
            UpdateButtons();
            ProfileServer.ConvertFreeXp(_selectedVehicle.vehicleId, amount);
        }

        private void Hide()
        {
            _isSubmitting = false;

            if (vehicleDropdown != null)
            {
                vehicleDropdown.Hide();
            }

            gameObject.SetActive(false);
        }
    }
}
