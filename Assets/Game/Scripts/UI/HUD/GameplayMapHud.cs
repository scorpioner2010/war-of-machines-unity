using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Client;
using Game.Scripts.Diagnostics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.HUD
{
    public class GameplayMapHud : MonoBehaviour
    {
        public Image miniMapImage;
        public RectTransform miniMapRobotCanvas;
        public Image fullMapImage;
        public RectTransform fullMapRobotCanvas;
        public RectTransform localPlayerIconMini;
        public RectTransform localPlayerIconFull;
        public GameObject fullMapRoot;
        public Sprite alliedVehicleIcon;
        public Sprite enemyVehicleIcon;
        public Vector2 worldMin = Vector2.zero;
        public Vector2 worldMax = new Vector2(256f, 256f);
        public bool useActiveTerrainBounds = true;

        private VehicleRoot _localPlayer;
        private RectTransform _miniMapImageRect;
        private RectTransform _miniMapRect;
        private RectTransform _fullMapImageRect;
        private RectTransform _fullMapRect;
        private Terrain _terrainBoundsSource;
        private bool _terrainBoundsResolved;
        private bool _fullMapVisible;
        private int _appliedVisibilityVersion = -1;
        private Color _localPlayerMiniIconColor;
        private Color _localPlayerFullIconColor;
        private Vector3 _miniMapImageBaseScale = Vector3.one;
        private Vector2 _miniMapImageBasePosition;
        private Vector3 _miniMapRobotCanvasBaseScale = Vector3.one;
        private Vector2 _miniMapRobotCanvasBasePosition;
        private Vector3 _fullMapImageBaseScale = Vector3.one;
        private Vector2 _fullMapImageBasePosition;
        private Vector3 _fullMapRobotCanvasBaseScale = Vector3.one;
        private Vector2 _fullMapRobotCanvasBasePosition;
        private Vector3 _localPlayerMiniIconBaseScale = Vector3.one;
        private Vector3 _localPlayerFullIconBaseScale = Vector3.one;
        private Graphic _localPlayerMiniIconGraphic;
        private Graphic _localPlayerFullIconGraphic;
        private bool _hasLocalPlayerMiniIconColor;
        private bool _hasLocalPlayerFullIconColor;
        private GameplayRuntimeSettings _runtimeSettings = GameplayRuntimeSettings.Default;
        private readonly List<TrackedVehicleIcon> _trackedVehicleIcons = new List<TrackedVehicleIcon>(16);

        private void Awake()
        {
            CacheRects();
            CacheLocalPlayerIconColors();
            CacheBaseScales();
            RefreshTerrainBounds();
            _fullMapVisible = true;
            SetFullMapVisible(false);
            SetIconVisible(localPlayerIconMini, false);
            SetIconVisible(localPlayerIconFull, false);
        }

        private void OnEnable()
        {
            VehicleRoot.LocalPlayerVehicleChanged += SetLocalPlayer;
            GameplayMapVisibilityState.Changed += HandleVisibilitySnapshotChanged;
            SetLocalPlayer(VehicleRoot.LocalPlayerVehicle);
        }

        private void OnDisable()
        {
            VehicleRoot.LocalPlayerVehicleChanged -= SetLocalPlayer;
            GameplayMapVisibilityState.Changed -= HandleVisibilitySnapshotChanged;
            ClearTrackedVehicleIcons();
        }

        private void Update()
        {
            using (ProfileScope.Measure("Client.UI.GameplayMapHud.Update", DiagnosticsCategories.Ui))
            {
                _runtimeSettings = GameplayRuntimeSettingsProvider.Get();
                ApplyRuntimeVisualSettings();

                if (useActiveTerrainBounds && (!_terrainBoundsResolved || _terrainBoundsSource == null))
                {
                    RefreshTerrainBounds();
                }

                bool shouldShowFullMap = Input.GetKey(_runtimeSettings.mapFullMapKey);
                SetFullMapVisible(shouldShowFullMap);

                bool spectatorActive = CameraController.ActiveDeathSpectator != null;
                if (_localPlayer == null && !spectatorActive)
                {
                    SetIconVisible(localPlayerIconMini, false);
                    SetIconVisible(localPlayerIconFull, false);
                    ClearTrackedVehicleIcons();
                    return;
                }

                RefreshTrackedVehiclesIfNeeded();

                if (_localPlayer != null)
                {
                    Transform trackedTransform = GetTrackedTransform(_localPlayer);
                    Vector3 worldPosition = trackedTransform.position;
                    float yaw = trackedTransform.eulerAngles.y;

                    UpdateIcon(_miniMapRect, localPlayerIconMini, worldPosition, yaw, true, _runtimeSettings.mapMiniMapIconOffset);
                    UpdateIcon(_fullMapRect, localPlayerIconFull, worldPosition, yaw, _fullMapVisible, Vector2.zero);
                    ApplyLocalPlayerIconColor();
                }
                else
                {
                    SetIconVisible(localPlayerIconMini, false);
                    SetIconVisible(localPlayerIconFull, false);
                }

                UpdateTrackedVehicleIcons();
                BringLocalPlayerIconsToFront();
            }
        }

        public void SetLocalPlayer(VehicleRoot vehicleRoot)
        {
            _localPlayer = vehicleRoot;
            _appliedVisibilityVersion = -1;
        }

        private void HandleVisibilitySnapshotChanged()
        {
            _appliedVisibilityVersion = -1;
        }

        private void CacheRects()
        {
            _miniMapImageRect = miniMapImage != null ? miniMapImage.rectTransform : null;
            _miniMapRect = miniMapRobotCanvas != null
                ? miniMapRobotCanvas
                : _miniMapImageRect;
            _fullMapImageRect = fullMapImage != null ? fullMapImage.rectTransform : null;
            _fullMapRect = fullMapRobotCanvas != null
                ? fullMapRobotCanvas
                : _fullMapImageRect;
        }

        private void CacheBaseScales()
        {
            if (_miniMapImageRect != null)
            {
                _miniMapImageBaseScale = _miniMapImageRect.localScale;
                _miniMapImageBasePosition = _miniMapImageRect.anchoredPosition;
            }

            if (_miniMapRect != null)
            {
                _miniMapRobotCanvasBaseScale = _miniMapRect.localScale;
                _miniMapRobotCanvasBasePosition = _miniMapRect.anchoredPosition;
            }

            if (_fullMapImageRect != null)
            {
                _fullMapImageBaseScale = _fullMapImageRect.localScale;
                _fullMapImageBasePosition = _fullMapImageRect.anchoredPosition;
            }

            if (_fullMapRect != null)
            {
                _fullMapRobotCanvasBaseScale = _fullMapRect.localScale;
                _fullMapRobotCanvasBasePosition = _fullMapRect.anchoredPosition;
            }

            if (localPlayerIconMini != null)
            {
                _localPlayerMiniIconBaseScale = localPlayerIconMini.localScale;
            }

            if (localPlayerIconFull != null)
            {
                _localPlayerFullIconBaseScale = localPlayerIconFull.localScale;
            }
        }

        private void ApplyRuntimeVisualSettings()
        {
            float miniMapImageScale = Mathf.Max(0.1f, _runtimeSettings.mapMiniMapImageScale);
            Vector3 miniMapImageScaleVector = new Vector3(
                miniMapImageScale * Mathf.Max(0.1f, _runtimeSettings.mapMiniMapImageScaleX),
                miniMapImageScale * Mathf.Max(0.1f, _runtimeSettings.mapMiniMapImageScaleY),
                1f);
            if (_miniMapImageRect != null)
            {
                _miniMapImageRect.localScale = Vector3.Scale(_miniMapImageBaseScale, miniMapImageScaleVector);
                _miniMapImageRect.anchoredPosition = _miniMapImageBasePosition + _runtimeSettings.mapMiniMapImageOffset;
            }

            if (_miniMapRect != null && _miniMapRect != _miniMapImageRect)
            {
                Vector3 miniMapRobotCanvasScaleVector = new Vector3(
                    Mathf.Max(0.1f, _runtimeSettings.mapMiniMapRobotCanvasScaleX),
                    Mathf.Max(0.1f, _runtimeSettings.mapMiniMapRobotCanvasScaleY),
                    1f);
                _miniMapRect.localScale = Vector3.Scale(_miniMapRobotCanvasBaseScale, miniMapRobotCanvasScaleVector);
                _miniMapRect.anchoredPosition = _miniMapRobotCanvasBasePosition
                    + _runtimeSettings.mapMiniMapRobotCanvasOffset;
            }

            float fullMapImageScale = Mathf.Max(0.1f, _runtimeSettings.mapFullMapImageScale);
            Vector3 fullMapImageScaleVector = new Vector3(
                fullMapImageScale * Mathf.Max(0.1f, _runtimeSettings.mapFullMapImageScaleX),
                fullMapImageScale * Mathf.Max(0.1f, _runtimeSettings.mapFullMapImageScaleY),
                1f);
            if (_fullMapImageRect != null)
            {
                _fullMapImageRect.localScale = Vector3.Scale(_fullMapImageBaseScale, fullMapImageScaleVector);
                _fullMapImageRect.anchoredPosition = _fullMapImageBasePosition + _runtimeSettings.mapFullMapImageOffset;
            }

            if (_fullMapRect != null && _fullMapRect != _fullMapImageRect)
            {
                Vector3 fullMapRobotCanvasScaleVector = new Vector3(
                    Mathf.Max(0.1f, _runtimeSettings.mapFullMapRobotCanvasScaleX),
                    Mathf.Max(0.1f, _runtimeSettings.mapFullMapRobotCanvasScaleY),
                    1f);
                _fullMapRect.localScale = Vector3.Scale(_fullMapRobotCanvasBaseScale, fullMapRobotCanvasScaleVector);
                _fullMapRect.anchoredPosition = _fullMapRobotCanvasBasePosition
                    + _runtimeSettings.mapFullMapRobotCanvasOffset;
            }

            float miniMapIconScale = Mathf.Max(0.1f, _runtimeSettings.mapMiniMapIconScale);
            Vector3 miniMapIconScaleVector = new Vector3(
                miniMapIconScale * Mathf.Max(0.1f, _runtimeSettings.mapMiniMapIconScaleX),
                miniMapIconScale * Mathf.Max(0.1f, _runtimeSettings.mapMiniMapIconScaleY),
                1f);
            if (localPlayerIconMini != null)
            {
                localPlayerIconMini.localScale = Vector3.Scale(_localPlayerMiniIconBaseScale, miniMapIconScaleVector);
            }

            for (int i = 0; i < _trackedVehicleIcons.Count; i++)
            {
                TrackedVehicleIcon icon = _trackedVehicleIcons[i];
                if (icon.MiniIcon != null)
                {
                    icon.MiniIcon.localScale = Vector3.Scale(_localPlayerMiniIconBaseScale, miniMapIconScaleVector);
                    icon.MiniIcon.sizeDelta = localPlayerIconMini != null
                        ? localPlayerIconMini.sizeDelta * Mathf.Max(0.1f, _runtimeSettings.mapTrackedVehicleIconScale)
                        : icon.MiniIcon.sizeDelta;
                }

                if (icon.FullIcon != null)
                {
                    icon.FullIcon.localScale = _localPlayerFullIconBaseScale;
                    icon.FullIcon.sizeDelta = localPlayerIconFull != null
                        ? localPlayerIconFull.sizeDelta * Mathf.Max(0.1f, _runtimeSettings.mapTrackedVehicleIconScale)
                        : icon.FullIcon.sizeDelta;
                }
            }
        }

        private static Transform GetTrackedTransform(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot.objectMover != null)
            {
                return vehicleRoot.objectMover.transform;
            }

            return vehicleRoot.transform;
        }

        private static int GetObjectId(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null || vehicleRoot.networkObject == null)
            {
                return -1;
            }

            return vehicleRoot.networkObject.ObjectId;
        }

        private void CacheLocalPlayerIconColors()
        {
            _hasLocalPlayerMiniIconColor = TryGetIconGraphicAndColor(localPlayerIconMini, out _localPlayerMiniIconGraphic, out _localPlayerMiniIconColor);
            _hasLocalPlayerFullIconColor = TryGetIconGraphicAndColor(localPlayerIconFull, out _localPlayerFullIconGraphic, out _localPlayerFullIconColor);
        }

        private void ApplyLocalPlayerIconColor()
        {
            bool destroyed = _localPlayer != null && _localPlayer.health != null && _localPlayer.health.IsDead;

            if (_hasLocalPlayerMiniIconColor)
            {
                ApplyIconColor(_localPlayerMiniIconGraphic, destroyed ? _runtimeSettings.mapDestroyedIconColor : _localPlayerMiniIconColor);
            }

            if (_hasLocalPlayerFullIconColor)
            {
                ApplyIconColor(_localPlayerFullIconGraphic, destroyed ? _runtimeSettings.mapDestroyedIconColor : _localPlayerFullIconColor);
            }
        }

        private void RefreshTrackedVehiclesIfNeeded()
        {
            if (_appliedVisibilityVersion == GameplayMapVisibilityState.Version)
            {
                return;
            }

            _appliedVisibilityVersion = GameplayMapVisibilityState.Version;
            RefreshTrackedVehicles();
        }

        private void RefreshTrackedVehicles()
        {
            for (int i = 0; i < _trackedVehicleIcons.Count; i++)
            {
                _trackedVehicleIcons[i].Seen = false;
            }

            int localObjectId = GetObjectId(_localPlayer);
            int count = GameplayMapVisibilityState.Count;
            for (int i = 0; i < count; i++)
            {
                GameplayMapVisibilityEntry entry = GameplayMapVisibilityState.GetEntry(i);
                if (entry.ObjectId < 0 || entry.ObjectId == localObjectId)
                {
                    continue;
                }

                TrackedVehicleIcon icon = FindTrackedVehicleIcon(entry.ObjectId);
                if (icon == null)
                {
                    icon = CreateTrackedVehicleIcon(entry);
                    if (icon == null)
                    {
                        continue;
                    }

                    _trackedVehicleIcons.Add(icon);
                }

                icon.Seen = true;
                icon.WorldPosition = entry.Position;
                icon.Yaw = entry.Yaw;
                ApplyTrackedVehicleRelation(icon, entry.Relation);
            }

            for (int i = _trackedVehicleIcons.Count - 1; i >= 0; i--)
            {
                TrackedVehicleIcon icon = _trackedVehicleIcons[i];
                if (!icon.Seen)
                {
                    DestroyTrackedVehicleIcon(icon);
                    _trackedVehicleIcons.RemoveAt(i);
                }
            }
        }

        private void UpdateTrackedVehicleIcons()
        {
            for (int i = _trackedVehicleIcons.Count - 1; i >= 0; i--)
            {
                TrackedVehicleIcon icon = _trackedVehicleIcons[i];
                UpdateIcon(_miniMapRect, icon.MiniIcon, icon.WorldPosition, icon.Yaw, true, _runtimeSettings.mapMiniMapIconOffset);
                UpdateIcon(_fullMapRect, icon.FullIcon, icon.WorldPosition, icon.Yaw, _fullMapVisible, Vector2.zero);
            }
        }

        private TrackedVehicleIcon FindTrackedVehicleIcon(int objectId)
        {
            for (int i = 0; i < _trackedVehicleIcons.Count; i++)
            {
                TrackedVehicleIcon icon = _trackedVehicleIcons[i];
                if (icon.ObjectId == objectId)
                {
                    return icon;
                }
            }

            return null;
        }

        private TrackedVehicleIcon CreateTrackedVehicleIcon(GameplayMapVisibilityEntry entry)
        {
            RectTransform miniIcon = CreateIconInstance(localPlayerIconMini, "TrackedVehicleIconMini");
            RectTransform fullIcon = CreateIconInstance(localPlayerIconFull, "TrackedVehicleIconFull");
            if (miniIcon == null && fullIcon == null)
            {
                return null;
            }

            TrackedVehicleIcon icon = new TrackedVehicleIcon
            {
                ObjectId = entry.ObjectId,
                MiniIcon = miniIcon,
                FullIcon = fullIcon,
                MiniGraphic = GetIconGraphic(miniIcon),
                FullGraphic = GetIconGraphic(fullIcon),
                Relation = MapVehicleVisibilityRelation.Hidden,
                WorldPosition = entry.Position,
                Yaw = entry.Yaw,
                Seen = true
            };

            ApplyTrackedVehicleRelation(icon, entry.Relation);
            return icon;
        }

        private RectTransform CreateIconInstance(RectTransform template, string iconName)
        {
            if (template == null || template.parent == null)
            {
                return null;
            }

            RectTransform icon = Instantiate(template, template.parent);
            icon.name = iconName;
            bool isMiniMapIcon = template == localPlayerIconMini;
            icon.localScale = isMiniMapIcon
                ? Vector3.Scale(
                    _localPlayerMiniIconBaseScale,
                    new Vector3(
                        Mathf.Max(0.1f, _runtimeSettings.mapMiniMapIconScale)
                            * Mathf.Max(0.1f, _runtimeSettings.mapMiniMapIconScaleX),
                        Mathf.Max(0.1f, _runtimeSettings.mapMiniMapIconScale)
                            * Mathf.Max(0.1f, _runtimeSettings.mapMiniMapIconScaleY),
                        1f))
                : _localPlayerFullIconBaseScale;
            icon.sizeDelta = template.sizeDelta * Mathf.Max(0.1f, _runtimeSettings.mapTrackedVehicleIconScale);
            SetIconVisible(icon, false);

            Graphic graphic = icon.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = false;
            }

            return icon;
        }

        private void ApplyTrackedVehicleRelation(TrackedVehicleIcon icon, MapVehicleVisibilityRelation relation)
        {
            icon.Relation = relation;
            bool enemy = relation == MapVehicleVisibilityRelation.Enemy
                         || relation == MapVehicleVisibilityRelation.EnemyLastKnown;
            Color color = relation == MapVehicleVisibilityRelation.Destroyed
                ? _runtimeSettings.mapDestroyedIconColor
                : enemy
                    ? _runtimeSettings.mapEnemyIconColor
                    : _runtimeSettings.mapAllyIconColor;
            Sprite sprite = enemy || relation == MapVehicleVisibilityRelation.Destroyed
                ? enemyVehicleIcon
                : alliedVehicleIcon;
            ApplyIconSprite(icon.MiniGraphic, sprite);
            ApplyIconSprite(icon.FullGraphic, sprite);
            ApplyIconColor(icon.MiniGraphic, color);
            ApplyIconColor(icon.FullGraphic, color);
        }

        private static void ApplyIconSprite(Graphic target, Sprite sprite)
        {
            if (target == null || sprite == null)
            {
                return;
            }

            Image image = target as Image;
            if (image != null)
            {
                image.sprite = sprite;
            }
        }

        private static bool TryGetIconGraphicAndColor(RectTransform iconRect, out Graphic graphic, out Color color)
        {
            graphic = GetIconGraphic(iconRect);
            if (graphic != null)
            {
                color = graphic.color;
                return true;
            }

            color = Color.white;
            return false;
        }

        private static Graphic GetIconGraphic(RectTransform iconRect)
        {
            if (iconRect != null)
            {
                return iconRect.GetComponent<Graphic>();
            }

            return null;
        }

        private static void ApplyIconColor(Graphic graphic, Color color)
        {
            if (graphic != null)
            {
                graphic.color = color;
            }
        }

        private void DestroyTrackedVehicleIcon(TrackedVehicleIcon icon)
        {
            if (icon == null)
            {
                return;
            }

            DestroyIcon(icon.MiniIcon);
            DestroyIcon(icon.FullIcon);
        }

        private void ClearTrackedVehicleIcons()
        {
            for (int i = 0; i < _trackedVehicleIcons.Count; i++)
            {
                DestroyTrackedVehicleIcon(_trackedVehicleIcons[i]);
            }

            _trackedVehicleIcons.Clear();
        }

        private void BringLocalPlayerIconsToFront()
        {
            if (localPlayerIconMini != null)
            {
                localPlayerIconMini.SetAsLastSibling();
            }

            if (localPlayerIconFull != null)
            {
                localPlayerIconFull.SetAsLastSibling();
            }
        }

        private static void DestroyIcon(RectTransform iconRect)
        {
            if (iconRect != null)
            {
                Destroy(iconRect.gameObject);
            }
        }

        private void UpdateIcon(
            RectTransform mapRect,
            RectTransform iconRect,
            Vector3 worldPosition,
            float yaw,
            bool visible,
            Vector2 positionOffset)
        {
            if (!visible || mapRect == null || iconRect == null || !TryGetNormalizedPosition(worldPosition, out Vector2 normalized))
            {
                SetIconVisible(iconRect, false);
                return;
            }

            SetIconVisible(iconRect, true);

            Rect rect = mapRect.rect;
            Vector2 localMapPosition = new Vector2(
                (normalized.x - mapRect.pivot.x) * rect.width,
                (normalized.y - mapRect.pivot.y) * rect.height);

            iconRect.position = mapRect.TransformPoint(localMapPosition) + (Vector3)positionOffset;

            if (_runtimeSettings.mapRotateIcons)
            {
                iconRect.localRotation = Quaternion.Euler(0f, 0f, -yaw);
            }
            else
            {
                iconRect.localRotation = Quaternion.identity;
            }
        }

        private bool TryGetNormalizedPosition(Vector3 worldPosition, out Vector2 normalized)
        {
            float width = worldMax.x - worldMin.x;
            float height = worldMax.y - worldMin.y;
            if (Mathf.Abs(width) <= 0.0001f || Mathf.Abs(height) <= 0.0001f)
            {
                normalized = default;
                return false;
            }

            float normalizedX = (worldPosition.x - worldMin.x) / width;
            float normalizedY = (worldPosition.z - worldMin.y) / height;
            normalized = new Vector2(
                Mathf.Clamp01(normalizedX),
                Mathf.Clamp01(normalizedY));
            return true;
        }

        private void RefreshTerrainBounds()
        {
            if (!useActiveTerrainBounds)
            {
                return;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            if (terrainSize.x <= 0.0001f || terrainSize.z <= 0.0001f)
            {
                return;
            }

            worldMin = new Vector2(terrainPosition.x, terrainPosition.z);
            worldMax = new Vector2(terrainPosition.x + terrainSize.x, terrainPosition.z + terrainSize.z);
            _terrainBoundsSource = terrain;
            _terrainBoundsResolved = true;
        }

        private void SetFullMapVisible(bool visible)
        {
            if (_fullMapVisible == visible)
            {
                return;
            }

            _fullMapVisible = visible;
            GameObject root = fullMapRoot != null
                ? fullMapRoot
                : fullMapImage != null && fullMapImage.transform.parent != null
                    ? fullMapImage.transform.parent.gameObject
                    : null;

            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        private static void SetIconVisible(RectTransform iconRect, bool visible)
        {
            if (iconRect != null && iconRect.gameObject.activeSelf != visible)
            {
                iconRect.gameObject.SetActive(visible);
            }
        }

        private sealed class TrackedVehicleIcon
        {
            public int ObjectId;
            public RectTransform MiniIcon;
            public RectTransform FullIcon;
            public Graphic MiniGraphic;
            public Graphic FullGraphic;
            public MapVehicleVisibilityRelation Relation;
            public Vector3 WorldPosition;
            public float Yaw;
            public bool Seen;
        }
    }
}
