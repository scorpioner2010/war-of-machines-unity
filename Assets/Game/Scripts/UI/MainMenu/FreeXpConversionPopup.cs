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
        [SerializeField] private TMP_Text selectedVehicleText;
        [SerializeField] private Button vehicleDropdownButton;
        [SerializeField] private GameObject vehicleOptionsRoot;
        [SerializeField] private Transform vehicleOptionsContainer;
        [SerializeField] private FreeXpVehicleOption vehicleOptionTemplate;
        [SerializeField] private Slider conversionSlider;
        [SerializeField] private TMP_Text conversionAmountText;
        [SerializeField] private Button convertButton;
        [SerializeField] private Button closeButton;

        private static FreeXpConversionPopup _in;

        private readonly List<FreeXpVehicleOption> _generatedOptions = new();
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

        public void Configure(
            TMP_Text availableXp,
            TMP_Text selectedVehicle,
            Button dropdownButton,
            GameObject optionsRoot,
            Transform optionsContainer,
            FreeXpVehicleOption optionTemplate,
            Slider slider,
            TMP_Text amountText,
            Button convert,
            Button close)
        {
            availableFreeXpText = availableXp;
            selectedVehicleText = selectedVehicle;
            vehicleDropdownButton = dropdownButton;
            vehicleOptionsRoot = optionsRoot;
            vehicleOptionsContainer = optionsContainer;
            vehicleOptionTemplate = optionTemplate;
            conversionSlider = slider;
            conversionAmountText = amountText;
            convertButton = convert;
            closeButton = close;
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

            if (selectedVehicleText != null)
            {
                selectedVehicleText.text = "Choose vehicle";
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

            if (vehicleOptionsRoot != null)
            {
                vehicleOptionsRoot.SetActive(false);
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

            if (vehicleDropdownButton != null)
            {
                vehicleDropdownButton.onClick.AddListener(ToggleVehicleOptions);
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

            if (vehicleOptionTemplate != null)
            {
                vehicleOptionTemplate.gameObject.SetActive(false);
            }

            if (vehicleOptionsRoot != null)
            {
                vehicleOptionsRoot.SetActive(false);
            }
        }

        private void ToggleVehicleOptions()
        {
            if (vehicleOptionsRoot == null || _isSubmitting || _generatedOptions.Count == 0)
            {
                return;
            }

            vehicleOptionsRoot.SetActive(!vehicleOptionsRoot.activeSelf);
        }

        private void PopulateVehicleOptions(OwnedVehicleDto[] vehicles)
        {
            ClearVehicleOptions();

            if (vehicles == null)
            {
                UpdateButtons();
                return;
            }

            for (int i = 0; i < vehicles.Length; i++)
            {
                OwnedVehicleDto vehicle = vehicles[i];
                if (vehicle == null || vehicle.vehicleId <= 0 || vehicleOptionTemplate == null || vehicleOptionsContainer == null)
                {
                    continue;
                }

                FreeXpVehicleOption option = Instantiate(vehicleOptionTemplate, vehicleOptionsContainer);
                option.gameObject.SetActive(true);
                option.Configure(vehicle, SelectVehicle);
                _generatedOptions.Add(option);
            }

            UpdateButtons();
        }

        private void ClearVehicleOptions()
        {
            for (int i = 0; i < _generatedOptions.Count; i++)
            {
                FreeXpVehicleOption option = _generatedOptions[i];
                if (option != null)
                {
                    Destroy(option.gameObject);
                }
            }

            _generatedOptions.Clear();
        }

        private void SelectVehicle(OwnedVehicleDto vehicle)
        {
            _selectedVehicle = vehicle;

            if (selectedVehicleText != null)
            {
                selectedVehicleText.text = string.IsNullOrWhiteSpace(vehicle.name) ? vehicle.code : vehicle.name;
            }

            if (vehicleOptionsRoot != null)
            {
                vehicleOptionsRoot.SetActive(false);
            }

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

            if (vehicleDropdownButton != null)
            {
                vehicleDropdownButton.interactable = !_isSubmitting && _generatedOptions.Count > 0;
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

            if (vehicleOptionsRoot != null)
            {
                vehicleOptionsRoot.SetActive(false);
            }

            gameObject.SetActive(false);
        }
    }
}
