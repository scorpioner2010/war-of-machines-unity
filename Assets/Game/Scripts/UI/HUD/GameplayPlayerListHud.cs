using System;
using System.Collections.Generic;
using Game.Scripts.Client;
using Game.Scripts.Diagnostics;
using Game.Scripts.Gameplay.Robots;
using Game.Scripts.Networking.Lobby;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.UI.HUD
{
    public class GameplayPlayerListHud : MonoBehaviour
    {
        public static GameplayPlayerListHud In { get; private set; }

        [SerializeField] private GameplayPlayerListItem itemPrefab;
        [SerializeField] private RectTransform alliesContainer;
        [SerializeField] private RectTransform enemiesContainer;
        [SerializeField] private bool autoTrackSpawnedVehicles = true;
        [SerializeField] private float vehicleScanInterval = 0.5f;
        [SerializeField] private float rowRefreshInterval = 0.25f;
        [SerializeField] private float rowHeight = 24f;
        [SerializeField] private float rowSpacing = 2f;
        [SerializeField] private bool hideEmptyLists;
        [SerializeField] private Color fallbackAlliedHpColor = new Color(0.1f, 0.35f, 1f, 1f);
        [SerializeField] private Color fallbackEnemyHpColor = new Color(1f, 0.08f, 0.04f, 1f);

        private readonly List<PlayerListRow> _rows = new List<PlayerListRow>(32);
        private float _nextVehicleScanTime;
        private float _nextRowRefreshTime;

        public bool AutoTrackSpawnedVehicles
        {
            get
            {
                return autoTrackSpawnedVehicles;
            }
            set
            {
                if (autoTrackSpawnedVehicles == value)
                {
                    return;
                }

                autoTrackSpawnedVehicles = value;
                _nextVehicleScanTime = 0f;

                if (autoTrackSpawnedVehicles)
                {
                    Clear();
                    RefreshFromSpawnedVehicles();
                }
            }
        }

        private void Awake()
        {
            In = this;
            _nextVehicleScanTime = 0f;
        }

        private void OnEnable()
        {
            VehicleRoot.LocalPlayerVehicleChanged += OnLocalPlayerVehicleChanged;

            if (autoTrackSpawnedVehicles)
            {
                RefreshFromSpawnedVehicles();
            }
        }

        private void OnDisable()
        {
            VehicleRoot.LocalPlayerVehicleChanged -= OnLocalPlayerVehicleChanged;
        }

        private void OnDestroy()
        {
            VehicleRoot.LocalPlayerVehicleChanged -= OnLocalPlayerVehicleChanged;
            Clear();

            if (In == this)
            {
                In = null;
            }
        }

        private void Update()
        {
            using (ProfileScope.Measure("Client.UI.GameplayPlayerListHud.Update", DiagnosticsCategories.Ui))
            {
                float now = Time.unscaledTime;
                if (autoTrackSpawnedVehicles && now >= _nextVehicleScanTime)
                {
                    RefreshFromSpawnedVehicles();
                }

                if (now >= _nextRowRefreshTime)
                {
                    _nextRowRefreshTime = now + Mathf.Max(0.05f, rowRefreshInterval);
                    RefreshVisibleRows();
                }
            }
        }

        public void Initialize(PlayerListEntryData[] allies, PlayerListEntryData[] enemies)
        {
            Initialize((IList<PlayerListEntryData>)allies, enemies);
        }

        public void Initialize(IList<PlayerListEntryData> allies, IList<PlayerListEntryData> enemies)
        {
            autoTrackSpawnedVehicles = false;
            Clear();
            AddManualRows(allies, PlayerListRelation.Ally);
            AddManualRows(enemies, PlayerListRelation.Enemy);
            LayoutRows();
        }

        public void SetPlayerHealth(string playerId, float currentHealth, float maxHealth)
        {
            PlayerListRow row = FindRowById(playerId);
            if (row == null)
            {
                return;
            }

            row.CurrentHealth = Mathf.Max(0f, currentHealth);
            row.MaxHealth = Mathf.Max(1f, maxHealth);

            if (row.CurrentHealth <= 0f)
            {
                row.IsDead = true;
            }

            ApplyRow(row);
        }

        public void SetPlayerDead(string playerId, bool isDead)
        {
            PlayerListRow row = FindRowById(playerId);
            if (row == null)
            {
                return;
            }

            row.IsDead = isDead;

            if (isDead)
            {
                row.CurrentHealth = 0f;
            }

            ApplyRow(row);
        }

        public void Clear()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                PlayerListRow row = _rows[i];
                if (row != null && row.Item != null)
                {
                    Destroy(row.Item.gameObject);
                }

                UnbindRow(row);
            }

            _rows.Clear();
            SetContainerVisible(alliesContainer, false);
            SetContainerVisible(enemiesContainer, false);
        }

        public void RefreshFromSpawnedVehicles()
        {
            using (ProfileScope.Measure("Client.UI.GameplayPlayerListHud.RefreshFromSpawnedVehicles", DiagnosticsCategories.Ui))
            {
                RefreshFromSpawnedVehiclesInternal();
            }
        }

        private void RefreshFromSpawnedVehiclesInternal()
        {
            float interval = Mathf.Max(0.1f, vehicleScanInterval);
            _nextVehicleScanTime = Time.unscaledTime + interval;

            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].Seen = false;
            }

            int vehicleCount = VehicleRoot.ActiveVehicleCount;
            for (int i = 0; i < vehicleCount; i++)
            {
                VehicleRoot vehicleRoot = VehicleRoot.GetActiveVehicle(i);
                if (!ShouldTrackVehicle(vehicleRoot))
                {
                    continue;
                }

                PlayerListRelation relation = GetRelation(vehicleRoot);
                PlayerListRow row = FindRow(vehicleRoot);
                if (row == null)
                {
                    row = CreateRow(BuildVehiclePlayerId(vehicleRoot), relation, vehicleRoot);
                    if (row == null)
                    {
                        continue;
                    }

                    _rows.Add(row);
                }

                row.Seen = true;
                BindRow(row, vehicleRoot);
                MoveRow(row, relation);
                ReadVehicleRowData(row);
                ApplyRow(row);
            }

            for (int i = _rows.Count - 1; i >= 0; i--)
            {
                PlayerListRow row = _rows[i];
                if (row.VehicleRoot != null && !row.Seen)
                {
                    if (row.Item != null)
                    {
                        Destroy(row.Item.gameObject);
                    }

                    UnbindRow(row);
                    _rows.RemoveAt(i);
                }
            }

            LayoutRows();
        }

        private void AddManualRows(IList<PlayerListEntryData> source, PlayerListRelation relation)
        {
            if (source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                PlayerListEntryData data = source[i];
                string playerId = string.IsNullOrEmpty(data.playerId) ? data.nickname : data.playerId;
                PlayerListRow row = CreateRow(playerId, relation, null);
                if (row == null)
                {
                    continue;
                }

                row.Nickname = data.nickname;
                row.VehicleType = data.vehicleType;
                row.CurrentHealth = Mathf.Max(0f, data.currentHealth);
                row.MaxHealth = Mathf.Max(1f, data.maxHealth);
                row.IsDead = data.isDead || row.CurrentHealth <= 0f;
                row.Seen = true;
                _rows.Add(row);
                ApplyRow(row);
            }
        }

        private PlayerListRow CreateRow(string playerId, PlayerListRelation relation, VehicleRoot vehicleRoot)
        {
            RectTransform parent = GetContainer(relation);
            if (itemPrefab == null || parent == null)
            {
                return null;
            }

            GameplayPlayerListItem item = Instantiate(itemPrefab, parent, false);
            item.name = "PlayerListItem";

            RectTransform rectTransform = item.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(0f, rowHeight);
            }

            PlayerListRow row = new PlayerListRow
            {
                PlayerId = playerId,
                Item = item,
                Relation = relation,
                MaxHealth = 1f
            };

            BindRow(row, vehicleRoot);
            return row;
        }

        private void MoveRow(PlayerListRow row, PlayerListRelation relation)
        {
            if (row == null || row.Relation == relation || row.Item == null)
            {
                return;
            }

            RectTransform targetParent = GetContainer(relation);
            if (targetParent == null)
            {
                return;
            }

            row.Relation = relation;
            row.Item.transform.SetParent(targetParent, false);
        }

        private void RefreshVisibleRows()
        {
            using (ProfileScope.Measure("Client.UI.GameplayPlayerListHud.RefreshVisibleRows", DiagnosticsCategories.Ui))
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    PlayerListRow row = _rows[i];
                    if (row == null)
                    {
                        continue;
                    }

                    if (row.VehicleRoot != null)
                    {
                        ReadVehicleRowData(row);
                    }

                    ApplyRow(row);
                }
            }
        }

        private void ReadVehicleRowData(PlayerListRow row)
        {
            VehicleRoot vehicleRoot = row.VehicleRoot;
            if (vehicleRoot == null)
            {
                return;
            }

            row.Nickname = ResolveNickname(vehicleRoot);
            row.VehicleType = ResolveVehicleType(vehicleRoot);

            if (vehicleRoot.health != null)
            {
                row.CurrentHealth = vehicleRoot.health.Current;
                row.MaxHealth = vehicleRoot.health.MaxHealth;
                row.IsDead = vehicleRoot.health.IsDead || row.CurrentHealth <= 0f;
            }
            else
            {
                row.CurrentHealth = 0f;
                row.MaxHealth = 1f;
                row.IsDead = true;
            }
        }

        private void BindRow(PlayerListRow row, VehicleRoot vehicleRoot)
        {
            if (row == null)
            {
                return;
            }

            VehicleHealth health = vehicleRoot != null ? vehicleRoot.health : null;
            if (row.VehicleRoot == vehicleRoot && row.Health == health)
            {
                return;
            }

            UnbindRow(row);
            row.VehicleRoot = vehicleRoot;
            row.Health = health;

            if (health == null)
            {
                return;
            }

            row.HealthChangedHandler = (currentHealth, maxHealth) =>
            {
                OnRowHealthChanged(row, currentHealth, maxHealth);
            };
            row.DamagedHandler = (damage, currentHealth, maxHealth) =>
            {
                OnRowHealthChanged(row, currentHealth, maxHealth);
            };
            row.DeathHandler = () =>
            {
                OnRowDeath(row);
            };

            health.OnHealthChanged += row.HealthChangedHandler;
            health.OnDamaged += row.DamagedHandler;
            health.onDeath.AddListener(row.DeathHandler);
        }

        private void UnbindRow(PlayerListRow row)
        {
            if (row == null || row.Health == null)
            {
                return;
            }

            if (row.HealthChangedHandler != null)
            {
                row.Health.OnHealthChanged -= row.HealthChangedHandler;
            }

            if (row.DamagedHandler != null)
            {
                row.Health.OnDamaged -= row.DamagedHandler;
            }

            if (row.DeathHandler != null)
            {
                row.Health.onDeath.RemoveListener(row.DeathHandler);
            }

            row.Health = null;
            row.HealthChangedHandler = null;
            row.DamagedHandler = null;
            row.DeathHandler = null;
        }

        private void OnRowHealthChanged(PlayerListRow row, float currentHealth, float maxHealth)
        {
            if (row == null || row.Item == null)
            {
                return;
            }

            row.CurrentHealth = Mathf.Max(0f, currentHealth);
            row.MaxHealth = Mathf.Max(1f, maxHealth);
            row.IsDead = row.CurrentHealth <= 0f || row.Health != null && row.Health.IsDead;
            ApplyRow(row);
        }

        private void OnRowDeath(PlayerListRow row)
        {
            if (row == null || row.Item == null)
            {
                return;
            }

            row.CurrentHealth = 0f;
            row.IsDead = true;
            ApplyRow(row);
        }

        private void ApplyRow(PlayerListRow row)
        {
            if (row == null || row.Item == null)
            {
                return;
            }

            row.Item.SetData(
                row.Nickname,
                row.VehicleType,
                row.CurrentHealth,
                row.MaxHealth,
                row.IsDead,
                GetHealthColor(row.Relation));
        }

        private void LayoutRows()
        {
            LayoutRows(PlayerListRelation.Ally);
            LayoutRows(PlayerListRelation.Enemy);
        }

        private void LayoutRows(PlayerListRelation relation)
        {
            RectTransform container = GetContainer(relation);
            if (container == null)
            {
                return;
            }

            int rowIndex = 0;
            for (int i = 0; i < _rows.Count; i++)
            {
                PlayerListRow row = _rows[i];
                if (row == null || row.Relation != relation || row.Item == null)
                {
                    continue;
                }

                RectTransform rectTransform = row.Item.transform as RectTransform;
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0f, 1f);
                    rectTransform.anchorMax = new Vector2(1f, 1f);
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                    rectTransform.anchoredPosition = new Vector2(0f, -rowIndex * (rowHeight + rowSpacing));
                    rectTransform.sizeDelta = new Vector2(0f, rowHeight);
                }

                row.Item.transform.SetSiblingIndex(rowIndex);
                rowIndex++;
            }

            float height = rowIndex > 0 ? rowIndex * rowHeight + Mathf.Max(0, rowIndex - 1) * rowSpacing : rowHeight;
            container.sizeDelta = new Vector2(container.sizeDelta.x, height);
            SetContainerVisible(container, !hideEmptyLists || rowIndex > 0);
        }

        private PlayerListRow FindRow(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null)
            {
                return null;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                PlayerListRow row = _rows[i];
                if (row != null && row.VehicleRoot == vehicleRoot)
                {
                    return row;
                }
            }

            return null;
        }

        private PlayerListRow FindRowById(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                return null;
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                PlayerListRow row = _rows[i];
                if (row != null && row.PlayerId == playerId)
                {
                    return row;
                }
            }

            return null;
        }

        private RectTransform GetContainer(PlayerListRelation relation)
        {
            return relation == PlayerListRelation.Ally ? alliesContainer : enemiesContainer;
        }

        private Color GetHealthColor(PlayerListRelation relation)
        {
            GameplayRuntimeSettings settings = GameplayRuntimeSettingsProvider.Get();
            if (settings == null)
            {
                return relation == PlayerListRelation.Ally ? fallbackAlliedHpColor : fallbackEnemyHpColor;
            }

            return relation == PlayerListRelation.Ally ? settings.alliedHpColor : settings.enemyHpColor;
        }

        private PlayerListRelation GetRelation(VehicleRoot vehicleRoot)
        {
            VehicleRoot localPlayer = VehicleRoot.LocalPlayerVehicle;
            if (localPlayer != null && localPlayer.characterInit != null && vehicleRoot.characterInit != null)
            {
                MatchTeam localTeam = localPlayer.characterInit.Team.Value;
                MatchTeam targetTeam = vehicleRoot.characterInit.Team.Value;
                if (MatchTeamUtility.IsAssigned(localTeam) && MatchTeamUtility.IsAssigned(targetTeam))
                {
                    return MatchTeamUtility.AreSameAssignedTeam(localTeam, targetTeam)
                        ? PlayerListRelation.Ally
                        : PlayerListRelation.Enemy;
                }
            }

            if (vehicleRoot.characterInit != null && vehicleRoot.characterInit.Team.Value == MatchTeam.TeamB)
            {
                return PlayerListRelation.Enemy;
            }

            return PlayerListRelation.Ally;
        }

        private static bool ShouldTrackVehicle(VehicleRoot vehicleRoot)
        {
            return vehicleRoot != null && !vehicleRoot.IsMenu;
        }

        private static string BuildVehiclePlayerId(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null)
            {
                return string.Empty;
            }

            if (vehicleRoot.characterInit != null && !string.IsNullOrEmpty(vehicleRoot.characterInit.LoginName.Value))
            {
                return vehicleRoot.characterInit.LoginName.Value;
            }

            return vehicleRoot.GetInstanceID().ToString();
        }

        private static string ResolveNickname(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot != null && vehicleRoot.characterInit != null && !string.IsNullOrEmpty(vehicleRoot.characterInit.LoginName.Value))
            {
                return vehicleRoot.characterInit.LoginName.Value;
            }

            if (vehicleRoot != null && vehicleRoot.OwnerId >= 0)
            {
                return "Player " + vehicleRoot.OwnerId;
            }

            return "-";
        }

        private static string ResolveVehicleType(VehicleRoot vehicleRoot)
        {
            if (vehicleRoot == null)
            {
                return "-";
            }

            VehicleRuntimeStats stats = vehicleRoot.RuntimeStats;
            if (stats != null)
            {
                if (!string.IsNullOrEmpty(stats.Name))
                {
                    return stats.Name;
                }

                if (!string.IsNullOrEmpty(stats.Code))
                {
                    return stats.Code;
                }
            }

            return vehicleRoot.name;
        }

        private static void SetContainerVisible(RectTransform container, bool visible)
        {
            if (container != null && container.gameObject.activeSelf != visible)
            {
                container.gameObject.SetActive(visible);
            }
        }

        private void OnLocalPlayerVehicleChanged(VehicleRoot vehicleRoot)
        {
            _nextVehicleScanTime = 0f;

            if (autoTrackSpawnedVehicles)
            {
                RefreshFromSpawnedVehicles();
            }
        }

        [Serializable]
        public struct PlayerListEntryData
        {
            public string playerId;
            public string nickname;
            public string vehicleType;
            public float currentHealth;
            public float maxHealth;
            public bool isDead;
        }

        private enum PlayerListRelation
        {
            Ally,
            Enemy
        }

        private sealed class PlayerListRow
        {
            public string PlayerId;
            public VehicleRoot VehicleRoot;
            public GameplayPlayerListItem Item;
            public PlayerListRelation Relation;
            public bool Seen;
            public string Nickname;
            public string VehicleType;
            public float CurrentHealth;
            public float MaxHealth = 1f;
            public bool IsDead;
            public VehicleHealth Health;
            public Action<float, float> HealthChangedHandler;
            public Action<float, float, float> DamagedHandler;
            public UnityAction DeathHandler;
        }
    }
}
