using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public sealed class RobotDestructionParticleController : MonoBehaviour, IVehicleRootAware
    {
        [Header("Robot destruction bursts")]
        [SerializeField] private VehicleRoot vehicleRoot;
        [SerializeField] private PooledImpactFx mainExplosionPrefab;
        [SerializeField] private PooledImpactFx[] explosionPrefabs = System.Array.Empty<PooledImpactFx>();
        [SerializeField] private PooledImpactFx[] freeExplosionPrefabs = System.Array.Empty<PooledImpactFx>();

        [Header("Detached-part fire")]
        [SerializeField] private GameObject partFirePrefab;
        [SerializeField, Min(0)] private int additionalFirePartCount = 3;
        [SerializeField, Min(0.1f)] private float partFireLifetime = 30f;

        [Header("FX pooling")]
        [SerializeField, Min(1)] private int fxPoolMaxInactive = 8;
        [SerializeField, Min(0)] private int fxPoolPrewarmCount = 1;

        private bool _destructionPlayed;

        public void SetVehicleRoot(VehicleRoot root)
        {
            vehicleRoot = root;
        }

        private void Awake()
        {
            ConfigurePools();
        }

        private void OnEnable()
        {
            _destructionPlayed = false;
            ConfigurePools();
        }

        public void PlayDestruction(
            Vector3 center,
            IList<GameObject> detachedParts,
            IList<Vector3> detachedPartCenters)
        {
            if (_destructionPlayed)
            {
                return;
            }

            _destructionPlayed = true;
            Vector3 safeCenter = IsFinite(center) ? center : transform.position;

            SpawnOneShot(mainExplosionPrefab, safeCenter);
            SpawnOneShot(SelectRandomPrefab(explosionPrefabs), safeCenter);
            SpawnOneShot(SelectRandomPrefab(freeExplosionPrefabs), safeCenter);
            SpawnPartFire(detachedParts, detachedPartCenters);
        }

        private void ConfigurePools()
        {
            ConfigurePool(mainExplosionPrefab);
            ConfigurePools(explosionPrefabs);
            ConfigurePools(freeExplosionPrefabs);
        }

        private void ConfigurePools(PooledImpactFx[] prefabs)
        {
            if (prefabs == null)
            {
                return;
            }

            for (int i = 0; i < prefabs.Length; i++)
            {
                ConfigurePool(prefabs[i]);
            }
        }

        private void ConfigurePool(PooledImpactFx prefab)
        {
            if (prefab == null)
            {
                return;
            }

            ProjectileRuntimePool.ConfigureVisualFxPool(prefab, fxPoolMaxInactive);
            if (fxPoolPrewarmCount > 0)
            {
                ProjectileRuntimePool.PrewarmVisualFx(prefab, fxPoolPrewarmCount, fxPoolMaxInactive);
            }
        }

        private static void SpawnOneShot(PooledImpactFx prefab, Vector3 position)
        {
            if (prefab != null)
            {
                ProjectileRuntimePool.SpawnVisualFx(prefab, position, Quaternion.identity, 1f);
            }
        }

        private void SpawnPartFire(IList<GameObject> detachedParts, IList<Vector3> detachedPartCenters)
        {
            if (partFirePrefab == null || detachedParts == null || detachedParts.Count == 0)
            {
                return;
            }

            GameObject mandatoryPart = null;
            for (int i = 0; i < detachedParts.Count; i++)
            {
                if (detachedParts[i] != null)
                {
                    mandatoryPart = detachedParts[i];
                    break;
                }
            }

            if (mandatoryPart == null)
            {
                return;
            }

            AttachPartFire(mandatoryPart, GetDetachedPartCenter(mandatoryPart, detachedParts, detachedPartCenters));

            List<GameObject> candidates = new List<GameObject>(detachedParts.Count - 1);
            for (int i = 0; i < detachedParts.Count; i++)
            {
                GameObject part = detachedParts[i];
                if (part != null && part != mandatoryPart)
                {
                    candidates.Add(part);
                }
            }

            int fireCount = Mathf.Min(additionalFirePartCount, candidates.Count);
            for (int i = 0; i < fireCount; i++)
            {
                int candidateIndex = Random.Range(0, candidates.Count);
                GameObject candidate = candidates[candidateIndex];
                AttachPartFire(candidate, GetDetachedPartCenter(candidate, detachedParts, detachedPartCenters));
                candidates.RemoveAt(candidateIndex);
            }
        }

        private void AttachPartFire(GameObject part, Vector3 partCenter)
        {
            if (part == null)
            {
                return;
            }

            GameObject fire = Instantiate(partFirePrefab);
            Transform fireTransform = fire.transform;
            fireTransform.SetParent(part.transform, true);
            fireTransform.SetPositionAndRotation(partCenter, part.transform.rotation);
            SetWorldScale(fireTransform, Vector3.one);
            Destroy(fire, Mathf.Max(0.1f, partFireLifetime));
        }

        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            if (target == null || target.parent == null)
            {
                if (target != null)
                {
                    target.localScale = worldScale;
                }

                return;
            }

            Vector3 parentLossyScale = target.parent.lossyScale;
            target.localScale = new Vector3(
                worldScale.x * GetSafeInverseScale(parentLossyScale.x),
                worldScale.y * GetSafeInverseScale(parentLossyScale.y),
                worldScale.z * GetSafeInverseScale(parentLossyScale.z));
        }

        private static float GetSafeInverseScale(float scale)
        {
            return Mathf.Abs(scale) > 0.0001f ? 1f / scale : 1f;
        }

        private static Vector3 GetDetachedPartCenter(
            GameObject part,
            IList<GameObject> detachedParts,
            IList<Vector3> detachedPartCenters)
        {
            if (detachedParts != null && detachedPartCenters != null)
            {
                int count = Mathf.Min(detachedParts.Count, detachedPartCenters.Count);
                for (int i = 0; i < count; i++)
                {
                    if (detachedParts[i] == part)
                    {
                        return detachedPartCenters[i];
                    }
                }
            }

            return part != null ? part.transform.position : Vector3.zero;
        }

        private static PooledImpactFx SelectRandomPrefab(PooledImpactFx[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, prefabs.Length);
            for (int i = 0; i < prefabs.Length; i++)
            {
                PooledImpactFx prefab = prefabs[(startIndex + i) % prefabs.Length];
                if (prefab != null)
                {
                    return prefab;
                }
            }

            return null;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                   && !float.IsNaN(value.y)
                   && !float.IsNaN(value.z)
                   && !float.IsInfinity(value.x)
                   && !float.IsInfinity(value.y)
                   && !float.IsInfinity(value.z);
        }
    }
}
