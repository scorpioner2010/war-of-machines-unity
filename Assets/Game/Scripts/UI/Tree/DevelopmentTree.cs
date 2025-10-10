using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using Game.Scripts.API;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Player.Data;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.Screens;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.MenuController;
using Game.Scripts.Server;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.MainMenu;
using NewDropDude.Script.API.ServerManagers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        private readonly List<Transform> _fractionRoots = new();
        private readonly List<Button> _factionButtons = new();
        private readonly Dictionary<string, string> _factionNameByCode = new(StringComparer.OrdinalIgnoreCase);
        public bool isInitialized;

        private class FactionView
        {
            public RectTransform Root;
            public RectTransform ArrowsLayer;
            public VehicleEdge[] Edges;
            public Dictionary<int, RectTransform> NodeMap;
        }

        private readonly List<FactionView> _views = new();

        public VehicleLite GetVehicleLite(int id)
        {
            foreach (VehicleLite vehicleLite in _vehicleLites)
            {
                if (vehicleLite.id == id)
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
        public async void Init()
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
            _factionCodes = items.Select(v => v.factionCode).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            // отримуємо графи для кожної фракції
            List<VehicleGraph> graphs = new();
        
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
                return;

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

            var nodeMap = new Dictionary<int, RectTransform>();

            // шукаємо стартовий вузол
            VehicleNode starter = graph.nodes
                .Where(n => n.level == 1)
                .OrderBy(n => n.code)
                .FirstOrDefault() ?? graph.nodes.OrderBy(n => n.level).ThenBy(n => n.code).First();

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

            IEnumerable<VehicleNode> nodes = graph.nodes
                .Where(n => string.Equals(n.@class, className, StringComparison.OrdinalIgnoreCase) && n.level >= 2)
                .OrderBy(n => n.level)
                .ThenBy(n => n.code);

            foreach (var n in nodes)
            {
                CreateTreeItemFromNode(grid.transform, n, nodeMap);
            }
        }

        // створює елемент дерева (машину)
        private void CreateTreeItemFromNode(Transform parent, VehicleNode node, Dictionary<int, RectTransform> nodeMap)
        {
            TreeItem item = Instantiate(treeItemPrefab, parent);
            item.vehicleName.text = node.name;
            item.vehicleType = ParseVehicleClass(node.@class);
            item.level.text = node.level.ToString();
            item.isClose.SetActive(!node.isVisible);
            
            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            bool isHave = clientInfo.Profile.IsHave(node.id);
            item.isHave.gameObject.SetActive(isHave);
        
            Sprite sprite = ResourceManager.GetIcon(node.code);
            item.image.sprite = sprite;
            VehicleLite result = GetVehicleLite(node.id);

            item.price.text = result.purchaseCost.ToString();
        
            item.button.onClick.AddListener(() =>
            {
                IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
                bool have = info.Profile.IsHave(node.id);
            
                if (have)
                {
                    return;
                }
            
                int bolts = info.Profile.bolts;
                VehicleLite lite = GetVehicleLite(node.id);

                if (bolts >= lite.purchaseCost)
                {
                    Popup.ShowText($"Do you want buy?\nprice: {lite.purchaseCost}", Color.green, () =>
                    {
                        Helpers.Loading.Show();
                        BuyRPC(ClientManager.Connection.ClientId, lite.code);
                    }, TypePopup.Confirm);
                }
            });
        
        
            RectTransform rt = item.rectTransform;
            nodeMap[node.id] = rt;
        }

        [ServerRpc(RequireOwnership = false)]
        private void BuyRPC(int clientID, string code)
        {
            Buy(clientID, code);
        }
            
        private async void Buy(int clientID, string code)
        {
            string token = RegisterServer.GetToken(clientID);
            (bool ok, string msg, BuyVehicleResult data) result =  await UserVehiclesManager.Buy(code, token);
            NetworkConnection senderConn = ServerManager.Clients[clientID];
            TargetRpcBuy(senderConn, result.ok, result.msg);
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
            if (string.Equals(cls, "Guardian", StringComparison.OrdinalIgnoreCase)) return Vehicle.Guardian;
            if (string.Equals(cls, "Colossus", StringComparison.OrdinalIgnoreCase)) return Vehicle.Colossus;
            return Vehicle.Scout;
        }

        // перемальовує стрілки лише для активної фракції
        private async UniTask RedrawActive(int index)
        {
            await GameplayAssistant.RebuildAllLayouts(_fractionRoots);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            if (arrowDrawer == null || index < 0 || index >= _views.Count)
                return;

            var v = _views[index];
            if (v.ArrowsLayer == null || v.Edges == null || v.Edges.Length == 0)
                return;

            arrowDrawer.Draw(v.Edges, v.NodeMap, v.ArrowsLayer);
        }

        // перемальовує всі стрілки (для всіх фракцій)
        private void DrawArrowsAll()
        {
            if (arrowDrawer == null) return;

            foreach (var v in _views)
            {
                if (v.ArrowsLayer == null || v.Edges == null || v.Edges.Length == 0)
                    continue;

                arrowDrawer.Draw(v.Edges, v.NodeMap, v.ArrowsLayer);
            }
        }
    
        // очищення UI
        private void WipeUI()
        {
            foreach (Transform t in _fractionRoots.Where(t => t != null))
            {
                DestroyImmediate(t.gameObject);
            }
            _fractionRoots.Clear();

            foreach (Button b in _factionButtons.Where(b => b != null))
            {
                DestroyImmediate(b.gameObject);
            }
            _factionButtons.Clear();
        }
    }
}
