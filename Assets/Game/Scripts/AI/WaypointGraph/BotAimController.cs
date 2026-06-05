using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Server;
using UnityEngine;

namespace Game.Scripts.AI.WaypointGraph
{
    internal sealed class BotAimController
    {
        public VehicleAimInputResult SolveAim(VehicleRoot vehicleRoot, Vector3 aimPoint, Vector3 aimForward)
        {
            if (vehicleRoot == null || vehicleRoot.robotHullRotation == null || vehicleRoot.weaponAimAtCamera == null)
            {
                return default;
            }

            return VehicleAimInputSolver.SolveForAimPoint(
                vehicleRoot,
                aimPoint,
                aimForward,
                vehicleRoot.robotHullRotation.CurrentLocalYaw,
                vehicleRoot.weaponAimAtCamera.CurrentLocalPitch);
        }

        public Vector3 ResolveAimForward(VehicleRoot vehicleRoot, Vector3 aimPoint)
        {
            Vector3 origin = BotCombatUtility.GetAimOrigin(vehicleRoot);
            Vector3 forward = aimPoint - origin;
            if (!BotCombatUtility.IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                forward = vehicleRoot != null ? vehicleRoot.transform.forward : Vector3.forward;
            }

            if (!BotCombatUtility.IsFinite(forward) || forward.sqrMagnitude <= 0.000001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        public bool IsAimAligned(
            VehicleRoot vehicleRoot,
            VehicleAimInputResult aimResult,
            Vector3 aimPoint,
            BotCombatSettings settings)
        {
            if (vehicleRoot == null || vehicleRoot.robotHullRotation == null || vehicleRoot.weaponAimAtCamera == null)
            {
                return false;
            }

            VehicleTurretRotationController turret = vehicleRoot.robotHullRotation;
            WeaponAimController weaponAim = vehicleRoot.weaponAimAtCamera;

            float yawError = Mathf.Abs(Mathf.DeltaAngle(turret.CurrentLocalYaw, aimResult.YawDeg));
            if (yawError > settings.maxAimYawErrorDeg)
            {
                return false;
            }

            float pitchError = Mathf.Abs(Mathf.DeltaAngle(weaponAim.CurrentLocalPitch, aimResult.PitchDeg));
            if (pitchError > settings.maxAimPitchErrorDeg)
            {
                return false;
            }

            return GetMuzzleAimErrorDeg(vehicleRoot, aimPoint) <= settings.maxMuzzleAimErrorDeg;
        }

        public float EstimateAimErrorDeg(VehicleRoot vehicleRoot, VehicleAimInputResult aimResult, Vector3 aimPoint)
        {
            if (vehicleRoot == null || vehicleRoot.robotHullRotation == null || vehicleRoot.weaponAimAtCamera == null)
            {
                return float.PositiveInfinity;
            }

            float yawError = Mathf.Abs(Mathf.DeltaAngle(vehicleRoot.robotHullRotation.CurrentLocalYaw, aimResult.YawDeg));
            float pitchError = Mathf.Abs(Mathf.DeltaAngle(vehicleRoot.weaponAimAtCamera.CurrentLocalPitch, aimResult.PitchDeg));
            float muzzleError = GetMuzzleAimErrorDeg(vehicleRoot, aimPoint);
            return yawError + pitchError + muzzleError;
        }

        private static float GetMuzzleAimErrorDeg(VehicleRoot vehicleRoot, Vector3 aimPoint)
        {
            if (vehicleRoot == null || vehicleRoot.weaponAimAtCamera == null)
            {
                return float.PositiveInfinity;
            }

            Vector3 origin = BotCombatUtility.GetAimOrigin(vehicleRoot);
            Vector3 desiredDirection = aimPoint - origin;
            if (!BotCombatUtility.IsFinite(desiredDirection) || desiredDirection.sqrMagnitude <= 0.000001f)
            {
                return float.PositiveInfinity;
            }

            desiredDirection.Normalize();
            Vector3 muzzleForward = vehicleRoot.weaponAimAtCamera.GetLogicalAimForwardWorld();
            if (!BotCombatUtility.IsFinite(muzzleForward) || muzzleForward.sqrMagnitude <= 0.000001f)
            {
                return float.PositiveInfinity;
            }

            muzzleForward.Normalize();
            return Vector3.Angle(muzzleForward, desiredDirection);
        }
    }
}
