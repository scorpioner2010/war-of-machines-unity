using System.Collections.Generic;
using Game.Scripts.Client;
using UnityEditor;
using UnityEngine;

namespace Toras.Editor
{
    [CustomEditor(typeof(ClientSettings))]
    public class ClientSettingsEditor : UnityEditor.Editor
    {
        private static readonly IReadOnlyDictionary<string, string> Documentation =
            new Dictionary<string, string>
            {
                ["buttonColors"] =
                    "Єдина палітра станів UI-кнопок. Наприклад: після зміни pressedColor на помаранчевий натисніть Apply Button Colors To All UI Buttons, щоб оновити Client та UI-префаби.",
                ["buttonColors.normalColor"] =
                    "Колір самої кнопки без наведення чи вибору. Наприклад: білий залишає кнопку нейтральною.",
                ["buttonColors.highlightedColor"] =
                    "Колір кнопки під курсором. Наприклад: світліший відтінок показує гравцеві, що кнопка готова до натискання.",
                ["buttonColors.pressedColor"] =
                    "Колір кнопки під час утримання лівої кнопки миші. Наприклад: темніший відтінок робить натиснення помітним.",
                ["buttonColors.selectedColor"] =
                    "Колір кнопки після вибору клавіатурою або геймпадом. Наприклад: відтінок, відмінний від normalColor, показує фокус UI.",
                ["buttonColors.disabledColor"] =
                    "Колір недоступної кнопки та її тексту/іконок. Наприклад: напівпрозорий сірий показує відсутність дії.",
                ["buttonColors.colorMultiplier"] =
                    "Множник яскравості для всіх кольорів станів. Наприклад: 1 використовує колір без посилення.",
                ["buttonColors.fadeDuration"] =
                    "Час плавного переходу між станами кнопки та її тексту/іконок. Наприклад: 0.1 дає швидку, але помітну анімацію.",
                ["framePacing"] =
                    "Локальні параметри частоти кадрів клієнта. Наприклад: розгорніть групу, щоб обмежити клієнт 144 FPS без VSync.",
                ["projectileVisuals"] =
                    "Локальні візуальні параметри снаряда, трасера, ефектів дула та їхніх пулів; сервер їх не читає. Наприклад: розгорніть групу, щоб зробити трасер коротшим.",
                ["gameplayRuntime"] =
                    "Локальні runtime-параметри HUD, карти, автоприцілу, камери та підсвічування цілі. Наприклад: розгорніть групу, щоб змінити клавішу повної карти.",

                ["framePacing.verticalSyncEnabled"] =
                    "Вмикає вертикальну синхронізацію; тоді темп кадрів визначається частотою монітора, а targetFrameRate не використовується як ліміт. Наприклад: увімкніть для монітора 60 Гц, якщо потрібно уникнути розривів кадру.",
                ["framePacing.targetFrameRate"] =
                    "Цільова частота кадрів клієнта, коли VSync вимкнено; валідні значення нормалізуються до 60, 144 або 240. Наприклад: 144 обмежує клієнт приблизно 144 кадрами за секунду.",

                ["projectileVisuals.projectilePrefab"] =
                    "Необов'язкова клієнтська заміна prefab візуального снаряда; порожнє поле використовує prefab зброї. Наприклад: задайте тестовий prefab, щоб перевірити нову модель без зміни серверної стрільби.",
                ["projectileVisuals.projectileMaterial"] =
                    "Матеріал mesh візуального снаряда на клієнті. Наприклад: призначте emissive-матеріал, щоб снаряд краще читався в темній сцені.",
                ["projectileVisuals.tracerMaterial"] =
                    "Матеріал хвоста-трасера візуального снаряда. Наприклад: призначте прозорий additive-матеріал для яскравого сліду.",
                ["projectileVisuals.muzzleFlashPrefab"] =
                    "Одноразовий клієнтський ефект спалаху біля дула. Наприклад: призначте prefab короткого вогняного спалаху.",
                ["projectileVisuals.muzzleSmokePrefab"] =
                    "Одноразовий клієнтський ефект диму біля дула. Наприклад: призначте prefab невеликої хмари диму після пострілу.",
                ["projectileVisuals.overrideProjectileMaterial"] =
                    "Дозволяє runtime-коду перевизначати базовий колір і emission матеріалу снаряда. Наприклад: увімкніть, щоб застосувалися projectileBaseColor та projectileEmissionColor.",
                ["projectileVisuals.projectileBaseColor"] =
                    "Базовий HDR-колір візуального снаряда при overrideProjectileMaterial. Наприклад: помаранчевий колір робить shell помітним на фоні неба.",
                ["projectileVisuals.projectileEmissionColor"] =
                    "HDR-колір світіння візуального снаряда. Наприклад: теплий жовтогарячий emission імітує розпечений shell.",
                ["projectileVisuals.projectileEmissionIntensity"] =
                    "Інтенсивність світіння візуального снаряда. Наприклад: 4 робить emission помітним без зміни фізики пострілу.",
                ["projectileVisuals.tracerEnabled"] =
                    "Вмикає локальний хвіст-трасер за візуальним снарядом. Наприклад: вимкніть, щоб порівняти читабельність польоту без сліду.",
                ["projectileVisuals.tracerLifetime"] =
                    "Тривалість видимості точок трасера в секундах; менше значення робить слід коротшим. Наприклад: 0.06 залишає короткий хвіст приблизно на 60 мілісекунд.",
                ["projectileVisuals.tracerStartWidth"] =
                    "Товщина трасера біля голови снаряда. Наприклад: 0.07 робить передню частину сліду ширшою за хвіст.",
                ["projectileVisuals.tracerEndWidth"] =
                    "Товщина трасера на згасаючому кінці хвоста. Наприклад: 0.012 звужує слід майже до тонкої лінії.",
                ["projectileVisuals.tracerMinVertexDistance"] =
                    "Мінімальна відстань між точками TrailRenderer; більше значення дешевше, але менш деталізоване. Наприклад: 0.08 не створює зайві точки на кожному малому зміщенні.",
                ["projectileVisuals.tracerCornerVertices"] =
                    "Кількість додаткових вершин для заокруглення кутів трасера. Наприклад: 0 дешевше, а 2 робить різкі вигини плавнішими.",
                ["projectileVisuals.tracerCapVertices"] =
                    "Кількість додаткових вершин для заокруглення кінців трасера. Наприклад: 0 залишає прості кінці, а 2 робить їх м'якшими.",
                ["projectileVisuals.tracerOpacity"] =
                    "Глобальний множник прозорості градієнта трасера. Наприклад: 0.5 робить увесь слід удвічі прозорішим.",
                ["projectileVisuals.tracerMaterialAlpha"] =
                    "Alpha, що застосовується до матеріалу трасера. Наприклад: 0.75 залишає матеріал яскравим, а основне налаштування прозорості виконує tracerOpacity.",
                ["projectileVisuals.tracerFadeMidpoint"] =
                    "Позиція середньої точки згасання вздовж трасера: 0 біля снаряда, 1 біля хвоста. Наприклад: 0.55 переносить основне згасання трохи за середину сліду.",
                ["projectileVisuals.tracerMidOpacityMultiplier"] =
                    "Множник прозорості в tracerFadeMidpoint відносно alpha голови трасера. Наприклад: 0.45 залишає в середині 45 відсотків початкової видимості.",
                ["projectileVisuals.tracerHeadColor"] =
                    "HDR-колір голови трасера біля снаряда. Наприклад: світло-жовтий із помітним alpha робить початок сліду яскравим.",
                ["projectileVisuals.tracerTailColor"] =
                    "HDR-колір згасаючого хвоста трасера. Наприклад: помаранчевий з alpha 0 плавно розчиняє кінець сліду.",
                ["projectileVisuals.tracerEmissionColor"] =
                    "HDR-колір світіння матеріалу трасера. Наприклад: теплий помаранчевий emission підсилює видимість сліду.",
                ["projectileVisuals.tracerEmissionIntensity"] =
                    "Інтенсивність світіння трасера. Наприклад: 1.35 додає помірне світіння без надмірної яскравості.",
                ["projectileVisuals.muzzleFxEnabled"] =
                    "Вмикає локальні ефекти спалаху та диму біля дула. Наприклад: вимкніть для оцінки вартості кадру без muzzle FX.",
                ["projectileVisuals.hideMuzzleFxInSniperMode"] =
                    "Приховує локальні ефекти дула власника у повному снайперському режимі. Наприклад: увімкніть, щоб спалах не перекривав приціл від першої особи.",
                ["projectileVisuals.muzzleForwardOffset"] =
                    "Зміщення muzzle FX вперед від дула в метрах. Наприклад: 0.25 виносить спалах трохи перед моделлю гармати.",
                ["projectileVisuals.muzzleFlashScale"] =
                    "Масштаб prefab спалаху біля дула. Наприклад: 0.55 робить спалах трохи більшим за половину базового розміру.",
                ["projectileVisuals.muzzleSmokeScale"] =
                    "Масштаб prefab диму біля дула. Наприклад: 0.65 залишає дим компактним біля гармати.",
                ["projectileVisuals.clientProjectilePoolPrewarmCount"] =
                    "Кількість візуальних снарядів, створених у пулі заздалегідь. Наприклад: 16 готує шістнадцять shell-об'єктів до початку активної стрільби.",
                ["projectileVisuals.clientProjectilePoolMaxInactive"] =
                    "Максимальна кількість неактивних візуальних снарядів у пулі. Наприклад: 64 не зберігає безмежну кількість завершених shell-об'єктів.",
                ["projectileVisuals.clientImpactFxPoolPrewarmCount"] =
                    "Кількість impact FX, створених у пулі заздалегідь. Наприклад: 16 зменшує створення ефектів під час першої серії влучань.",
                ["projectileVisuals.clientImpactFxPoolMaxInactive"] =
                    "Максимальна кількість неактивних impact FX у пулі. Наприклад: 64 обмежує пам'ять після великої кількості влучань.",
                ["projectileVisuals.muzzleFxPoolPrewarmCount"] =
                    "Кількість muzzle FX, створених у пулі заздалегідь. Наприклад: 8 готує ефекти для першої короткої черги.",
                ["projectileVisuals.muzzleFxPoolMaxInactive"] =
                    "Максимальна кількість неактивних muzzle FX у пулі. Наприклад: 32 залишає запас ефектів, але обмежує накопичення об'єктів.",

                ["gameplayRuntime.alliedHpColor"] =
                    "Колір world-space смуги HP своєї машини та союзників. Наприклад: синій відрізняє дружні машини від червоних ворогів.",
                ["gameplayRuntime.enemyHpColor"] =
                    "Колір world-space смуги HP противників. Наприклад: червоний одразу позначає ворожу техніку.",
                ["gameplayRuntime.hpTeamColorRefreshInterval"] =
                    "Інтервал повторної перевірки команди та кольору world-space HP. Наприклад: 0.25 оновлює колір до чотирьох разів за секунду.",
                ["gameplayRuntime.ownerHealthBarSmoothSpeed"] =
                    "Швидкість згладжування основної смуги HP локального гравця на HUD. Наприклад: 10 плавно наздоганяє нове значення після отримання шкоди.",
                ["gameplayRuntime.worldHpBarDistanceScaleEnabled"] =
                    "Вмикає зміну масштабу world-space HP-смуг залежно від дистанції до камери. Наприклад: увімкніть, щоб далекі смуги залишалися читабельними.",
                ["gameplayRuntime.worldHpBarScaleMinDistance"] =
                    "Дистанція, на якій HP-смуга використовує ближній масштаб. Наприклад: 5 залишає стандартний розмір у межах п'яти метрів.",
                ["gameplayRuntime.worldHpBarScaleMaxDistance"] =
                    "Дистанція, на якій HP-смуга досягає дальнього масштабу. Наприклад: 90 завершує збільшення для машини за 90 метрів.",
                ["gameplayRuntime.worldHpBarMinDistanceScale"] =
                    "Множник масштабу HP-смуги біля камери. Наприклад: 1 залишає початковий розмір prefab.",
                ["gameplayRuntime.worldHpBarMaxDistanceScale"] =
                    "Множник масштабу HP-смуги далеко від камери. Наприклад: 5 робить далеку смугу вп'ятеро більшою за базовий розмір.",
                ["gameplayRuntime.mapAllyIconColor"] =
                    "Колір іконок союзників на мінікарті та повній карті. Наприклад: синій відповідає дружнім HP-смугам.",
                ["gameplayRuntime.mapEnemyIconColor"] =
                    "Колір іконок видимих противників на карті. Наприклад: червоний чітко відділяє ворога від союзників.",
                ["gameplayRuntime.mapDestroyedIconColor"] =
                    "Колір іконок знищених машин на карті. Наприклад: чорний показує, що машина більше не активна.",
                ["gameplayRuntime.mapMiniMapImageScale"] =
                    "Масштаб лише фонового зображення мінікарти. Наприклад: 0.8 робить карту меншою всередині її рамки без зміни canvas маркерів.",
                ["gameplayRuntime.mapMiniMapImageScaleX"] =
                    "Додатковий горизонтальний множник масштабу фону мінікарти. Наприклад: 0.95 трохи стискає карту по X.",
                ["gameplayRuntime.mapMiniMapImageScaleY"] =
                    "Додатковий вертикальний множник масштабу фону мінікарти. Наприклад: 1.05 трохи розтягує карту по Y.",
                ["gameplayRuntime.mapMiniMapImageOffset"] =
                    "Піксельний X/Y-офсет лише фонового зображення мінікарти відносно центру рамки.",
                ["gameplayRuntime.mapFullMapImageScale"] =
                    "Масштаб лише фонового зображення великої карти; не змінює canvas маркерів.",
                ["gameplayRuntime.mapFullMapImageScaleX"] =
                    "Додатковий горизонтальний масштаб фонового зображення великої карти.",
                ["gameplayRuntime.mapFullMapImageScaleY"] =
                    "Додатковий вертикальний масштаб фонового зображення великої карти.",
                ["gameplayRuntime.mapFullMapImageOffset"] =
                    "Піксельний X/Y-офсет лише фонового зображення великої карти.",
                ["gameplayRuntime.mapFullMapRobotCanvasScaleX"] =
                    "Горизонтальний масштаб незалежного canvas маркерів великої карти.",
                ["gameplayRuntime.mapFullMapRobotCanvasScaleY"] =
                    "Вертикальний масштаб незалежного canvas маркерів великої карти.",
                ["gameplayRuntime.mapFullMapRobotCanvasOffset"] =
                    "Піксельний X/Y-офсет незалежного canvas маркерів великої карти.",
                ["gameplayRuntime.mapMiniMapRobotCanvasScaleX"] =
                    "Горизонтальний масштаб незалежного canvas-маркера роботів. Наприклад: 0.95 стискає координатну область маркерів по X без зміни картинки.",
                ["gameplayRuntime.mapMiniMapRobotCanvasScaleY"] =
                    "Вертикальний масштаб незалежного canvas-маркера роботів. Наприклад: 1.05 розтягує координатну область маркерів по Y без зміни картинки.",
                ["gameplayRuntime.mapMiniMapRobotCanvasOffset"] =
                    "Піксельний X/Y-офсет незалежного canvas-маркера роботів відносно рамки мінікарти.",
                ["gameplayRuntime.mapMiniMapIconScale"] =
                    "Глобальний масштаб усіх маркерів роботів на мінікарті, включно з локальним гравцем. Наприклад: 0.75 робить маркери компактнішими.",
                ["gameplayRuntime.mapMiniMapIconScaleX"] =
                    "Додатковий горизонтальний множник масштабу маркерів роботів. Наприклад: 0.8 робить canvas маркерів вужчим.",
                ["gameplayRuntime.mapMiniMapIconScaleY"] =
                    "Додатковий вертикальний множник масштабу маркерів роботів. Наприклад: 1.2 робить canvas маркерів вищим.",
                ["gameplayRuntime.mapMiniMapIconOffset"] =
                    "Піксельний X/Y-офсет усіх маркерів роботів відносно їхньої розрахованої позиції.",
                ["gameplayRuntime.mapRotateIcons"] =
                    "Повертає іконки машин на карті за напрямком їхнього корпусу. Наприклад: увімкніть, щоб бачити, куди їде союзник.",
                ["gameplayRuntime.mapTrackedVehicleIconScale"] =
                    "Масштаб іконок інших машин відносно іконки локального гравця. Наприклад: 0.8 робить відстежувані машини трохи меншими.",
                ["gameplayRuntime.mapTrackedVehicleRefreshInterval"] =
                    "Інтервал перебудови списку видимих машин на карті; позиції вже знайдених іконок оновлюються щокадру. Наприклад: 0.5 оновлює склад списку двічі за секунду.",
                ["gameplayRuntime.mapFullMapKey"] =
                    "Клавіша показу повної карти під час бою. Наприклад: M відкриває карту, поки гравець використовує цю клавішу.",
                ["gameplayRuntime.autoAimMaxAcquireDistance"] =
                    "Максимальна дистанція променя захоплення цілі автоприцілом. Наприклад: 2000 дозволяє перевірити наведення на далеку ціль у відкритій сцені.",
                ["gameplayRuntime.autoAimFallbackTargetHeight"] =
                    "Запасна висота точки автоприцілу над коренем цілі, якщо bounds недоступні. Наприклад: 1.2 наводить приблизно в центр невисокого корпусу.",
                ["gameplayRuntime.autoAimRejectSameTeam"] =
                    "Забороняє автоприцілу захоплювати союзників. Наприклад: увімкнене значення пропускає дружню машину перед ворогом.",
                ["gameplayRuntime.autoAimPreferTurretTarget"] =
                    "За можливості веде автоприціл у башту противника замість центру всього корпусу. Наприклад: увімкніть для більш природної точки наведення.",
                ["gameplayRuntime.cameraZoomProfiles"] =
                    "Єдиний масив zoom-профілів. У кожному елементі zoomIndex: 0 — default, додатні значення — наближення, від'ємні — віддалення; distance/FOV абсолютні, showReticle вмикає чорний sniper scope, а aimingCircleDiameter задає базовий діаметр круга у повністю зведеному стані. Динамічний bloom додається поверх цієї бази.",
                ["gameplayRuntime.runtimeZoomOverrideEnabled"] =
                    "Вмикає debug-примус локального zoom у Play Mode. Коли прапорець активний, runtimeZoomIndex має пріоритет над колесом миші та використовує той самий перехід камери.",
                ["gameplayRuntime.runtimeZoomIndex"] =
                    "Signed zoomIndex із cameraZoomProfiles для runtime-примусу. Наприклад: 0 — default, 1 — один крок наближення, -1 — один крок віддалення; відсутній індекс ігнорується.",
                ["gameplayRuntime.cameraSniperFromMuzzle"] =
                    "За можливості прив'язує снайперську камеру до дула. Наприклад: увімкніть, щоб приціл у sniper-режимі відповідав позиції гармати.",
                ["gameplayRuntime.cameraSniperForwardOffset"] =
                    "Зміщення снайперської камери вперед від точки прив'язки. Наприклад: 0.15 допомагає не бачити внутрішню частину моделі дула.",
                ["gameplayRuntime.cameraSniperVerticalOffset"] =
                    "Вертикальне зміщення снайперської камери від точки прив'язки. Наприклад: 0 залишає камеру на висоті прив'язки.",
                ["gameplayRuntime.cameraHorizontalSpeed"] =
                    "Базова горизонтальна швидкість обертання камери до застосування чутливості миші. Наприклад: 120 дає швидкий поворот огляду по yaw.",
                ["gameplayRuntime.cameraVerticalSpeed"] =
                    "Базова вертикальна швидкість обертання камери до застосування чутливості миші. Наприклад: 120 дає аналогічну швидкість руху по pitch.",
                ["gameplayRuntime.cameraMinPitch"] =
                    "Мінімальний вертикальний кут камери. Наприклад: -20 дозволяє трохи дивитися вниз, але не перевертати огляд.",
                ["gameplayRuntime.cameraMaxPitch"] =
                    "Максимальний вертикальний кут камери. Наприклад: 80 дозволяє дивитися майже вгору.",
                ["gameplayRuntime.cameraFovLerpSpeed"] =
                    "Швидкість плавного переходу між значеннями FOV. Наприклад: 10 швидко, але не миттєво перемикає зум.",
                ["gameplayRuntime.cameraDistanceLerpSpeed"] =
                    "Швидкість плавної зміни дистанції камери. Наприклад: 12 згладжує перехід між кроками наближення.",
                ["gameplayRuntime.cameraScrollDeadZone"] =
                    "Мертва зона колеса миші для перемикання наближення. Наприклад: 0.001 ігнорує дуже малий шум input.",
                ["gameplayRuntime.aimReadyColor"] =
                    "Колір тексту стану прицілу після повного зведення. Наприклад: світло-зелений показує готовність точного пострілу.",
                ["gameplayRuntime.aimProgressColor"] =
                    "Колір тексту стану прицілу під час зведення. Наприклад: жовтий показує, що розкид ще зменшується.",
                ["gameplayRuntime.aimReadyText"] =
                    "Текст стану прицілу після повного зведення. Наприклад: AIM READY прямо повідомляє про готовність.",
                ["gameplayRuntime.aimProgressTextPrefix"] =
                    "Префікс тексту стану під час зведення перед числовим прогресом. Наприклад: AIM разом із відсотком формує напис на кшталт AIM 75%.",
                ["gameplayRuntime.reticleFallbackSmoothSpeed"] =
                    "Запасна швидкість згладжування прицілу, якщо глобальні server settings розкиду недоступні. Наприклад: 20 залишає рух UI-кола плавним у тестовій сцені.",
                ["gameplayRuntime.reticleHideWhenBehindCamera"] =
                    "Приховує приціл, якщо точка наведення опинилася позаду камери. Наприклад: увімкніть, щоб UI-коло не стрибало на протилежний край екрана.",
                ["gameplayRuntime.reticleClampToCanvas"] =
                    "Обмежує позицію прицілу межами canvas. Наприклад: увімкніть, щоб коло не виходило за видиму область HUD.",
                ["gameplayRuntime.reticleHideWhenAngleGreaterThan"] =
                    "Кут між напрямком гармати та камерою, після якого приціл ховається. Наприклад: 90 приховує UI, якщо гармата дивиться більш ніж на чверть оберту вбік.",
                ["gameplayRuntime.reticleShowServerReticle"] =
                    "Дозволяє показувати серверну позначку прицілу, якщо її увімкнув гравець. Наприклад: вимкніть для спрощеного HUD лише з основним колом.",
                ["gameplayRuntime.hoverOutlineMaxDistance"] =
                    "Максимальна дистанція пошуку машини під прицілом для outline-підсвічування. Наприклад: 2000 дозволяє тестувати підсвічування на далекій цілі.",
                ["gameplayRuntime.hoverOutlineWidth"] =
                    "Товщина outline навколо машини під прицілом. Наприклад: 3 робить контур добре видимим без надмірної рамки.",
                ["gameplayRuntime.hoverOutlineColor"] =
                    "Колір outline навколо машини під прицілом. Наприклад: червоний підкреслює ворожу ціль.",
                ["gameplayRuntime.hoverOutlineRejectSameTeam"] =
                    "Не підсвічує союзників outline-контуром під прицілом. Наприклад: увімкніть, щоб контур з'являвся лише на ворогах.",
                ["gameplayRuntime.speedHudDisplaySpeedMultiplier"] =
                    "Множник переведення фактичної швидкості машини у число спідометра HUD. Наприклад: 10 показує 80 для руху зі швидкістю 8 метрів за секунду.",
                ["gameplayRuntime.speedHudSampleInterval"] =
                    "Інтервал вимірювання позиції для розрахунку швидкості HUD. Наприклад: 0.12 оновлює базову оцінку приблизно вісім разів за секунду.",
                ["gameplayRuntime.speedHudSmoothRate"] =
                    "Швидкість згладжування числа спідометра. Наприклад: 8 прибирає різкі стрибки між вимірюваннями.",
                ["gameplayRuntime.speedHudStopSnapThreshold"] =
                    "Поріг швидкості, нижче якого HUD одразу показує нуль. Наприклад: 0.05 прибирає дрібне тремтіння числа після зупинки.",
                ["gameplayRuntime.floatingDamageTextDuration"] =
                    "Тривалість анімації плаваючого тексту шкоди. Наприклад: 0.8 швидко прибирає число після влучання.",
                ["gameplayRuntime.floatingDamageTextMoveUp"] =
                    "Висота підйому плаваючого тексту шкоди у світових метрах. Наприклад: 1.5 підіймає число над точкою появи на півтора метра.",
                ["gameplayRuntime.floatingDamageTextEndScale"] =
                    "Фінальний множник масштабу плаваючого тексту шкоди. Наприклад: 1.3 трохи збільшує число до кінця анімації.",
                ["gameplayRuntime.sniperScopeOverlayFadeDuration"] =
                    "Тривалість fade-переходу снайперського overlay. Наприклад: 0.18 робить появу маски швидкою, але не миттєвою."
            };

        public override void OnInspectorGUI()
        {
            DocumentedSettingsInspector.Draw(
                serializedObject,
                "Клієнтські налаштування керують локальним FPS, projectile visuals, HUD, картою, камерою та автоприцілом. Сервер їх не читає і не синхронізує. Наведіть курсор на назву параметра, щоб побачити український опис і приклад.",
                Documentation);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("UI Button Colors", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Applies Button Colors to every Button in Client.unity and Assets/Game prefabs. It also adds or refreshes ButtonComponent child-graphic references.", MessageType.Info);

            if (GUILayout.Button("Apply Button Colors To All UI Buttons"))
            {
                ClientButtonColorApplier.ApplyToGameUiButtons(((ClientSettings)target).buttonColors);
            }
        }
    }
}
