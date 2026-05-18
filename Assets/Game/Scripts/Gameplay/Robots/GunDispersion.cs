using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.Gameplay.Robots
{
    [Serializable]
    public class GunDispersionSettings
    {
        [Header("Зведення")]
        public float minDispersionDeg = 0.35f;
        public float maxDispersionDeg = 6f;
        public float aimTime = 2f;

        [Header("Розширення розкиду")]
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

        [Tooltip("Увімкнути серверну систему розкиду, зведення та bloom для пострілів.")]
        public bool enabled = true;
        [Tooltip("Час, за який коло зведення швидко розширюється після руху, повороту або пострілу. Менше значення = різкіше розширення.")]
        public float expandTime = 0.12f;

        [Header("Еталонні швидкості")]
        [Tooltip("Еталонна швидкість повороту корпусу в градусах за секунду, при якій додається повний штраф розкиду від повороту корпусу.")]
        public float referenceHullTraverseDegPerSec = 90f;
        [Tooltip("Еталонна швидкість повороту башти в градусах за секунду, при якій додається повний штраф розкиду від повороту башти.")]
        public float referenceTurretTraverseDegPerSec = 45f;
        [Tooltip("Еталонна швидкість вертикального руху гармати в градусах за секунду, при якій додається повний штраф розкиду від руху гармати.")]
        public float referenceGunTraverseDegPerSec = 35f;
        [Tooltip("Еталонна швидкість руху камери/прицілу в градусах за секунду, при якій UI коло отримує штраф розкиду від різкого наведення.")]
        public float referenceCameraAimDegPerSec = 120f;

        [Header("Точність із бази даних")]
        [Tooltip("Дистанція, на якій інтерпретується точність з бази даних. Стиль World of Tanks: метри максимального радіального розкиду на 100 метрах.")]
        public float accuracyReferenceDistanceMeters = 100f;

        [Header("Інтерфейс")]
        [Tooltip("Мінімальний діаметр UI-кола зведення в пікселях навіть при ідеально точній гарматі.")]
        public float uiMinDiameter = 55f;
        [Tooltip("Максимальний діаметр UI-кола зведення в пікселях.")]
        public float uiMaxDiameter = 340f;
        [Tooltip("Скільки пікселів на градус точності додається до повністю зведеного кола в максимальному зумі/снайперському режимі.")]
        [FormerlySerializedAs("uiFullyAimedPixelsPerDegree")]
        public float uiFullyAimedPixelsPerDegreeAtMaxZoom = 85f;
        [Tooltip("Скільки пікселів на градус точності додається до повністю зведеного кола на максимальній дистанції камери від третьої особи.")]
        public float uiFullyAimedPixelsPerDegreeAtMaxDistance = 34f;
        [Tooltip("Скільки пікселів на градус bloom додається до кола від руху/повороту/пострілу в максимальному зумі/снайперському режимі.")]
        [FormerlySerializedAs("uiPixelsPerDegree")]
        public float uiBloomPixelsPerDegreeAtMaxZoom = 42f;
        [Tooltip("Скільки пікселів на градус bloom додається до кола від руху/повороту/пострілу на максимальній дистанції камери від третьої особи.")]
        public float uiBloomPixelsPerDegreeAtMaxDistance = 17f;
        [Tooltip("Швидкість згладжування діаметра UI-кола зведення. 0 = діаметр змінюється миттєво.")]
        public float uiDiameterLerpSpeed = 18f;
        [Tooltip("Швидкість, з якою UI-коло доганяє логічну точку прицілу по горизонталі. 0 = горизонталь миттєва.")]
        public float uiReticleHorizontalLerpSpeed = 20f;
        [Tooltip("Швидкість, з якою UI-коло доганяє логічну точку прицілу по вертикалі. 0 = вертикаль миттєва.")]
        [FormerlySerializedAs("uiReticlePositionLerpSpeed")]
        public float uiReticleVerticalLerpSpeed = 20f;
        [Tooltip("Швидкість, з якою UI-коло в снайперському режимі доганяє логічну точку прицілу по горизонталі. Менше значення сильніше згладжує дрібне смикання, 0 = миттєво.")]
        public float uiSniperReticleHorizontalLerpSpeed = 18f;
        [Tooltip("Швидкість, з якою UI-коло в снайперському режимі доганяє логічну точку прицілу по вертикалі. Менше значення сильніше згладжує дрібне смикання, 0 = миттєво.")]
        public float uiSniperReticleVerticalLerpSpeed = 18f;

        [Header("Мережа")]
        [Tooltip("Як часто сервер синхронізує значення розкиду з клієнтами. Менше значення = частіше оновлення, але більше мережевого трафіку.")]
        public float serverSyncInterval = 0.05f;
        [Tooltip("Мінімальна зміна розкиду в градусах, після якої сервер відправляє оновлення клієнтам.")]
        public float serverSyncDeadZoneDeg = 0.03f;

        public static GunDispersionGlobalSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public float GetUiDiameter(float dispersionDeg, float fullyAimedDispersionDeg)
        {
            return GetUiDiameter(dispersionDeg, fullyAimedDispersionDeg, 1f);
        }

        public float GetUiDiameter(float dispersionDeg, float fullyAimedDispersionDeg, float cameraZoom01)
        {
            Validate();

            float fullyAimedDeg = Mathf.Max(0f, fullyAimedDispersionDeg);
            float currentDeg = Mathf.Max(fullyAimedDeg, dispersionDeg);
            float zoom = Clamp01Finite(cameraZoom01, 1f);
            float fullyAimedPixelsPerDegree = Mathf.Lerp(
                uiFullyAimedPixelsPerDegreeAtMaxDistance,
                uiFullyAimedPixelsPerDegreeAtMaxZoom,
                zoom);
            float bloomPixelsPerDegree = Mathf.Lerp(
                uiBloomPixelsPerDegreeAtMaxDistance,
                uiBloomPixelsPerDegreeAtMaxZoom,
                zoom);
            float fullyAimedDiameter = fullyAimedDeg * fullyAimedPixelsPerDegree;
            float bloomDiameter = Mathf.Max(0f, currentDeg - fullyAimedDeg) * bloomPixelsPerDegree;
            float diameter = uiMinDiameter + fullyAimedDiameter + bloomDiameter;
            return Mathf.Clamp(diameter, uiMinDiameter, uiMaxDiameter);
        }

        public float GetAccuracyDispersionDeg(float accuracyMetersAtReferenceDistance, float fallbackDispersionDeg)
        {
            if (float.IsNaN(accuracyMetersAtReferenceDistance)
                || float.IsInfinity(accuracyMetersAtReferenceDistance)
                || accuracyMetersAtReferenceDistance <= 0f)
            {
                return Mathf.Max(0f, fallbackDispersionDeg);
            }

            Validate();

            float referenceDistance = Mathf.Max(0.0001f, accuracyReferenceDistanceMeters);
            return Mathf.Atan(Mathf.Max(0f, accuracyMetersAtReferenceDistance) / referenceDistance) * Mathf.Rad2Deg;
        }

        public void Validate()
        {
            accuracyReferenceDistanceMeters = ClampFinite(accuracyReferenceDistanceMeters, 0.0001f, Default.accuracyReferenceDistanceMeters);
            uiMinDiameter = ClampFinite(uiMinDiameter, 1f, Default.uiMinDiameter);
            uiMaxDiameter = ClampFinite(uiMaxDiameter, uiMinDiameter, Default.uiMaxDiameter);
            if (uiMaxDiameter < uiMinDiameter)
            {
                uiMaxDiameter = uiMinDiameter;
            }

            uiFullyAimedPixelsPerDegreeAtMaxZoom = ClampFinite(uiFullyAimedPixelsPerDegreeAtMaxZoom, 0f, Default.uiFullyAimedPixelsPerDegreeAtMaxZoom);
            uiFullyAimedPixelsPerDegreeAtMaxDistance = ClampFinite(uiFullyAimedPixelsPerDegreeAtMaxDistance, 0f, Default.uiFullyAimedPixelsPerDegreeAtMaxDistance);
            uiBloomPixelsPerDegreeAtMaxZoom = ClampFinite(uiBloomPixelsPerDegreeAtMaxZoom, 0f, Default.uiBloomPixelsPerDegreeAtMaxZoom);
            uiBloomPixelsPerDegreeAtMaxDistance = ClampFinite(uiBloomPixelsPerDegreeAtMaxDistance, 0f, Default.uiBloomPixelsPerDegreeAtMaxDistance);
            uiDiameterLerpSpeed = ClampFinite(uiDiameterLerpSpeed, 0f, Default.uiDiameterLerpSpeed);
            uiReticleHorizontalLerpSpeed = ClampFinite(uiReticleHorizontalLerpSpeed, 0f, Default.uiReticleHorizontalLerpSpeed);
            uiReticleVerticalLerpSpeed = ClampFinite(uiReticleVerticalLerpSpeed, 0f, Default.uiReticleVerticalLerpSpeed);
            uiSniperReticleHorizontalLerpSpeed = ClampFinite(uiSniperReticleHorizontalLerpSpeed, 0f, Default.uiSniperReticleHorizontalLerpSpeed);
            uiSniperReticleVerticalLerpSpeed = ClampFinite(uiSniperReticleVerticalLerpSpeed, 0f, Default.uiSniperReticleVerticalLerpSpeed);
            expandTime = ClampFinite(expandTime, 0.001f, Default.expandTime);
            referenceHullTraverseDegPerSec = ClampFinite(referenceHullTraverseDegPerSec, 0.001f, Default.referenceHullTraverseDegPerSec);
            referenceTurretTraverseDegPerSec = ClampFinite(referenceTurretTraverseDegPerSec, 0.001f, Default.referenceTurretTraverseDegPerSec);
            referenceGunTraverseDegPerSec = ClampFinite(referenceGunTraverseDegPerSec, 0.001f, Default.referenceGunTraverseDegPerSec);
            referenceCameraAimDegPerSec = ClampFinite(referenceCameraAimDegPerSec, 0.001f, Default.referenceCameraAimDegPerSec);
            serverSyncInterval = ClampFinite(serverSyncInterval, 0.001f, Default.serverSyncInterval);
            serverSyncDeadZoneDeg = ClampFinite(serverSyncDeadZoneDeg, 0f, Default.serverSyncDeadZoneDeg);
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
            accuracyReferenceDistanceMeters = source.accuracyReferenceDistanceMeters;
            uiMinDiameter = source.uiMinDiameter;
            uiMaxDiameter = source.uiMaxDiameter;
            uiFullyAimedPixelsPerDegreeAtMaxZoom = source.uiFullyAimedPixelsPerDegreeAtMaxZoom;
            uiFullyAimedPixelsPerDegreeAtMaxDistance = source.uiFullyAimedPixelsPerDegreeAtMaxDistance;
            uiBloomPixelsPerDegreeAtMaxZoom = source.uiBloomPixelsPerDegreeAtMaxZoom;
            uiBloomPixelsPerDegreeAtMaxDistance = source.uiBloomPixelsPerDegreeAtMaxDistance;
            uiDiameterLerpSpeed = source.uiDiameterLerpSpeed;
            uiReticleHorizontalLerpSpeed = source.uiReticleHorizontalLerpSpeed;
            uiReticleVerticalLerpSpeed = source.uiReticleVerticalLerpSpeed;
            uiSniperReticleHorizontalLerpSpeed = source.uiSniperReticleHorizontalLerpSpeed;
            uiSniperReticleVerticalLerpSpeed = source.uiSniperReticleVerticalLerpSpeed;
            serverSyncInterval = source.serverSyncInterval;
            serverSyncDeadZoneDeg = source.serverSyncDeadZoneDeg;
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                if (float.IsNaN(fallback) || float.IsInfinity(fallback))
                {
                    return minValue;
                }

                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }

        private static float Clamp01Finite(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return Mathf.Clamp01(fallback);
            }

            return Mathf.Clamp01(value);
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

        public void ForceFullyAimed(VehicleRoot root, GunDispersionSettings settings, bool includeCameraAimMotion)
        {
            CurrentDeg = settings != null ? settings.MinDispersion : 0f;
            _forcedExpandTargetDeg = 0f;
            Sample(root, includeCameraAimMotion);
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
                return CameraSync.In.GetAimRotation();
            }

            return Quaternion.identity;
        }
    }
}
