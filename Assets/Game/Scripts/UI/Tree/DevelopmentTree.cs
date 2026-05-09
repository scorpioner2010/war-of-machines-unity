using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using Game.Scripts.API;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.MainMenu;
using Game.Scripts.Player.Data;
using Game.Scripts.UI.Screens;
using NaughtyAttributes;
using FishNet.Connection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public class DevelopmentTree : NetworkBehaviour
    {
        public Transform animationPanel;
        public Transform fractionTreePrefab;
        public Transform starterContainerPrefab;
        public FactionContainer factionContainerPrefab;
        public TreeGrid treeGridPrefab;
        public TreeItem treeItemPrefab;
        
        public Transform buttonsContainer;
        public Button factionButtonPrefab;
        public Button buttonBack;
    
        public ArrowDrawer arrowDrawer;
        public RectTransform arrowsLayerPrefab;

        // збережені дані з сервера
        private VehicleGraph[] _graphs;
        private VehicleLite[] _vehicleLites;
        private string[] _factionCodes;

        private readonly List<Transform> _fractionRoots = new List<Transform>();
        private readonly List<Button> _factionButtons = new List<Button>();
        private readonly Dictionary<string, string> _factionNameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public bool isInitialized;

        private class FactionView
        {
            public RectTransform Root;
            public RectTransform ArrowsLayer;
            public VehicleEdge[] Edges;
            public Dictionary<int, RectTransform> NodeMap;
        }

        private readonly List<FactionView> _views = new List<FactionView>();

        public VehicleLite GetVehicleLite(int id)
        {
            if (_vehicleLites == null)
            {
                return null;
            }

            foreach (VehicleLite vehicleLite in _vehicleLites)
            {
                if (vehicleLite != null && vehicleLite.id == id)
                {
                    return vehicleLite;
                }
            }
            return null;
        }

        private void Awake()
        {
            // кнопка "Назад" повертає в головне меню
            if (buttonBack != null)
            {
                buttonBack.onClick.AddListener(() =>
                {
                    MenuManager.OpenMenu(MenuType.MainMenu);
                    RobotView.UpdateUI();
                });
            }
        }

        // ініціалізація — отримує дані з сервера, а потім оновлює UI
        [Button]
        public void Init()
        {
            InitAsync().Forget();
        }

        private async UniTask InitAsync()
        {
            if (isInitialized == false)
            {
                isInitialized = true;
                bool result = await LoadDataFromServer();
                isInitialized = result;
            }
        
            await UpdateUI();
        }

        // отримання всіх потрібних даних із сервера
        private async UniTask<bool> LoadDataFromServer()
        {
            // очищаємо попередні дані
            _factionNameByCode.Clear();

            (bool ok, _, VehicleLite[] items) = await VehiclesManager.GetAll();

            _vehicleLites = items;
        
            if (!ok || items == null || items.Length == 0)
            {
                Popup.ShowText("No vehicle data received from server!", Color.red);
                _graphs = Array.Empty<VehicleGraph>();
                _factionCodes = Array.Empty<string>();
                return ok;
            }

            // формуємо словник назв фракцій
            foreach (VehicleLite v in items)
            {
                if (!string.IsNullOrWhiteSpace(v.factionCode) && !_factionNameByCode.ContainsKey(v.factionCode))
                {
                    _factionNameByCode[v.factionCode] = string.IsNullOrWhiteSpace(v.factionName) ? v.factionCode : v.factionName;
                }
            }

            // отримуємо список кодів фракцій
            _factionCodes = BuildFactionCodes(items);

            // отримуємо графи для кожної фракції
            List<VehicleGraph> graphs = new List<VehicleGraph>();
        
            foreach (string faction in _factionCodes)
            {
                (bool okGraph, _, VehicleGraph graph) = await VehiclesManager.GetGraph(faction);
            
                if (okGraph && graph != null && graph.nodes != null && graph.nodes.Length > 0)
                {
                    graphs.Add(graph);
                }
                else
                {
                    Popup.ShowText($"Failed to get graph for faction: {faction}", Color.red);
                    graphs.Add(new VehicleGraph { nodes = Array.Empty<VehicleNode>(), edges = Array.Empty<VehicleEdge>() });
                }
            }

            _graphs = graphs.ToArray();

            return true;
        }

        // оновлює весь UI (будує кнопки, дерева, стрілки)
        private static string[] BuildFactionCodes(VehicleLite[] items)
        {
            List<string> factionCodes = new List<string>();

            for (int i = 0; i < items.Length; i++)
            {
                VehicleLite item = items[i];
                if (item == null || string.IsNullOrWhiteSpace(item.factionCode))
                {
                    continue;
                }

                if (!ContainsFactionCode(factionCodes, item.factionCode))
                {
                    factionCodes.Add(item.factionCode);
                }
            }

            return factionCodes.ToArray();
        }

        private static bool ContainsFactionCode(List<string> factionCodes, string factionCode)
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

        public async UniTask UpdateUI()
        {
            WipeUI();
            _views.Clear();

            if (_factionCodes == null || _factionCodes.Length == 0 || _graphs == null || _graphs.Length == 0)
            {
                Popup.ShowText("No data to build UI!", Color.red);
                return;
            }

            // створюємо кнопки фракцій
            for (int i = 0; i < _factionCodes.Length; i++)
            {
                int idx = i;
                string code = _factionCodes[i];
                string title = _factionNameByCode.TryGetValue(code, out string n) && !string.IsNullOrWhiteSpace(n)
                    ? n
                    : code;

                Button btn = Instantiate(factionButtonPrefab, buttonsContainer);
                _factionButtons.Add(btn);
                btn.GetComponentInChildren<TMP_Text>().text = title;
                btn.onClick.AddListener(() =>
                {
                    SetActiveContainer(idx);
                    _ = RedrawActive(idx);
                });
            }

            // створюємо UI для кожної фракції
            for (int i = 0; i < _graphs.Length; i++)
            {
                await BuildFactionTreeUI(_factionCodes[i], _graphs[i]);
            }

            // активуємо першу фракцію
            if (_fractionRoots.Count > 0)
            {
                SetActiveContainer(0);
            }

            await GameplayAssistant.RebuildAllLayouts(_fractionRoots);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            DrawArrowsAll();
        }

        // показує активну фракцію
        public void SetActiveContainer(int number)
        {
            for (int i = 0; i < _fractionRoots.Count; i++)
            {
                _fractionRoots[i].gameObject.SetActive(i == number);
            }
        }

        // будує дерево UI для однієї фракції
        private async UniTask BuildFactionTreeUI(string factionCode, VehicleGraph graph)
        {
            if (graph == null || graph.nodes == null || graph.nodes.Length == 0)
            {
                return;
            }

            // кореневий контейнер фракції
            Transform rootT = Instantiate(fractionTreePrefab, animationPanel);
            rootT.name = $"FactionRoot_{factionCode}";
            _fractionRoots.Add(rootT);

            // шар для стрілок (завжди під елементами)
            RectTransform arrowsLayer = null;
            if (arrowsLayerPrefab != null)
            {
                arrowsLayer = Instantiate(arrowsLayerPrefab, rootT).GetComponent<RectTransform>();
                arrowsLayer.name = "ArrowsLayer";
                arrowsLayer.SetAsFirstSibling();
            }

            // контейнер стартової машини
            Transform starterContainer = Instantiate(starterContainerPrefab, rootT);
            starterContainer.name = "StarterContainer";

            Dictionary<int, RectTransform> nodeMap = new Dictionary<int, RectTransform>();

            // шукаємо стартовий вузол
            VehicleNode starter = FindStarterNode(graph.nodes);

            CreateTreeItemFromNode(starterContainer, starter, nodeMap);

            // колонки для класів машин
            FactionContainer columns = Instantiate(factionContainerPrefab, rootT);
            columns.name = "ColumnsContainer";

            BuildColumn(columns.transform, graph, "Scout", "Scout_Column", nodeMap);
            BuildColumn(columns.transform, graph, "Guardian", "Guardian_Column", nodeMap);
            BuildColumn(columns.transform, graph, "Colossus", "Colossus_Column", nodeMap);

            _views.Add(new FactionView
            {
                Root = (RectTransform)rootT,
                ArrowsLayer = arrowsLayer,
                Edges = graph.edges ?? Array.Empty<VehicleEdge>(),
                NodeMap = nodeMap
            });

            await UniTask.Yield();
        }

        // створює одну колонку
        private void BuildColumn(Transform parent, VehicleGraph graph, string className, string columnName, Dictionary<int, RectTransform> nodeMap)
        {
            TreeGrid grid = Instantiate(treeGridPrefab, parent);
            grid.name = columnName;
            grid.Init(ParseVehicleClass(className));

            List<VehicleNode> nodes = GetColumnNodes(graph.nodes, className);

            foreach (VehicleNode n in nodes)
            {
                CreateTreeItemFromNode(grid.transform, n, nodeMap);
            }
        }

        // створює елемент дерева (машину)
        private static VehicleNode FindStarterNode(VehicleNode[] nodes)
        {
            VehicleNode starter = null;
            VehicleNode fallback = null;

            for (int i = 0; i < nodes.Length; i++)
            {
                VehicleNode node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                if (node.level == 1 && IsBetterByCode(node, starter))
                {
                    starter = node;
                }

                if (IsBetterByLevelThenCode(node, fallback))
                {
                    fallback = node;
                }
            }

            return starter ?? fallback;
        }

        private static List<VehicleNode> GetColumnNodes(VehicleNode[] nodes, string className)
        {
            List<VehicleNode> result = new List<VehicleNode>();

            for (int i = 0; i < nodes.Length; i++)
            {
                VehicleNode node = nodes[i];
                if (node != null
                    && node.level >= 2
                    && string.Equals(node.@class, className, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(node);
                }
            }

            result.Sort(CompareByLevelThenCode);
            return result;
        }

        private static bool IsBetterByCode(VehicleNode candidate, VehicleNode current)
        {
            if (current == null)
            {
                return true;
            }

            return string.Compare(candidate.code, current.code, StringComparison.Ordinal) < 0;
        }

        private static bool IsBetterByLevelThenCode(VehicleNode candidate, VehicleNode current)
        {
            if (current == null)
            {
                return true;
            }

            int levelCompare = candidate.level.CompareTo(current.level);
            if (levelCompare != 0)
            {
                return levelCompare < 0;
            }

            return string.Compare(candidate.code, current.code, StringComparison.Ordinal) < 0;
        }

        private static int CompareByLevelThenCode(VehicleNode left, VehicleNode right)
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int levelCompare = left.level.CompareTo(right.level);
            if (levelCompare != 0)
            {
                return levelCompare;
            }

            return string.Compare(left.code, right.code, StringComparison.Ordinal);
        }

        private void CreateTreeItemFromNode(Transform parent, VehicleNode node, Dictionary<int, RectTransform> nodeMap)
        {
            if (node == null)
            {
                return;
            }

            TreeItem item = Instantiate(treeItemPrefab, parent);
            item.vehicleName.text = node.name;
            item.vehicleType = ParseVehicleClass(node.@class);
            item.level.text = node.level.ToString();
            item.isClose.SetActive(!node.isVisible);
            
            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            bool isHave = clientInfo != null && clientInfo.Profile != null && clientInfo.Profile.IsHave(node.id);
            item.isHave.gameObject.SetActive(isHave);
        
            Sprite sprite = ResourceManager.GetIcon(node.code);
            item.image.sprite = sprite;
            VehicleLite result = GetVehicleLite(node.id);

            item.price.text = result != null ? result.purchaseCost.ToString() : "0";
        
            item.button.onClick.AddListener(() =>
            {
                IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
                if (info == null || info.Profile == null)
                {
                    return;
                }

                bool have = info.Profile.IsHave(node.id);
            
                if (have)
                {
                    return;
                }
            
                int bolts = info.Profile.bolts;
                VehicleLite lite = GetVehicleLite(node.id);
                if (lite == null)
                {
                    return;
                }

                if (bolts >= lite.purchaseCost)
                {
                    Popup.ShowText($"Do you want buy?\nprice: {lite.purchaseCost}", Color.green, () =>
                    {
                        Helpers.Loading.Show();
                        BuyVehicleServerRpc(lite.code);
                    }, TypePopup.Confirm);
                }
            });
        
        
            RectTransform rt = item.rectTransform;
            nodeMap[node.id] = rt;
        }

        [ServerRpc(RequireOwnership = false)]
        private void BuyVehicleServerRpc(string code, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            BuyVehicleAsync(sender, code).Forget();
        }
            
        private async UniTask BuyVehicleAsync(NetworkConnection sender, string code)
        {
            string token = ServerPlayerSessions.GetToken(sender.ClientId);
            if (string.IsNullOrEmpty(token))
            {
                TargetRpcBuy(sender, false, "Not logged in.");
                return;
            }

            (bool ok, string msg, BuyVehicleResult data) result =  await UserVehiclesManager.Buy(code, token);
            TargetRpcBuy(sender, result.ok, result.msg);
        }
        
        [TargetRpc]
        private void TargetRpcBuy(NetworkConnection target, bool success, string errorMessage)
        {
            if (success)
            {
                ProfileServer.UpdateProfile();
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
            }
            
            MenuManager.OpenMenu(MenuType.MainMenu);
            RobotView.UpdateUI();
            Helpers.Loading.Hide();
        }
        
        private Vehicle ParseVehicleClass(string cls)
        {
            if (string.Equals(cls, "Guardian", StringComparison.OrdinalIgnoreCase))
            {
                return Vehicle.Guardian;
            }

            if (string.Equals(cls, "Colossus", StringComparison.OrdinalIgnoreCase))
            {
                return Vehicle.Colossus;
            }

            return Vehicle.Scout;
        }

        // перемальовує стрілки лише для активної фракції
        private async UniTask RedrawActive(int index)
        {
            await GameplayAssistant.RebuildAllLayouts(_fractionRoots);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            if (arrowDrawer == null || index < 0 || index >= _views.Count)
            {
                return;
            }

            FactionView v = _views[index];
            if (v.ArrowsLayer == null || v.Edges == null || v.Edges.Length == 0)
            {
                return;
            }

            arrowDrawer.Draw(v.Edges, v.NodeMap, v.ArrowsLayer);
        }

        // перемальовує всі стрілки (для всіх фракцій)
        private void DrawArrowsAll()
        {
            if (arrowDrawer == null)
            {
                return;
            }

            foreach (FactionView v in _views)
            {
                if (v.ArrowsLayer == null || v.Edges == null || v.Edges.Length == 0)
                {
                    continue;
                }

                arrowDrawer.Draw(v.Edges, v.NodeMap, v.ArrowsLayer);
            }
        }
    
        // очищення UI
        private void WipeUI()
        {
            for (int i = 0; i < _fractionRoots.Count; i++)
            {
                Transform root = _fractionRoots[i];
                if (root != null)
                {
                    DestroyImmediate(root.gameObject);
                }
            }

            _fractionRoots.Clear();

            for (int i = 0; i < _factionButtons.Count; i++)
            {
                Button button = _factionButtons[i];
                if (button != null)
                {
                    DestroyImmediate(button.gameObject);
                }
            }

            _factionButtons.Clear();
        }
    }
}
