using Game.Scripts.Gameplay.Robots;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Testing
{
    public class VehicleTestRuntimeSettings : MonoBehaviour
    {
        [Header("Combat Overrides")]
        public bool activateTestParameters;
        [Min(0.01f)] public float reloadTime = 0.25f;
        [Min(1)] public int shellsCount = 999;

        [Header("Accuracy Debug")]
        [Tooltip("Keeps the gun fully aimed in VehicleTest. Robot accuracy still controls the ring size and shot spread.")]
        [FormerlySerializedAs("disableDispersionAndForceFullyAimedReticle")]
        public bool forceFullyAimedAccuracyOnly;

        [Header("Hit Markers")]
        public bool createHitMarkerSphere;
        [Min(0.01f)] public float hitMarkerRadius = 0.18f;
        public Color hitMarkerColor = new Color(1f, 0.85f, 0.05f, 1f);

        private Material _hitMarkerMaterial;

        public bool HasActiveTestParameters => activateTestParameters;

        private void OnEnable()
        {
            NetworkWeaponShooter.AuthoritativeProjectileHit += OnAuthoritativeProjectileHit;
        }

        private void OnDisable()
        {
            NetworkWeaponShooter.AuthoritativeProjectileHit -= OnAuthoritativeProjectileHit;
        }

        private void OnDestroy()
        {
            if (_hitMarkerMaterial != null)
            {
                Destroy(_hitMarkerMaterial);
                _hitMarkerMaterial = null;
            }
        }

        public VehicleRuntimeStats BuildRuntimeStats(VehicleRuntimeStats source)
        {
            if (source == null)
            {
                return null;
            }

            VehicleRuntimeStats stats = source.Clone();
            if (!activateTestParameters)
            {
                return stats;
            }

            stats.ReloadTime = Mathf.Max(0.01f, reloadTime);
            stats.ShellsCount = Mathf.Max(1, shellsCount);
            stats.NormalizeCombatStats();
            return stats;
        }

        public void ApplyToVehicle(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null || vehicleRoot.shooterNet == null)
            {
                return;
            }

            vehicleRoot.shooterNet.SetTestAccuracyDebugMode(forceFullyAimedAccuracyOnly);
        }

        private void OnAuthoritativeProjectileHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (!createHitMarkerSphere || gameObject.scene.name != "VehicleTest")
            {
                return;
            }

            CreateHitMarker(hitPoint, hitNormal);
        }

        private void CreateHitMarker(Vector3 hitPoint, Vector3 hitNormal)
        {
            Vector3 normal = hitNormal.sqrMagnitude > 0.000001f ? hitNormal.normalized : Vector3.up;
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "VehicleTest_HitMarker";
            marker.transform.position = hitPoint + normal * 0.015f;

            float diameter = Mathf.Max(0.01f, hitMarkerRadius) * 2f;
            marker.transform.localScale = new Vector3(diameter, diameter, diameter);

            Collider markerCollider = marker.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                markerRenderer.sharedMaterial = GetHitMarkerMaterial();
            }
        }

        private Material GetHitMarkerMaterial()
        {
            if (_hitMarkerMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                _hitMarkerMaterial = new Material(shader);
            }

            if (_hitMarkerMaterial.HasProperty("_BaseColor"))
            {
                _hitMarkerMaterial.SetColor("_BaseColor", hitMarkerColor);
            }
            else
            {
                _hitMarkerMaterial.color = hitMarkerColor;
            }

            return _hitMarkerMaterial;
        }
    }
}
