using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.MainMenu
{
    public class VehicleSlotView : MonoBehaviour
    {
        [SerializeField] private Button slotButton;
        [SerializeField] private Transform vehicleRoot;

        private VehicleItemView _vehicle;
        private int _vehicleId;
        private Action<int> _onVehicleSelected;

        public int SlotIndex { get; private set; }
        public bool HasVehicle => _vehicle != null;

        public void InitEmpty(int slotIndex)
        {
            SlotIndex = slotIndex;
            _vehicleId = 0;
            _onVehicleSelected = null;

            if (_vehicle != null)
            {
                Destroy(_vehicle.gameObject);
                _vehicle = null;
            }

            if (slotButton != null)
            {
                slotButton.onClick.RemoveListener(OnSlotClicked);
                slotButton.interactable = false;
            }
        }

        public void PlaceVehicle(VehicleItemView vehicle, VehicleSlotVehicleData data, Action<int> onVehicleSelected)
        {
            if (vehicle == null || data == null)
            {
                return;
            }

            if (_vehicle != null)
            {
                Destroy(_vehicle.gameObject);
            }

            _vehicle = vehicle;
            _vehicleId = data.VehicleId;
            _onVehicleSelected = onVehicleSelected;

            Transform root = vehicleRoot != null ? vehicleRoot : transform;
            _vehicle.transform.SetParent(root, false);
            _vehicle.Apply(data);

            if (slotButton != null)
            {
                slotButton.onClick.RemoveListener(OnSlotClicked);
                slotButton.onClick.AddListener(OnSlotClicked);
                slotButton.interactable = !data.IsSelected;
            }
        }

        private void OnSlotClicked()
        {
            if (_vehicleId <= 0 || _onVehicleSelected == null)
            {
                return;
            }

            _onVehicleSelected.Invoke(_vehicleId);
        }
    }

    public class VehicleSlotVehicleData
    {
        public int VehicleId;
        public string Name;
        public Sprite Icon;
        public bool IsSelected;
    }
}
