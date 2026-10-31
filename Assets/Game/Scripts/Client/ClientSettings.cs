using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Client
{
    [System.Serializable]
    public class ClientButtonColorSettings
    {
        [Tooltip("Base UI button color in the normal state.")]
        public Color normalColor = Color.white;
        [Tooltip("UI button color when the pointer hovers over it.")]
        public Color highlightedColor = new Color(0.88235295f, 0.88235295f, 0.88235295f, 1f);
        [Tooltip("UI button color while it is pressed.")]
        public Color pressedColor = new Color(0.69803923f, 0.69803923f, 0.69803923f, 1f);
        [Tooltip("UI button color when selected by keyboard or gamepad.")]
        public Color selectedColor = new Color(0.88235295f, 0.88235295f, 0.88235295f, 1f);
        [Tooltip("UI button color when it cannot be pressed.")]
        public Color disabledColor = new Color(0.52156866f, 0.52156866f, 0.52156866f, 0.5019608f);
        [Min(0f), Tooltip("Brightness multiplier for all UI button states.")]
        public float colorMultiplier = 1f;
        [Min(0f), Tooltip("Duration of the smooth color transition between UI button states.")]
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
        [Tooltip("Base fully aimed circle diameter for this zoom profile in UI pixels. Values from 0 to 1 render as a minimum 1x1 UI pixel; dispersion above the fully aimed diameter adds bloom on top of the base.")]
        public float aimingCircleDiameter = 55f;
        [Tooltip("Show the black sniper-scope circle with a central reticle. For a forward profile, true marks this step as sniper mode.")]
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

        [Header("UI Health")]
        [Tooltip("HP bar color for the local vehicle and allies in the vehicle world-space UI.")]
        public Color alliedHpColor = new Color(0.1f, 0.35f, 1f, 1f);
        [Tooltip("HP bar color for enemies in the vehicle world-space UI.")]
        public Color enemyHpColor = new Color(1f, 0.08f, 0.04f, 1f);
        [Tooltip("How often the vehicle world-space UI reevaluates the target team and updates the HP color.")]
        public float hpTeamColorRefreshInterval = 0.25f;
        [Tooltip("Smoothing speed for the local player's main HP bar on GameplayHUD.")]
        public float ownerHealthBarSmoothSpeed = 10f;
        [Header("World-Space HP Scale")]
        [Tooltip("Enable distance-based scaling for robot world-space HP bars.")]
        public bool worldHpBarDistanceScaleEnabled = true;
        [Tooltip("Distance at which robot HP bars use the near scale.")]
        public float worldHpBarScaleMinDistance = 5f;
        [Tooltip("Distance at which robot HP bars reach the far scale.")]
        public float worldHpBarScaleMaxDistance = 90f;
        [Tooltip("HP bar scale multiplier near the camera.")]
        public float worldHpBarMinDistanceScale = 1f;
        [Tooltip("HP bar scale multiplier far from the camera.")]
        public float worldHpBarMaxDistanceScale = 5f;
        [Header("World-Space NameCanvas Opacity")]
        [Tooltip("Distance from the gameplay camera at which NameCanvas starts fading out.")]
        public float worldHpBarCanvasFadeStartDistance = 5f;
        [Tooltip("Distance from the gameplay camera at which NameCanvas becomes fully transparent.")]
        public float worldHpBarCanvasFullyTransparentDistance = 3f;

        [Header("Map")]
        [Tooltip("Ally icon color on the minimap and full map.")]
        public Color mapAllyIconColor = new Color(0.1f, 0.35f, 1f, 1f);
        [Tooltip("Enemy icon color on the minimap and full map.")]
        public Color mapEnemyIconColor = new Color(1f, 0.08f, 0.04f, 1f);
        [Tooltip("Destroyed-robot icon color on the minimap and full map.")]
        public Color mapDestroyedIconColor = Color.black;
        [Tooltip("Scale of the minimap background image only. 1 preserves the base scene size.")]
        public float mapMiniMapImageScale = 1f;
        [Tooltip("Additional horizontal scale multiplier for the minimap background. Applied on top of mapMiniMapImageScale.")]
        public float mapMiniMapImageScaleX = 1f;
        [Tooltip("Additional vertical scale multiplier for the minimap background. Applied on top of mapMiniMapImageScale.")]
        public float mapMiniMapImageScaleY = 1f;
        [Tooltip("Pixel X/Y offset of the minimap background image relative to the MiniMapRoot center.")]
        public Vector2 mapMiniMapImageOffset = Vector2.zero;
        [Tooltip("Scale of the full-map background image only.")]
        public float mapFullMapImageScale = 1f;
        [Tooltip("Additional horizontal scale multiplier for the full-map background.")]
        public float mapFullMapImageScaleX = 1f;
        [Tooltip("Additional vertical scale multiplier for the full-map background.")]
        public float mapFullMapImageScaleY = 1f;
        [Tooltip("Pixel X/Y offset of the full-map background image only.")]
        public Vector2 mapFullMapImageOffset = Vector2.zero;
        [Tooltip("Horizontal scale of the independent canvas robot marker on the full map.")]
        public float mapFullMapRobotCanvasScaleX = 1f;
        [Tooltip("Vertical scale of the independent canvas robot marker on the full map.")]
        public float mapFullMapRobotCanvasScaleY = 1f;
        [Tooltip("Pixel X/Y offset of the canvas robot marker on the full map.")]
        public Vector2 mapFullMapRobotCanvasOffset = Vector2.zero;
        [Tooltip("Horizontal scale of the independent canvas robot marker on the minimap.")]
        public float mapMiniMapRobotCanvasScaleX = 1f;
        [Tooltip("Vertical scale of the independent canvas robot marker on the minimap.")]
        public float mapMiniMapRobotCanvasScaleY = 1f;
        [Tooltip("Pixel X/Y offset of the independent canvas robot marker relative to the minimap frame.")]
        public Vector2 mapMiniMapRobotCanvasOffset = Vector2.zero;
        [Tooltip("Global scale of all robot markers on the minimap, including the local-player icon.")]
        public float mapMiniMapIconScale = 1f;
        [Tooltip("Additional horizontal scale multiplier for robot markers on the minimap.")]
        public float mapMiniMapIconScaleX = 1f;
        [Tooltip("Additional vertical scale multiplier for robot markers on the minimap.")]
        public float mapMiniMapIconScaleY = 1f;
        [Tooltip("Pixel X/Y offset of all robot markers on the minimap relative to their calculated positions.")]
        public Vector2 mapMiniMapIconOffset = Vector2.zero;
        [Tooltip("Whether to rotate map robot icons with their hull direction.")]
        public bool mapRotateIcons = true;
        [Tooltip("Scale of ally and enemy icons relative to the local-player icon.")]
        public float mapTrackedVehicleIconScale = 0.8f;
        [Tooltip("How often the map rebuilds the visible-robot list. Positions of existing icons update every frame.")]
        public float mapTrackedVehicleRefreshInterval = 0.5f;
        [Tooltip("Key that shows the full map during battle.")]
        public KeyCode mapFullMapKey = KeyCode.M;

        [Header("Autolock")]
        [Tooltip("Maximum ray distance for autolock target acquisition.")]
        public float autoAimMaxAcquireDistance = 2000f;
        [Tooltip("Fallback aiming-point height when the target has no colliders or turret.")]
        public float autoAimFallbackTargetHeight = 1.2f;
        [Tooltip("Prevent autolock from acquiring allies.")]
        public bool autoAimRejectSameTeam = true;
        [Tooltip("After acquiring an enemy, aim autolock at the turret instead of the center of the entire hull.")]
        public bool autoAimPreferTurretTarget = true;

        [Header("Camera")]
        [Tooltip("Single array of zoom profiles. Set zoomIndex: 0 = base, positive values = zoom in, negative values = zoom out.")]
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
        [Tooltip("Enable forced local zoom switching from runtimeZoomIndex during Play Mode. Works like the mouse wheel, but the Inspector value has priority.")]
        public bool runtimeZoomOverrideEnabled = false;
        [Tooltip("Current signed zoomIndex from cameraZoomProfiles, forcibly applied at runtime when runtimeZoomOverrideEnabled is enabled. Example: 0 = default, 1 = one zoom-in step, -1 = one zoom-out step.")]
        public int runtimeZoomIndex = 0;
        [Tooltip("Use the barrel as the camera point in sniper mode when available.")]
        public bool cameraSniperFromMuzzle = true;
        [Tooltip("Forward offset of the sniper camera from its anchor point.")]
        public float cameraSniperForwardOffset = 0.15f;
        [Tooltip("Vertical offset of the sniper camera from its anchor point.")]
        public float cameraSniperVerticalOffset = 0f;
        [Tooltip("Horizontal camera rotation speed before mouse sensitivity is applied.")]
        public float cameraHorizontalSpeed = 120f;
        [Tooltip("Vertical camera rotation speed before mouse sensitivity is applied.")]
        public float cameraVerticalSpeed = 120f;
        [Tooltip("Minimum camera pitch angle.")]
        public float cameraMinPitch = -20f;
        [Tooltip("Maximum camera pitch angle.")]
        public float cameraMaxPitch = 80f;
        [Tooltip("FOV change smoothing speed.")]
        public float cameraFovLerpSpeed = 10f;
        [Tooltip("Camera-distance smoothing speed.")]
        public float cameraDistanceLerpSpeed = 12f;
        [Tooltip("Mouse-wheel dead zone for zoom switching.")]
        public float cameraScrollDeadZone = 0.001f;
        [Header("Reticle")]
        [Tooltip("Text color when dispersion is fully aimed.")]
        public Color aimReadyColor = new Color(0.5f, 1f, 0.5f, 1f);
        [Tooltip("Text color while dispersion is still aiming.")]
        public Color aimProgressColor = new Color(1f, 0.86f, 0.2f, 1f);
        [Tooltip("Reticle status text when the weapon is fully aimed.")]
        public string aimReadyText = "AIM READY";
        [Tooltip("Reticle status text prefix while aiming.")]
        public string aimProgressTextPrefix = "AIM ";
        [Tooltip("Fallback reticle smoothing speed when global dispersion settings are unavailable.")]
        public float reticleFallbackSmoothSpeed = 20f;
        [Tooltip("Hide the reticle when the aiming point is behind the camera.")]
        public bool reticleHideWhenBehindCamera = true;
        [Tooltip("Constrain the reticle position to the canvas bounds.")]
        public bool reticleClampToCanvas = true;
        [Tooltip("Angle between the gun direction and camera after which the reticle is hidden.")]
        public float reticleHideWhenAngleGreaterThan = 90f;
        [Tooltip("Show the server reticle when enabled by the player in settings.")]
        public bool reticleShowServerReticle = true;

        [Header("Reticle Target Outline")]
        [Tooltip("Maximum target-search distance for the reticle outline.")]
        public float hoverOutlineMaxDistance = 2000f;
        [Tooltip("Vehicle reticle-outline thickness.")]
        public float hoverOutlineWidth = 3f;
        [Tooltip("Vehicle reticle-outline color.")]
        public Color hoverOutlineColor = Color.red;
        [Tooltip("Do not outline allies under the reticle.")]
        public bool hoverOutlineRejectSameTeam = true;

        [Header("Speed HUD")]
        [Tooltip("Multiplier that converts actual vehicle speed to the HUD speedometer value.")]
        public float speedHudDisplaySpeedMultiplier = 10f;
        [Tooltip("Position-sampling interval used to calculate HUD speed.")]
        public float speedHudSampleInterval = 0.12f;
        [Tooltip("Smoothing speed for the HUD speed value.")]
        public float speedHudSmoothRate = 8f;
        [Tooltip("Threshold below which HUD speed immediately becomes zero.")]
        public float speedHudStopSnapThreshold = 0.05f;
        [Tooltip("Duration of the floating damage-text animation.")]
        public float floatingDamageTextDuration = 0.8f;
        [Tooltip("Distance in meters that floating damage text rises.")]
        public float floatingDamageTextMoveUp = 1.5f;
        [Tooltip("Final scale of floating damage text.")]
        public float floatingDamageTextEndScale = 1.3f;
        [Tooltip("Duration of the sniper overlay fade animation.")]
        public float sniperScopeOverlayFadeDuration = 0.18f;

        [Header("Development tree arrows")]
        [Tooltip("Thickness of development-tree arrow shafts in UI pixels.")]
        public float treeArrowShaftThickness = 5f;
        [Tooltip("Color applied to development-tree arrow shafts.")]
        public Color treeArrowColor = new Color(0.77254903f, 0.5568628f, 0.2f, 1f);
        [Tooltip("Color applied to the development-tree arrowhead Image.")]
        public Color treeArrowHeadColor = new Color(0.77254903f, 0.5568628f, 0.2f, 1f);
        [Tooltip("Height of the arrowhead in UI pixels.")]
        public float treeArrowHeadHeight = 28f;
        [Tooltip("Additional scale multiplier for the development-tree arrowhead Image.")]
        public float treeArrowHeadScale = 1f;
        [Tooltip("Offset of the arrowhead along its final route direction. Positive values move it toward the target item.")]
        public float treeArrowHeadOffset = 0f;
        [Tooltip("Distance in UI pixels between each arrow endpoint and its connected item.")]
        public float treeArrowItemOffset = 0f;

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
            treeArrowShaftThickness = ClampFinite(treeArrowShaftThickness, 0.1f, Default.treeArrowShaftThickness);
            treeArrowColor = ClampColor(treeArrowColor, Default.treeArrowColor);
            treeArrowHeadColor = ClampColor(treeArrowHeadColor, Default.treeArrowHeadColor);
            treeArrowHeadHeight = ClampFinite(treeArrowHeadHeight, 0.1f, Default.treeArrowHeadHeight);
            treeArrowHeadScale = ClampFinite(treeArrowHeadScale, 0.01f, Default.treeArrowHeadScale);
            treeArrowHeadOffset = ClampFinite(treeArrowHeadOffset, Default.treeArrowHeadOffset);
            treeArrowItemOffset = ClampFinite(treeArrowItemOffset, 0f, Default.treeArrowItemOffset);
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
            treeArrowShaftThickness = source.treeArrowShaftThickness;
            treeArrowColor = source.treeArrowColor;
            treeArrowHeadColor = source.treeArrowHeadColor;
            treeArrowHeadHeight = source.treeArrowHeadHeight;
            treeArrowHeadScale = source.treeArrowHeadScale;
            treeArrowHeadOffset = source.treeArrowHeadOffset;
            treeArrowItemOffset = source.treeArrowItemOffset;
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

        [Header("Projectile Visual References")]
        [Tooltip("Client-side projectile prefab override. Leave empty to use the weapon prefab.")]
        public Projectile projectilePrefab;
        [Tooltip("Material applied to the projectile mesh on the client.")]
        public Material projectileMaterial;
        [Tooltip("Projectile tracer-tail material.")]
        public Material tracerMaterial;
        [Tooltip("One-shot muzzle flash effect.")]
        public PooledImpactFx muzzleFlashPrefab;
        [Tooltip("One-shot muzzle smoke effect.")]
        public PooledImpactFx muzzleSmokePrefab;

        [Header("Projectile Glow")]
        public bool overrideProjectileMaterial = true;
        [ColorUsage(false, true)] public Color projectileBaseColor = new Color(1f, 0.58f, 0.16f, 1f);
        [ColorUsage(false, true)] public Color projectileEmissionColor = new Color(1f, 0.38f, 0.06f, 1f);
        [Min(0f)] public float projectileEmissionIntensity = 4f;

        [Header("Tracer")]
        public bool tracerEnabled = true;
        [Tooltip("How many seconds the tracer remains visible. Lower values make the tracer shorter.")]
        [InspectorName("Tracer Duration, s")]
        [Min(0.01f)] public float tracerLifetime = 0.06f;
        [Tooltip("Tracer thickness near the projectile head.")]
        [Min(0.001f)] public float tracerStartWidth = 0.07f;
        [Tooltip("Tracer thickness at the fading tail end.")]
        [Min(0f)] public float tracerEndWidth = 0.012f;
        [Tooltip("Distance between tail points. Higher values are cheaper but less detailed.")]
        [Min(0.001f)] public float tracerMinVertexDistance = 0.08f;
        [Range(0, 8)] public int tracerCornerVertices;
        [Range(0, 8)] public int tracerCapVertices;
        [Tooltip("Global opacity multiplier for the tracer gradient.")]
        [Range(0f, 1f)] public float tracerOpacity = 1f;
        [Tooltip("Tracer material alpha. Keep it high when tracerOpacity provides the main opacity control.")]
        [Range(0f, 1f)] public float tracerMaterialAlpha = 0.75f;
        [Tooltip("Position of the tracer fade midpoint. 0 is near the projectile head; 1 is near the tail.")]
        [Range(0.01f, 0.99f)] public float tracerFadeMidpoint = 0.55f;
        [Tooltip("Opacity multiplier at tracerFadeMidpoint relative to tracerHeadColor alpha.")]
        [Range(0f, 1f)] public float tracerMidOpacityMultiplier = 0.45f;
        [ColorUsage(false, true)] public Color tracerHeadColor = new Color(1f, 0.82f, 0.35f, 0.42f);
        [ColorUsage(false, true)] public Color tracerTailColor = new Color(1f, 0.45f, 0.1f, 0f);
        [ColorUsage(false, true)] public Color tracerEmissionColor = new Color(1f, 0.55f, 0.16f, 1f);
        [Min(0f)] public float tracerEmissionIntensity = 1.35f;

        [Header("Muzzle Effects")]
        public bool muzzleFxEnabled = true;
        [Tooltip("Do not create local muzzle flash and smoke while the owner is in full sniper mode.")]
        public bool hideMuzzleFxInSniperMode = true;
        [Min(0f)] public float muzzleForwardOffset = 0.25f;
        [Min(0.01f)] public float muzzleFlashScale = 0.55f;
        [Min(0.01f)] public float muzzleSmokeScale = 0.65f;

        [Header("Visual Effect Pools")]
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

        [Tooltip("Enable vertical synchronization. When active, Unity matches the display refresh rate.")]
        public bool verticalSyncEnabled = DefaultVerticalSyncEnabled;

        [Tooltip("Target client frame rate used when vertical synchronization is disabled.")]
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

        [Tooltip("Client frame-pacing settings for production builds.")]
        public ClientFramePacingSettings framePacing = new ClientFramePacingSettings();

        [Tooltip("Client visual settings for projectiles, tracers, and muzzle effects. The server does not read or synchronize them.")]
        public ClientProjectileVisualSettings projectileVisuals = new ClientProjectileVisualSettings();

        [Tooltip("Local client runtime settings for the HUD, map, and autolock. The server does not read or synchronize them.")]
        public GameplayRuntimeSettings gameplayRuntime = new GameplayRuntimeSettings();

        [Tooltip("Unified UI button-state palette. Apply it to Client and UI prefabs with the ClientSettings Inspector button.")]
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
