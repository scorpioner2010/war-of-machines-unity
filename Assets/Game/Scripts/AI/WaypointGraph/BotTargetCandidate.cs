using Game.Scripts.Gameplay.Robots;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal struct BotTargetCandidate
    {
        public VehicleRoot Root;
        public Vector3 MapPosition;
        public Vector3 AimPoint;
        public bool HasLineOfFire;
        public bool HasAimSolution;
        public bool IsCurrentTarget;
        public bool IsDirectlySpotted;
        public float AimErrorDeg;
        public float DistanceSqr;
    }
}
