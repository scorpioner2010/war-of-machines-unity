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
        [SerializeField] private int testMmr = 1000;
        [SerializeField] private int testBolts = 25000;
        [SerializeField] private int testAdamant = 120;
        [SerializeField] private int testFreeXp = 1500;

        [Header("Тестові машини")]
        [Tooltip("Усі машини масиву належать тестовому профілю: вони з'являються у виборі роботів і в дереві своєї фракції.")]
        [SerializeField] private ClientMenuTestVehicle[] testVehicles =
        {
            new ClientMenuTestVehicle
            {
                vehicleId = 1,
                code = "ia_l1_starter",
                vehicleName = "IA Starter",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Assault",
                level = 1
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 2,
                code = "ia_l2_scout",
                vehicleName = "IA Scout",
                factionCode = "iron_alliance",
                factionName = "Iron Alliance",
                vehicleClass = "Scout",
                level = 2
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 3,
                code = "nv_l1_starter",
                vehicleName = "NV Starter",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Assault",
                level = 1
            },
            new ClientMenuTestVehicle
            {
                vehicleId = 4,
                code = "nv_l2_scout",
                vehicleName = "NV Scout",
                factionCode = "nova_syndicate",
                factionName = "Nova Syndicate",
                vehicleClass = "Scout",
                level = 2
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
                    xp = 1250 + vehicle.level * 250,
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
                branch = "test",
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
