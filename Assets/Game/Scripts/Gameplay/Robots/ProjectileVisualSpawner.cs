using System;
using UnityEngine;

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
    }

    public static class ProjectileVisualSpawner
    {
        public static Projectile Spawn(ProjectileVisualSpawnParams spawnParams)
        {
            Projectile projectile = UnityEngine.Object.Instantiate(
                spawnParams.ProjectilePrefab,
                spawnParams.StartPosition,
                Quaternion.identity);

            projectile.hitMask = spawnParams.HitMask;
            projectile.damage = spawnParams.Damage;

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

            if (!spawnParams.Visible)
            {
                projectile.SetVisualsEnabled(false);
            }

            return projectile;
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
            if (projectilePrefab == null || projectilePrefab.explosionFX == null)
            {
                return;
            }

            Vector3 normal = impactNormal.sqrMagnitude > 0.000001f ? impactNormal : Vector3.up;
            UnityEngine.Object.Instantiate(projectilePrefab.explosionFX, impactPoint, Quaternion.LookRotation(normal));
        }
    }
}
