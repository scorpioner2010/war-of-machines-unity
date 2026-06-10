using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public sealed class VehicleColliderReference : MonoBehaviour, IVehicleRootAware
    {
        [FormerlySerializedAs("armorMap")]
        [SerializeField, HideInInspector] private ArmorMap _armorMap;

        [FormerlySerializedAs("targetCollider")]
        [SerializeField, HideInInspector] private Collider _targetCollider;

        private readonly List<Collider> _registeredColliders = new List<Collider>(4);
        private VehicleRoot _vehicleRoot;

        public ArmorMap ArmorMap => _armorMap;
        public Collider TargetCollider => _targetCollider;

        public void SetVehicleRoot(VehicleRoot root)
        {
            _vehicleRoot = root;
            if (_targetCollider == null)
            {
                Debug.LogError(
                    $"{nameof(VehicleColliderReference)} on {name} has no cached local Collider. Re-save the prefab after adding the component.",
                    this);
                return;
            }

            if (_armorMap != null)
            {
                _armorMap.SetVehicleRoot(root);
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
            if (_vehicleRoot == null)
            {
                return;
            }

            RegisterCollider(_targetCollider);
        }

        private void Unregister()
        {
            for (int i = 0; i < _registeredColliders.Count; i++)
            {
                VehicleColliderRegistry.Unregister(_registeredColliders[i], _vehicleRoot);
            }

            _registeredColliders.Clear();
        }

        private void RegisterCollider(Collider collider)
        {
            if (collider == null || _registeredColliders.Contains(collider))
            {
                return;
            }

            VehicleColliderRegistry.Register(collider, _vehicleRoot, _armorMap);
            _registeredColliders.Add(collider);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            CacheLocalComponents();
        }

        private void OnValidate()
        {
            CacheLocalComponents();
        }

        private void CacheLocalComponents()
        {
            _armorMap = GetComponent<ArmorMap>();
            _targetCollider = GetComponent<Collider>();
        }
#endif
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
