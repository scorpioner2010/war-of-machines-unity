using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public class ArmorMap : MonoBehaviour, IVehicleRootAware, IVehicleStatsConsumer
    {
        public enum ArmorZone
        {
            Auto = 0,
            Hull = 1,
            Turret = 2
        }

        public Texture2D thicknessMap;
        public ArmorZone armorZone = ArmorZone.Auto;
        [Min(0f)] public float minMm = 0;
        [Min(0f)] public float maxMm = 500;

        [FormerlySerializedAs("armorCollider")]
        [SerializeField, HideInInspector] private Collider _armorCollider;

        private bool _useRuntimeArmor;
        private VehicleArmorValues _runtimeHullArmor;
        private VehicleArmorValues _runtimeTurretArmor;
        private bool _resolvedArmorZoneCached;
        private ArmorZone _resolvedArmorZone;

        public VehicleRoot VehicleRoot { get; private set; }
        public Collider ArmorCollider => _armorCollider;
        public ArmorZone ResolvedArmorZone => ResolveArmorZone();

        public void SetVehicleRoot(VehicleRoot root)
        {
            VehicleRoot = root;
            _resolvedArmorZoneCached = false;

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
                return;
            }

            _runtimeHullArmor = stats.HullArmor;
            _runtimeTurretArmor = stats.TurretArmor;
            _useRuntimeArmor = _runtimeHullArmor.HasAny || _runtimeTurretArmor.HasAny;

            VehicleArmorValues armor = GetRuntimeArmorValues();
            if (armor.HasAny)
            {
                minMm = GetPositiveOrFallback(armor.Rear, minMm);
                maxMm = GetPositiveOrFallback(armor.Front, maxMm);
            }
        }

        public float SampleMm(Vector2 uv)
        {
            float u = Mathf.Clamp01(uv.x);
            float v = Mathf.Clamp01(uv.y);
            Color c = thicknessMap.GetPixelBilinear(u, v);
            float t = Mathf.Clamp01(c.r);
            return Mathf.Lerp(maxMm, minMm, t);
        }

        public bool TryGetArmorLoS(RaycastHit hit, Vector3 shotDir, float normDeg, out float baseMm, out float losMm)
        {
            baseMm = _useRuntimeArmor
                ? SampleRuntimeArmor(hit)
                : SampleMm(hit.textureCoord);

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

        private float SampleRuntimeArmor(RaycastHit hit)
        {
            VehicleArmorValues armor = GetRuntimeArmorValues();
            if (!armor.HasAny)
            {
                return SampleMm(hit.textureCoord);
            }

            Transform reference = GetArmorReferenceTransform();
            Vector3 normal = hit.normal.normalized;

            float forwardDot = reference != null ? Vector3.Dot(normal, reference.forward) : 0f;
            float absForward = Mathf.Abs(forwardDot);
            float absRight = reference != null ? Mathf.Abs(Vector3.Dot(normal, reference.right)) : 0f;

            if (absRight > absForward)
            {
                return GetPositiveOrFallback(armor.Side, GetPositiveOrFallback(armor.Front, maxMm));
            }

            if (forwardDot < -0.35f)
            {
                return GetPositiveOrFallback(armor.Rear, GetPositiveOrFallback(armor.Side, minMm));
            }

            return GetPositiveOrFallback(armor.Front, maxMm);
        }

        private VehicleArmorValues GetRuntimeArmorValues()
        {
            ArmorZone resolvedZone = ResolveArmorZone();
            if (resolvedZone == ArmorZone.Turret && _runtimeTurretArmor.HasAny)
            {
                return _runtimeTurretArmor;
            }

            if (resolvedZone == ArmorZone.Hull && _runtimeHullArmor.HasAny)
            {
                return _runtimeHullArmor;
            }

            if (_runtimeHullArmor.HasAny)
            {
                return _runtimeHullArmor;
            }

            return _runtimeTurretArmor;
        }

        private Transform GetArmorReferenceTransform()
        {
            ArmorZone resolvedZone = ResolveArmorZone();
            if (resolvedZone == ArmorZone.Turret && VehicleRoot != null && VehicleRoot.robotHullRotation != null)
            {
                return VehicleRoot.robotHullRotation.transform;
            }

            if (VehicleRoot != null && VehicleRoot.objectMover != null)
            {
                return VehicleRoot.objectMover.transform;
            }

            return transform;
        }

        private ArmorZone ResolveArmorZone()
        {
            if (armorZone != ArmorZone.Auto)
            {
                return armorZone;
            }

            if (_resolvedArmorZoneCached)
            {
                return _resolvedArmorZone;
            }

            Transform current = transform;
            while (current != null)
            {
                string objectName = current.name;
                if (!string.IsNullOrEmpty(objectName))
                {
                    string lower = objectName.ToLowerInvariant();
                    if (lower.Contains("turret") || lower.Contains("tower") || lower.Contains("head"))
                    {
                        _resolvedArmorZone = ArmorZone.Turret;
                        _resolvedArmorZoneCached = true;
                        return _resolvedArmorZone;
                    }

                    if (lower.Contains("hull") || lower.Contains("body") || lower.Contains("chassis"))
                    {
                        _resolvedArmorZone = ArmorZone.Hull;
                        _resolvedArmorZoneCached = true;
                        return _resolvedArmorZone;
                    }
                }

                if (VehicleRoot != null && current == VehicleRoot.transform)
                {
                    break;
                }

                current = current.parent;
            }

            _resolvedArmorZone = ArmorZone.Hull;
            _resolvedArmorZoneCached = true;
            return _resolvedArmorZone;
        }

        private static float GetPositiveOrFallback(float value, float fallback)
        {
            return value > 0f ? value : fallback;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            CacheLocalCollider();
        }

        private void OnValidate()
        {
            CacheLocalCollider();
            _resolvedArmorZoneCached = false;
        }

        private void CacheLocalCollider()
        {
            _armorCollider = GetComponent<Collider>();
        }
#endif
    }
}
