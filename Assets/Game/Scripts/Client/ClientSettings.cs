using UnityEngine;

namespace Game.Scripts.Client
{
    [System.Serializable]
    public class GameplayRuntimeSettings
    {
        private static readonly GameplayRuntimeSettings DefaultSettings = new GameplayRuntimeSettings();

        [Header("UI здоров'я")]
        [Tooltip("Колір смуги HP для свого робота та союзників у world-space UI техніки.")]
        public Color alliedHpColor = new Color(1f, 0.08f, 0.04f, 1f);
        [Tooltip("Колір смуги HP для противників у world-space UI техніки.")]
        public Color enemyHpColor = new Color(0.1f, 0.35f, 1f, 1f);
        [Tooltip("Як часто world-space UI техніки переоцінює команду цілі та оновлює колір HP.")]
        public float hpTeamColorRefreshInterval = 0.25f;
        [Tooltip("Швидкість згладження основної смуги HP локального гравця на GameplayHUD.")]
        public float ownerHealthBarSmoothSpeed = 10f;

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

    public class ClientSettings : MonoBehaviour
    {
        public static ClientSettings In;

        [Tooltip("Локальні клієнтські runtime-налаштування HUD, карти та автоприцілу. Сервер їх не читає і не синхронізує.")]
        public GameplayRuntimeSettings gameplayRuntime = new GameplayRuntimeSettings();

        private void Awake()
        {
            ValidateSettings();
            In = this;
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

        private void ValidateSettings()
        {
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
}
