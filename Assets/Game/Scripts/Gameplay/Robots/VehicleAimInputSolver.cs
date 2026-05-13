using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    public struct VehicleAimInputResult
    {
        public bool HasState;
        public float YawDeg;
        public float PitchDeg;
        public Vector3 CameraAimPoint;
        public Vector3 CameraAimForward;
    }

    public static class VehicleAimInputSolver
    {
        public static VehicleAimInputResult Solve(
            VehicleRoot vehicleRoot,
            Transform cameraTransform,
            float currentTurretYawLocal,
            float currentGunPitchLocal)
        {
            VehicleAimInputResult result = new VehicleAimInputResult();

            if (vehicleRoot == null
                || vehicleRoot.objectMover == null
                || vehicleRoot.robotHullRotation == null
                || vehicleRoot.weaponAimAtCamera == null)
            {
                return result;
            }

            WeaponAimController weaponAim = vehicleRoot.weaponAimAtCamera;
            Transform chassis = vehicleRoot.objectMover.transform;
            Vector3 chassisFwd = chassis.forward;
            chassisFwd.y = 0f;

            if (cameraTransform == null)
            {
                result.HasState = true;
                result.CameraAimPoint = weaponAim.CurrentAimPoint;
                return result;
            }

            weaponAim.ResolveCameraAim(cameraTransform, out Vector3 cameraAimPoint, out Vector3 cameraAimForward);

            float turretYawLocal = currentTurretYawLocal;
            Vector3 targetYawFlat = cameraAimPoint - vehicleRoot.robotHullRotation.transform.position;
            targetYawFlat.y = 0f;
            if (targetYawFlat.sqrMagnitude <= 1e-6f)
            {
                targetYawFlat = cameraAimForward;
                targetYawFlat.y = 0f;
            }

            if (chassisFwd.sqrMagnitude > 1e-6f && targetYawFlat.sqrMagnitude > 1e-6f)
            {
                chassisFwd.Normalize();
                targetYawFlat.Normalize();
                turretYawLocal = Vector3.SignedAngle(chassisFwd, targetYawFlat, Vector3.up);

                float maxLocalYaw = vehicleRoot.robotHullRotation.maxLocalYaw;
                if (maxLocalYaw > 0f)
                {
                    float half = Mathf.Abs(maxLocalYaw);
                    turretYawLocal = Mathf.Clamp(turretYawLocal, -half, half);
                }
            }

            float gunPitchLocal = ComputeTargetGunPitch(
                vehicleRoot,
                cameraAimPoint,
                cameraAimForward,
                turretYawLocal,
                currentGunPitchLocal);

            result.HasState = true;
            result.YawDeg = turretYawLocal;
            result.PitchDeg = gunPitchLocal;
            result.CameraAimPoint = cameraAimPoint;
            result.CameraAimForward = cameraAimForward;
            return result;
        }

        private static float ComputeTargetGunPitch(
            VehicleRoot vehicleRoot,
            Vector3 cameraAimPoint,
            Vector3 cameraAimForward,
            float targetYawDeg,
            float currentGunPitchLocal)
        {
            WeaponAimController weaponAim = vehicleRoot.weaponAimAtCamera;
            Transform gun = weaponAim.gun;
            if (gun == null)
            {
                return 0f;
            }

            Transform turret = vehicleRoot.robotHullRotation.transform;
            Transform chassis = vehicleRoot.objectMover.transform;
            Quaternion targetTurretRotation = chassis.rotation * Quaternion.Euler(0f, targetYawDeg, 0f);
            Quaternion turretDelta = targetTurretRotation * Quaternion.Inverse(turret.rotation);

            Vector3 targetGunPosition = turret.position + turretDelta * (gun.position - turret.position);
            Quaternion targetParentRotation = gun.parent != null
                ? turretDelta * gun.parent.rotation
                : targetTurretRotation;

            Quaternion baseGunRotation = targetParentRotation * weaponAim.InitialLocalRotation;
            Vector3 pitchAxisWorld = baseGunRotation * WeaponAimController.AxisToVector(weaponAim.localPitchAxis);
            Vector3 forwardWorld = baseGunRotation * WeaponAimController.AxisToVector(weaponAim.localForwardAxis);

            if (pitchAxisWorld.sqrMagnitude <= 1e-6f || forwardWorld.sqrMagnitude <= 1e-6f)
            {
                return currentGunPitchLocal;
            }

            pitchAxisWorld.Normalize();
            forwardWorld.Normalize();

            Vector3 targetDirection = cameraAimPoint - targetGunPosition;
            if (targetDirection.sqrMagnitude <= 1e-6f)
            {
                targetDirection = cameraAimForward;
            }
            if (targetDirection.sqrMagnitude <= 1e-6f)
            {
                return currentGunPitchLocal;
            }

            targetDirection.Normalize();

            Vector3 forwardProjected = Vector3.ProjectOnPlane(forwardWorld, pitchAxisWorld);
            Vector3 targetProjected = Vector3.ProjectOnPlane(targetDirection, pitchAxisWorld);
            if (forwardProjected.sqrMagnitude <= 1e-6f || targetProjected.sqrMagnitude <= 1e-6f)
            {
                return currentGunPitchLocal;
            }

            forwardProjected.Normalize();
            targetProjected.Normalize();

            float pitch = Vector3.SignedAngle(forwardProjected, targetProjected, pitchAxisWorld) + weaponAim.pitchOffset;
            return Mathf.Clamp(pitch, weaponAim.minPitch, weaponAim.maxPitch);
        }
    }
}
