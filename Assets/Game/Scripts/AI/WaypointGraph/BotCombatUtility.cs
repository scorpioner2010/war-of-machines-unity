using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal static class BotCombatUtility
    {
        public static Vector3 GetMovePosition(VehicleRoot root)
        {
            Transform moveTransform = GetMoveTransform(root);
            return moveTransform != null ? moveTransform.position : Vector3.zero;
        }

        public static Transform GetMoveTransform(VehicleRoot root)
        {
            if (root != null && root.objectMover != null)
            {
                return root.objectMover.transform;
            }

            return root != null ? root.transform : null;
        }

        public static Vector3 GetAimOrigin(VehicleRoot root)
        {
            if (root != null && root.shooterNet != null && root.shooterNet.muzzleTransform != null)
            {
                return root.shooterNet.muzzleTransform.position;
            }

            if (root != null && root.weaponAimAtCamera != null && root.weaponAimAtCamera.gun != null)
            {
                return root.weaponAimAtCamera.gun.position;
            }

            return root != null ? root.transform.position : Vector3.zero;
        }

        public static float GetShellSpeed(VehicleRoot root)
        {
            if (root != null && root.shooterNet != null)
            {
                return Mathf.Max(0f, root.shooterNet.projectileSpeed);
            }

            if (root != null && root.HasRuntimeStats)
            {
                return VehicleRuntimeStats.ResolveShellSpeed(root.RuntimeStats.ShellSpeed);
            }

            return VehicleRuntimeStats.DefaultShellSpeed;
        }

        public static Vector3 BuildAimOffset(BotCombatSettings settings)
        {
            float radius = Mathf.Max(0f, settings.randomAimRadius);
            if (radius <= 0f)
            {
                return Vector3.zero;
            }

            return new Vector3(
                Random.Range(-radius, radius),
                Random.Range(-radius * 0.5f, radius * 0.5f),
                Random.Range(-radius, radius));
        }

        public static bool IsUnderRoot(Transform transform, Transform root)
        {
            if (transform == null || root == null)
            {
                return false;
            }

            Transform current = transform;
            while (current != null)
            {
                if (current == root)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        public static bool IsFinite(Vector3 value)
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
