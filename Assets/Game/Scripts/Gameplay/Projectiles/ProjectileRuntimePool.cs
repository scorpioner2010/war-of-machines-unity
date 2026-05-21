using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

public static class ProjectileRuntimePool
{
    private const int DefaultProjectileMaxInactive = 64;
    private const int DefaultImpactFxMaxInactive = 64;
    private const float DefaultImpactFxFallbackLifetime = 2f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static readonly ProfilerMarker RentProjectileMarker = new ProfilerMarker("ProjectilePool.Rent");
    private static readonly ProfilerMarker ReleaseProjectileMarker = new ProfilerMarker("ProjectilePool.Release");
    private static readonly ProfilerMarker SpawnImpactFxMarker = new ProfilerMarker("ProjectilePool.SpawnImpactFx");
#endif

    private static readonly Dictionary<int, ProjectilePool> ProjectilePools = new Dictionary<int, ProjectilePool>(16);
    private static readonly Dictionary<int, ProjectilePool> ProjectileOwners = new Dictionary<int, ProjectilePool>(128);
    private static readonly Dictionary<int, ImpactFxPool> ImpactFxPools = new Dictionary<int, ImpactFxPool>(16);
    private static readonly Dictionary<int, ImpactFxPool> ImpactFxOwners = new Dictionary<int, ImpactFxPool>(128);

    public static int ProjectileOverflowInstantiates { get; private set; }
    public static int ImpactFxOverflowInstantiates { get; private set; }

    public static void ConfigureProjectilePool(Projectile prefab, int maxInactive)
    {
        if (prefab == null)
        {
            return;
        }

        ProjectilePool pool = GetProjectilePool(prefab);
        pool.MaxInactive = Mathf.Max(1, maxInactive);
    }

    public static void ConfigureImpactFxPool(GameObject prefab, int maxInactive)
    {
        if (prefab == null)
        {
            return;
        }

        ImpactFxPool pool = GetImpactFxPool(prefab);
        pool.MaxInactive = Mathf.Max(1, maxInactive);
    }

    public static void PrewarmProjectile(Projectile prefab, int count, int maxInactive)
    {
        if (prefab == null || count <= 0)
        {
            return;
        }

        ProjectilePool pool = GetProjectilePool(prefab);
        pool.MaxInactive = Mathf.Max(1, maxInactive);
        int targetInactive = Mathf.Min(count, pool.MaxInactive);
        while (pool.Inactive.Count < targetInactive)
        {
            Projectile projectile = Object.Instantiate(prefab, pool.Root);
            RegisterProjectileOwner(projectile, pool);
            projectile.PrepareForPoolRelease();
            pool.Inactive.Push(projectile);
        }
    }

    public static void PrewarmImpactFx(GameObject prefab, int count, int maxInactive)
    {
        if (prefab == null || count <= 0)
        {
            return;
        }

        ImpactFxPool pool = GetImpactFxPool(prefab);
        pool.MaxInactive = Mathf.Max(1, maxInactive);
        int targetInactive = Mathf.Min(count, pool.MaxInactive);
        while (pool.Inactive.Count < targetInactive)
        {
            PooledImpactFx fx = CreateImpactFxInstance(pool);
            fx.PrepareForPoolRelease();
            pool.Inactive.Push(fx);
        }
    }

    public static Projectile RentProjectile(Projectile prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (RentProjectileMarker.Auto())
#endif
        {
            ProjectilePool pool = GetProjectilePool(prefab);
            Projectile projectile;
            if (pool.Inactive.Count > 0)
            {
                projectile = pool.Inactive.Pop();
            }
            else
            {
                projectile = Object.Instantiate(prefab);
                ProjectileOverflowInstantiates++;
                RegisterProjectileOwner(projectile, pool);
            }

            Transform projectileTransform = projectile.transform;
            projectileTransform.SetParent(null, false);
            projectileTransform.SetPositionAndRotation(position, rotation);

            GameObject projectileObject = projectile.gameObject;
            if (!projectileObject.activeSelf)
            {
                projectileObject.SetActive(true);
            }

            return projectile;
        }
    }

    public static bool Release(Projectile projectile)
    {
        if (projectile == null)
        {
            return false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (ReleaseProjectileMarker.Auto())
#endif
        {
            int instanceId = projectile.GetInstanceID();
            if (!ProjectileOwners.TryGetValue(instanceId, out ProjectilePool pool))
            {
                return false;
            }

            if (pool.Inactive.Count >= pool.MaxInactive)
            {
                ProjectileOwners.Remove(instanceId);
                Object.Destroy(projectile.gameObject);
                return true;
            }

            projectile.PrepareForPoolRelease();
            projectile.transform.SetParent(pool.Root, false);
            pool.Inactive.Push(projectile);
            return true;
        }
    }

    public static void SpawnImpactFx(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (SpawnImpactFxMarker.Auto())
#endif
        {
            ImpactFxPool pool = GetImpactFxPool(prefab);
            PooledImpactFx fx;
            if (pool.Inactive.Count > 0)
            {
                fx = pool.Inactive.Pop();
            }
            else
            {
                fx = CreateImpactFxInstance(pool);
                ImpactFxOverflowInstantiates++;
            }

            Transform fxTransform = fx.transform;
            fxTransform.SetParent(null, false);
            fxTransform.SetPositionAndRotation(position, rotation);
            fx.Play();
        }
    }

    internal static bool ReleaseImpactFx(PooledImpactFx fx)
    {
        if (fx == null)
        {
            return false;
        }

        int instanceId = fx.GetInstanceID();
        if (!ImpactFxOwners.TryGetValue(instanceId, out ImpactFxPool pool))
        {
            return false;
        }

        if (pool.Inactive.Count >= pool.MaxInactive)
        {
            ImpactFxOwners.Remove(instanceId);
            Object.Destroy(fx.gameObject);
            return true;
        }

        fx.PrepareForPoolRelease();
        fx.transform.SetParent(pool.Root, false);
        pool.Inactive.Push(fx);
        return true;
    }

    private static ProjectilePool GetProjectilePool(Projectile prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (ProjectilePools.TryGetValue(prefabId, out ProjectilePool pool))
        {
            return pool;
        }

        pool = new ProjectilePool(prefab, DefaultProjectileMaxInactive);
        ProjectilePools.Add(prefabId, pool);
        return pool;
    }

    private static ImpactFxPool GetImpactFxPool(GameObject prefab)
    {
        int prefabId = prefab.GetInstanceID();
        if (ImpactFxPools.TryGetValue(prefabId, out ImpactFxPool pool))
        {
            return pool;
        }

        pool = new ImpactFxPool(prefab, DefaultImpactFxMaxInactive);
        ImpactFxPools.Add(prefabId, pool);
        return pool;
    }

    private static void RegisterProjectileOwner(Projectile projectile, ProjectilePool pool)
    {
        if (projectile == null || pool == null)
        {
            return;
        }

        ProjectileOwners[projectile.GetInstanceID()] = pool;
    }

    private static PooledImpactFx CreateImpactFxInstance(ImpactFxPool pool)
    {
        GameObject instance = Object.Instantiate(pool.Prefab, pool.Root);
        PooledImpactFx fx = instance.GetComponent<PooledImpactFx>();
        if (fx == null)
        {
            fx = instance.AddComponent<PooledImpactFx>();
        }

        fx.Initialize(DefaultImpactFxFallbackLifetime);
        ImpactFxOwners[fx.GetInstanceID()] = pool;
        return fx;
    }

    private sealed class ProjectilePool
    {
        public readonly Projectile Prefab;
        public readonly Stack<Projectile> Inactive;
        public readonly Transform Root;
        public int MaxInactive;

        public ProjectilePool(Projectile prefab, int maxInactive)
        {
            Prefab = prefab;
            MaxInactive = Mathf.Max(1, maxInactive);
            Inactive = new Stack<Projectile>(MaxInactive);
            Root = CreateRoot("ProjectilePool_" + prefab.name);
        }
    }

    private sealed class ImpactFxPool
    {
        public readonly GameObject Prefab;
        public readonly Stack<PooledImpactFx> Inactive;
        public readonly Transform Root;
        public int MaxInactive;

        public ImpactFxPool(GameObject prefab, int maxInactive)
        {
            Prefab = prefab;
            MaxInactive = Mathf.Max(1, maxInactive);
            Inactive = new Stack<PooledImpactFx>(MaxInactive);
            Root = CreateRoot("ImpactFxPool_" + prefab.name);
        }
    }

    private static Transform CreateRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(root);
        return root.transform;
    }
}

public sealed class PooledImpactFx : MonoBehaviour
{
    private ParticleSystem[] _particles;
    private float _fallbackLifetime;
    private float _releaseTime;
    private bool _playing;

    public void Initialize(float fallbackLifetime)
    {
        _fallbackLifetime = Mathf.Max(0.1f, fallbackLifetime);
        _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Play()
    {
        if (_particles == null)
        {
            Initialize(_fallbackLifetime);
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        float lifetime = _fallbackLifetime;
        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem particle = _particles[i];
            if (particle == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particle.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
            particle.Clear(true);
            particle.Play(true);
        }

        _releaseTime = Time.unscaledTime + lifetime + 0.05f;
        _playing = true;
    }

    public void PrepareForPoolRelease()
    {
        _playing = false;

        if (_particles != null)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystem particle = _particles[i];
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!_playing || Time.unscaledTime < _releaseTime)
        {
            return;
        }

        if (!ProjectileRuntimePool.ReleaseImpactFx(this))
        {
            Destroy(gameObject);
        }
    }
}
