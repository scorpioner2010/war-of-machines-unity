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
        public Image fullMapImage;
        public RectTransform localPlayerIconMini;
        public RectTransform localPlayerIconFull;
        public GameObject fullMapRoot;
        public Vector2 worldMin = Vector2.zero;
        public Vector2 worldMax = new Vector2(256f, 256f);
        public bool useActiveTerrainBounds = true;

        private VehicleRoot _localPlayer;
        private RectTransform _miniMapRect;
        private RectTransform _fullMapRect;
        private Terrain _terrainBoundsSource;
        private bool _terrainBoundsResolved;
        private bool _fullMapVisible;
        private int _appliedVisibilityVersion = -1;
        private Color _localPlayerMiniIconColor;
        private Color _localPlayerFullIconColor;
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

                if (useActiveTerrainBounds && (!_terrainBoundsResolved || _terrainBoundsSource == null))
                {
                    RefreshTerrainBounds();
                }

                bool shouldShowFullMap = Input.GetKey(_runtimeSettings.mapFullMapKey);
                SetFullMapVisible(shouldShowFullMap);

                if (_localPlayer == null)
                {
                    SetIconVisible(localPlayerIconMini, false);
                    SetIconVisible(localPlayerIconFull, false);
                    ClearTrackedVehicleIcons();
                    return;
                }

                RefreshTrackedVehiclesIfNeeded();

                Transform trackedTransform = GetTrackedTransform(_localPlayer);
                Vector3 worldPosition = trackedTransform.position;
                float yaw = trackedTransform.eulerAngles.y;

                UpdateIcon(_miniMapRect, localPlayerIconMini, worldPosition, yaw, true);
                UpdateIcon(_fullMapRect, localPlayerIconFull, worldPosition, yaw, _fullMapVisible);
                ApplyLocalPlayerIconColor();
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
            _miniMapRect = miniMapImage != null ? miniMapImage.rectTransform : null;
            _fullMapRect = fullMapImage != null ? fullMapImage.rectTransform : null;
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
                UpdateIcon(_miniMapRect, icon.MiniIcon, icon.WorldPosition, icon.Yaw, true);
                UpdateIcon(_fullMapRect, icon.FullIcon, icon.WorldPosition, icon.Yaw, _fullMapVisible);
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
            icon.localScale = template.localScale;
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
            ApplyIconColor(icon.MiniGraphic, color);
            ApplyIconColor(icon.FullGraphic, color);
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
            bool visible)
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

            iconRect.position = mapRect.TransformPoint(localMapPosition);

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
