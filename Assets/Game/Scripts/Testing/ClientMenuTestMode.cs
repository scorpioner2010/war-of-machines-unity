using System;
using System.Collections.Generic;
using Game.Scripts.API;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.MenuController;
using UnityEngine;

namespace Game.Scripts.Testing
{
    [Serializable]
    public class ClientMenuTestVehicle
    {
        [Min(1)] public int vehicleId = 1;
        [Tooltip("Vehicle code from RobotRegistry, for example ia_l1_starter or nv_l1_starter.")]
        public string code = "nv_l1_starter";
        public string vehicleName = "NV Starter";
        [Tooltip("Use iron_alliance or nova_syndicate for the ready-made tech-tree visuals.")]
        public string factionCode = "nova_syndicate";
        public string factionName = "Nova Syndicate";
        public string vehicleClass = "Assault";
        [Range(1, 10)] public int level = 1;
        public string branch = "vanguard";

        [Header("Profile history demo")]
        [Min(0)] public int profileCurrentXp;
        [Min(0)] public int profileBattles;
        [Min(0)] public int profileWins;
        [Min(0)] public int profileDraws;
        [Min(0)] public int profileKills;
        [Min(0)] public int profileDamage;
        [Min(0)] public int profileBestKills;
        [Min(0)] public int profileBestDamage;
        [Min(0)] public int profileXpEarned;
        [Min(0)] public int profileBoltsEarned;
        [Min(0)] public int profileFreeXpEarned;
        public string profileLastPlayedAt;
    }

    /// <summary>
    /// Scene-local switch for exercising the client menus without a FishNet connection or backend data.
    /// </summary>
    public class ClientMenuTestMode : MonoBehaviour
    {
        private static ClientMenuTestMode _instance;

        [Header("Offline Menu Testing")]
        [Tooltip("Enable this to open Client.unity in the main menu with local demo data without connecting to the server.")]
        public bool enableOfflineMenuTest;

        [Header("Demo Profile")]
        [SerializeField] private string testPlayerName = "Menu Tester";
        [SerializeField] private int testMmr = 1620;
        [SerializeField] private int testBolts = 54320;
        [SerializeField] private int testAdamant = 880;
        [SerializeField] private int testFreeXp = 26400;
        [SerializeField] private int testMmrDelta = 112;
        [SerializeField] private string testLastBattleAt = "2026-11-03T20:12:00Z";

        [Header("Test Vehicles")]
        [Tooltip("The offline catalog is regenerated on Awake so every run starts from the same clear, coherent demo roster.")]
        [SerializeField] private ClientMenuTestVehicle[] testVehicles;

        private static ClientMenuTestVehicle[] CreateGeneratedTestVehicles()
        {
            return new[]
            {
                CreateTestVehicleDefinition(1, "ia_l1_starter", "Aegis Vanguard", "iron_alliance", "Iron Alliance", "Assault", 1, "vanguard", "2026-11-03T18:42:00Z", 1),
                CreateTestVehicleDefinition(2, "ia_l2_scout", "Strider Scout", "iron_alliance", "Iron Alliance", "Scout", 2, "recon", "2026-11-02T21:18:00Z", 2),
                CreateTestVehicleDefinition(3, "ia_l2_guardian", "Bastion Guardian", "iron_alliance", "Iron Alliance", "Defender", 2, "bulwark", "2026-11-02T16:05:00Z", 3),
                CreateTestVehicleDefinition(4, "ia_l2_colossus", "Colossus Breaker", "iron_alliance", "Iron Alliance", "Heavy", 2, "siege", "2026-11-01T19:42:00Z", 4),
                CreateTestVehicleDefinition(5, "ia_l2_scout", "Longshot Mk II", "iron_alliance", "Iron Alliance", "Sniper", 2, "artillery", "2026-10-31T20:27:00Z", 5),
                CreateTestVehicleDefinition(6, "ia_l2_guardian", "Bulwark Medic", "iron_alliance", "Iron Alliance", "Support", 2, "support", "2026-10-30T18:54:00Z", 6),
                CreateTestVehicleDefinition(7, "ia_l2_colossus", "Raptor Assault", "iron_alliance", "Iron Alliance", "Assault", 2, "striker", "2026-10-29T22:11:00Z", 7),
                CreateTestVehicleDefinition(8, "nv_l1_starter", "Specter Recon", "nova_syndicate", "Nova Syndicate", "Scout", 1, "recon", "2026-11-03T19:46:00Z", 8),
                CreateTestVehicleDefinition(9, "nv_l2_scout", "Harrier Strike", "nova_syndicate", "Nova Syndicate", "Assault", 2, "striker", "2026-11-02T20:36:00Z", 9),
                CreateTestVehicleDefinition(10, "nv_l2_guardian", "Warden Guardian", "nova_syndicate", "Nova Syndicate", "Defender", 2, "bulwark", "2026-11-01T17:32:00Z", 10),
                CreateTestVehicleDefinition(11, "nv_l2_colossus", "Atlas Siege", "nova_syndicate", "Nova Syndicate", "Heavy", 2, "siege", "2026-10-31T21:04:00Z", 11),
                CreateTestVehicleDefinition(12, "nv_l2_scout", "Shade Longshot", "nova_syndicate", "Nova Syndicate", "Sniper", 2, "artillery", "2026-10-30T20:18:00Z", 12),
                CreateTestVehicleDefinition(13, "nv_l2_guardian", "Aegis Nova", "nova_syndicate", "Nova Syndicate", "Support", 2, "support", "2026-10-29T18:21:00Z", 13),
                CreateTestVehicleDefinition(14, "nv_l2_colossus", "Ravager Assault", "nova_syndicate", "Nova Syndicate", "Assault", 2, "striker", "2026-10-28T22:44:00Z", 14),
                CreateTestVehicleDefinition(15, "nv_l1_starter", "Nomad Patrol", "nova_syndicate", "Nova Syndicate", "Scout", 1, "recon", "2026-10-27T19:08:00Z", 15)
            };
        }
        private static ClientMenuTestVehicle CreateTestVehicleDefinition(
            int vehicleId,
            string code,
            string vehicleName,
            string factionCode,
            string factionName,
            string vehicleClass,
            int level,
            string branch,
            string lastPlayedAt,
            int historySeed)
        {
            int battles = 22 + historySeed * 3;
            int draws = historySeed % 3;
            int wins = Mathf.Clamp(10 + historySeed * 2, 0, battles - draws);
            int kills = 24 + historySeed * 6;
            int damage = battles * (650 + historySeed * 18);
            int bestKills = Mathf.Min(kills, 3 + historySeed % 4);
            int bestDamage = Mathf.Min(damage, 1350 + historySeed * 95);
            return new ClientMenuTestVehicle
            {
                vehicleId = vehicleId,
                code = code,
                vehicleName = vehicleName,
                factionCode = factionCode,
                factionName = factionName,
                vehicleClass = vehicleClass,
                level = level,
                branch = branch,
                profileCurrentXp = 700 + historySeed * 310,
                profileBattles = battles,
                profileWins = wins,
                profileDraws = draws,
                profileKills = kills,
                profileDamage = damage,
                profileBestKills = bestKills,
                profileBestDamage = bestDamage,
                profileXpEarned = battles * 280 + historySeed * 175,
                profileBoltsEarned = battles * 360 + historySeed * 220,
                profileFreeXpEarned = battles * 24 + historySeed * 18,
                profileLastPlayedAt = lastPlayedAt
            };
        }

        private int _selectedVehicleId;

        public bool IsOfflineMenuTestEnabled => enableOfflineMenuTest;

        public static bool IsEnabled => _instance != null && _instance.enableOfflineMenuTest;

        private void Awake()
        {
            _instance = this;
            testVehicles = CreateGeneratedTestVehicles();
            _selectedVehicleId = 0;
        }

        private void Start()
        {
            if (!enableOfflineMenuTest)
            {
                return;
            }

            MenuManager.OpenMenu(MenuType.MainMenu);
            ProfileServer.UpdateProfile();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public static bool TryCreateProfile(out PlayerProfile profile)
        {
            profile = null;
            if (!IsEnabled)
            {
                return false;
            }

            profile = _instance.CreateProfile();
            return true;
        }

        public static bool TryCreateProfileStatistics(out PlayerStatisticsDto statistics)
        {
            statistics = null;
            if (!IsEnabled)
            {
                return false;
            }

            statistics = _instance.CreateProfileStatistics();
            return statistics != null;
        }

        public static bool TryCreateVehicles(out VehicleLite[] vehicles)
        {
            vehicles = null;
            if (!IsEnabled)
            {
                return false;
            }

            vehicles = _instance.CreateVehicles();
            return vehicles.Length > 0;
        }

        public static bool TryCreateDevelopmentTrees(out VehicleLite[] vehicles, out VehicleGraph[] graphs)
        {
            vehicles = null;
            graphs = null;
            if (!TryCreateVehicles(out vehicles))
            {
                return false;
            }

            graphs = _instance.CreateDevelopmentTrees(vehicles);
            return graphs.Length > 0;
        }

        public static bool TrySelectVehicle(int vehicleId)
        {
            if (!IsEnabled || vehicleId <= 0)
            {
                return false;
            }

            VehicleLite[] vehicles = _instance.CreateVehicles();
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicles[i].id != vehicleId)
                {
                    continue;
                }

                _instance._selectedVehicleId = vehicleId;
                return true;
            }

            return false;
        }

        private PlayerProfile CreateProfile()
        {
            VehicleLite[] vehicles = CreateVehicles();
            int selectedVehicleId = ResolveSelectedVehicleId(vehicles);
            OwnedVehicleDto[] ownedVehicles = new OwnedVehicleDto[vehicles.Length];
            VehicleLite selectedVehicle = null;

            for (int i = 0; i < vehicles.Length; i++)
            {
                VehicleLite vehicle = vehicles[i];
                bool isSelected = vehicle.id == selectedVehicleId;
                ownedVehicles[i] = new OwnedVehicleDto
                {
                    vehicleId = vehicle.id,
                    code = vehicle.code,
                    name = vehicle.name,
                    isActive = isSelected,
                    xp = ResolveProfileCurrentXp(GetDefinition(vehicle.id), vehicle.level),
                    isResearched = true,
                    viewRange = vehicle.viewRange,
                    enginePower = vehicle.enginePower
                };

                if (isSelected)
                {
                    selectedVehicle = vehicle;
                }
            }

            if (selectedVehicle == null)
            {
                selectedVehicle = vehicles[0];
            }

            return new PlayerProfile
            {
                id = 0,
                username = string.IsNullOrWhiteSpace(testPlayerName) ? "Menu Tester" : testPlayerName.Trim(),
                mmr = Mathf.Max(0, testMmr),
                bolts = Mathf.Max(0, testBolts),
                adamant = Mathf.Max(0, testAdamant),
                freeXp = Mathf.Max(0, testFreeXp),
                activeVehicleId = selectedVehicle.id,
                activeVehicleCode = selectedVehicle.code,
                activeVehicleName = selectedVehicle.name,
                activeVehicleEnginePower = selectedVehicle.enginePower,
                ownedVehicles = ownedVehicles,
                researchedVehicles = Array.Empty<ResearchedVehicleDto>()
            };
        }

        private PlayerStatisticsDto CreateProfileStatistics()
        {
            VehicleLite[] vehicles = CreateVehicles();
            int selectedVehicleId = ResolveSelectedVehicleId(vehicles);
            List<VehicleStatisticsDto> vehicleStatistics = new List<VehicleStatisticsDto>(vehicles.Length);

            int totalBattles = 0;
            int totalWins = 0;
            int totalLosses = 0;
            int totalDraws = 0;
            int totalKills = 0;
            int totalDamage = 0;
            int totalXpEarned = 0;
            int totalBoltsEarned = 0;
            int totalFreeXpEarned = 0;

            for (int index = 0; index < vehicles.Length; index++)
            {
                VehicleLite vehicle = vehicles[index];
                ClientMenuTestVehicle definition = GetDefinition(vehicle.id);
                int battles = ResolveProfileBattles(definition, vehicle.level, index);
                int wins = Mathf.Clamp(definition != null ? definition.profileWins : 0, 0, battles);
                int draws = Mathf.Clamp(definition != null ? definition.profileDraws : 0, 0, battles - wins);
                int losses = battles - wins - draws;
                int kills = ResolveProfileKills(definition, battles, index);
                int damage = ResolveProfileDamage(definition, battles, index);
                int bestKills = Mathf.Clamp(definition != null ? definition.profileBestKills : 0, 0, kills);
                int bestDamage = Mathf.Clamp(definition != null ? definition.profileBestDamage : 0, 0, damage);
                int xpEarned = ResolveProfileReward(definition != null ? definition.profileXpEarned : 0, battles * 250);
                int boltsEarned = ResolveProfileReward(definition != null ? definition.profileBoltsEarned : 0, battles * 350);
                int freeXpEarned = ResolveProfileReward(definition != null ? definition.profileFreeXpEarned : 0, battles * 20);
                float winRate = battles > 0 ? wins / (float)battles * 100f : 0f;

                vehicleStatistics.Add(new VehicleStatisticsDto
                {
                    vehicleId = vehicle.id,
                    code = vehicle.code,
                    name = vehicle.name,
                    factionCode = vehicle.factionCode,
                    branch = vehicle.branch,
                    tier = vehicle.level,
                    isOwned = true,
                    isActive = vehicle.id == selectedVehicleId,
                    currentXp = ResolveProfileCurrentXp(definition, vehicle.level),
                    battles = battles,
                    wins = wins,
                    losses = losses,
                    draws = draws,
                    winRate = winRate,
                    kills = kills,
                    damage = damage,
                    averageDamage = battles > 0 ? damage / (float)battles : 0f,
                    averageKills = battles > 0 ? kills / (float)battles : 0f,
                    bestKills = bestKills,
                    bestDamage = bestDamage,
                    xpEarned = xpEarned,
                    boltsEarned = boltsEarned,
                    freeXpEarned = freeXpEarned,
                    lastPlayedAt = definition != null && !string.IsNullOrWhiteSpace(definition.profileLastPlayedAt)
                        ? definition.profileLastPlayedAt.Trim()
                        : "2026-10-01T12:00:00Z"
                });

                totalBattles += battles;
                totalWins += wins;
                totalLosses += losses;
                totalDraws += draws;
                totalKills += kills;
                totalDamage += damage;
                totalXpEarned += xpEarned;
                totalBoltsEarned += boltsEarned;
                totalFreeXpEarned += freeXpEarned;
            }

            return new PlayerStatisticsDto
            {
                username = string.IsNullOrWhiteSpace(testPlayerName) ? "Menu Tester" : testPlayerName.Trim(),
                mmr = Mathf.Max(0, testMmr),
                bolts = Mathf.Max(0, testBolts),
                adamant = Mathf.Max(0, testAdamant),
                freeXp = Mathf.Max(0, testFreeXp),
                battles = totalBattles,
                wins = totalWins,
                losses = totalLosses,
                draws = totalDraws,
                winRate = totalBattles > 0 ? totalWins / (float)totalBattles * 100f : 0f,
                kills = totalKills,
                damage = totalDamage,
                averageDamage = totalBattles > 0 ? totalDamage / (float)totalBattles : 0f,
                averageKills = totalBattles > 0 ? totalKills / (float)totalBattles : 0f,
                xpEarned = totalXpEarned,
                boltsEarned = totalBoltsEarned,
                freeXpEarned = totalFreeXpEarned,
                mmrDelta = testMmrDelta,
                lastBattleAt = string.IsNullOrWhiteSpace(testLastBattleAt)
                    ? "2026-10-01T12:00:00Z"
                    : testLastBattleAt.Trim(),
                vehicles = vehicleStatistics.ToArray()
            };
        }

        private VehicleLite[] CreateVehicles()
        {
            if (testVehicles == null || testVehicles.Length == 0)
            {
                testVehicles = CreateGeneratedTestVehicles();
            }

            List<VehicleLite> vehicles = new List<VehicleLite>();
            HashSet<int> usedIds = new HashSet<int>();

            if (testVehicles != null)
            {
                for (int i = 0; i < testVehicles.Length; i++)
                {
                    ClientMenuTestVehicle definition = testVehicles[i];
                    if (definition == null)
                    {
                        continue;
                    }

                    int vehicleId = Mathf.Max(1, definition.vehicleId);
                    if (!usedIds.Add(vehicleId))
                    {
                        Debug.LogWarning("ClientMenuTestMode skipped a duplicate test vehicle id: " + vehicleId);
                        continue;
                    }

                    vehicles.Add(CreateVehicle(definition, vehicleId));
                }
            }

            if (vehicles.Count > 0)
            {
                return vehicles.ToArray();
            }

            ClientMenuTestVehicle fallback = new ClientMenuTestVehicle();
            return new[] { CreateVehicle(fallback, 1) };
        }

        private ClientMenuTestVehicle GetDefinition(int vehicleId)
        {
            if (testVehicles == null)
            {
                return null;
            }

            for (int index = 0; index < testVehicles.Length; index++)
            {
                ClientMenuTestVehicle definition = testVehicles[index];
                if (definition != null && Mathf.Max(1, definition.vehicleId) == vehicleId)
                {
                    return definition;
                }
            }

            return null;
        }

        private static int ResolveProfileCurrentXp(ClientMenuTestVehicle definition, int level)
        {
            if (definition != null && definition.profileCurrentXp > 0)
            {
                return definition.profileCurrentXp;
            }

            return 1250 + Mathf.Max(1, level) * 250;
        }

        private static int ResolveProfileBattles(ClientMenuTestVehicle definition, int level, int index)
        {
            if (definition != null && definition.profileBattles > 0)
            {
                return definition.profileBattles;
            }

            return 24 + Mathf.Max(1, level) * 6 + index * 3;
        }

        private static int ResolveProfileKills(ClientMenuTestVehicle definition, int battles, int index)
        {
            if (definition != null && definition.profileKills > 0)
            {
                return definition.profileKills;
            }

            return Mathf.Max(1, Mathf.RoundToInt(battles * (0.85f + index % 4 * 0.08f)));
        }

        private static int ResolveProfileDamage(ClientMenuTestVehicle definition, int battles, int index)
        {
            if (definition != null && definition.profileDamage > 0)
            {
                return definition.profileDamage;
            }

            return Mathf.Max(1, Mathf.RoundToInt(battles * (620f + index % 5 * 45f)));
        }

        private static int ResolveProfileReward(int configuredValue, int fallbackValue)
        {
            return configuredValue > 0 ? configuredValue : Mathf.Max(0, fallbackValue);
        }

        private VehicleGraph[] CreateDevelopmentTrees(VehicleLite[] vehicles)
        {
            List<string> factionCodes = new List<string>();
            for (int i = 0; i < vehicles.Length; i++)
            {
                string factionCode = vehicles[i].factionCode;
                if (string.IsNullOrWhiteSpace(factionCode) || ContainsFaction(factionCodes, factionCode))
                {
                    continue;
                }

                factionCodes.Add(factionCode);
            }

            VehicleGraph[] graphs = new VehicleGraph[factionCodes.Count];
            for (int factionIndex = 0; factionIndex < factionCodes.Count; factionIndex++)
            {
                string factionCode = factionCodes[factionIndex];
                List<VehicleLite> factionVehicles = new List<VehicleLite>();
                for (int vehicleIndex = 0; vehicleIndex < vehicles.Length; vehicleIndex++)
                {
                    VehicleLite vehicle = vehicles[vehicleIndex];
                    if (string.Equals(vehicle.factionCode, factionCode, StringComparison.OrdinalIgnoreCase))
                    {
                        factionVehicles.Add(vehicle);
                    }
                }

                factionVehicles.Sort(CompareVehiclesByLevel);
                VehicleNode[] nodes = new VehicleNode[factionVehicles.Count];
                List<VehicleEdge> edges = new List<VehicleEdge>();
                VehicleLite previousLevelVehicle = null;

                for (int vehicleIndex = 0; vehicleIndex < factionVehicles.Count; vehicleIndex++)
                {
                    VehicleLite vehicle = factionVehicles[vehicleIndex];
                    nodes[vehicleIndex] = CreateNode(vehicle);

                    if (previousLevelVehicle != null && vehicle.level > previousLevelVehicle.level)
                    {
                        edges.Add(new VehicleEdge
                        {
                            fromId = previousLevelVehicle.id,
                            toId = vehicle.id,
                            requiredXp = 0
                        });
                    }

                    if (previousLevelVehicle == null || vehicle.level >= previousLevelVehicle.level)
                    {
                        previousLevelVehicle = vehicle;
                    }
                }

                graphs[factionIndex] = new VehicleGraph
                {
                    nodes = nodes,
                    edges = edges.ToArray()
                };
            }

            return graphs;
        }

        private int ResolveSelectedVehicleId(VehicleLite[] vehicles)
        {
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicles[i].id == _selectedVehicleId)
                {
                    return _selectedVehicleId;
                }
            }

            _selectedVehicleId = vehicles[0].id;
            return _selectedVehicleId;
        }

        private static bool ContainsFaction(List<string> factionCodes, string factionCode)
        {
            for (int i = 0; i < factionCodes.Count; i++)
            {
                if (string.Equals(factionCodes[i], factionCode, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CompareVehiclesByLevel(VehicleLite left, VehicleLite right)
        {
            int levelComparison = left.level.CompareTo(right.level);
            if (levelComparison != 0)
            {
                return levelComparison;
            }

            return left.id.CompareTo(right.id);
        }

        private static VehicleNode CreateNode(VehicleLite vehicle)
        {
            return new VehicleNode
            {
                id = vehicle.id,
                code = vehicle.code,
                name = vehicle.name,
                @class = vehicle.@class,
                level = vehicle.level,
                branch = vehicle.branch,
                factionCode = vehicle.factionCode,
                isVisible = true,
                shellSpeed = vehicle.shellSpeed,
                enginePower = vehicle.enginePower,
                shellsCount = vehicle.shellsCount,
                damageMin = vehicle.damageMin,
                damageMax = vehicle.damageMax,
                viewRange = vehicle.viewRange
            };
        }

        private static VehicleLite CreateVehicle(ClientMenuTestVehicle definition, int vehicleId)
        {
            string factionCode = string.IsNullOrWhiteSpace(definition.factionCode)
                ? "nova_syndicate"
                : definition.factionCode.Trim();
            string factionName = string.IsNullOrWhiteSpace(definition.factionName)
                ? GetFactionName(factionCode)
                : definition.factionName.Trim();
            string code = string.IsNullOrWhiteSpace(definition.code)
                ? GetFallbackVehicleCode(factionCode)
                : definition.code.Trim();
            string name = string.IsNullOrWhiteSpace(definition.vehicleName)
                ? code
                : definition.vehicleName.Trim();
            int level = Mathf.Clamp(definition.level, 1, 10);
            int classOffset = ResolveClassOffset(definition.vehicleClass);
            int variantOffset = Mathf.Max(0, vehicleId - 1);
            int armorOffset = level * 8 + classOffset * 4 + variantOffset;
            float damageMin = 88f + level * 18f + classOffset * 7f + variantOffset * 2f;
            float damageMax = damageMin + 34f + classOffset * 3f;

            return new VehicleLite
            {
                id = vehicleId,
                code = code,
                name = name,
                branch = string.IsNullOrWhiteSpace(definition.branch) ? "test" : definition.branch.Trim(),
                factionCode = factionCode,
                factionName = factionName,
                @class = string.IsNullOrWhiteSpace(definition.vehicleClass) ? "Assault" : definition.vehicleClass.Trim(),
                level = level,
                purchaseCost = level <= 1 ? 0 : 2400 + level * 1200 + variantOffset * 180,
                isVisible = true,
                hp = 380 + level * 95 + classOffset * 34 + variantOffset * 9,
                damage = Mathf.RoundToInt((damageMin + damageMax) * 0.5f),
                penetration = 68 + level * 11 + classOffset * 5 + variantOffset,
                shellSpeed = 720f + level * 42f + classOffset * 18f + variantOffset * 4f,
                shellsCount = 16 + level * 3 + classOffset + variantOffset % 4,
                damageMin = damageMin,
                damageMax = damageMax,
                reloadTime = Mathf.Max(1.2f, 5.4f - level * 0.24f - classOffset * 0.08f - variantOffset * 0.035f),
                accuracy = Mathf.Clamp(0.22f + classOffset * 0.018f + variantOffset * 0.006f, 0.1f, 0.95f),
                aimTime = Mathf.Max(0.8f, 2.25f - classOffset * 0.08f - variantOffset * 0.025f),
                viewRange = 300f + level * 16f + classOffset * 9f + variantOffset * 2f,
                speed = 34f + level * 2.5f + ResolveSpeedOffset(definition.vehicleClass) + variantOffset * 0.45f,
                acceleration = 5.5f + level * 0.65f + classOffset * 0.22f + variantOffset * 0.08f,
                traverseAcceleration = 4f + level * 0.45f + classOffset * 0.18f,
                traverseSpeed = 25f + level * 1.8f + classOffset * 1.7f + variantOffset * 0.35f,
                turretTraverseSpeed = 29f + level * 2f + classOffset * 1.9f + variantOffset * 0.4f,
                enginePower = 420f + level * 65f + classOffset * 24f + variantOffset * 7f,
                turretArmor = BuildArmor(62 + armorOffset, 40 + level * 4 + classOffset * 3, 24 + level * 3 + variantOffset / 2),
                hullArmor = BuildArmor(74 + armorOffset + classOffset * 2, 48 + level * 4 + classOffset * 2, 30 + level * 3 + variantOffset / 2)
            };
        }

        private static int ResolveClassOffset(string vehicleClass)
        {
            if (string.Equals(vehicleClass, "Heavy", StringComparison.OrdinalIgnoreCase))
            {
                return 4;
            }

            if (string.Equals(vehicleClass, "Defender", StringComparison.OrdinalIgnoreCase))
            {
                return 3;
            }

            if (string.Equals(vehicleClass, "Support", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            if (string.Equals(vehicleClass, "Sniper", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(vehicleClass, "Scout", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            return 0;
        }

        private static float ResolveSpeedOffset(string vehicleClass)
        {
            if (string.Equals(vehicleClass, "Scout", StringComparison.OrdinalIgnoreCase))
            {
                return 7f;
            }

            if (string.Equals(vehicleClass, "Heavy", StringComparison.OrdinalIgnoreCase))
            {
                return -4f;
            }

            if (string.Equals(vehicleClass, "Defender", StringComparison.OrdinalIgnoreCase))
            {
                return -2f;
            }

            return 0f;
        }

        private static string BuildArmor(int front, int side, int rear)
        {
            return front + "/" + side + "/" + rear;
        }

        private static string GetFactionName(string factionCode)
        {
            if (string.Equals(factionCode, "iron_alliance", StringComparison.OrdinalIgnoreCase))
            {
                return "Iron Alliance";
            }

            if (string.Equals(factionCode, "nova_syndicate", StringComparison.OrdinalIgnoreCase))
            {
                return "Nova Syndicate";
            }

            return factionCode;
        }

        private static string GetFallbackVehicleCode(string factionCode)
        {
            return string.Equals(factionCode, "iron_alliance", StringComparison.OrdinalIgnoreCase)
                ? "ia_l1_starter"
                : "nv_l1_starter";
        }
    }
}
