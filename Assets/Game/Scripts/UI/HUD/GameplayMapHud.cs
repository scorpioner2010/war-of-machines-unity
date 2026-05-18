using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using Game.Scripts.Client;
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
        private float _nextTrackedVehicleRefreshTime;
        private Color _localPlayerMiniIconColor;
        private Color _localPlayerFullIconColor;
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
            SetLocalPlayer(VehicleRoot.LocalPlayerVehicle);
        }

        private void OnDisable()
        {
            VehicleRoot.LocalPlayerVehicleChanged -= SetLocalPlayer;
            ClearTrackedVehicleIcons();
        }

        private void Update()
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

        public void SetLocalPlayer(VehicleRoot vehicleRoot)
        {
            _localPlayer = vehicleRoot;
            _nextTrackedVehicleRefreshTime = 0f;
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

        private void CacheLocalPlayerIconColors()
        {
            _hasLocalPlayerMiniIconColor = TryGetIconColor(localPlayerIconMini, out _localPlayerMiniIconColor);
            _hasLocalPlayerFullIconColor = TryGetIconColor(localPlayerIconFull, out _localPlayerFullIconColor);
        }

        private void ApplyLocalPlayerIconColor()
        {
            bool destroyed = _localPlayer != null && _localPlayer.health != null && _localPlayer.health.IsDead;

            if (_hasLocalPlayerMiniIconColor)
            {
                ApplyIconColor(localPlayerIconMini, destroyed ? _runtimeSettings.mapDestroyedIconColor : _localPlayerMiniIconColor);
            }

            if (_hasLocalPlayerFullIconColor)
            {
                ApplyIconColor(localPlayerIconFull, destroyed ? _runtimeSettings.mapDestroyedIconColor : _localPlayerFullIconColor);
            }
        }

        private void RefreshTrackedVehiclesIfNeeded()
        {
            if (Time.unscaledTime < _nextTrackedVehicleRefreshTime)
            {
                return;
            }

            float interval = Mathf.Max(0.1f, _runtimeSettings.mapTrackedVehicleRefreshInterval);
            _nextTrackedVehicleRefreshTime = Time.unscaledTime + interval;
            RefreshTrackedVehicles();
        }

        private void RefreshTrackedVehicles()
        {
            for (int i = 0; i < _trackedVehicleIcons.Count; i++)
            {
                _trackedVehicleIcons[i].Seen = false;
            }

            VehicleRoot[] vehicles = FindObjectsByType<VehicleRoot>(FindObjectsSortMode.None);
            for (int i = 0; i < vehicles.Length; i++)
            {
                VehicleRoot vehicleRoot = vehicles[i];
                MapVehicleRelation relation = GetVehicleRelation(vehicleRoot);
                if (relation == MapVehicleRelation.Hidden)
                {
                    continue;
                }

                TrackedVehicleIcon icon = FindTrackedVehicleIcon(vehicleRoot);
                if (icon == null)
                {
                    icon = CreateTrackedVehicleIcon(vehicleRoot, relation);
                    if (icon == null)
                    {
                        continue;
                    }

                    _trackedVehicleIcons.Add(icon);
                }

                icon.Seen = true;
                ApplyTrackedVehicleRelation(icon, relation);
            }

            for (int i = _trackedVehicleIcons.Count - 1; i >= 0; i--)
            {
                TrackedVehicleIcon icon = _trackedVehicleIcons[i];
                if (!icon.Seen || icon.VehicleRoot == null)
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
                if (icon.VehicleRoot == null)
                {
                    DestroyTrackedVehicleIcon(icon);
                    _trackedVehicleIcons.RemoveAt(i);
                    continue;
                }

                Transform trackedTransform = GetTrackedTransform(icon.VehicleRoot);
                Vector3 worldPosition = trackedTransform.position;
                float yaw = trackedTransform.eulerAngles.y;
                UpdateIcon(_miniMapRect, icon.MiniIcon, worldPosition, yaw, true);
                UpdateIcon(_fullMapRect, icon.FullIcon, worldPosition, yaw, _fullMapVisible);
            }
        }

        private MapVehicleRelation GetVehicleRelation(VehicleRoot vehicleRoot)
        {
            if (_localPlayer == null || vehicleRoot == null || vehicleRoot == _localPlayer)
            {
                return MapVehicleRelation.Hidden;
            }

            if (_localPlayer.IsMenu || vehicleRoot.IsMenu)
            {
                return MapVehicleRelation.Hidden;
            }

            if (vehicleRoot.health != null && vehicleRoot.health.IsDead)
            {
                return MapVehicleRelation.Destroyed;
            }

            if (_localPlayer.characterInit == null || vehicleRoot.characterInit == null)
            {
                return MapVehicleRelation.Hidden;
            }

            MatchTeam localTeam = _localPlayer.characterInit.Team.Value;
            MatchTeam targetTeam = vehicleRoot.characterInit.Team.Value;
            if (!MatchTeamUtility.IsAssigned(localTeam) || !MatchTeamUtility.IsAssigned(targetTeam))
            {
                return MapVehicleRelation.Hidden;
            }

            if (MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam))
            {
                return MapVehicleRelation.Ally;
            }

            return MapVehicleRelation.Enemy;
        }

        private TrackedVehicleIcon FindTrackedVehicleIcon(VehicleRoot vehicleRoot)
        {
            for (int i = 0; i < _trackedVehicleIcons.Count; i++)
            {
                TrackedVehicleIcon icon = _trackedVehicleIcons[i];
                if (icon.VehicleRoot == vehicleRoot)
                {
                    return icon;
                }
            }

            return null;
        }

        private TrackedVehicleIcon CreateTrackedVehicleIcon(VehicleRoot vehicleRoot, MapVehicleRelation relation)
        {
            RectTransform miniIcon = CreateIconInstance(localPlayerIconMini, "TrackedVehicleIconMini");
            RectTransform fullIcon = CreateIconInstance(localPlayerIconFull, "TrackedVehicleIconFull");
            if (miniIcon == null && fullIcon == null)
            {
                return null;
            }

            TrackedVehicleIcon icon = new TrackedVehicleIcon
            {
                VehicleRoot = vehicleRoot,
                MiniIcon = miniIcon,
                FullIcon = fullIcon,
                Relation = MapVehicleRelation.Hidden,
                Seen = true
            };

            ApplyTrackedVehicleRelation(icon, relation);
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

        private void ApplyTrackedVehicleRelation(TrackedVehicleIcon icon, MapVehicleRelation relation)
        {
            icon.Relation = relation;
            Color color = relation == MapVehicleRelation.Destroyed
                ? _runtimeSettings.mapDestroyedIconColor
                : relation == MapVehicleRelation.Enemy
                    ? _runtimeSettings.mapEnemyIconColor
                    : _runtimeSettings.mapAllyIconColor;
            ApplyIconColor(icon.MiniIcon, color);
            ApplyIconColor(icon.FullIcon, color);
        }

        private static bool TryGetIconColor(RectTransform iconRect, out Color color)
        {
            if (iconRect != null)
            {
                Graphic graphic = iconRect.GetComponent<Graphic>();
                if (graphic != null)
                {
                    color = graphic.color;
                    return true;
                }
            }

            color = Color.white;
            return false;
        }

        private static void ApplyIconColor(RectTransform iconRect, Color color)
        {
            if (iconRect == null)
            {
                return;
            }

            Graphic graphic = iconRect.GetComponent<Graphic>();
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

        private enum MapVehicleRelation
        {
            Hidden,
            Ally,
            Enemy,
            Destroyed
        }

        private sealed class TrackedVehicleIcon
        {
            public VehicleRoot VehicleRoot;
            public RectTransform MiniIcon;
            public RectTransform FullIcon;
            public MapVehicleRelation Relation;
            public bool Seen;
        }
    }
}
