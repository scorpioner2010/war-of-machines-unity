using Game.Scripts.Gameplay.Robots;

namespace Game.Scripts.Audio
{
    public static class AudioEventSelector
    {
        public static string GetShotEvent(VehicleRoot vehicleRoot)
        {
            return IsHeavyRobot(vehicleRoot)
                ? AudioEventNames.HeavyShot
                : AudioEventNames.StandardShot;
        }

        public static string GetWalkerStepEvent(VehicleRoot vehicleRoot)
        {
            return IsHeavyRobot(vehicleRoot)
                ? AudioEventNames.HeavyWalkerStep
                : AudioEventNames.WalkerStep;
        }

        public static string GetWalkerTurnEvent(VehicleRoot vehicleRoot)
        {
            return IsHeavyRobot(vehicleRoot)
                ? AudioEventNames.HeavyWalkerTurn
                : AudioEventNames.WalkerTurn;
        }

        public static string GetReloadReadyEvent(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot != null && vehicleRoot.trackedVehicleAnimator != null)
            {
                return AudioEventNames.TrackedReloadReady;
            }

            return AudioEventNames.WalkerReloadReady;
        }

        public static string GetArmorImpactEvent(
            VehicleRoot targetRoot,
            VehicleArmorController.ArmorZone armorZone,
            UnityEngine.Vector3 hitNormal)
        {
            bool isRear = IsRearHit(targetRoot, armorZone, hitNormal);
            if (armorZone == VehicleArmorController.ArmorZone.Turret)
            {
                return AudioEventNames.PenetrationTurret;
            }

            return isRear
                ? AudioEventNames.PenetrationRear
                : AudioEventNames.PenetrationHull;
        }

        public static string GetNoPenetrationEvent(
            VehicleRoot targetRoot,
            VehicleArmorController.ArmorZone armorZone,
            UnityEngine.Vector3 hitNormal)
        {
            bool isRear = IsRearHit(targetRoot, armorZone, hitNormal);
            if (armorZone == VehicleArmorController.ArmorZone.Turret)
            {
                return AudioEventNames.NoPenetrationTurret;
            }

            return isRear
                ? AudioEventNames.NoPenetrationRear
                : AudioEventNames.NoPenetrationHull;
        }

        public static bool IsHeavyRobot(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null || vehicleRoot.RuntimeStats == null)
            {
                return false;
            }

            string code = vehicleRoot.RuntimeStats.Code ?? string.Empty;
            string name = vehicleRoot.RuntimeStats.Name ?? string.Empty;
            string identity = (code + " " + name).ToLowerInvariant();
            return identity.Contains("colossus") || identity.Contains("heavy");
        }

        private static bool IsRearHit(
            VehicleRoot targetRoot,
            VehicleArmorController.ArmorZone armorZone,
            UnityEngine.Vector3 hitNormal)
        {
            UnityEngine.Transform reference = armorZone == VehicleArmorController.ArmorZone.Turret
                ? targetRoot != null && targetRoot.robotHullRotation != null
                    ? targetRoot.robotHullRotation.transform
                    : null
                : targetRoot != null && targetRoot.objectMover != null
                    ? targetRoot.objectMover.transform
                    : null;

            return reference != null
                   && UnityEngine.Vector3.Dot(hitNormal.normalized, reference.forward) < -0.35f;
        }
    }
}
