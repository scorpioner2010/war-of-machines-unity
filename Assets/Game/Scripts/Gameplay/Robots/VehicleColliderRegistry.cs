using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public static class VehicleColliderRegistry
    {
        private static readonly Dictionary<Collider, VehicleColliderData> DataByCollider =
            new Dictionary<Collider, VehicleColliderData>(512);

        public static void RegisterArmor(
            Collider collider,
            VehicleRoot root,
            VehicleArmorController armor,
            VehicleArmorController.ArmorZone zone)
        {
            if (collider == null || root == null || armor == null)
            {
                return;
            }

            DataByCollider[collider] = new VehicleColliderData(root, armor, zone, true);
        }

        public static void RegisterVehicle(Collider collider, VehicleRoot root)
        {
            if (collider == null || root == null)
            {
                return;
            }

            DataByCollider[collider] = new VehicleColliderData(
                root,
                null,
                VehicleArmorController.ArmorZone.Turret,
                false);
        }

        public static void Unregister(Collider collider, VehicleRoot root)
        {
            if (collider == null
                || !DataByCollider.TryGetValue(collider, out VehicleColliderData data))
            {
                return;
            }

            if (data.Root == root)
            {
                DataByCollider.Remove(collider);
            }
        }

        public static bool TryGetRoot(Collider collider, out VehicleRoot root)
        {
            root = null;
            if (collider == null
                || !DataByCollider.TryGetValue(collider, out VehicleColliderData data))
            {
                return false;
            }

            root = data.Root;
            return root != null;
        }

        public static bool TryGetArmor(
            Collider collider,
            out VehicleArmorController armor,
            out VehicleArmorController.ArmorZone zone,
            out VehicleRoot root)
        {
            armor = null;
            zone = VehicleArmorController.ArmorZone.Turret;
            root = null;
            if (collider == null
                || !DataByCollider.TryGetValue(collider, out VehicleColliderData data)
                || !data.IsArmor)
            {
                return false;
            }

            armor = data.Armor;
            zone = data.Zone;
            root = data.Root;
            return armor != null && root != null;
        }

        public static bool TryGetData(Collider collider, out VehicleColliderData data)
        {
            if (collider == null)
            {
                data = default;
                return false;
            }

            return DataByCollider.TryGetValue(collider, out data);
        }
    }

    public readonly struct VehicleColliderData
    {
        public VehicleColliderData(
            VehicleRoot root,
            VehicleArmorController armor,
            VehicleArmorController.ArmorZone zone,
            bool isArmor)
        {
            Root = root;
            Armor = armor;
            Zone = zone;
            IsArmor = isArmor;
        }

        public VehicleRoot Root { get; }
        public VehicleArmorController Armor { get; }
        public VehicleArmorController.ArmorZone Zone { get; }
        public bool IsArmor { get; }
    }
}
