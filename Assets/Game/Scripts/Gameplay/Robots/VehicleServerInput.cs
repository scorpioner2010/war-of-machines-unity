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

        public static VehicleServerInput Movement(Vector2 move)
        {
            return new VehicleServerInput
            {
                Move = move,
                Shoot = false,
                Action = false
            };
        }

        public static VehicleServerInput None()
        {
            return new VehicleServerInput
            {
                Move = Vector2.zero,
                Shoot = false,
                Action = false
            };
        }
    }
}
