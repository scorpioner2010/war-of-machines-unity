using System;
using UnityEngine;

namespace Game.Scripts.Gameplay.Robots
{
    [Serializable]
    public class GunDispersionSettings
    {
        [Header("Aiming")]
        public float minDispersionDeg = 0.35f;
        public float maxDispersionDeg = 6f;
        public float aimTime = 2f;

        [Header("Bloom")]
        public float movingDispersionDeg = 1.5f;
        public float hullTraverseDispersionDeg = 1.25f;
        public float turretTraverseDispersionDeg = 1.5f;
        public float gunTraverseDispersionDeg = 1f;
        public float shotDispersionDeg = 2f;

        public float MinDispersion
        {
            get
            {
                return Mathf.Max(0f, minDispersionDeg);
            }
        }

        public float MaxDispersion
        {
            get
            {
                return Mathf.Max(MinDispersion, maxDispersionDeg);
            }
        }
    }

    [Serializable]
    public class GunDispersionGlobalSettings
    {
        private static readonly GunDispersionGlobalSettings DefaultSettings = new GunDispersionGlobalSettings();

        public bool enabled = true;
        public float expandTime = 0.12f;

        [Header("Reference Speeds")]
        public float referenceHullTraverseDegPerSec = 90f;
        public float referenceTurretTraverseDegPerSec = 45f;
        public float referenceGunTraverseDegPerSec = 35f;
        public float referenceCameraAimDegPerSec = 120f;

        [Header("UI")]
        public float uiMinDiameter = 80f;
        public float uiMaxDiameter = 340f;
        public float uiPixelsPerDegree = 42f;

        [Header("Networking")]
        public float serverSyncInterval = 0.05f;
        public float serverSyncDeadZoneDeg = 0.03f;

        public static GunDispersionGlobalSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public float GetUiDiameter(float dispersionDeg, float minDispersionDeg)
        {
            float diameter = uiMinDiameter + Mathf.Max(0f, dispersionDeg - Mathf.Max(0f, minDispersionDeg)) * uiPixelsPerDegree;
            return Mathf.Clamp(diameter, uiMinDiameter, uiMaxDiameter);
        }

        public void CopyFrom(GunDispersionGlobalSettings source)
        {
            if (source == null)
            {
                return;
            }

            enabled = source.enabled;
            expandTime = source.expandTime;
            referenceHullTraverseDegPerSec = source.referenceHullTraverseDegPerSec;
            referenceTurretTraverseDegPerSec = source.referenceTurretTraverseDegPerSec;
            referenceGunTraverseDegPerSec = source.referenceGunTraverseDegPerSec;
            referenceCameraAimDegPerSec = source.referenceCameraAimDegPerSec;
            uiMinDiameter = source.uiMinDiameter;
            uiMaxDiameter = source.uiMaxDiameter;
            uiPixelsPerDegree = source.uiPixelsPerDegree;
            serverSyncInterval = source.serverSyncInterval;
            serverSyncDeadZoneDeg = source.serverSyncDeadZoneDeg;
        }
    }

    public sealed class GunDispersionModel
    {
        private bool _hasLastSample;
        private Quaternion _lastHullRotation;
        private Quaternion _lastTurretRotation;
        private Quaternion _lastGunRotation;
        private Quaternion _lastCameraRotation;
        private float _forcedExpandTargetDeg;

        public float CurrentDeg { get; private set; }

        public void Reset(VehicleRoot root, GunDispersionSettings settings)
        {
            CurrentDeg = settings != null ? settings.MinDispersion : 0f;
            _forcedExpandTargetDeg = 0f;
            Sample(root, includeCamera: true);
        }

        public float Tick(
            VehicleRoot root,
            GunDispersionSettings settings,
            GunDispersionGlobalSettings globalSettings,
            float dt,
            bool includeCameraAimMotion)
        {
            if (root == null || settings == null)
            {
                return CurrentDeg;
            }

            globalSettings ??= GunDispersionGlobalSettings.Default;

            if (!globalSettings.enabled)
            {
                CurrentDeg = settings.MinDispersion;
                Sample(root, includeCameraAimMotion);
                return CurrentDeg;
            }

            if (dt <= 0f)
            {
                return CurrentDeg;
            }

            if (!_hasLastSample)
            {
                Reset(root, settings);
                return CurrentDeg;
            }

            float targetDeg = settings.MinDispersion;
            targetDeg += GetMovementPenalty(root, settings);

            float hullPenalty = GetRotationPenalty(GetHullRotation(root), _lastHullRotation, dt, globalSettings.referenceHullTraverseDegPerSec, settings.hullTraverseDispersionDeg);
            float aimRotationPenalty =
                GetRotationPenalty(GetTurretRotation(root), _lastTurretRotation, dt, globalSettings.referenceTurretTraverseDegPerSec, settings.turretTraverseDispersionDeg) +
                GetRotationPenalty(GetGunRotation(root), _lastGunRotation, dt, globalSettings.referenceGunTraverseDegPerSec, settings.gunTraverseDispersionDeg);

            if (includeCameraAimMotion)
            {
                float cameraPenalty = GetRotationPenalty(GetCameraRotation(), _lastCameraRotation, dt, globalSettings.referenceCameraAimDegPerSec, settings.gunTraverseDispersionDeg);
                aimRotationPenalty = Mathf.Max(aimRotationPenalty, cameraPenalty);
            }

            targetDeg += hullPenalty + aimRotationPenalty;
            if (_forcedExpandTargetDeg > 0f)
            {
                targetDeg = Mathf.Max(targetDeg, _forcedExpandTargetDeg);
            }

            targetDeg = Mathf.Clamp(targetDeg, settings.MinDispersion, settings.MaxDispersion);

            if (targetDeg > CurrentDeg)
            {
                float expandRate = globalSettings.expandTime > 0.001f ? 3f / globalSettings.expandTime : 1000f;
                float t = 1f - Mathf.Exp(-expandRate * dt);
                CurrentDeg = Mathf.Lerp(CurrentDeg, targetDeg, t);
            }
            else
            {
                float settleRate = settings.aimTime > 0.001f ? 3f / settings.aimTime : 1000f;
                float t = 1f - Mathf.Exp(-settleRate * dt);
                CurrentDeg = Mathf.Lerp(CurrentDeg, targetDeg, t);
            }

            CurrentDeg = Mathf.Clamp(CurrentDeg, settings.MinDispersion, settings.MaxDispersion);
            if (_forcedExpandTargetDeg > 0f && CurrentDeg >= _forcedExpandTargetDeg - 0.02f)
            {
                _forcedExpandTargetDeg = 0f;
            }

            Sample(root, includeCameraAimMotion);
            return CurrentDeg;
        }

        public void AddShotBloom(GunDispersionSettings settings, GunDispersionGlobalSettings globalSettings)
        {
            globalSettings ??= GunDispersionGlobalSettings.Default;
            if (settings == null || !globalSettings.enabled)
            {
                return;
            }

            float target = CurrentDeg + Mathf.Max(0f, settings.shotDispersionDeg);
            _forcedExpandTargetDeg = Mathf.Clamp(target, settings.MinDispersion, settings.MaxDispersion);
        }

        private static float GetMovementPenalty(VehicleRoot root, GunDispersionSettings settings)
        {
            if (root.inputManager == null)
            {
                return 0f;
            }

            float move = Mathf.Clamp01(root.inputManager.Move.magnitude);
            return settings.movingDispersionDeg * move;
        }

        private static float GetRotationPenalty(Quaternion current, Quaternion last, float dt, float referenceDegPerSec, float maxPenaltyDeg)
        {
            if (referenceDegPerSec <= 0.001f || maxPenaltyDeg <= 0f)
            {
                return 0f;
            }

            float degPerSec = Quaternion.Angle(last, current) / dt;
            float factor = Mathf.Clamp01(degPerSec / referenceDegPerSec);
            return maxPenaltyDeg * factor;
        }

        private void Sample(VehicleRoot root, bool includeCamera)
        {
            _lastHullRotation = GetHullRotation(root);
            _lastTurretRotation = GetTurretRotation(root);
            _lastGunRotation = GetGunRotation(root);
            if (includeCamera)
            {
                _lastCameraRotation = GetCameraRotation();
            }

            _hasLastSample = true;
        }

        private static Quaternion GetHullRotation(VehicleRoot root)
        {
            if (root != null && root.objectMover != null)
            {
                return root.objectMover.transform.rotation;
            }

            return root != null ? root.transform.rotation : Quaternion.identity;
        }

        private static Quaternion GetTurretRotation(VehicleRoot root)
        {
            if (root != null && root.robotHullRotation != null)
            {
                return root.robotHullRotation.transform.rotation;
            }

            return GetHullRotation(root);
        }

        private static Quaternion GetGunRotation(VehicleRoot root)
        {
            if (root != null && root.weaponAimAtCamera != null && root.weaponAimAtCamera.gun != null)
            {
                return root.weaponAimAtCamera.gun.rotation;
            }

            return GetTurretRotation(root);
        }

        private static Quaternion GetCameraRotation()
        {
            if (CameraSync.In != null)
            {
                return CameraSync.In.transform.rotation;
            }

            return Quaternion.identity;
        }
    }
}
