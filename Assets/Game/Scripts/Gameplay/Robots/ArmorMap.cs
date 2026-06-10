using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public class ArmorMap : MonoBehaviour, IVehicleRootAware, IVehicleStatsConsumer
    {
        private const float FallbackArmorMm = 1000f;

        public enum ArmorZone
        {
            Turret = 0,
            Hull = 1
        }

        public ArmorZone armorZone = ArmorZone.Turret;

        [FormerlySerializedAs("armorCollider")]
        [SerializeField, HideInInspector] private Collider _armorCollider;

        private VehicleArmorValues _runtimeHullArmor;
        private VehicleArmorValues _runtimeTurretArmor;

        public VehicleRoot VehicleRoot { get; private set; }
        public Collider ArmorCollider => _armorCollider;
        public ArmorZone ResolvedArmorZone => armorZone;

        public void SetVehicleRoot(VehicleRoot root)
        {
            VehicleRoot = root;

            if (_armorCollider == null)
            {
                Debug.LogError(
                    $"{nameof(ArmorMap)} on {name} has no cached local Collider. Re-save the prefab after adding the component.",
                    this);
            }
        }

        public void ApplyVehicleStats(VehicleRuntimeStats stats)
        {
            if (stats == null)
            {
                _runtimeHullArmor = default;
                _runtimeTurretArmor = default;
                return;
            }

            _runtimeHullArmor = stats.HullArmor;
            _runtimeTurretArmor = stats.TurretArmor;
        }

        public bool TryGetArmorLoS(RaycastHit hit, Vector3 shotDir, float normDeg, out float baseMm, out float losMm)
        {
            baseMm = SampleArmor(hit);

            Vector3 dir = shotDir.normalized;
            Vector3 n = hit.normal.normalized;
            float cosTheta = Mathf.Clamp(Vector3.Dot(-dir, n), -1f, 1f);
            float thetaDeg = Mathf.Acos(cosTheta) * Mathf.Rad2Deg;
            float thetaPrime = Mathf.Max(0f, thetaDeg - Mathf.Max(0f, normDeg));
            float cosThetaPrime = Mathf.Cos(thetaPrime * Mathf.Deg2Rad);
            if (cosThetaPrime <= 0.0001f)
            {
                cosThetaPrime = 0.0001f;
            }

            losMm = baseMm / cosThetaPrime;
            return true;
        }

        private float SampleArmor(RaycastHit hit)
        {
            VehicleArmorValues armor = GetRuntimeArmorValues();
            Transform reference = GetArmorReferenceTransform();
            Vector3 normal = hit.normal.normalized;

            float forwardDot = reference != null ? Vector3.Dot(normal, reference.forward) : 0f;
            float absForward = Mathf.Abs(forwardDot);
            float absRight = reference != null ? Mathf.Abs(Vector3.Dot(normal, reference.right)) : 0f;

            if (absRight > absForward)
            {
                return GetArmorOrFallback(armor.Side);
            }

            if (forwardDot < -0.35f)
            {
                return GetArmorOrFallback(armor.Rear);
            }

            return GetArmorOrFallback(armor.Front);
        }

        private VehicleArmorValues GetRuntimeArmorValues()
        {
            if (armorZone == ArmorZone.Turret)
            {
                return _runtimeTurretArmor;
            }

            return _runtimeHullArmor;
        }

        private Transform GetArmorReferenceTransform()
        {
            if (armorZone == ArmorZone.Turret && VehicleRoot != null && VehicleRoot.robotHullRotation != null)
            {
                return VehicleRoot.robotHullRotation.transform;
            }

            if (VehicleRoot != null && VehicleRoot.objectMover != null)
            {
                return VehicleRoot.objectMover.transform;
            }

            return transform;
        }

        private static float GetArmorOrFallback(float value)
        {
            return value > 0f ? value : FallbackArmorMm;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            CacheLocalCollider();
        }

        private void OnValidate()
        {
            CacheLocalCollider();
        }

        private void CacheLocalCollider()
        {
            _armorCollider = GetComponent<Collider>();
        }
#endif
    }
}
