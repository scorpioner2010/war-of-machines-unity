using System;
using Game.Scripts.API;
using Game.Scripts.Gameplay.Robots;
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
        private VehicleSlotVehicleData _vehicleData;
        private Action<int> _onVehicleSelected;

        public int SlotIndex { get; private set; }
        public bool HasVehicle => _vehicle != null;

        public void InitEmpty(int slotIndex)
        {
            SlotIndex = slotIndex;
            _vehicleId = 0;
            _vehicleData = null;
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
            _vehicleData = data;
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
        public string FactionName;
        public string Class;
        public int Level;
        public int Hp;
        public int Penetration;
        public float ShellSpeed;
        public int ShellsCount;
        public float DamageMin;
        public float DamageMax;
        public float ReloadTime;
        public float Accuracy;
        public float AimTime;
        public float ViewRange;
        public float Speed;
        public float Acceleration;
        public float TraverseSpeed;
        public float TurretTraverseSpeed;
        public string TurretArmor;
        public string HullArmor;

        public void ApplyVehicleLite(VehicleLite vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(Name))
            {
                Name = vehicle.name;
            }

            FactionName = vehicle.factionName;
            Class = vehicle.@class;
            Level = vehicle.level;
            Hp = vehicle.hp;
            Penetration = vehicle.penetration;
            ShellSpeed = VehicleRuntimeStats.ResolveShellSpeed(vehicle.shellSpeed);
            ShellsCount = VehicleRuntimeStats.ResolveShellsCount(vehicle.shellsCount);
            VehicleRuntimeStats.ResolveDamageRange(vehicle.damageMin, vehicle.damageMax, out DamageMin, out DamageMax);
            ReloadTime = vehicle.reloadTime;
            Accuracy = vehicle.accuracy;
            AimTime = vehicle.aimTime;
            ViewRange = VehicleRuntimeStats.ResolveViewRange(vehicle.viewRange);
            Speed = vehicle.speed;
            Acceleration = vehicle.acceleration;
            TraverseSpeed = vehicle.traverseSpeed;
            TurretTraverseSpeed = vehicle.turretTraverseSpeed;
            TurretArmor = vehicle.turretArmor;
            HullArmor = vehicle.hullArmor;
        }
    }
}
