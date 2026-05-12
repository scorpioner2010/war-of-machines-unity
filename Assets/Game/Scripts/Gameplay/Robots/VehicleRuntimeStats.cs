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
        public const float DefaultShellSpeed = 70f;
        public const int DefaultShellsCount = 20;
        public const float DefaultDamageMin = 90f;
        public const float DefaultDamageMax = 110f;

        public int VehicleId;
        public string Code;
        public string Name;
        public int Level;

        public float MaxHealth;
        public float Penetration;
        public float ShellSpeed;
        public int ShellsCount;
        public float DamageMin;
        public float DamageMax;
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

            ResolveDamageRange(vehicle.damageMin, vehicle.damageMax, out float damageMin, out float damageMax);

            VehicleRuntimeStats stats = new VehicleRuntimeStats
            {
                VehicleId = vehicle.id,
                Code = vehicle.code,
                Name = vehicle.name,
                Level = vehicle.level,
                MaxHealth = Mathf.Max(0f, vehicle.hp),
                Penetration = Mathf.Max(0f, vehicle.penetration),
                ShellSpeed = ResolveShellSpeed(vehicle.shellSpeed),
                ShellsCount = ResolveShellsCount(vehicle.shellsCount),
                DamageMin = damageMin,
                DamageMax = damageMax,
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

        public void NormalizeCombatStats()
        {
            ShellSpeed = ResolveShellSpeed(ShellSpeed);
            ShellsCount = ResolveShellsCount(ShellsCount);
            ResolveDamageRange(DamageMin, DamageMax, out float damageMin, out float damageMax);
            DamageMin = damageMin;
            DamageMax = damageMax;
        }

        public static float ResolveShellSpeed(float value)
        {
            if (value > 0f)
            {
                return value;
            }

            return DefaultShellSpeed;
        }

        public static int ResolveShellsCount(int value)
        {
            if (value > 0)
            {
                return value;
            }

            return DefaultShellsCount;
        }

        public static void ResolveDamageRange(float sourceMin, float sourceMax, out float damageMin, out float damageMax)
        {
            if (sourceMin <= 0f && sourceMax <= 0f)
            {
                damageMin = DefaultDamageMin;
                damageMax = DefaultDamageMax;
                return;
            }

            damageMin = Mathf.Max(0f, sourceMin);
            damageMax = Mathf.Max(0f, sourceMax);

            if (damageMin <= 0f)
            {
                damageMin = damageMax;
            }

            if (damageMax <= 0f)
            {
                damageMax = damageMin;
            }

            if (damageMin > damageMax)
            {
                float temp = damageMin;
                damageMin = damageMax;
                damageMax = temp;
            }
        }

        public VehicleRuntimeStats Clone()
        {
            VehicleRuntimeStats clone = new VehicleRuntimeStats
            {
                VehicleId = VehicleId,
                Code = Code,
                Name = Name,
                Level = Level,
                MaxHealth = MaxHealth,
                Penetration = Penetration,
                ShellSpeed = ShellSpeed,
                ShellsCount = ShellsCount,
                DamageMin = DamageMin,
                DamageMax = DamageMax,
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

            clone.NormalizeCombatStats();
            return clone;
        }
    }
}
