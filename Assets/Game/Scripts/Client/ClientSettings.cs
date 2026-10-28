using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Client
{
    [System.Serializable]
    public class ClientButtonColorSettings
    {
        [Tooltip("Базовий колір UI-кнопки у звичайному стані.")]
        public Color normalColor = Color.white;
        [Tooltip("Колір UI-кнопки, коли курсор наведений на неї.")]
        public Color highlightedColor = new Color(0.88235295f, 0.88235295f, 0.88235295f, 1f);
        [Tooltip("Колір UI-кнопки, поки вона натиснута.")]
        public Color pressedColor = new Color(0.69803923f, 0.69803923f, 0.69803923f, 1f);
        [Tooltip("Колір UI-кнопки, коли її вибрано клавіатурою або геймпадом.")]
        public Color selectedColor = new Color(0.88235295f, 0.88235295f, 0.88235295f, 1f);
        [Tooltip("Колір UI-кнопки, коли вона недоступна для натискання.")]
        public Color disabledColor = new Color(0.52156866f, 0.52156866f, 0.52156866f, 0.5019608f);
        [Min(0f), Tooltip("Множник яскравості кольорів усіх станів UI-кнопок.")]
        public float colorMultiplier = 1f;
        [Min(0f), Tooltip("Тривалість плавної зміни кольору між станами UI-кнопки.")]
        public float fadeDuration = 0.1f;

        public void ApplyTo(ref ColorBlock colors)
        {
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.selectedColor = selectedColor;
            colors.disabledColor = disabledColor;
            colors.colorMultiplier = colorMultiplier;
            colors.fadeDuration = fadeDuration;
        }

        public void Validate()
        {
            if (float.IsNaN(colorMultiplier) || float.IsInfinity(colorMultiplier) || colorMultiplier < 0f)
            {
                colorMultiplier = 1f;
            }

            if (float.IsNaN(fadeDuration) || float.IsInfinity(fadeDuration) || fadeDuration < 0f)
            {
                fadeDuration = 0.1f;
            }
        }
    }

    [System.Serializable]
    public class CameraZoomProfile
    {
        [Tooltip("Zoom index: 0 is default, positive values zoom in, negative values zoom out.")]
        public int zoomIndex;
        [Min(0.01f)]
        public float cameraDistance = 10f;
        [Range(1f, 179f)]
        public float fieldOfView = 60f;
        [Min(0f)]
        [Tooltip("Базовий діаметр круга у повністю зведеному стані для цього zoom-профілю в UI-пікселях. Значення 0 або менше 1 відображаються як мінімальні 1x1 UI-піксель; при розкиді вище повністю зведеного діаметра bloom додається поверх бази.")]
        public float aimingCircleDiameter = 55f;
        [Tooltip("Показувати чорний круг снайперського прицілу з центральним прицілом. Для forward-профілю true позначає цей крок як снайперський.")]
        public bool showReticle = false;

        public CameraZoomProfile Clone()
        {
            return new CameraZoomProfile
            {
                zoomIndex = zoomIndex,
                cameraDistance = cameraDistance,
                fieldOfView = fieldOfView,
                aimingCircleDiameter = aimingCircleDiameter,
                showReticle = showReticle
            };
        }

        public void Validate(CameraZoomProfile fallback)
        {
            CameraZoomProfile safeFallback = fallback ?? new CameraZoomProfile();
            cameraDistance = ClampFinite(cameraDistance, 0.01f, safeFallback.cameraDistance);
            fieldOfView = Mathf.Clamp(ClampFinite(fieldOfView, 1f, safeFallback.fieldOfView), 1f, 179f);
            aimingCircleDiameter = ClampFinite(aimingCircleDiameter, 0f, safeFallback.aimingCircleDiameter);
        }

        private static float ClampFinite(float value, float minValue, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return Mathf.Max(minValue, fallback);
            }

            return Mathf.Max(minValue, value);
        }
    }

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
        [Header("Масштаб world-space HP")]
        [Tooltip("Увімкнути масштабування world-space смуг HP роботів залежно від дистанції.")]
        public bool worldHpBarDistanceScaleEnabled = true;
        [Tooltip("Дистанція, на якій смуги HP роботів використовують ближній масштаб.")]
        public float worldHpBarScaleMinDistance = 5f;
        [Tooltip("Дистанція, на якій смуги HP роботів досягають дальнього масштабу.")]
        public float worldHpBarScaleMaxDistance = 90f;
        [Tooltip("Множник масштабу смуги HP біля камери.")]
        public float worldHpBarMinDistanceScale = 1f;
        [Tooltip("Множник масштабу смуги HP далеко від камери.")]
        public float worldHpBarMaxDistanceScale = 5f;
        [Header("Прозорість world-space NameCanvas")]
        [Tooltip("Відстань до gameplay-камери, з якої NameCanvas починає плавно ставати прозорим.")]
        public float worldHpBarCanvasFadeStartDistance = 5f;
        [Tooltip("Відстань до gameplay-камери, на якій NameCanvas стає повністю прозорим.")]
        public float worldHpBarCanvasFullyTransparentDistance = 3f;

        [Header("Карта")]
        [Tooltip("Колір іконок союзників на мінікарті та повній карті.")]
        public Color mapAllyIconColor = new Color(0.1f, 0.35f, 1f, 1f);
        [Tooltip("Колір іконок противників на мінікарті та повній карті.")]
        public Color mapEnemyIconColor = new Color(1f, 0.08f, 0.04f, 1f);
        [Tooltip("Колір іконок знищених роботів на мінікарті та повній карті.")]
        public Color mapDestroyedIconColor = Color.black;
        [Tooltip("Масштаб лише фонового зображення мінікарти. 1 залишає базовий розмір сцени.")]
        public float mapMiniMapImageScale = 1f;
        [Tooltip("Додатковий горизонтальний множник масштабу фону мінікарти. Застосовується поверх mapMiniMapImageScale.")]
        public float mapMiniMapImageScaleX = 1f;
        [Tooltip("Додатковий вертикальний множник масштабу фону мінікарти. Застосовується поверх mapMiniMapImageScale.")]
        public float mapMiniMapImageScaleY = 1f;
        [Tooltip("Піксельний X/Y-офсет лише фонового зображення мінікарти відносно центру MiniMapRoot.")]
        public Vector2 mapMiniMapImageOffset = Vector2.zero;
        [Tooltip("Масштаб лише фонового зображення великої карти.")]
        public float mapFullMapImageScale = 1f;
        [Tooltip("Додатковий горизонтальний множник масштабу фону великої карти.")]
        public float mapFullMapImageScaleX = 1f;
        [Tooltip("Додатковий вертикальний множник масштабу фону великої карти.")]
        public float mapFullMapImageScaleY = 1f;
        [Tooltip("Піксельний X/Y-офсет лише фонового зображення великої карти.")]
        public Vector2 mapFullMapImageOffset = Vector2.zero;
        [Tooltip("Горизонтальний масштаб незалежного canvas-маркера роботів на великій карті.")]
        public float mapFullMapRobotCanvasScaleX = 1f;
        [Tooltip("Вертикальний масштаб незалежного canvas-маркера роботів на великій карті.")]
        public float mapFullMapRobotCanvasScaleY = 1f;
        [Tooltip("Піксельний X/Y-офсет canvas-маркера роботів на великій карті.")]
        public Vector2 mapFullMapRobotCanvasOffset = Vector2.zero;
        [Tooltip("Горизонтальний масштаб незалежного canvas-маркера роботів на мінікарті.")]
        public float mapMiniMapRobotCanvasScaleX = 1f;
        [Tooltip("Вертикальний масштаб незалежного canvas-маркера роботів на мінікарті.")]
        public float mapMiniMapRobotCanvasScaleY = 1f;
        [Tooltip("Піксельний X/Y-офсет незалежного canvas-маркера роботів відносно рамки мінікарти.")]
        public Vector2 mapMiniMapRobotCanvasOffset = Vector2.zero;
        [Tooltip("Глобальний масштаб усіх маркерів роботів на мінікарті, включно з іконкою локального гравця.")]
        public float mapMiniMapIconScale = 1f;
        [Tooltip("Додатковий горизонтальний множник масштабу маркерів роботів на мінікарті.")]
        public float mapMiniMapIconScaleX = 1f;
        [Tooltip("Додатковий вертикальний множник масштабу маркерів роботів на мінікарті.")]
        public float mapMiniMapIconScaleY = 1f;
        [Tooltip("Піксельний X/Y-офсет усіх маркерів роботів на мінікарті відносно їхньої розрахованої позиції.")]
        public Vector2 mapMiniMapIconOffset = Vector2.zero;
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
        [Tooltip("Єдиний масив zoom-профілів. Вкажіть zoomIndex: 0 — базовий, додатні значення — наближення, від'ємні — віддалення.")]
        public CameraZoomProfile[] cameraZoomProfiles =
        {
            new CameraZoomProfile { zoomIndex = 0, cameraDistance = 10f, fieldOfView = 60f },
            new CameraZoomProfile { zoomIndex = 1, cameraDistance = 6f, fieldOfView = 50f },
            new CameraZoomProfile { zoomIndex = 2, cameraDistance = 4f, fieldOfView = 45f },
            new CameraZoomProfile { zoomIndex = 3, cameraDistance = 2.5f, fieldOfView = 40f },
            new CameraZoomProfile { zoomIndex = 4, cameraDistance = 0.25f, fieldOfView = 24f, showReticle = true },
            new CameraZoomProfile { zoomIndex = -1, cameraDistance = 12f, fieldOfView = 68f },
            new CameraZoomProfile { zoomIndex = -2, cameraDistance = 15f, fieldOfView = 76f }
        };
        [Tooltip("Увімкнути примусове перемикання локального зуму з параметра runtimeZoomIndex під час Play Mode. Працює як колесо миші, але значення з інспектора має пріоритет.")]
        public bool runtimeZoomOverrideEnabled = false;
        [Tooltip("Поточний signed zoomIndex із cameraZoomProfiles, який примусово застосовується в реальному часі, коли runtimeZoomOverrideEnabled увімкнено. Наприклад: 0 — default, 1 — один крок наближення, -1 — один крок віддалення.")]
        public int runtimeZoomIndex = 0;
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
        [Tooltip("Швидкість згладження зміни FOV.")]
        public float cameraFovLerpSpeed = 10f;
        [Tooltip("Швидкість згладження дистанції камери.")]
        public float cameraDistanceLerpSpeed = 12f;
        [Tooltip("Мертва зона колеса миші для перемикання наближення.")]
        public float cameraScrollDeadZone = 0.001f;
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

        [Header("Outline під прицілом")]
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

            worldHpBarCanvasFadeStartDistance = ClampFinite(
                worldHpBarCanvasFadeStartDistance,
                0.01f,
                Default.worldHpBarCanvasFadeStartDistance);
            worldHpBarCanvasFullyTransparentDistance = ClampFinite(
                worldHpBarCanvasFullyTransparentDistance,
                0f,
                Default.worldHpBarCanvasFullyTransparentDistance);
            if (worldHpBarCanvasFullyTransparentDistance >= worldHpBarCanvasFadeStartDistance)
            {
                worldHpBarCanvasFullyTransparentDistance = Mathf.Max(
                    0f,
                    worldHpBarCanvasFadeStartDistance - 0.01f);
            }

            mapAllyIconColor = ClampColor(mapAllyIconColor, Default.mapAllyIconColor);
            mapEnemyIconColor = ClampColor(mapEnemyIconColor, Default.mapEnemyIconColor);
            mapDestroyedIconColor = ClampColor(mapDestroyedIconColor, Default.mapDestroyedIconColor);
            mapMiniMapImageScale = ClampFinite(mapMiniMapImageScale, 0.1f, Default.mapMiniMapImageScale);
            mapMiniMapImageScaleX = ClampFinite(mapMiniMapImageScaleX, 0.1f, Default.mapMiniMapImageScaleX);
            mapMiniMapImageScaleY = ClampFinite(mapMiniMapImageScaleY, 0.1f, Default.mapMiniMapImageScaleY);
            mapMiniMapImageOffset = ClampFinite(mapMiniMapImageOffset, Default.mapMiniMapImageOffset);
            mapFullMapImageScale = ClampFinite(mapFullMapImageScale, 0.1f, Default.mapFullMapImageScale);
            mapFullMapImageScaleX = ClampFinite(mapFullMapImageScaleX, 0.1f, Default.mapFullMapImageScaleX);
            mapFullMapImageScaleY = ClampFinite(mapFullMapImageScaleY, 0.1f, Default.mapFullMapImageScaleY);
            mapFullMapImageOffset = ClampFinite(mapFullMapImageOffset, Default.mapFullMapImageOffset);
            mapFullMapRobotCanvasScaleX = ClampFinite(mapFullMapRobotCanvasScaleX, 0.1f, Default.mapFullMapRobotCanvasScaleX);
            mapFullMapRobotCanvasScaleY = ClampFinite(mapFullMapRobotCanvasScaleY, 0.1f, Default.mapFullMapRobotCanvasScaleY);
            mapFullMapRobotCanvasOffset = ClampFinite(mapFullMapRobotCanvasOffset, Default.mapFullMapRobotCanvasOffset);
            mapMiniMapRobotCanvasScaleX = ClampFinite(mapMiniMapRobotCanvasScaleX, 0.1f, Default.mapMiniMapRobotCanvasScaleX);
            mapMiniMapRobotCanvasScaleY = ClampFinite(mapMiniMapRobotCanvasScaleY, 0.1f, Default.mapMiniMapRobotCanvasScaleY);
            mapMiniMapRobotCanvasOffset = ClampFinite(mapMiniMapRobotCanvasOffset, Default.mapMiniMapRobotCanvasOffset);
            mapMiniMapIconScale = ClampFinite(mapMiniMapIconScale, 0.1f, Default.mapMiniMapIconScale);
            mapMiniMapIconScaleX = ClampFinite(mapMiniMapIconScaleX, 0.1f, Default.mapMiniMapIconScaleX);
            mapMiniMapIconScaleY = ClampFinite(mapMiniMapIconScaleY, 0.1f, Default.mapMiniMapIconScaleY);
            mapMiniMapIconOffset = ClampFinite(mapMiniMapIconOffset, Default.mapMiniMapIconOffset);
            mapTrackedVehicleIconScale = ClampFinite(mapTrackedVehicleIconScale, 0.1f, Default.mapTrackedVehicleIconScale);
            mapTrackedVehicleRefreshInterval = ClampFinite(mapTrackedVehicleRefreshInterval, 0.1f, Default.mapTrackedVehicleRefreshInterval);
            autoAimMaxAcquireDistance = ClampFinite(autoAimMaxAcquireDistance, 0.1f, Default.autoAimMaxAcquireDistance);
            autoAimFallbackTargetHeight = ClampFinite(autoAimFallbackTargetHeight, 0f, Default.autoAimFallbackTargetHeight);
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

            cameraFovLerpSpeed = ClampFinite(cameraFovLerpSpeed, 0f, Default.cameraFovLerpSpeed);
            cameraDistanceLerpSpeed = ClampFinite(cameraDistanceLerpSpeed, 0f, Default.cameraDistanceLerpSpeed);
            cameraScrollDeadZone = ClampFinite(cameraScrollDeadZone, 0f, Default.cameraScrollDeadZone);
            ValidateCameraZoomProfiles();
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
            worldHpBarCanvasFadeStartDistance = source.worldHpBarCanvasFadeStartDistance;
            worldHpBarCanvasFullyTransparentDistance = source.worldHpBarCanvasFullyTransparentDistance;
            mapAllyIconColor = source.mapAllyIconColor;
            mapEnemyIconColor = source.mapEnemyIconColor;
            mapDestroyedIconColor = source.mapDestroyedIconColor;
            mapMiniMapImageScale = source.mapMiniMapImageScale;
            mapMiniMapImageScaleX = source.mapMiniMapImageScaleX;
            mapMiniMapImageScaleY = source.mapMiniMapImageScaleY;
            mapMiniMapImageOffset = source.mapMiniMapImageOffset;
            mapFullMapImageScale = source.mapFullMapImageScale;
            mapFullMapImageScaleX = source.mapFullMapImageScaleX;
            mapFullMapImageScaleY = source.mapFullMapImageScaleY;
            mapFullMapImageOffset = source.mapFullMapImageOffset;
            mapFullMapRobotCanvasScaleX = source.mapFullMapRobotCanvasScaleX;
            mapFullMapRobotCanvasScaleY = source.mapFullMapRobotCanvasScaleY;
            mapFullMapRobotCanvasOffset = source.mapFullMapRobotCanvasOffset;
            mapMiniMapRobotCanvasScaleX = source.mapMiniMapRobotCanvasScaleX;
            mapMiniMapRobotCanvasScaleY = source.mapMiniMapRobotCanvasScaleY;
            mapMiniMapRobotCanvasOffset = source.mapMiniMapRobotCanvasOffset;
            mapMiniMapIconScale = source.mapMiniMapIconScale;
            mapMiniMapIconScaleX = source.mapMiniMapIconScaleX;
            mapMiniMapIconScaleY = source.mapMiniMapIconScaleY;
            mapMiniMapIconOffset = source.mapMiniMapIconOffset;
            mapRotateIcons = source.mapRotateIcons;
            mapTrackedVehicleIconScale = source.mapTrackedVehicleIconScale;
            mapTrackedVehicleRefreshInterval = source.mapTrackedVehicleRefreshInterval;
            mapFullMapKey = source.mapFullMapKey;
            autoAimMaxAcquireDistance = source.autoAimMaxAcquireDistance;
            autoAimFallbackTargetHeight = source.autoAimFallbackTargetHeight;
            autoAimRejectSameTeam = source.autoAimRejectSameTeam;
            autoAimPreferTurretTarget = source.autoAimPreferTurretTarget;
            cameraZoomProfiles = CloneZoomProfiles(source.cameraZoomProfiles);
            runtimeZoomOverrideEnabled = source.runtimeZoomOverrideEnabled;
            runtimeZoomIndex = source.runtimeZoomIndex;
            cameraSniperFromMuzzle = source.cameraSniperFromMuzzle;
            cameraSniperForwardOffset = source.cameraSniperForwardOffset;
            cameraSniperVerticalOffset = source.cameraSniperVerticalOffset;
            cameraHorizontalSpeed = source.cameraHorizontalSpeed;
            cameraVerticalSpeed = source.cameraVerticalSpeed;
            cameraMinPitch = source.cameraMinPitch;
            cameraMaxPitch = source.cameraMaxPitch;
            cameraFovLerpSpeed = source.cameraFovLerpSpeed;
            cameraDistanceLerpSpeed = source.cameraDistanceLerpSpeed;
            cameraScrollDeadZone = source.cameraScrollDeadZone;
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
            floatingDamageTextDuration = source.floatingDamageTextDuration;
            floatingDamageTextMoveUp = source.floatingDamageTextMoveUp;
            floatingDamageTextEndScale = source.floatingDamageTextEndScale;
            sniperScopeOverlayFadeDuration = source.sniperScopeOverlayFadeDuration;
        }

        private void ValidateCameraZoomProfiles()
        {
            cameraZoomProfiles = ValidateZoomProfiles(cameraZoomProfiles, Default.cameraZoomProfiles);
        }

        private static CameraZoomProfile[] ValidateZoomProfiles(
            CameraZoomProfile[] profiles,
            CameraZoomProfile[] fallbackProfiles)
        {
            if (profiles == null)
            {
                return CloneZoomProfiles(fallbackProfiles);
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] == null)
                {
                    profiles[i] = fallbackProfiles != null && i < fallbackProfiles.Length
                        ? fallbackProfiles[i].Clone()
                        : new CameraZoomProfile();
                }

                CameraZoomProfile fallback = FindZoomProfile(fallbackProfiles, profiles[i].zoomIndex)
                    ?? FindZoomProfile(Default.cameraZoomProfiles, 0)
                    ?? new CameraZoomProfile();
                profiles[i].Validate(fallback);
            }

            return profiles;
        }

        private static CameraZoomProfile FindZoomProfile(CameraZoomProfile[] profiles, int zoomIndex)
        {
            if (profiles == null)
            {
                return null;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] != null && profiles[i].zoomIndex == zoomIndex)
                {
                    return profiles[i];
                }
            }

            return null;
        }

        private static CameraZoomProfile[] CloneZoomProfiles(CameraZoomProfile[] profiles)
        {
            if (profiles == null)
            {
                return null;
            }

            CameraZoomProfile[] clone = new CameraZoomProfile[profiles.Length];
            for (int i = 0; i < profiles.Length; i++)
            {
                clone[i] = profiles[i] != null ? profiles[i].Clone() : new CameraZoomProfile();
            }

            return clone;
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

        private static Vector2 ClampFinite(Vector2 value, Vector2 fallback)
        {
            return new Vector2(
                ClampFinite(value.x, fallback.x),
                ClampFinite(value.y, fallback.y));
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

        [Header("Посилання візуального снаряда")]
        [Tooltip("Клієнтська заміна prefab снаряда. Залиште порожнім, щоб використовувати prefab зброї.")]
        public Projectile projectilePrefab;
        [Tooltip("Матеріал, який застосовується до mesh снаряда на клієнті.")]
        public Material projectileMaterial;
        [Tooltip("Матеріал хвоста-трасера снаряда.")]
        public Material tracerMaterial;
        [Tooltip("Одноразовий ефект спалаху пострілу біля дула.")]
        public PooledImpactFx muzzleFlashPrefab;
        [Tooltip("Одноразовий ефект диму біля дула.")]
        public PooledImpactFx muzzleSmokePrefab;

        [Header("Світіння снаряда")]
        public bool overrideProjectileMaterial = true;
        [ColorUsage(false, true)] public Color projectileBaseColor = new Color(1f, 0.58f, 0.16f, 1f);
        [ColorUsage(false, true)] public Color projectileEmissionColor = new Color(1f, 0.38f, 0.06f, 1f);
        [Min(0f)] public float projectileEmissionIntensity = 4f;

        [Header("Трасер")]
        public bool tracerEnabled = true;
        [Tooltip("Скільки секунд трасер залишається видимим. Менше значення робить трасер коротшим.")]
        [InspectorName("Тривалість трасера, с")]
        [Min(0.01f)] public float tracerLifetime = 0.06f;
        [Tooltip("Товщина трасера біля голови снаряда.")]
        [Min(0.001f)] public float tracerStartWidth = 0.07f;
        [Tooltip("Товщина трасера на згасаючому кінці хвоста.")]
        [Min(0f)] public float tracerEndWidth = 0.012f;
        [Tooltip("Дистанція між точками хвоста. Більше значення дешевше, але дає менше деталей.")]
        [Min(0.001f)] public float tracerMinVertexDistance = 0.08f;
        [Range(0, 8)] public int tracerCornerVertices;
        [Range(0, 8)] public int tracerCapVertices;
        [Tooltip("Глобальний множник прозорості градієнта трасера.")]
        [Range(0f, 1f)] public float tracerOpacity = 1f;
        [Tooltip("Alpha матеріалу трасера. Залишайте високим, якщо основне налаштування прозорості виконує tracerOpacity.")]
        [Range(0f, 1f)] public float tracerMaterialAlpha = 0.75f;
        [Tooltip("Позиція середньої точки згасання трасера. 0 біля голови снаряда, 1 біля хвоста.")]
        [Range(0.01f, 0.99f)] public float tracerFadeMidpoint = 0.55f;
        [Tooltip("Множник прозорості в tracerFadeMidpoint відносно alpha tracerHeadColor.")]
        [Range(0f, 1f)] public float tracerMidOpacityMultiplier = 0.45f;
        [ColorUsage(false, true)] public Color tracerHeadColor = new Color(1f, 0.82f, 0.35f, 0.42f);
        [ColorUsage(false, true)] public Color tracerTailColor = new Color(1f, 0.45f, 0.1f, 0f);
        [ColorUsage(false, true)] public Color tracerEmissionColor = new Color(1f, 0.55f, 0.16f, 1f);
        [Min(0f)] public float tracerEmissionIntensity = 1.35f;

        [Header("Ефекти дула")]
        public bool muzzleFxEnabled = true;
        [Tooltip("Не створювати локальні спалах і дим біля дула, поки власник перебуває в повному снайперському режимі.")]
        public bool hideMuzzleFxInSniperMode = true;
        [Min(0f)] public float muzzleForwardOffset = 0.25f;
        [Min(0.01f)] public float muzzleFlashScale = 0.55f;
        [Min(0.01f)] public float muzzleSmokeScale = 0.65f;

        [Header("Пули візуальних ефектів")]
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
            tracerOpacity = Mathf.Clamp01(ClampFinite(tracerOpacity, 0f, Default.tracerOpacity));
            tracerMaterialAlpha = Mathf.Clamp01(ClampFinite(tracerMaterialAlpha, 0f, Default.tracerMaterialAlpha));
            tracerFadeMidpoint = Mathf.Clamp(ClampFinite(tracerFadeMidpoint, 0.01f, Default.tracerFadeMidpoint), 0.01f, 0.99f);
            tracerMidOpacityMultiplier = Mathf.Clamp01(ClampFinite(tracerMidOpacityMultiplier, 0f, Default.tracerMidOpacityMultiplier));
            tracerHeadColor = ClampHdrColor(tracerHeadColor, Default.tracerHeadColor);
            tracerTailColor = ClampHdrColor(tracerTailColor, Default.tracerTailColor);
            tracerEmissionColor = ClampHdrColor(tracerEmissionColor, Default.tracerEmissionColor);
            tracerEmissionIntensity = ClampFinite(tracerEmissionIntensity, 0f, Default.tracerEmissionIntensity);
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
            tracerOpacity = source.tracerOpacity;
            tracerMaterialAlpha = source.tracerMaterialAlpha;
            tracerFadeMidpoint = source.tracerFadeMidpoint;
            tracerMidOpacityMultiplier = source.tracerMidOpacityMultiplier;
            tracerHeadColor = source.tracerHeadColor;
            tracerTailColor = source.tracerTailColor;
            tracerEmissionColor = source.tracerEmissionColor;
            tracerEmissionIntensity = source.tracerEmissionIntensity;
            muzzleFxEnabled = source.muzzleFxEnabled;
            hideMuzzleFxInSniperMode = source.hideMuzzleFxInSniperMode;
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

        [Tooltip("Увімкнути вертикальну синхронізацію. Коли вона активна, Unity визначає темп кадрів за частотою оновлення дисплея.")]
        public bool verticalSyncEnabled = DefaultVerticalSyncEnabled;

        [Tooltip("Цільова частота кадрів клієнта, яка використовується коли вертикальну синхронізацію вимкнено.")]
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

        [Tooltip("Клієнтські налаштування темпу кадрів для production-збірок.")]
        public ClientFramePacingSettings framePacing = new ClientFramePacingSettings();

        [Tooltip("Клієнтські візуальні налаштування снаряда, трасера та ефектів дула. Сервер їх не читає і не синхронізує.")]
        public ClientProjectileVisualSettings projectileVisuals = new ClientProjectileVisualSettings();

        [Tooltip("Локальні клієнтські runtime-налаштування HUD, карти та автоприцілу. Сервер їх не читає і не синхронізує.")]
        public GameplayRuntimeSettings gameplayRuntime = new GameplayRuntimeSettings();

        [Tooltip("Єдина палітра станів UI-кнопок. Застосуйте її до Client і UI-префабів кнопкою в інспекторі ClientSettings.")]
        public ClientButtonColorSettings buttonColors = new ClientButtonColorSettings();

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

            if (buttonColors != null)
            {
                buttonColors.Validate();
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
