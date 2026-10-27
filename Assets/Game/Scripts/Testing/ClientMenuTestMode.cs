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
        [Tooltip("Код машини з RobotRegistry, наприклад ia_l1_starter або nv_l1_starter.")]
        public string code = "nv_l1_starter";
        public string vehicleName = "NV Starter";
        [Tooltip("Для готових візуалів дерева використовуйте iron_alliance або nova_syndicate.")]
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

        [Header("Офлайн-тестування меню")]
        [Tooltip("Увімкніть, щоб Client.unity відкривала головне меню з локальними демо-даними та не запускала підключення до сервера.")]
        public bool enableOfflineMenuTest;

        [Header("Демо-профіль")]
        [SerializeField] private string testPlayerName = "Menu Tester";
        [SerializeField] private int testMmr = 1475;
        [SerializeField] private int testBolts = 48620;
        [SerializeField] private int testAdamant = 720;
        [SerializeField] private int testFreeXp = 23850;
        [SerializeField] private int testMmrDelta = 86;
        [SerializeField] private string testLastBattleAt = "2026-10-26T19:42:00Z";

        [Header("Тестові машини")]
        [Tooltip("Усі машини масиву належать тестовому профілю: вони з'являються у виборі роботів і в дереві своєї фракції.")]
        [SerializeField] private ClientMenuTestVehicle[] testVehicles =
        {
            new ClientMenuTestVehicle
            {
                vehicleId = 1,
                code = "ia_l1_starter",
                vehicleName = "Aegis Vanguard",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Assault",
                level = 1,
                branch = "vanguard",
                profileCurrentXp = 1840,
                profileBattles = 48,
                profileWins = 29,
                profileDraws = 2,
                profileKills = 61,
                profileDamage = 46200,
                profileBestKills = 4,
                profileBestDamage = 2100,
                profileXpEarned = 12400,
                profileBoltsEarned = 18600,
                profileFreeXpEarned = 1240,
                profileLastPlayedAt = "2026-10-26T18:42:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 2,
                code = "ia_l2_scout",
                vehicleName = "Strider Scout",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Scout",
                level = 2,
                branch = "recon",
                profileCurrentXp = 920,
                profileBattles = 37,
                profileWins = 20,
                profileDraws = 1,
                profileKills = 43,
                profileDamage = 33800,
                profileBestKills = 4,
                profileBestDamage = 1950,
                profileXpEarned = 10300,
                profileBoltsEarned = 15100,
                profileFreeXpEarned = 980,
                profileLastPlayedAt = "2026-10-25T21:18:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 3,
                code = "ia_l2_guardian",
                vehicleName = "Bastion Guardian",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Defender",
                level = 2,
                branch = "bulwark",
                profileCurrentXp = 3450,
                profileBattles = 29,
                profileWins = 18,
                profileDraws = 1,
                profileKills = 36,
                profileDamage = 29900,
                profileBestKills = 3,
                profileBestDamage = 2300,
                profileXpEarned = 11800,
                profileBoltsEarned = 14200,
                profileFreeXpEarned = 1100,
                profileLastPlayedAt = "2026-10-24T16:05:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 4,
                code = "ia_l2_colossus",
                vehicleName = "Colossus Breaker",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Heavy",
                level = 2,
                branch = "siege",
                profileCurrentXp = 6120,
                profileBattles = 64,
                profileWins = 39,
                profileDraws = 3,
                profileKills = 85,
                profileDamage = 72800,
                profileBestKills = 6,
                profileBestDamage = 3200,
                profileXpEarned = 22800,
                profileBoltsEarned = 26600,
                profileFreeXpEarned = 2010,
                profileLastPlayedAt = "2026-10-26T19:42:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 5,
                code = "ia_l2_scout",
                vehicleName = "Longshot Mk II",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Sniper",
                level = 2,
                branch = "artillery",
                profileCurrentXp = 2780,
                profileBattles = 22,
                profileWins = 12,
                profileDraws = 0,
                profileKills = 27,
                profileDamage = 21500,
                profileBestKills = 3,
                profileBestDamage = 1800,
                profileXpEarned = 7300,
                profileBoltsEarned = 9200,
                profileFreeXpEarned = 760,
                profileLastPlayedAt = "2026-10-22T20:27:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 6,
                code = "ia_l2_guardian",
                vehicleName = "Bulwark Medic",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Support",
                level = 2,
                branch = "support",
                profileCurrentXp = 1960,
                profileBattles = 31,
                profileWins = 17,
                profileDraws = 1,
                profileKills = 40,
                profileDamage = 33200,
                profileBestKills = 4,
                profileBestDamage = 2400,
                profileXpEarned = 10100,
                profileBoltsEarned = 13200,
                profileFreeXpEarned = 1040,
                profileLastPlayedAt = "2026-10-21T18:54:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 7,
                code = "ia_l2_colossus",
                vehicleName = "Raptor Assault",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Assault",
                level = 2,
                branch = "striker",
                profileCurrentXp = 740,
                profileBattles = 18,
                profileWins = 8,
                profileDraws = 1,
                profileKills = 17,
                profileDamage = 12400,
                profileBestKills = 3,
                profileBestDamage = 1600,
                profileXpEarned = 5600,
                profileBoltsEarned = 7600,
                profileFreeXpEarned = 540,
                profileLastPlayedAt = "2026-10-19T22:11:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 8,
                code = "nv_l1_starter",
                vehicleName = "Specter Recon",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Scout",
                level = 1,
                branch = "recon",
                profileCurrentXp = 4120,
                profileBattles = 53,
                profileWins = 32,
                profileDraws = 2,
                profileKills = 72,
                profileDamage = 51900,
                profileBestKills = 5,
                profileBestDamage = 2600,
                profileXpEarned = 17800,
                profileBoltsEarned = 22300,
                profileFreeXpEarned = 1860,
                profileLastPlayedAt = "2026-10-25T19:46:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 9,
                code = "nv_l2_scout",
                vehicleName = "Harrier Strike",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Assault",
                level = 2,
                branch = "striker",
                profileCurrentXp = 2680,
                profileBattles = 41,
                profileWins = 25,
                profileDraws = 1,
                profileKills = 55,
                profileDamage = 42800,
                profileBestKills = 5,
                profileBestDamage = 2400,
                profileXpEarned = 14900,
                profileBoltsEarned = 19100,
                profileFreeXpEarned = 1520,
                profileLastPlayedAt = "2026-10-24T20:36:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 10,
                code = "nv_l2_guardian",
                vehicleName = "Warden Guardian",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Defender",
                level = 2,
                branch = "bulwark",
                profileCurrentXp = 1320,
                profileBattles = 27,
                profileWins = 14,
                profileDraws = 1,
                profileKills = 32,
                profileDamage = 26600,
                profileBestKills = 4,
                profileBestDamage = 2200,
                profileXpEarned = 8900,
                profileBoltsEarned = 11600,
                profileFreeXpEarned = 900,
                profileLastPlayedAt = "2026-10-20T17:32:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 11,
                code = "nv_l2_colossus",
                vehicleName = "Atlas Siege",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Heavy",
                level = 2,
                branch = "siege",
                profileCurrentXp = 5240,
                profileBattles = 36,
                profileWins = 21,
                profileDraws = 2,
                profileKills = 49,
                profileDamage = 40100,
                profileBestKills = 6,
                profileBestDamage = 2900,
                profileXpEarned = 13500,
                profileBoltsEarned = 18400,
                profileFreeXpEarned = 1480,
                profileLastPlayedAt = "2026-10-23T21:04:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 12,
                code = "nv_l2_scout",
                vehicleName = "Shade Longshot",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Sniper",
                level = 2,
                branch = "artillery",
                profileCurrentXp = 1680,
                profileBattles = 19,
                profileWins = 10,
                profileDraws = 0,
                profileKills = 21,
                profileDamage = 18500,
                profileBestKills = 3,
                profileBestDamage = 1700,
                profileXpEarned = 6600,
                profileBoltsEarned = 8400,
                profileFreeXpEarned = 680,
                profileLastPlayedAt = "2026-10-18T20:18:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 13,
                code = "nv_l2_guardian",
                vehicleName = "Aegis Nova",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Support",
                level = 2,
                branch = "support",
                profileCurrentXp = 3860,
                profileBattles = 44,
                profileWins = 27,
                profileDraws = 1,
                profileKills = 63,
                profileDamage = 49700,
                profileBestKills = 5,
                profileBestDamage = 2800,
                profileXpEarned = 16800,
                profileBoltsEarned = 21600,
                profileFreeXpEarned = 1720,
                profileLastPlayedAt = "2026-10-24T18:21:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 14,
                code = "nv_l2_colossus",
                vehicleName = "Ravager Assault",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Assault",
                level = 2,
                branch = "striker",
                profileCurrentXp = 2290,
                profileBattles = 33,
                profileWins = 19,
                profileDraws = 1,
                profileKills = 45,
                profileDamage = 36600,
                profileBestKills = 5,
                profileBestDamage = 2700,
                profileXpEarned = 12700,
                profileBoltsEarned = 16900,
                profileFreeXpEarned = 1350,
                profileLastPlayedAt = "2026-10-22T22:44:00Z"
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 15,
                code = "nv_l1_starter",
                vehicleName = "Nomad Patrol",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Scout",
                level = 1,
                branch = "recon",
                profileCurrentXp = 1180,
                profileBattles = 26,
                profileWins = 13,
                profileDraws = 1,
                profileKills = 28,
                profileDamage = 23700,
                profileBestKills = 4,
                profileBestDamage = 1900,
                profileXpEarned = 8200,
                profileBoltsEarned = 10700,
                profileFreeXpEarned = 860,
                profileLastPlayedAt = "2026-10-17T19:08:00Z"
            }
        };

        private int _selectedVehicleId;

        public bool IsOfflineMenuTestEnabled => enableOfflineMenuTest;

        public static bool IsEnabled => _instance != null && _instance.enableOfflineMenuTest;

        private void Awake()
        {
            _instance = this;
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
                    viewRange = vehicle.viewRange
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
                purchaseCost = 0,
                isVisible = true,
                hp = 300 + level * 50,
                penetration = 70 + level * 10,
                shellSpeed = 800f + level * 25f,
                shellsCount = 20 + level * 2,
                damageMin = 75f + level * 15f,
                damageMax = 105f + level * 15f,
                reloadTime = Mathf.Max(1f, 4.2f - level * 0.15f),
                accuracy = 0.35f,
                aimTime = 2f,
                viewRange = 320f + level * 10f,
                speed = 42f + level * 2f,
                acceleration = 7f + level,
                traverseSpeed = 30f + level * 2f,
                turretTraverseSpeed = 34f + level * 2f,
                turretArmor = "70/45/30",
                hullArmor = "80/50/35"
            };
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
