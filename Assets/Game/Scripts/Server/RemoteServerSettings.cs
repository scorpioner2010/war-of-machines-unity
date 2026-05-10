using Game.Scripts.Gameplay.Robots;

namespace Game.Scripts.Server
{
    public static class RemoteServerSettings
    {
        public static int MaxPlayersForFindRoom { get; private set; } = 1;
        public static int FindRoomSeconds { get; private set; } = 60;
        public static GunDispersionGlobalSettings GunDispersion { get; } = new GunDispersionGlobalSettings();
        public static bool IsLoaded { get; private set; }

        public static void Apply(
            int maxPlayersForFindRoom,
            int findRoomSeconds,
            bool gunDispersionEnabled,
            float gunDispersionExpandTime,
            float gunDispersionReferenceHullTraverse,
            float gunDispersionReferenceTurretTraverse,
            float gunDispersionReferenceGunTraverse,
            float gunDispersionReferenceCameraAim,
            float gunDispersionUiMinDiameter,
            float gunDispersionUiMaxDiameter,
            float gunDispersionUiPixelsPerDegree,
            float gunDispersionServerSyncInterval,
            float gunDispersionServerSyncDeadZoneDeg)
        {
            MaxPlayersForFindRoom = maxPlayersForFindRoom > 0 ? maxPlayersForFindRoom : 1;
            FindRoomSeconds = findRoomSeconds > 0 ? findRoomSeconds : 60;

            GunDispersion.enabled = gunDispersionEnabled;
            GunDispersion.expandTime = gunDispersionExpandTime > 0f ? gunDispersionExpandTime : GunDispersionGlobalSettings.Default.expandTime;
            GunDispersion.referenceHullTraverseDegPerSec = gunDispersionReferenceHullTraverse > 0f ? gunDispersionReferenceHullTraverse : GunDispersionGlobalSettings.Default.referenceHullTraverseDegPerSec;
            GunDispersion.referenceTurretTraverseDegPerSec = gunDispersionReferenceTurretTraverse > 0f ? gunDispersionReferenceTurretTraverse : GunDispersionGlobalSettings.Default.referenceTurretTraverseDegPerSec;
            GunDispersion.referenceGunTraverseDegPerSec = gunDispersionReferenceGunTraverse > 0f ? gunDispersionReferenceGunTraverse : GunDispersionGlobalSettings.Default.referenceGunTraverseDegPerSec;
            GunDispersion.referenceCameraAimDegPerSec = gunDispersionReferenceCameraAim > 0f ? gunDispersionReferenceCameraAim : GunDispersionGlobalSettings.Default.referenceCameraAimDegPerSec;
            GunDispersion.uiMinDiameter = gunDispersionUiMinDiameter > 0f ? gunDispersionUiMinDiameter : GunDispersionGlobalSettings.Default.uiMinDiameter;
            GunDispersion.uiMaxDiameter = gunDispersionUiMaxDiameter > GunDispersion.uiMinDiameter ? gunDispersionUiMaxDiameter : GunDispersionGlobalSettings.Default.uiMaxDiameter;
            GunDispersion.uiPixelsPerDegree = gunDispersionUiPixelsPerDegree > 0f ? gunDispersionUiPixelsPerDegree : GunDispersionGlobalSettings.Default.uiPixelsPerDegree;
            GunDispersion.serverSyncInterval = gunDispersionServerSyncInterval > 0f ? gunDispersionServerSyncInterval : GunDispersionGlobalSettings.Default.serverSyncInterval;
            GunDispersion.serverSyncDeadZoneDeg = gunDispersionServerSyncDeadZoneDeg >= 0f ? gunDispersionServerSyncDeadZoneDeg : GunDispersionGlobalSettings.Default.serverSyncDeadZoneDeg;

            IsLoaded = true;
        }
    }
}
