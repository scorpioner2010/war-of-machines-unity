using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public sealed class VehicleColliderReference : MonoBehaviour, IVehicleRootAware
    {
        public VehicleRoot vehicleRoot;
        public ArmorMap armorMap;
        public Collider targetCollider;
        public Collider[] targetColliders = System.Array.Empty<Collider>();

        private readonly List<Collider> _registeredColliders = new List<Collider>(4);

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
            if (armorMap != null)
            {
                armorMap.SetVehicleRoot(root);
            }

            Register();
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }

        private void Register()
        {
            if (vehicleRoot == null)
            {
                return;
            }

            if (targetColliders != null && targetColliders.Length > 0)
            {
                for (int i = 0; i < targetColliders.Length; i++)
                {
                    RegisterCollider(targetColliders[i]);
                }

                return;
            }

            RegisterCollider(targetCollider);
        }

        private void Unregister()
        {
            for (int i = 0; i < _registeredColliders.Count; i++)
            {
                VehicleColliderRegistry.Unregister(_registeredColliders[i], vehicleRoot);
            }

            _registeredColliders.Clear();
        }

        private void RegisterCollider(Collider collider)
        {
            if (collider == null || _registeredColliders.Contains(collider))
            {
                return;
            }

            VehicleColliderRegistry.Register(collider, vehicleRoot, armorMap);
            _registeredColliders.Add(collider);
        }
    }

    public static class VehicleColliderRegistry
    {
        private static readonly Dictionary<Collider, VehicleColliderData> DataByCollider =
            new Dictionary<Collider, VehicleColliderData>(512);

        public static void Register(Collider collider, VehicleRoot root, ArmorMap armor)
        {
            if (collider == null || root == null)
            {
                return;
            }

            DataByCollider[collider] = new VehicleColliderData(root, armor);
        }

        public static void Unregister(Collider collider, VehicleRoot root)
        {
            if (collider == null)
            {
                return;
            }

            if (!DataByCollider.TryGetValue(collider, out VehicleColliderData data))
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
            if (collider == null)
            {
                return false;
            }

            if (!DataByCollider.TryGetValue(collider, out VehicleColliderData data))
            {
                return false;
            }

            root = data.Root;
            return root != null;
        }

        public static bool TryGetArmor(Collider collider, out ArmorMap armor, out VehicleRoot root)
        {
            armor = null;
            root = null;
            if (collider == null)
            {
                return false;
            }

            if (!DataByCollider.TryGetValue(collider, out VehicleColliderData data))
            {
                return false;
            }

            armor = data.Armor;
            root = data.Root;
            return armor != null;
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
        public readonly VehicleRoot Root;
        public readonly ArmorMap Armor;

        public VehicleColliderData(VehicleRoot root, ArmorMap armor)
        {
            Root = root;
            Armor = armor;
        }
    }
}
