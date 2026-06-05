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

        [Tooltip("Вмикає серверну систему розкиду за швидкістю руху танка, фактичним рухом башти і пострілами.")]
        public bool enabled = true;
        [Tooltip("Час, за який коло зведення швидко розширюється після початку руху танка, повороту башти або пострілу. Менше значення = різкіше розширення.")]
        public float expandTime = 0.12f;

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
        [Tooltip("Скільки пікселів на градус поточного розкиду понад мінімальний додається в максимальному зумі/снайперському режимі.")]
        [FormerlySerializedAs("uiPixelsPerDegree")]
        public float uiBloomPixelsPerDegreeAtMaxZoom = 42f;
        [Tooltip("Скільки пікселів на градус поточного розкиду понад мінімальний додається на максимальній дистанції камери від третьої особи.")]
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
        private const float MaxTurretMovementDispersion01 = 0.5f;
        private const float MaxVehicleMovementDispersion01 = 0.5f;
        private const float ShotDispersion01 = 0.5f;
        private const float TurretSpeedDeadZone01 = 0.05f;

        private bool _hasLastSample;
        private float _lastTurretLocalYaw;
        private Vector3 _lastMovePosition;
        private bool _hasLastMovePosition;
        private float _shotDispersion01;

        public float CurrentDeg { get; private set; }

        public void Reset(VehicleRoot root, GunDispersionSettings settings)
        {
            CurrentDeg = settings != null ? settings.MinDispersion : 0f;
            _shotDispersion01 = 0f;
            _hasLastMovePosition = false;
            Sample(root);
        }

        public void ForceFullyAimed(VehicleRoot root, GunDispersionSettings settings, bool includeCameraAimMotion)
        {
            CurrentDeg = settings != null ? settings.MinDispersion : 0f;
            _shotDispersion01 = 0f;
            _hasLastMovePosition = false;
            Sample(root);
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
                Sample(root);
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

            float dispersion01 = GetMovementSpeed01(root, dt) * MaxVehicleMovementDispersion01;
            dispersion01 += GetTurretSpeed01(root, dt) * MaxTurretMovementDispersion01;
            dispersion01 += _shotDispersion01;
            dispersion01 = Mathf.Clamp01(dispersion01);
            float targetDeg = Mathf.Lerp(settings.MinDispersion, settings.MaxDispersion, dispersion01);

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
            if (_shotDispersion01 > 0f && CurrentDeg >= targetDeg - 0.02f)
            {
                _shotDispersion01 = 0f;
            }

            Sample(root);
            return CurrentDeg;
        }

        public void AddShotDispersion(GunDispersionSettings settings, GunDispersionGlobalSettings globalSettings)
        {
            globalSettings ??= GunDispersionGlobalSettings.Default;
            if (settings == null || !globalSettings.enabled)
            {
                return;
            }

            _shotDispersion01 = Mathf.Max(_shotDispersion01, ShotDispersion01);
            float shotTargetDeg = Mathf.Lerp(settings.MinDispersion, settings.MaxDispersion, ShotDispersion01);
            CurrentDeg = Mathf.Clamp(Mathf.Max(CurrentDeg, shotTargetDeg), settings.MinDispersion, settings.MaxDispersion);
        }

        private float GetMovementSpeed01(VehicleRoot root, float dt)
        {
            if (dt <= 0f)
            {
                return 0f;
            }

            Vector3 currentPosition = GetMovementPosition(root);
            if (!_hasLastMovePosition)
            {
                _lastMovePosition = currentPosition;
                _hasLastMovePosition = true;
                return 0f;
            }

            Vector3 delta = currentPosition - _lastMovePosition;
            delta.y = 0f;

            float speed = delta.magnitude / dt;
            float maxSpeed = GetMaxMovementSpeed(root);
            if (maxSpeed <= 0.001f)
            {
                return 0f;
            }

            return Mathf.Clamp01(speed / maxSpeed);
        }

        private float GetTurretSpeed01(VehicleRoot root, float dt)
        {
            if (dt <= 0f || root == null || root.robotHullRotation == null)
            {
                return 0f;
            }

            float current = GetTurretLocalYaw(root);
            float degPerSecond = Mathf.Abs(Mathf.DeltaAngle(_lastTurretLocalYaw, current)) / dt;
            float referenceDegPerSecond = root.robotHullRotation.rotationSpeed;
            if (referenceDegPerSecond <= 0.001f)
            {
                return 0f;
            }

            float speed01 = Mathf.Clamp01(degPerSecond / referenceDegPerSecond);
            if (speed01 <= TurretSpeedDeadZone01)
            {
                return 0f;
            }

            return Mathf.InverseLerp(TurretSpeedDeadZone01, 1f, speed01);
        }

        private static float GetMaxMovementSpeed(VehicleRoot root)
        {
            if (root == null)
            {
                return 0f;
            }

            if (root.HasRuntimeStats && root.RuntimeStats.Speed > 0f)
            {
                return root.RuntimeStats.Speed;
            }

            if (root.objectMover != null && root.objectMover.maxSpeed > 0f)
            {
                return root.objectMover.maxSpeed;
            }

            return 0f;
        }

        private void Sample(VehicleRoot root)
        {
            _lastTurretLocalYaw = GetTurretLocalYaw(root);
            _lastMovePosition = GetMovementPosition(root);
            _hasLastMovePosition = true;
            _hasLastSample = true;
        }

        private static float GetTurretLocalYaw(VehicleRoot root)
        {
            if (root != null && root.robotHullRotation != null)
            {
                return root.robotHullRotation.CurrentLocalYaw;
            }

            return 0f;
        }

        private static Vector3 GetMovementPosition(VehicleRoot root)
        {
            if (root != null && root.objectMover != null)
            {
                return root.objectMover.transform.position;
            }

            return root != null ? root.transform.position : Vector3.zero;
        }

    }
}
