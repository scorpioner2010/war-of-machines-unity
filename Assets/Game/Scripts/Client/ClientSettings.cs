using UnityEngine;

namespace Game.Scripts.Client
{
    [System.Serializable]
    public class GameplayRuntimeSettings
    {
        private static readonly GameplayRuntimeSettings DefaultSettings = new GameplayRuntimeSettings();

        [Header("UI здоров'я")]
        [Tooltip("Колір смуги HP для свого робота та союзників у world-space UI техніки.")]
        public Color alliedHpColor = new Color(0.1f, 0.35f, 1f, 1f);
        [Tooltip("Колір смуги HP для противників у world-space UI техніки.")]
        public Color enemyHpColor = new Color(1f, 0.08f, 0.04f, 1f);
        [Tooltip("Як часто world-space UI техніки переоцінює команду цілі та оновлює колір HP.")]
        public float hpTeamColorRefreshInterval = 0.25f;
        [Tooltip("Швидкість згладження основної смуги HP локального гравця на GameplayHUD.")]
        public float ownerHealthBarSmoothSpeed = 10f;
        [Header("World-space HP scale")]
        [Tooltip("Enable distance based scaling for robot HP bars in world-space UI.")]
        public bool worldHpBarDistanceScaleEnabled = true;
        [Tooltip("Distance where robot HP bars use the near scale.")]
        public float worldHpBarScaleMinDistance = 5f;
        [Tooltip("Distance where robot HP bars reach the far scale.")]
        public float worldHpBarScaleMaxDistance = 90f;
        [Tooltip("HP bar scale multiplier near the camera.")]
        public float worldHpBarMinDistanceScale = 1f;
        [Tooltip("HP bar scale multiplier far from the camera.")]
        public float worldHpBarMaxDistanceScale = 5f;

        [Header("Карта")]
        [Tooltip("Колір іконок союзників на мінікарті та повній карті.")]
        public Color mapAllyIconColor = new Color(0.1f, 0.35f, 1f, 1f);
        [Tooltip("Колір іконок противників на мінікарті та повній карті.")]
        public Color mapEnemyIconColor = new Color(1f, 0.08f, 0.04f, 1f);
        [Tooltip("Колір іконок знищених роботів на мінікарті та повній карті.")]
        public Color mapDestroyedIconColor = Color.black;
        [Tooltip("Чи повертати іконки роботів на карті за напрямком їхнього корпусу.")]
        public bool mapRotateIcons = true;
        [Tooltip("Масштаб іконок союзників і противників відносно іконки локального гравця.")]
        public float mapTrackedVehicleIconScale = 0.8f;
        [Tooltip("Як часто карта перебудовує список видимих роботів. Позиції вже знайдених іконок оновлюються щокадру.")]
        public float mapTrackedVehicleRefreshInterval = 0.5f;
        [Tooltip("Клавіша, яка показує повну карту під час бою.")]
        public KeyCode mapFullMapKey = KeyCode.M;

        [Header("Автоприціл")]
        [Tooltip("Максимальна дистанція променя для захоплення цілі автоприцілом.")]
        public float autoAimMaxAcquireDistance = 2000f;
        [Tooltip("Запасна висота точки прицілювання, якщо в цілі не знайдено колайдерів або башні.")]
        public float autoAimFallbackTargetHeight = 1.2f;
        [Tooltip("Не дозволяти автоприцілу захоплювати союзників.")]
        public bool autoAimRejectSameTeam = true;
        [Tooltip("Після захоплення противника вести автоприціл у башню, а не в центр усього корпусу.")]
        public bool autoAimPreferTurretTarget = true;

        [Header("Камера")]
        [Tooltip("Запасна дистанція камери в режимі наближення, якщо список кроків наближення порожній.")]
        public float cameraAimDistance = 6f;
        [Tooltip("Дистанція снайперської камери від точки прив'язки.")]
        public float cameraSniperDistance = 0.25f;
        [Tooltip("У снайперському режимі брати точку камери від дула, якщо воно доступне.")]
        public bool cameraSniperFromMuzzle = true;
        [Tooltip("Зміщення снайперської камери вперед від точки прив'язки.")]
        public float cameraSniperForwardOffset = 0.15f;
        [Tooltip("Вертикальне зміщення снайперської камери від точки прив'язки.")]
        public float cameraSniperVerticalOffset = 0f;
        [Tooltip("Горизонтальна швидкість обертання камери до застосування чутливості миші.")]
        public float cameraHorizontalSpeed = 120f;
        [Tooltip("Вертикальна швидкість обертання камери до застосування чутливості миші.")]
        public float cameraVerticalSpeed = 120f;
        [Tooltip("Мінімальний кут нахилу камери.")]
        public float cameraMinPitch = -20f;
        [Tooltip("Максимальний кут нахилу камери.")]
        public float cameraMaxPitch = 80f;
        [Tooltip("FOV камери у звичайному режимі.")]
        public float cameraNormalFov = 60f;
        [Tooltip("FOV камери в режимі наближення.")]
        public float cameraAimFov = 45f;
        [Tooltip("FOV камери у снайперському режимі.")]
        public float cameraSniperFov = 24f;
        [Tooltip("Швидкість згладження зміни FOV.")]
        public float cameraFovLerpSpeed = 10f;
        [Tooltip("Швидкість згладження дистанції камери.")]
        public float cameraDistanceLerpSpeed = 12f;
        [Tooltip("Мертва зона колеса миші для перемикання наближення.")]
        public float cameraScrollDeadZone = 0.001f;
        [Tooltip("Дистанції кроків наближення до снайперського режиму.")]
        public float[] cameraAimZoomDistances = { 6f, 4f, 2.5f };

        [Header("Приціл")]
        [Tooltip("Колір тексту, коли розкид повністю зведений.")]
        public Color aimReadyColor = new Color(0.5f, 1f, 0.5f, 1f);
        [Tooltip("Колір тексту, коли розкид ще зводиться.")]
        public Color aimProgressColor = new Color(1f, 0.86f, 0.2f, 1f);
        [Tooltip("Текст стану прицілу, коли зброя повністю зведена.")]
        public string aimReadyText = "AIM READY";
        [Tooltip("Префікс тексту стану прицілу під час зведення.")]
        public string aimProgressTextPrefix = "AIM ";
        [Tooltip("Запасна швидкість згладження прицілу, якщо глобальні налаштування розкиду недоступні.")]
        public float reticleFallbackSmoothSpeed = 20f;
        [Tooltip("Ховати приціл, коли точка прицілювання знаходиться позаду камери.")]
        public bool reticleHideWhenBehindCamera = true;
        [Tooltip("Обмежувати позицію прицілу межами canvas.")]
        public bool reticleClampToCanvas = true;
        [Tooltip("Кут між напрямком гармати і камерою, після якого приціл ховається.")]
        public float reticleHideWhenAngleGreaterThan = 90f;
        [Tooltip("Показувати серверний приціл, якщо гравець увімкнув його в налаштуваннях.")]
        public bool reticleShowServerReticle = true;

        [Header("Hover outline")]
        [Tooltip("Максимальна дистанція пошуку цілі для підсвічування під прицілом.")]
        public float hoverOutlineMaxDistance = 2000f;
        [Tooltip("Товщина outline підсвічування техніки під прицілом.")]
        public float hoverOutlineWidth = 3f;
        [Tooltip("Колір outline підсвічування техніки під прицілом.")]
        public Color hoverOutlineColor = Color.red;
        [Tooltip("Не підсвічувати союзників під прицілом.")]
        public bool hoverOutlineRejectSameTeam = true;

        [Header("HUD швидкості")]
        [Tooltip("Множник переведення фактичної швидкості машини в число на спідометрі HUD.")]
        public float speedHudDisplaySpeedMultiplier = 10f;
        [Tooltip("Інтервал семплування позиції для обчислення швидкості HUD.")]
        public float speedHudSampleInterval = 0.12f;
        [Tooltip("Швидкість згладження числа швидкості на HUD.")]
        public float speedHudSmoothRate = 8f;
        [Tooltip("Поріг, нижче якого швидкість на HUD одразу стає нулем.")]
        public float speedHudStopSnapThreshold = 0.05f;
        [Tooltip("Скільки секунд показувати текст результату пострілу на HUD.")]
        public float shotResultVisibleTime = 4f;
        [Tooltip("Тривалість анімації плаваючого тексту шкоди.")]
        public float floatingDamageTextDuration = 0.8f;
        [Tooltip("На скільки метрів плаваючий текст шкоди піднімається вгору.")]
        public float floatingDamageTextMoveUp = 1.5f;
        [Tooltip("Фінальний масштаб плаваючого тексту шкоди.")]
        public float floatingDamageTextEndScale = 1.3f;
        [Tooltip("Тривалість fade-анімації снайперського overlay.")]
        public float sniperScopeOverlayFadeDuration = 0.18f;

        public static GameplayRuntimeSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public void Validate()
        {
            alliedHpColor = ClampColor(alliedHpColor, Default.alliedHpColor);
            enemyHpColor = ClampColor(enemyHpColor, Default.enemyHpColor);
            hpTeamColorRefreshInterval = ClampFinite(hpTeamColorRefreshInterval, 0.1f, Default.hpTeamColorRefreshInterval);
            ownerHealthBarSmoothSpeed = ClampFinite(ownerHealthBarSmoothSpeed, 0f, Default.ownerHealthBarSmoothSpeed);
            worldHpBarScaleMinDistance = ClampFinite(worldHpBarScaleMinDistance, 0.01f, Default.worldHpBarScaleMinDistance);
            worldHpBarScaleMaxDistance = ClampFinite(worldHpBarScaleMaxDistance, 0.01f, Default.worldHpBarScaleMaxDistance);
            if (worldHpBarScaleMaxDistance <= worldHpBarScaleMinDistance)
            {
                worldHpBarScaleMaxDistance = worldHpBarScaleMinDistance + 0.01f;
            }

            worldHpBarMinDistanceScale = ClampFinite(worldHpBarMinDistanceScale, 0.01f, Default.worldHpBarMinDistanceScale);
            worldHpBarMaxDistanceScale = ClampFinite(worldHpBarMaxDistanceScale, 0.01f, Default.worldHpBarMaxDistanceScale);
            if (worldHpBarMaxDistanceScale < worldHpBarMinDistanceScale)
            {
                worldHpBarMaxDistanceScale = worldHpBarMinDistanceScale;
            }

            mapAllyIconColor = ClampColor(mapAllyIconColor, Default.mapAllyIconColor);
            mapEnemyIconColor = ClampColor(mapEnemyIconColor, Default.mapEnemyIconColor);
            mapDestroyedIconColor = ClampColor(mapDestroyedIconColor, Default.mapDestroyedIconColor);
            mapTrackedVehicleIconScale = ClampFinite(mapTrackedVehicleIconScale, 0.1f, Default.mapTrackedVehicleIconScale);
            mapTrackedVehicleRefreshInterval = ClampFinite(mapTrackedVehicleRefreshInterval, 0.1f, Default.mapTrackedVehicleRefreshInterval);
            autoAimMaxAcquireDistance = ClampFinite(autoAimMaxAcquireDistance, 0.1f, Default.autoAimMaxAcquireDistance);
            autoAimFallbackTargetHeight = ClampFinite(autoAimFallbackTargetHeight, 0f, Default.autoAimFallbackTargetHeight);
            cameraAimDistance = ClampFinite(cameraAimDistance, 0.01f, Default.cameraAimDistance);
            cameraSniperDistance = ClampFinite(cameraSniperDistance, 0.01f, Default.cameraSniperDistance);
            cameraSniperForwardOffset = ClampFinite(cameraSniperForwardOffset, 0f, Default.cameraSniperForwardOffset);
            cameraSniperVerticalOffset = ClampFinite(cameraSniperVerticalOffset, Default.cameraSniperVerticalOffset);
            cameraHorizontalSpeed = ClampFinite(cameraHorizontalSpeed, 0f, Default.cameraHorizontalSpeed);
            cameraVerticalSpeed = ClampFinite(cameraVerticalSpeed, 0f, Default.cameraVerticalSpeed);
            cameraMinPitch = ClampFinite(cameraMinPitch, Default.cameraMinPitch);
            cameraMaxPitch = ClampFinite(cameraMaxPitch, Default.cameraMaxPitch);
            if (cameraMaxPitch < cameraMinPitch)
            {
                cameraMaxPitch = cameraMinPitch;
            }

            cameraNormalFov = ClampFinite(cameraNormalFov, 1f, Default.cameraNormalFov);
            cameraAimFov = ClampFinite(cameraAimFov, 1f, Default.cameraAimFov);
            cameraSniperFov = ClampFinite(cameraSniperFov, 1f, Default.cameraSniperFov);
            cameraFovLerpSpeed = ClampFinite(cameraFovLerpSpeed, 0f, Default.cameraFovLerpSpeed);
            cameraDistanceLerpSpeed = ClampFinite(cameraDistanceLerpSpeed, 0f, Default.cameraDistanceLerpSpeed);
            cameraScrollDeadZone = ClampFinite(cameraScrollDeadZone, 0f, Default.cameraScrollDeadZone);
            ValidateCameraZoomDistances();
            aimReadyColor = ClampColor(aimReadyColor, Default.aimReadyColor);
            aimProgressColor = ClampColor(aimProgressColor, Default.aimProgressColor);
            if (string.IsNullOrEmpty(aimReadyText))
            {
                aimReadyText = Default.aimReadyText;
            }

            if (aimProgressTextPrefix == null)
            {
                aimProgressTextPrefix = Default.aimProgressTextPrefix;
            }

            reticleFallbackSmoothSpeed = ClampFinite(reticleFallbackSmoothSpeed, 0f, Default.reticleFallbackSmoothSpeed);
            reticleHideWhenAngleGreaterThan = ClampFinite(reticleHideWhenAngleGreaterThan, 0f, Default.reticleHideWhenAngleGreaterThan);
            hoverOutlineMaxDistance = ClampFinite(hoverOutlineMaxDistance, 0.1f, Default.hoverOutlineMaxDistance);
            hoverOutlineWidth = ClampFinite(hoverOutlineWidth, 1f, Default.hoverOutlineWidth);
            hoverOutlineColor = ClampColor(hoverOutlineColor, Default.hoverOutlineColor);
            speedHudDisplaySpeedMultiplier = ClampFinite(speedHudDisplaySpeedMultiplier, 0f, Default.speedHudDisplaySpeedMultiplier);
            speedHudSampleInterval = ClampFinite(speedHudSampleInterval, 0.02f, Default.speedHudSampleInterval);
            speedHudSmoothRate = ClampFinite(speedHudSmoothRate, 0.01f, Default.speedHudSmoothRate);
            speedHudStopSnapThreshold = ClampFinite(speedHudStopSnapThreshold, 0f, Default.speedHudStopSnapThreshold);
            shotResultVisibleTime = ClampFinite(shotResultVisibleTime, 0f, Default.shotResultVisibleTime);
            floatingDamageTextDuration = ClampFinite(floatingDamageTextDuration, 0f, Default.floatingDamageTextDuration);
            floatingDamageTextMoveUp = ClampFinite(floatingDamageTextMoveUp, Default.floatingDamageTextMoveUp);
            floatingDamageTextEndScale = ClampFinite(floatingDamageTextEndScale, 0f, Default.floatingDamageTextEndScale);
            sniperScopeOverlayFadeDuration = ClampFinite(sniperScopeOverlayFadeDuration, 0f, Default.sniperScopeOverlayFadeDuration);
        }

        public void CopyFrom(GameplayRuntimeSettings source)
        {
            if (source == null)
            {
                return;
            }

            alliedHpColor = source.alliedHpColor;
            enemyHpColor = source.enemyHpColor;
            hpTeamColorRefreshInterval = source.hpTeamColorRefreshInterval;
            ownerHealthBarSmoothSpeed = source.ownerHealthBarSmoothSpeed;
            worldHpBarDistanceScaleEnabled = source.worldHpBarDistanceScaleEnabled;
            worldHpBarScaleMinDistance = source.worldHpBarScaleMinDistance;
            worldHpBarScaleMaxDistance = source.worldHpBarScaleMaxDistance;
            worldHpBarMinDistanceScale = source.worldHpBarMinDistanceScale;
            worldHpBarMaxDistanceScale = source.worldHpBarMaxDistanceScale;
            mapAllyIconColor = source.mapAllyIconColor;
            mapEnemyIconColor = source.mapEnemyIconColor;
            mapDestroyedIconColor = source.mapDestroyedIconColor;
            mapRotateIcons = source.mapRotateIcons;
            mapTrackedVehicleIconScale = source.mapTrackedVehicleIconScale;
            mapTrackedVehicleRefreshInterval = source.mapTrackedVehicleRefreshInterval;
            mapFullMapKey = source.mapFullMapKey;
            autoAimMaxAcquireDistance = source.autoAimMaxAcquireDistance;
            autoAimFallbackTargetHeight = source.autoAimFallbackTargetHeight;
            autoAimRejectSameTeam = source.autoAimRejectSameTeam;
            autoAimPreferTurretTarget = source.autoAimPreferTurretTarget;
            cameraAimDistance = source.cameraAimDistance;
            cameraSniperDistance = source.cameraSniperDistance;
            cameraSniperFromMuzzle = source.cameraSniperFromMuzzle;
            cameraSniperForwardOffset = source.cameraSniperForwardOffset;
            cameraSniperVerticalOffset = source.cameraSniperVerticalOffset;
            cameraHorizontalSpeed = source.cameraHorizontalSpeed;
            cameraVerticalSpeed = source.cameraVerticalSpeed;
            cameraMinPitch = source.cameraMinPitch;
            cameraMaxPitch = source.cameraMaxPitch;
            cameraNormalFov = source.cameraNormalFov;
            cameraAimFov = source.cameraAimFov;
            cameraSniperFov = source.cameraSniperFov;
            cameraFovLerpSpeed = source.cameraFovLerpSpeed;
            cameraDistanceLerpSpeed = source.cameraDistanceLerpSpeed;
            cameraScrollDeadZone = source.cameraScrollDeadZone;
            cameraAimZoomDistances = source.cameraAimZoomDistances != null ? (float[])source.cameraAimZoomDistances.Clone() : null;
            aimReadyColor = source.aimReadyColor;
            aimProgressColor = source.aimProgressColor;
            aimReadyText = source.aimReadyText;
            aimProgressTextPrefix = source.aimProgressTextPrefix;
            reticleFallbackSmoothSpeed = source.reticleFallbackSmoothSpeed;
            reticleHideWhenBehindCamera = source.reticleHideWhenBehindCamera;
            reticleClampToCanvas = source.reticleClampToCanvas;
            reticleHideWhenAngleGreaterThan = source.reticleHideWhenAngleGreaterThan;
            reticleShowServerReticle = source.reticleShowServerReticle;
            hoverOutlineMaxDistance = source.hoverOutlineMaxDistance;
            hoverOutlineWidth = source.hoverOutlineWidth;
            hoverOutlineColor = source.hoverOutlineColor;
            hoverOutlineRejectSameTeam = source.hoverOutlineRejectSameTeam;
            speedHudDisplaySpeedMultiplier = source.speedHudDisplaySpeedMultiplier;
            speedHudSampleInterval = source.speedHudSampleInterval;
            speedHudSmoothRate = source.speedHudSmoothRate;
            speedHudStopSnapThreshold = source.speedHudStopSnapThreshold;
            shotResultVisibleTime = source.shotResultVisibleTime;
            floatingDamageTextDuration = source.floatingDamageTextDuration;
            floatingDamageTextMoveUp = source.floatingDamageTextMoveUp;
            floatingDamageTextEndScale = source.floatingDamageTextEndScale;
            sniperScopeOverlayFadeDuration = source.sniperScopeOverlayFadeDuration;
        }

        private void ValidateCameraZoomDistances()
        {
            if (cameraAimZoomDistances == null)
            {
                cameraAimZoomDistances = (float[])Default.cameraAimZoomDistances.Clone();
                return;
            }

            for (int i = 0; i < cameraAimZoomDistances.Length; i++)
            {
                cameraAimZoomDistances[i] = ClampFinite(cameraAimZoomDistances[i], 0.01f, Default.cameraAimDistance);
            }
        }

        private static Color ClampColor(Color value, Color fallback)
        {
            if (!IsFinite(value.r) || !IsFinite(value.g) || !IsFinite(value.b) || !IsFinite(value.a))
            {
                return fallback;
            }

            return new Color(
                Mathf.Clamp01(value.r),
                Mathf.Clamp01(value.g),
                Mathf.Clamp01(value.b),
                Mathf.Clamp01(value.a));
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

        private static float ClampFinite(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return float.IsNaN(fallback) || float.IsInfinity(fallback) ? 0f : fallback;
            }

            return value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    [System.Serializable]
    public class ClientProjectileVisualSettings
    {
        private static readonly ClientProjectileVisualSettings DefaultSettings = new ClientProjectileVisualSettings();

        [Header("Projectile references")]
        [Tooltip("Client-only projectile prefab override. Leave empty to use the weapon prefab.")]
        public Projectile projectilePrefab;
        [Tooltip("Material applied to the projectile mesh on the client.")]
        public Material projectileMaterial;
        [Tooltip("Material used by the projectile tracer trail.")]
        public Material tracerMaterial;
        [Tooltip("One-shot fire effect spawned at the muzzle.")]
        public PooledImpactFx muzzleFlashPrefab;
        [Tooltip("One-shot smoke effect spawned at the muzzle.")]
        public PooledImpactFx muzzleSmokePrefab;

        [Header("Projectile glow")]
        public bool overrideProjectileMaterial = true;
        [ColorUsage(false, true)] public Color projectileBaseColor = new Color(1f, 0.58f, 0.16f, 1f);
        [ColorUsage(false, true)] public Color projectileEmissionColor = new Color(1f, 0.38f, 0.06f, 1f);
        [Min(0f)] public float projectileEmissionIntensity = 4f;

        [Header("Tracer")]
        public bool tracerEnabled = true;
        [Min(0.01f)] public float tracerLifetime = 0.35f;
        [Min(0.001f)] public float tracerStartWidth = 0.18f;
        [Min(0f)] public float tracerEndWidth = 0.02f;
        [Min(0.001f)] public float tracerMinVertexDistance = 0.08f;
        [Range(0, 8)] public int tracerCornerVertices = 2;
        [Range(0, 8)] public int tracerCapVertices = 1;
        [ColorUsage(false, true)] public Color tracerHeadColor = new Color(1f, 0.72f, 0.2f, 0.95f);
        [ColorUsage(false, true)] public Color tracerTailColor = new Color(1f, 0.18f, 0.02f, 0f);

        [Header("Muzzle FX")]
        public bool muzzleFxEnabled = true;
        [Min(0f)] public float muzzleForwardOffset = 0.25f;
        [Min(0.01f)] public float muzzleFlashScale = 0.55f;
        [Min(0.01f)] public float muzzleSmokeScale = 0.65f;

        [Header("Visual pools")]
        [Min(0)] public int clientProjectilePoolPrewarmCount = 16;
        [Min(1)] public int clientProjectilePoolMaxInactive = 64;
        [Min(0)] public int clientImpactFxPoolPrewarmCount = 16;
        [Min(1)] public int clientImpactFxPoolMaxInactive = 64;
        [Min(0)] public int muzzleFxPoolPrewarmCount = 8;
        [Min(1)] public int muzzleFxPoolMaxInactive = 32;

        public static ClientProjectileVisualSettings Default
        {
            get
            {
                return DefaultSettings;
            }
        }

        public void Validate()
        {
            projectileBaseColor = ClampHdrColor(projectileBaseColor, Default.projectileBaseColor);
            projectileEmissionColor = ClampHdrColor(projectileEmissionColor, Default.projectileEmissionColor);
            projectileEmissionIntensity = ClampFinite(projectileEmissionIntensity, 0f, Default.projectileEmissionIntensity);
            tracerLifetime = ClampFinite(tracerLifetime, 0.01f, Default.tracerLifetime);
            tracerStartWidth = ClampFinite(tracerStartWidth, 0.001f, Default.tracerStartWidth);
            tracerEndWidth = ClampFinite(tracerEndWidth, 0f, Default.tracerEndWidth);
            if (tracerEndWidth > tracerStartWidth)
            {
                tracerEndWidth = tracerStartWidth;
            }

            tracerMinVertexDistance = ClampFinite(tracerMinVertexDistance, 0.001f, Default.tracerMinVertexDistance);
            tracerCornerVertices = Mathf.Clamp(tracerCornerVertices, 0, 8);
            tracerCapVertices = Mathf.Clamp(tracerCapVertices, 0, 8);
            tracerHeadColor = ClampHdrColor(tracerHeadColor, Default.tracerHeadColor);
            tracerTailColor = ClampHdrColor(tracerTailColor, Default.tracerTailColor);
            muzzleForwardOffset = ClampFinite(muzzleForwardOffset, 0f, Default.muzzleForwardOffset);
            muzzleFlashScale = ClampFinite(muzzleFlashScale, 0.01f, Default.muzzleFlashScale);
            muzzleSmokeScale = ClampFinite(muzzleSmokeScale, 0.01f, Default.muzzleSmokeScale);
            clientProjectilePoolPrewarmCount = Mathf.Max(0, clientProjectilePoolPrewarmCount);
            clientProjectilePoolMaxInactive = Mathf.Max(1, clientProjectilePoolMaxInactive);
            clientImpactFxPoolPrewarmCount = Mathf.Max(0, clientImpactFxPoolPrewarmCount);
            clientImpactFxPoolMaxInactive = Mathf.Max(1, clientImpactFxPoolMaxInactive);
            muzzleFxPoolPrewarmCount = Mathf.Max(0, muzzleFxPoolPrewarmCount);
            muzzleFxPoolMaxInactive = Mathf.Max(1, muzzleFxPoolMaxInactive);
        }

        public void CopyFrom(ClientProjectileVisualSettings source)
        {
            if (source == null)
            {
                return;
            }

            projectilePrefab = source.projectilePrefab;
            projectileMaterial = source.projectileMaterial;
            tracerMaterial = source.tracerMaterial;
            muzzleFlashPrefab = source.muzzleFlashPrefab;
            muzzleSmokePrefab = source.muzzleSmokePrefab;
            overrideProjectileMaterial = source.overrideProjectileMaterial;
            projectileBaseColor = source.projectileBaseColor;
            projectileEmissionColor = source.projectileEmissionColor;
            projectileEmissionIntensity = source.projectileEmissionIntensity;
            tracerEnabled = source.tracerEnabled;
            tracerLifetime = source.tracerLifetime;
            tracerStartWidth = source.tracerStartWidth;
            tracerEndWidth = source.tracerEndWidth;
            tracerMinVertexDistance = source.tracerMinVertexDistance;
            tracerCornerVertices = source.tracerCornerVertices;
            tracerCapVertices = source.tracerCapVertices;
            tracerHeadColor = source.tracerHeadColor;
            tracerTailColor = source.tracerTailColor;
            muzzleFxEnabled = source.muzzleFxEnabled;
            muzzleForwardOffset = source.muzzleForwardOffset;
            muzzleFlashScale = source.muzzleFlashScale;
            muzzleSmokeScale = source.muzzleSmokeScale;
            clientProjectilePoolPrewarmCount = source.clientProjectilePoolPrewarmCount;
            clientProjectilePoolMaxInactive = source.clientProjectilePoolMaxInactive;
            clientImpactFxPoolPrewarmCount = source.clientImpactFxPoolPrewarmCount;
            clientImpactFxPoolMaxInactive = source.clientImpactFxPoolMaxInactive;
            muzzleFxPoolPrewarmCount = source.muzzleFxPoolPrewarmCount;
            muzzleFxPoolMaxInactive = source.muzzleFxPoolMaxInactive;
        }

        private static Color ClampHdrColor(Color value, Color fallback)
        {
            if (!IsFinite(value.r) || !IsFinite(value.g) || !IsFinite(value.b) || !IsFinite(value.a))
            {
                return fallback;
            }

            return new Color(
                Mathf.Max(0f, value.r),
                Mathf.Max(0f, value.g),
                Mathf.Max(0f, value.b),
                Mathf.Clamp01(value.a));
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

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    [System.Serializable]
    public class ClientFramePacingSettings
    {
        public const int MinTargetFrameRate = 60;
        public const int MaxTargetFrameRate = 240;
        public const int DefaultTargetFrameRate = 144;
        public const bool DefaultVerticalSyncEnabled = false;
        private static readonly int[] SupportedTargetFrameRates =
        {
            60,
            144,
            240
        };

        [Tooltip("Enable vertical synchronization. When enabled, Unity controls pacing from the display refresh rate.")]
        public bool verticalSyncEnabled = DefaultVerticalSyncEnabled;

        [Tooltip("Client target frame rate used when vertical synchronization is disabled.")]
        [Range(MinTargetFrameRate, MaxTargetFrameRate)]
        public int targetFrameRate = DefaultTargetFrameRate;

        public void Validate()
        {
            targetFrameRate = ClampTargetFrameRate(targetFrameRate);
        }

        public void Apply()
        {
            Apply(targetFrameRate, verticalSyncEnabled);
        }

        public void CopyFrom(ClientFramePacingSettings source)
        {
            if (source == null)
            {
                return;
            }

            verticalSyncEnabled = source.verticalSyncEnabled;
            targetFrameRate = ClampTargetFrameRate(source.targetFrameRate);
        }

        public static int ClampTargetFrameRate(int value)
        {
            int bestValue = SupportedTargetFrameRates[0];
            int bestDistance = Mathf.Abs(value - bestValue);
            for (int i = 1; i < SupportedTargetFrameRates.Length; i++)
            {
                int candidate = SupportedTargetFrameRates[i];
                int distance = Mathf.Abs(value - candidate);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestValue = candidate;
                }
            }

            return bestValue;
        }

        public static int SupportedTargetFrameRateCount
        {
            get
            {
                return SupportedTargetFrameRates.Length;
            }
        }

        public static int GetSupportedTargetFrameRate(int index)
        {
            if (index < 0 || index >= SupportedTargetFrameRates.Length)
            {
                return DefaultTargetFrameRate;
            }

            return SupportedTargetFrameRates[index];
        }

        public static bool IsSupportedTargetFrameRate(int value)
        {
            for (int i = 0; i < SupportedTargetFrameRates.Length; i++)
            {
                if (SupportedTargetFrameRates[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        public static void Apply(int targetFrameRate, bool verticalSyncEnabled)
        {
            int safeTargetFrameRate = ClampTargetFrameRate(targetFrameRate);
            QualitySettings.vSyncCount = verticalSyncEnabled ? 1 : 0;
            Application.targetFrameRate = verticalSyncEnabled ? -1 : safeTargetFrameRate;
        }
    }

    public class ClientSettings : MonoBehaviour
    {
        public static ClientSettings In;

        [Tooltip("Client frame pacing settings for production client builds.")]
        public ClientFramePacingSettings framePacing = new ClientFramePacingSettings();

        [Tooltip("Client-only projectile, tracer and muzzle visual settings. The server does not read or synchronize these.")]
        public ClientProjectileVisualSettings projectileVisuals = new ClientProjectileVisualSettings();

        [Tooltip("Локальні клієнтські runtime-налаштування HUD, карти та автоприцілу. Сервер їх не читає і не синхронізує.")]
        public GameplayRuntimeSettings gameplayRuntime = new GameplayRuntimeSettings();

        private void Awake()
        {
            ValidateSettings();
            In = this;
            ApplyFramePacing();
        }

        private void OnValidate()
        {
            ValidateSettings();
        }

        private void OnDestroy()
        {
            if (In == this)
            {
                In = null;
            }
        }

        public static GameplayRuntimeSettings GetGameplayRuntime()
        {
            if (In == null || In.gameplayRuntime == null)
            {
                return GameplayRuntimeSettings.Default;
            }

            In.gameplayRuntime.Validate();
            return In.gameplayRuntime;
        }

        public static ClientFramePacingSettings GetFramePacing()
        {
            if (In == null || In.framePacing == null)
            {
                return null;
            }

            In.framePacing.Validate();
            return In.framePacing;
        }

        public static ClientProjectileVisualSettings GetProjectileVisuals()
        {
            if (In == null || In.projectileVisuals == null)
            {
                return ClientProjectileVisualSettings.Default;
            }

            In.projectileVisuals.Validate();
            return In.projectileVisuals;
        }

        public static void ApplyFramePacing(int targetFrameRate, bool verticalSyncEnabled)
        {
            if (In != null)
            {
                if (In.framePacing == null)
                {
                    In.framePacing = new ClientFramePacingSettings();
                }

                In.framePacing.targetFrameRate = ClientFramePacingSettings.ClampTargetFrameRate(targetFrameRate);
                In.framePacing.verticalSyncEnabled = verticalSyncEnabled;
            }

            ClientFramePacingSettings.Apply(targetFrameRate, verticalSyncEnabled);
        }

        private void ApplyFramePacing()
        {
            if (framePacing == null)
            {
                framePacing = new ClientFramePacingSettings();
            }

            framePacing.Validate();
            framePacing.Apply();
        }

        private void ValidateSettings()
        {
            if (framePacing != null)
            {
                framePacing.Validate();
            }

            if (projectileVisuals != null)
            {
                projectileVisuals.Validate();
            }

            if (gameplayRuntime != null)
            {
                gameplayRuntime.Validate();
            }
        }
    }

    public static class GameplayRuntimeSettingsProvider
    {
        public static GameplayRuntimeSettings Get()
        {
            return ClientSettings.GetGameplayRuntime();
        }
    }

    public static class ClientProjectileVisualSettingsProvider
    {
        public static ClientProjectileVisualSettings Get()
        {
            return ClientSettings.GetProjectileVisuals();
        }
    }
}
