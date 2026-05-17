using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using Game.Scripts;
using Game.Scripts.API;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Resources;
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
            if (buttonBack != null)
            {
                buttonBack.onClick.AddListener(() =>
                {
                    MenuManager.OpenMenu(MenuType.MainMenu);
                    RobotView.UpdateUI();
                });
            }
        }

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

        private async UniTask<bool> LoadDataFromServer()
        {
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

            foreach (VehicleLite v in items)
            {
                if (!string.IsNullOrWhiteSpace(v.factionCode) && !_factionNameByCode.ContainsKey(v.factionCode))
                {
                    _factionNameByCode[v.factionCode] = string.IsNullOrWhiteSpace(v.factionName) ? v.factionCode : v.factionName;
                }
            }

            _factionCodes = BuildFactionCodes(items);

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

            for (int i = 0; i < _graphs.Length; i++)
            {
                await BuildFactionTreeUI(_factionCodes[i], _graphs[i]);
            }

            if (_fractionRoots.Count > 0)
            {
                SetActiveContainer(0);
            }

            await GameplayAssistant.RebuildAllLayouts(_fractionRoots);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            DrawArrowsAll();
        }

        public void SetActiveContainer(int number)
        {
            for (int i = 0; i < _fractionRoots.Count; i++)
            {
                _fractionRoots[i].gameObject.SetActive(i == number);
            }
        }

        private async UniTask BuildFactionTreeUI(string factionCode, VehicleGraph graph)
        {
            if (graph == null || graph.nodes == null || graph.nodes.Length == 0)
            {
                return;
            }

            Transform rootT = Instantiate(fractionTreePrefab, animationPanel);
            rootT.name = $"FactionRoot_{factionCode}";
            _fractionRoots.Add(rootT);

            RectTransform arrowsLayer = null;
            if (arrowsLayerPrefab != null)
            {
                arrowsLayer = Instantiate(arrowsLayerPrefab, rootT).GetComponent<RectTransform>();
                arrowsLayer.name = "ArrowsLayer";
                arrowsLayer.SetAsFirstSibling();
            }

            Transform starterContainer = Instantiate(starterContainerPrefab, rootT);
            starterContainer.name = "StarterContainer";

            Dictionary<int, RectTransform> nodeMap = new Dictionary<int, RectTransform>();

            VehicleNode starter = FindStarterNode(graph.nodes);

            VehicleResearchProgressResolver progressResolver = CreateProgressResolver(graph);

            CreateTreeItemFromNode(starterContainer, starter, nodeMap, progressResolver);

            FactionContainer columns = Instantiate(factionContainerPrefab, rootT);
            columns.name = "ColumnsContainer";

            BuildColumn(columns.transform, graph, "Scout", "Scout_Column", nodeMap, progressResolver);
            BuildColumn(columns.transform, graph, "Guardian", "Guardian_Column", nodeMap, progressResolver);
            BuildColumn(columns.transform, graph, "Colossus", "Colossus_Column", nodeMap, progressResolver);

            _views.Add(new FactionView
            {
                Root = (RectTransform)rootT,
                ArrowsLayer = arrowsLayer,
                Edges = graph.edges ?? Array.Empty<VehicleEdge>(),
                NodeMap = nodeMap
            });

            await UniTask.Yield();
        }

        private void BuildColumn(
            Transform parent,
            VehicleGraph graph,
            string className,
            string columnName,
            Dictionary<int, RectTransform> nodeMap,
            VehicleResearchProgressResolver progressResolver)
        {
            TreeGrid grid = Instantiate(treeGridPrefab, parent);
            grid.name = columnName;
            grid.Init(ParseVehicleClass(className));

            List<VehicleNode> nodes = GetColumnNodes(graph.nodes, className);

            foreach (VehicleNode n in nodes)
            {
                CreateTreeItemFromNode(grid.transform, n, nodeMap, progressResolver);
            }
        }

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

        private VehicleResearchProgressResolver CreateProgressResolver(VehicleGraph graph)
        {
            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            return new VehicleResearchProgressResolver(clientInfo != null ? clientInfo.Profile : null, graph, _vehicleLites);
        }

        private void CreateTreeItemFromNode(
            Transform parent,
            VehicleNode node,
            Dictionary<int, RectTransform> nodeMap,
            VehicleResearchProgressResolver progressResolver)
        {
            if (node == null)
            {
                return;
            }

            TreeItem item = Instantiate(treeItemPrefab, parent);
            item.vehicleName.text = node.name;
            item.vehicleType = ParseVehicleClass(node.@class);
            item.level.text = node.level.ToString();

            VehicleResearchProgress progress = progressResolver != null
                ? progressResolver.Resolve(node)
                : new VehicleResearchProgress { Node = node, Status = VehicleResearchStatus.Hidden };

            ApplyTreeItemState(item, progress);
        
            Sprite sprite = GameResourceManager.GetIcon(node.code);
            item.image.sprite = sprite;
        
            item.button.onClick.AddListener(() =>
            {
                OnVehicleNodeClicked(progress);
            });
        
        
            RectTransform rt = item.rectTransform;
            nodeMap[node.id] = rt;
        }

        private void ApplyTreeItemState(TreeItem item, VehicleResearchProgress progress)
        {
            if (item == null || progress == null)
            {
                return;
            }

            bool isLocked = progress.Status == VehicleResearchStatus.Hidden
                || progress.Status == VehicleResearchStatus.LockedByResearch;

            if (item.isClose != null)
            {
                item.isClose.SetActive(isLocked);
            }

            if (item.isHave != null)
            {
                item.isHave.gameObject.SetActive(progress.IsOwned);
            }

            if (item.price == null)
            {
                return;
            }

            if (progress.Status == VehicleResearchStatus.Owned)
            {
                item.price.text = "Owned";
                return;
            }

            if (progress.Status == VehicleResearchStatus.LockedByResearch)
            {
                item.price.text = "XP " + progress.CurrentXp + "/" + progress.RequiredXp;
                return;
            }

            if (progress.Status == VehicleResearchStatus.AvailableToResearch)
            {
                item.price.text = "Research " + progress.RequiredXp + " XP";
                return;
            }

            if (progress.Status == VehicleResearchStatus.Hidden)
            {
                item.price.text = string.Empty;
                return;
            }

            item.price.text = progress.PurchaseCost.ToString();
        }

        private void OnVehicleNodeClicked(VehicleResearchProgress progress)
        {
            if (progress == null || progress.Node == null)
            {
                return;
            }

            if (progress.Status == VehicleResearchStatus.Owned)
            {
                return;
            }

            if (progress.Status == VehicleResearchStatus.Hidden)
            {
                Popup.ShowText("This vehicle is not available.", Color.red);
                return;
            }

            if (progress.Status == VehicleResearchStatus.LockedByResearch)
            {
                ShowResearchLockedPopup(progress);
                return;
            }

            if (progress.Status == VehicleResearchStatus.AvailableToResearch)
            {
                TryResearch(progress);
                return;
            }

            TryBuy(progress);
        }

        private void ShowResearchLockedPopup(VehicleResearchProgress progress)
        {
            Popup.ShowText(BuildResearchLockedMessage(progress), Color.red);
        }

        private static string BuildResearchLockedMessage(VehicleResearchProgress progress)
        {
            if (progress == null)
            {
                return "Research required.";
            }

            string predecessorName = string.IsNullOrEmpty(progress.PredecessorName)
                ? "previous vehicle"
                : progress.PredecessorName;

            string message = "Research required\n"
                + predecessorName + ": " + progress.CurrentXp + "/" + progress.RequiredXp + " XP";

            if (progress.MissingXp > 0)
            {
                message += "\nMissing: " + progress.MissingXp + " XP";
            }

            return message;
        }

        private void TryResearch(VehicleResearchProgress progress)
        {
            if (progress == null || progress.Node == null)
            {
                return;
            }

            if (progress.PredecessorVehicleId <= 0)
            {
                Popup.ShowText("Required predecessor vehicle is not owned.", Color.red);
                return;
            }

            string predecessorName = string.IsNullOrEmpty(progress.PredecessorName)
                ? "previous vehicle"
                : progress.PredecessorName;

            string message = "Research vehicle?\n"
                + predecessorName + ": -" + progress.RequiredXp + " XP";

            Popup.ShowText(message, Color.green, () =>
            {
                Helpers.StandardLoadingOverlay.Show();
                ResearchVehicleServerRpc(progress.Node.id, progress.PredecessorVehicleId);
            }, PopupType.Confirm);
        }

        private void TryBuy(VehicleResearchProgress progress)
        {
            IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
            if (info == null || info.Profile == null)
            {
                Popup.ShowText("Profile is not loaded.", Color.red);
                return;
            }

            VehicleLite lite = progress.Vehicle != null ? progress.Vehicle : GetVehicleLite(progress.Node.id);
            if (lite == null)
            {
                Popup.ShowText("Vehicle data is not loaded.", Color.red);
                return;
            }

            if (info.Profile.bolts < lite.purchaseCost)
            {
                Popup.ShowText("Not enough bolts.\nprice: " + lite.purchaseCost, Color.red);
                return;
            }

            Popup.ShowText("Do you want buy?\nprice: " + lite.purchaseCost, Color.green, () =>
            {
                Helpers.StandardLoadingOverlay.Show();
                BuyVehicleServerRpc(lite.code);
            }, PopupType.Confirm);
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

            (bool ok, string msg) validation = await ValidateVehiclePurchaseAsync(token, code);
            if (!validation.ok)
            {
                TargetRpcBuy(sender, false, validation.msg);
                return;
            }

            (bool ok, string msg, BuyVehicleResult data) result =  await UserVehiclesManager.Buy(code, token);
            TargetRpcBuy(sender, result.ok, result.msg);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ResearchVehicleServerRpc(int vehicleId, int predecessorVehicleId, NetworkConnection sender = null)
        {
            if (sender == null)
            {
                return;
            }

            ResearchVehicleAsync(sender, vehicleId, predecessorVehicleId).Forget();
        }

        private async UniTask ResearchVehicleAsync(NetworkConnection sender, int vehicleId, int predecessorVehicleId)
        {
            string token = ServerPlayerSessions.GetToken(sender.ClientId);
            if (string.IsNullOrEmpty(token))
            {
                TargetRpcResearch(sender, false, "Not logged in.");
                return;
            }

            (bool ok, string msg, ResearchVehicleResult data) result = await UserVehiclesManager.Research(vehicleId, predecessorVehicleId, token);
            TargetRpcResearch(sender, result.ok, result.msg);
        }

        [TargetRpc]
        private void TargetRpcResearch(NetworkConnection target, bool success, string errorMessage)
        {
            if (success)
            {
                ProfileServer.UpdateProfile();
            }
            else
            {
                Popup.ShowText(errorMessage, Color.red);
                Helpers.StandardLoadingOverlay.Hide();
            }
        }

        private async UniTask<(bool ok, string msg)> ValidateVehiclePurchaseAsync(string token, string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return (false, "Vehicle code is empty.");
            }

            (bool isSuccess, string message, PlayerProfile profile) profileResult = await PlayersManager.GetMyProfile(token);
            if (!profileResult.isSuccess || profileResult.profile == null)
            {
                return (false, string.IsNullOrEmpty(profileResult.message) ? "Failed to load profile." : profileResult.message);
            }

            (bool isSuccess, string message, VehicleLite item) vehicleResult = await VehiclesManager.GetByCode(code);
            if (!vehicleResult.isSuccess || vehicleResult.item == null)
            {
                return (false, string.IsNullOrEmpty(vehicleResult.message) ? "Vehicle data is not loaded." : vehicleResult.message);
            }

            if (profileResult.profile.IsHave(vehicleResult.item.id))
            {
                return (false, "Vehicle already owned.");
            }

            (bool isSuccess, string message, VehicleLite[] items) vehiclesResult = await VehiclesManager.GetAll(vehicleResult.item.factionCode);
            if (!vehiclesResult.isSuccess || vehiclesResult.items == null || vehiclesResult.items.Length == 0)
            {
                return (false, string.IsNullOrEmpty(vehiclesResult.message) ? "Failed to load vehicle list." : vehiclesResult.message);
            }

            (bool ok, string msg, VehicleGraph graph) graphResult = await VehiclesManager.GetGraph(vehicleResult.item.factionCode);
            if (!graphResult.ok || graphResult.graph == null || graphResult.graph.nodes == null)
            {
                return (false, string.IsNullOrEmpty(graphResult.msg) ? "Failed to load research tree." : graphResult.msg);
            }

            VehicleNode node = FindNode(graphResult.graph.nodes, vehicleResult.item.id);
            if (node == null)
            {
                return (false, "Vehicle is missing from research tree.");
            }

            VehicleResearchProgressResolver resolver = new VehicleResearchProgressResolver(
                profileResult.profile,
                graphResult.graph,
                vehiclesResult.items);
            VehicleResearchProgress progress = resolver.Resolve(node);

            if (progress.Status == VehicleResearchStatus.Hidden)
            {
                return (false, "This vehicle is not available.");
            }

            if (progress.Status == VehicleResearchStatus.LockedByResearch)
            {
                return (false, BuildResearchLockedMessage(progress));
            }

            if (progress.Status == VehicleResearchStatus.AvailableToResearch)
            {
                return (false, "Vehicle is not researched.");
            }

            if (profileResult.profile.bolts < vehicleResult.item.purchaseCost)
            {
                return (false, "Not enough bolts.\nprice: " + vehicleResult.item.purchaseCost);
            }

            return (true, string.Empty);
        }

        private static VehicleNode FindNode(VehicleNode[] nodes, int vehicleId)
        {
            if (nodes == null)
            {
                return null;
            }

            for (int i = 0; i < nodes.Length; i++)
            {
                VehicleNode node = nodes[i];
                if (node != null && node.id == vehicleId)
                {
                    return node;
                }
            }

            return null;
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
            Helpers.StandardLoadingOverlay.Hide();
        }
        
        private VehicleClass ParseVehicleClass(string cls)
        {
            if (string.Equals(cls, "Guardian", StringComparison.OrdinalIgnoreCase))
            {
                return VehicleClass.Guardian;
            }

            if (string.Equals(cls, "Colossus", StringComparison.OrdinalIgnoreCase))
            {
                return VehicleClass.Colossus;
            }

            return VehicleClass.Scout;
        }

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
