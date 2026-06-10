using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [DisallowMultipleComponent]
    public sealed class VehicleArmorController : MonoBehaviour, IVehicleRootAware, IVehicleStatsConsumer
    {
        private const float FallbackArmorMm = 1000f;

        public enum ArmorZone
        {
            Turret = 0,
            Hull = 1
        }

        [Header("Armor surfaces")]
        public Collider[] turretColliders = System.Array.Empty<Collider>();
        public Collider[] hullColliders = System.Array.Empty<Collider>();

        [Header("Registry-only surfaces")]
        public Collider[] additionalVehicleColliders = System.Array.Empty<Collider>();

        [Header("Editor")]
        public bool highlightArmorInPrefab = true;
        [SerializeField] private Renderer[] serverEditorArmorRenderers = System.Array.Empty<Renderer>();

        private readonly List<Collider> _registeredColliders = new List<Collider>(32);
        private VehicleArmorValues _runtimeHullArmor;
        private VehicleArmorValues _runtimeTurretArmor;
        private VehicleRoot _vehicleRoot;

        public VehicleRoot VehicleRoot => _vehicleRoot;

        public void SetVehicleRoot(VehicleRoot root)
        {
            if (_vehicleRoot == root)
            {
                Register();
                return;
            }

            Unregister();
            _vehicleRoot = root;
            Register();
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

        public bool TryGetArmorLoS(
            RaycastHit hit,
            ArmorZone zone,
            Vector3 shotDir,
            float normDeg,
            out float baseMm,
            out float losMm)
        {
            baseMm = SampleArmor(hit, zone);

            Vector3 dir = shotDir.normalized;
            Vector3 normal = hit.normal.normalized;
            float cosTheta = Mathf.Clamp(Vector3.Dot(-dir, normal), -1f, 1f);
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

        public void SetArmorCollidersEnabled(bool value)
        {
            SetCollidersEnabled(turretColliders, value);
            SetCollidersEnabled(hullColliders, value);
        }

        public Collider[] GetColliders(ArmorZone zone)
        {
            return zone == ArmorZone.Turret ? turretColliders : hullColliders;
        }

        public void SetServerEditorVisualization(bool visible)
        {
#if UNITY_EDITOR
            if (serverEditorArmorRenderers == null)
            {
                return;
            }

            for (int i = 0; i < serverEditorArmorRenderers.Length; i++)
            {
                Renderer armorRenderer = serverEditorArmorRenderers[i];
                if (armorRenderer != null)
                {
                    armorRenderer.enabled = visible;
                    armorRenderer.forceRenderingOff = !visible;
                }
            }
#endif
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

            RegisterArmorColliders(turretColliders, ArmorZone.Turret);
            RegisterArmorColliders(hullColliders, ArmorZone.Hull);
            RegisterAdditionalColliders(additionalVehicleColliders);
        }

        private void RegisterArmorColliders(Collider[] colliders, ArmorZone zone)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (targetCollider == null || _registeredColliders.Contains(targetCollider))
                {
                    continue;
                }

                VehicleColliderRegistry.RegisterArmor(targetCollider, _vehicleRoot, this, zone);
                _registeredColliders.Add(targetCollider);
            }
        }

        private void RegisterAdditionalColliders(Collider[] colliders)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (targetCollider == null || _registeredColliders.Contains(targetCollider))
                {
                    continue;
                }

                VehicleColliderRegistry.RegisterVehicle(targetCollider, _vehicleRoot);
                _registeredColliders.Add(targetCollider);
            }
        }

        private void Unregister()
        {
            for (int i = 0; i < _registeredColliders.Count; i++)
            {
                VehicleColliderRegistry.Unregister(_registeredColliders[i], _vehicleRoot);
            }

            _registeredColliders.Clear();
        }

        private float SampleArmor(RaycastHit hit, ArmorZone zone)
        {
            VehicleArmorValues armor = zone == ArmorZone.Turret
                ? _runtimeTurretArmor
                : _runtimeHullArmor;
            Transform reference = GetArmorReferenceTransform(zone);
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

        private Transform GetArmorReferenceTransform(ArmorZone zone)
        {
            if (zone == ArmorZone.Turret
                && _vehicleRoot != null
                && _vehicleRoot.robotHullRotation != null)
            {
                return _vehicleRoot.robotHullRotation.transform;
            }

            if (_vehicleRoot != null && _vehicleRoot.objectMover != null)
            {
                return _vehicleRoot.objectMover.transform;
            }

            return transform;
        }

        private static float GetArmorOrFallback(float value)
        {
            return value > 0f ? value : FallbackArmorMm;
        }

        private static void SetCollidersEnabled(Collider[] colliders, bool value)
        {
            if (colliders == null)
            {
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider = colliders[i];
                if (targetCollider != null)
                {
                    targetCollider.enabled = value;
                }
            }
        }
    }
}
