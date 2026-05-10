using System;
using Game.Scripts.API;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [Serializable]
    public struct VehicleArmorValues
    {
        public int Front;
        public int Side;
        public int Rear;

        public bool HasAny
        {
            get
            {
                return Front > 0 || Side > 0 || Rear > 0;
            }
        }

        public static VehicleArmorValues Parse(string value)
        {
            VehicleArmorValues armor = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return armor;
            }

            string[] parts = value.Split('/');
            if (parts.Length != 3)
            {
                return armor;
            }

            int.TryParse(parts[0], out armor.Front);
            int.TryParse(parts[1], out armor.Side);
            int.TryParse(parts[2], out armor.Rear);
            return armor;
        }
    }

    [Serializable]
    public class VehicleRuntimeStats
    {
        public int VehicleId;
        public string Code;
        public string Name;
        public int Level;

        public float MaxHealth;
        public float Damage;
        public float Penetration;
        public float ReloadTime;
        public float Accuracy;
        public float AimTime;
        public float Speed;
        public float Acceleration;
        public float TraverseSpeed;
        public float TurretTraverseSpeed;

        public VehicleArmorValues HullArmor;
        public VehicleArmorValues TurretArmor;

        public bool IsValid
        {
            get
            {
                return VehicleId > 0 || !string.IsNullOrEmpty(Code);
            }
        }

        public static VehicleRuntimeStats FromVehicleLite(VehicleLite vehicle)
        {
            if (vehicle == null)
            {
                return null;
            }

            VehicleRuntimeStats stats = new VehicleRuntimeStats
            {
                VehicleId = vehicle.id,
                Code = vehicle.code,
                Name = vehicle.name,
                Level = vehicle.level,
                MaxHealth = Mathf.Max(0f, vehicle.hp),
                Damage = Mathf.Max(0f, vehicle.damage),
                Penetration = Mathf.Max(0f, vehicle.penetration),
                ReloadTime = Mathf.Max(0f, vehicle.reloadTime),
                Accuracy = Mathf.Max(0f, vehicle.accuracy),
                AimTime = Mathf.Max(0f, vehicle.aimTime),
                Speed = Mathf.Max(0f, vehicle.speed),
                Acceleration = Mathf.Max(0f, vehicle.acceleration),
                TraverseSpeed = Mathf.Max(0f, vehicle.traverseSpeed),
                TurretTraverseSpeed = Mathf.Max(0f, vehicle.turretTraverseSpeed),
                HullArmor = VehicleArmorValues.Parse(vehicle.hullArmor),
                TurretArmor = VehicleArmorValues.Parse(vehicle.turretArmor)
            };

            return stats;
        }

        public VehicleRuntimeStats Clone()
        {
            return new VehicleRuntimeStats
            {
                VehicleId = VehicleId,
                Code = Code,
                Name = Name,
                Level = Level,
                MaxHealth = MaxHealth,
                Damage = Damage,
                Penetration = Penetration,
                ReloadTime = ReloadTime,
                Accuracy = Accuracy,
                AimTime = AimTime,
                Speed = Speed,
                Acceleration = Acceleration,
                TraverseSpeed = TraverseSpeed,
                TurretTraverseSpeed = TurretTraverseSpeed,
                HullArmor = HullArmor,
                TurretArmor = TurretArmor
            };
        }
    }
}
