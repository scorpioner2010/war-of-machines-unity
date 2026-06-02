using System;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [Serializable]
    public struct VehicleServerInput
    {
        public Vector2 Move;
        public bool Shoot;
        public bool Action;
        public bool HasAim;
        public float TargetYawDeg;
        public float TargetPitchDeg;
        public Vector3 AimPoint;
        public Vector3 AimForward;

        public static VehicleServerInput Movement(Vector2 move)
        {
            return new VehicleServerInput
            {
                Move = move,
                Shoot = false,
                Action = false,
                HasAim = false
            };
        }

        public static VehicleServerInput Combat(
            Vector2 move,
            bool shoot,
            bool action,
            float targetYawDeg,
            float targetPitchDeg,
            Vector3 aimPoint,
            Vector3 aimForward)
        {
            return new VehicleServerInput
            {
                Move = move,
                Shoot = shoot,
                Action = action,
                HasAim = true,
                TargetYawDeg = targetYawDeg,
                TargetPitchDeg = targetPitchDeg,
                AimPoint = aimPoint,
                AimForward = aimForward
            };
        }

        public static VehicleServerInput None()
        {
            return new VehicleServerInput
            {
                Move = Vector2.zero,
                Shoot = false,
                Action = false,
                HasAim = false
            };
        }
    }
}
