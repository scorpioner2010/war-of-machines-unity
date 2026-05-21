using System;
using Game.Scripts.Client;
using Game.Scripts.Diagnostics;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Unity.Profiling;
#endif

namespace Game.Scripts.Gameplay.Robots
{
    public struct ProjectileVisualSpawnParams
    {
        public Projectile ProjectilePrefab;
        public LayerMask HitMask;
        public int Damage;
        public Vector3 StartPosition;
        public Vector3 AimPoint;
        public float InitialSpeed;
        public Vector3 Gravity;
        public float LifeTime;
        public float CollisionRadius;
        public bool UseBallisticCompensation;
        public bool PreferHighArc;
        public bool DebugBallisticTrajectory;
        public float PassedTime;
        public bool Authoritative;
        public bool ExplodeOnArrival;
        public Vector3 ImpactNormal;
        public bool Visible;
        public Action OnAuthoritativeImpact;
        public bool ConfigureResolvedTarget;
        public float MaxShotDistance;
        public ClientProjectileVisualSettings VisualSettings;
    }

    public static class ProjectileVisualSpawner
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly ProfilerMarker SpawnMarker = new ProfilerMarker("ProjectileVisualSpawner.Spawn");
        private static readonly ProfilerMarker ImpactFxMarker = new ProfilerMarker("ProjectileVisualSpawner.ImpactFx");
#endif

        public static Projectile Spawn(ProjectileVisualSpawnParams spawnParams)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (SpawnMarker.Auto())
#endif
            using (ProfileScope.Measure(spawnParams.Authoritative ? "Server.Projectile.Spawn" : "Client.Projectile.Spawn", DiagnosticsCategories.Physics))
            {
                Projectile projectile = ProjectileRuntimePool.RentProjectile(
                    spawnParams.ProjectilePrefab,
                    spawnParams.StartPosition,
                    Quaternion.identity);
                if (projectile == null)
                {
                    return null;
                }

                projectile.hitMask = spawnParams.HitMask;
                projectile.damage = spawnParams.Damage;
                if (spawnParams.Visible && !spawnParams.Authoritative)
                {
                    projectile.ApplyClientVisualSettings(spawnParams.VisualSettings);
                }

                Vector3 initialVelocity = BuildInitialVelocity(
                    spawnParams,
                    out bool usedBallisticCompensation,
                    out bool ballisticSolutionFound);

                projectile.Init(
                    origin: spawnParams.StartPosition,
                    initialVelocity: initialVelocity,
                    gravity: spawnParams.Gravity,
                    maxLifetime: spawnParams.LifeTime,
                    maxDistance: spawnParams.MaxShotDistance,
                    collisionRadius: spawnParams.CollisionRadius,
                    passedTime: spawnParams.PassedTime,
                    authoritative: spawnParams.Authoritative
                );
                projectile.ConfigureBallisticDebug(
                    spawnParams.AimPoint,
                    initialVelocity,
                    spawnParams.Gravity,
                    BallisticProjectileMath.EstimateDirectDrop(
                        spawnParams.StartPosition,
                        spawnParams.AimPoint,
                        spawnParams.InitialSpeed,
                        spawnParams.Gravity),
                    usedBallisticCompensation,
                    ballisticSolutionFound,
                    spawnParams.DebugBallisticTrajectory);

                if (spawnParams.DebugBallisticTrajectory)
                {
                    string mode = usedBallisticCompensation
                        ? (spawnParams.PreferHighArc ? "ballistic compensated high arc" : "ballistic compensated low arc")
                        : "direct";
                    Debug.Log(
                        $"Projectile ballistic debug: mode={mode}, gravity={spawnParams.Gravity.magnitude:0.###}, speed={spawnParams.InitialSpeed:0.###}, estimatedDirectDrop={BallisticProjectileMath.EstimateDirectDrop(spawnParams.StartPosition, spawnParams.AimPoint, spawnParams.InitialSpeed, spawnParams.Gravity):0.###}, solutionFound={ballisticSolutionFound}");
                }

                if (spawnParams.ConfigureResolvedTarget)
                {
                    if (spawnParams.ExplodeOnArrival)
                    {
                        projectile.ConfigureResolvedImpact(
                            spawnParams.AimPoint,
                            spawnParams.ImpactNormal,
                            spawnParams.OnAuthoritativeImpact);
                    }
                    else
                    {
                        projectile.ConfigureResolvedMiss(
                            spawnParams.AimPoint,
                            spawnParams.MaxShotDistance,
                            spawnParams.OnAuthoritativeImpact);
                    }
                }

                projectile.SetVisualsEnabled(spawnParams.Visible);

                return projectile;
            }
        }

        public static Vector3 BuildInitialVelocity(
            ProjectileVisualSpawnParams spawnParams,
            out bool usedBallisticCompensation,
            out bool ballisticSolutionFound)
        {
            usedBallisticCompensation = false;
            ballisticSolutionFound = false;

            if (spawnParams.UseBallisticCompensation && spawnParams.Gravity.sqrMagnitude > 0.000001f)
            {
                ballisticSolutionFound = BallisticProjectileMath.TryBuildBallisticInitialVelocity(
                    spawnParams.StartPosition,
                    spawnParams.AimPoint,
                    spawnParams.InitialSpeed,
                    spawnParams.Gravity,
                    spawnParams.PreferHighArc,
                    out Vector3 ballisticVelocity);

                if (ballisticSolutionFound)
                {
                    usedBallisticCompensation = true;
                    return ballisticVelocity;
                }

                if (spawnParams.DebugBallisticTrajectory)
                {
                    Debug.LogWarning("Projectile ballistic solution was not reachable. Falling back to direct initial velocity.");
                }
            }

            return BallisticProjectileMath.BuildDirectInitialVelocity(
                spawnParams.StartPosition,
                spawnParams.AimPoint,
                spawnParams.InitialSpeed,
                Quaternion.identity);
        }

        public static void SpawnImpactFx(Projectile projectilePrefab, Vector3 impactPoint, Vector3 impactNormal)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (ImpactFxMarker.Auto())
#endif
            using (ProfileScope.Measure("Projectile.ImpactFx.Spawn", DiagnosticsCategories.Physics))
            {
                if (projectilePrefab == null || projectilePrefab.explosionFX == null)
                {
                    return;
                }

                Vector3 normal = impactNormal.sqrMagnitude > 0.000001f ? impactNormal : Vector3.up;
                ProjectileRuntimePool.SpawnImpactFx(projectilePrefab.explosionFX, impactPoint, Quaternion.LookRotation(normal));
            }
        }
    }
}
