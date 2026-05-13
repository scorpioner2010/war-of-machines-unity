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
        public float LifeTime;
        public bool UseArc;
        public float ArcScale;
        public float ArcMin;
        public float ArcMax;
        public float ArcExponent;
        public AnimationCurve ArcCurve;
        public bool ArcAlongWorldUp;
        public bool UseSlowdown;
        public float SlowdownAmount;
        public float SlowdownExponent;
        public AnimationCurve SlowdownCurve;
        public float MinSpeedMultiplier;
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
            projectile.Init(
                targetPoint: spawnParams.AimPoint,
                initialSpeed: spawnParams.InitialSpeed,
                lifeTime: spawnParams.LifeTime,
                useArc: spawnParams.UseArc,
                arcScale: spawnParams.ArcScale,
                arcMin: spawnParams.ArcMin,
                arcMax: spawnParams.ArcMax,
                arcExponent: spawnParams.ArcExponent,
                arcCurve: spawnParams.ArcCurve,
                arcAlongWorldUp: spawnParams.ArcAlongWorldUp,
                useSlowdown: spawnParams.UseSlowdown,
                slowdownAmount: spawnParams.SlowdownAmount,
                slowdownExponent: spawnParams.SlowdownExponent,
                slowdownCurve: spawnParams.SlowdownCurve,
                minSpeedMultiplier: spawnParams.MinSpeedMultiplier,
                passedTime: spawnParams.PassedTime,
                authoritative: spawnParams.Authoritative
            );

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
