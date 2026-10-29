using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using Game.Scripts.API;
using Game.Scripts.API.Endpoints;
using Game.Scripts.API.Models;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Core.Helpers;
using Game.Scripts.Core.Resources;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Networking.Sessions;
using Game.Scripts.Player.Data;
using Game.Scripts.Testing;
using Game.Scripts.UI.Helpers;
using Game.Scripts.UI.MainMenu;
using Game.Scripts.UI.Screens;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Tree
{
    public class DevelopmentTree : NetworkBehaviour
    {
        [Header("Runtime tree")]
        [SerializeField] private Transform treeHost;
        [SerializeField] private FactionTreeView factionTreePrefab;
        [SerializeField] private RectTransform levelRowPrefab;
        [SerializeField] private TreeItem treeItemPrefab;
        [SerializeField] private ArrowDrawer arrowDrawer;

        [Header("Screen controls")]
        [SerializeField] private Transform buttonsContainer;
        [SerializeField] private FactionButtonView factionButtonPrefab;
        [SerializeField] private Button buttonBack;
        [SerializeField] private GameObject treeMenuRoot;
        [SerializeField] private DevelopmentTreeDetailsPanel detailsPanel;

        private VehicleGraph[] _graphs;
        private VehicleLite[] _vehicleLites;
        private string[] _factionCodes;
        private bool _isInitializing;
        private TreeItem _selectedTreeItem;
        private int _selectedVehicleId;

        private readonly List<FactionView> _views = new List<FactionView>();
        private readonly List<FactionButtonView> _factionButtons = new List<FactionButtonView>();
        private readonly Dictionary<string, string> _factionNameByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool isInitialized;

        private sealed class FactionView
        {
            public string Code;
            public FactionTreeView Root;
            public VehicleGraph Graph;
            public VehicleEdge[] Edges;
            public Dictionary<int, RectTransform> NodeMap;
            public Dictionary<int, TreeItem> ItemMap;
            public VehicleResearchProgressResolver ProgressResolver;
            public TreeItem DefaultTreeItem;
            public VehicleResearchProgress DefaultProgress;
        }

        private void Awake()
        {
            Singleton<DevelopmentTree>.Register(this);

            if (buttonBack != null)
            {
                buttonBack.onClick.AddListener(ReturnToMainMenu);
                buttonBack.transform.SetAsLastSibling();
            }
        }

        [Button]
        public void Init()
        {
            InitAsync().Forget();
        }

        private async UniTask InitAsync()
        {
            if (_isInitializing)
            {
                return;
            }

            _isInitializing = true;

            try
            {
                if (!isInitialized)
                {
                    isInitialized = await LoadDataFromServer();
                }

                if (isInitialized)
                {
                    await UpdateUI();
                }
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private async UniTask<bool> LoadDataFromServer()
        {
            _factionNameByCode.Clear();

            if (ClientMenuTestMode.TryCreateDevelopmentTrees(out VehicleLite[] localTestVehicles, out VehicleGraph[] localTestGraphs))
            {
                _vehicleLites = localTestVehicles;
                _factionCodes = BuildFactionCodes(localTestVehicles);
                for (int i = 0; i < localTestVehicles.Length; i++)
                {
                    VehicleLite vehicle = localTestVehicles[i];
                    if (vehicle == null || string.IsNullOrWhiteSpace(vehicle.factionCode))
                    {
                        continue;
                    }

                    _factionNameByCode[vehicle.factionCode] = string.IsNullOrWhiteSpace(vehicle.factionName)
                        ? vehicle.factionCode
                        : vehicle.factionName;
                }

                _graphs = localTestGraphs;
                return true;
            }

            (bool ok, _, VehicleLite[] items) = await VehiclesManager.GetAll();
            if (!ok || items == null || items.Length == 0)
            {
                Popup.ShowText("No vehicle data received from server!", Color.red);
                _graphs = Array.Empty<VehicleGraph>();
                _vehicleLites = Array.Empty<VehicleLite>();
                _factionCodes = Array.Empty<string>();
                return false;
            }

            _vehicleLites = items;

            for (int i = 0; i < items.Length; i++)
            {
                VehicleLite vehicle = items[i];
                if (vehicle == null || string.IsNullOrWhiteSpace(vehicle.factionCode))
                {
                    continue;
                }

                if (!_factionNameByCode.ContainsKey(vehicle.factionCode))
                {
                    _factionNameByCode[vehicle.factionCode] = string.IsNullOrWhiteSpace(vehicle.factionName)
                        ? vehicle.factionCode
                        : vehicle.factionName;
                }
            }

            _factionCodes = BuildFactionCodes(items);
            List<VehicleGraph> graphs = new List<VehicleGraph>(_factionCodes.Length);

            for (int i = 0; i < _factionCodes.Length; i++)
            {
                string factionCode = _factionCodes[i];
                (bool graphOk, _, VehicleGraph graph) = await VehiclesManager.GetGraph(factionCode);
                if (graphOk && graph != null && graph.nodes != null && graph.nodes.Length > 0)
                {
                    graphs.Add(graph);
                    continue;
                }

                Popup.ShowText($"Failed to get graph for faction: {factionCode}", Color.red);
                graphs.Add(new VehicleGraph
                {
                    nodes = Array.Empty<VehicleNode>(),
                    edges = Array.Empty<VehicleEdge>()
                });
            }

            _graphs = graphs.ToArray();
            return true;
        }

        public async UniTask UpdateUI()
        {
            if (!HasRuntimeTreeConfiguration())
            {
                Popup.ShowText("Development tree UI is not configured.", Color.red);
                return;
            }

            ClearRuntimeTree();
            detailsPanel.Clear();

            if (_factionCodes == null || _graphs == null || _factionCodes.Length == 0 || _graphs.Length == 0)
            {
                Popup.ShowText("No data to build UI!", Color.red);
                return;
            }

            BuildFactionButtons();

            int factionCount = Mathf.Min(_factionCodes.Length, _graphs.Length);
            for (int i = 0; i < factionCount; i++)
            {
                BuildFactionTree(_factionCodes[i], _graphs[i]);
            }

            await RebuildLayoutsAndDrawArrows();

            if (_views.Count > 0)
            {
                SetActiveContainer(0);
            }
        }

        public void SetActiveContainer(int index)
        {
            if (index < 0 || index >= _views.Count)
            {
                return;
            }

            SetSelectedTreeItem(null);

            for (int i = 0; i < _views.Count; i++)
            {
                _views[i].Root.gameObject.SetActive(i == index);
            }

            for (int i = 0; i < _factionButtons.Count; i++)
            {
                _factionButtons[i].SetSelected(i == index);
            }

            detailsPanel.Clear();
            FactionView activeView = _views[index];
            SelectVehicle(activeView.DefaultTreeItem, activeView.DefaultProgress);
            RedrawActive(index).Forget();
        }

        private void BuildFactionButtons()
        {
            ClearButtons();

            for (int i = 0; i < _factionCodes.Length; i++)
            {
                int factionIndex = i;
                string code = _factionCodes[i];
                string title = _factionNameByCode.TryGetValue(code, out string factionName)
                    && !string.IsNullOrWhiteSpace(factionName)
                    ? factionName
                    : code;

                FactionButtonView factionButton = Instantiate(factionButtonPrefab, buttonsContainer);
                Button button = factionButton.Button;
                _factionButtons.Add(factionButton);
                factionButton.Configure(code, title);

                button.onClick.AddListener(() => SetActiveContainer(factionIndex));
            }
        }

        private void BuildFactionTree(string factionCode, VehicleGraph graph)
        {
            if (graph == null || graph.nodes == null || graph.nodes.Length == 0)
            {
                return;
            }

            FactionTreeView root = Instantiate(factionTreePrefab, treeHost);
            root.name = $"FactionTree_{factionCode}";

            VehicleResearchProgressResolver progressResolver = new VehicleResearchProgressResolver(
                GetCurrentProfile(),
                graph,
                _vehicleLites);

            Dictionary<int, RectTransform> nodeMap = new Dictionary<int, RectTransform>();
            Dictionary<int, TreeItem> itemMap = new Dictionary<int, TreeItem>();
            Dictionary<int, List<VehicleNode>> nodesByLevel = GroupNodesByLevel(graph.nodes);
            List<int> levels = GetSortedLevels(nodesByLevel);
            TreeItem defaultTreeItem = null;
            VehicleResearchProgress defaultProgress = null;

            for (int i = 0; i < levels.Count; i++)
            {
                int level = levels[i];
                RectTransform row = Instantiate(levelRowPrefab, root.RowsContainer);
                row.name = $"Level_{level}";
                row.SetAsFirstSibling();

                List<VehicleNode> nodes = nodesByLevel[level];
                nodes.Sort(CompareNodesByCode);

                for (int nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
                {
                    VehicleResearchProgress itemProgress;
                    TreeItem item = CreateTreeItem(row, nodes[nodeIndex], nodeMap, itemMap, progressResolver, out itemProgress);
                    if (i == 0 && defaultTreeItem == null && item != null && itemProgress != null)
                    {
                        defaultTreeItem = item;
                        defaultProgress = itemProgress;
                    }
                }
            }

            _views.Add(new FactionView
            {
                Code = factionCode,
                Root = root,
                Graph = graph,
                Edges = graph.edges ?? Array.Empty<VehicleEdge>(),
                NodeMap = nodeMap,
                ItemMap = itemMap,
                ProgressResolver = progressResolver,
                DefaultTreeItem = defaultTreeItem,
                DefaultProgress = defaultProgress
            });
        }

        private TreeItem CreateTreeItem(
            Transform parent,
            VehicleNode node,
            Dictionary<int, RectTransform> nodeMap,
            Dictionary<int, TreeItem> itemMap,
            VehicleResearchProgressResolver progressResolver,
            out VehicleResearchProgress progress)
        {
            progress = null;
            if (node == null)
            {
                return null;
            }

            TreeItem item = Instantiate(treeItemPrefab, parent);
            item.name = $"Vehicle_{node.code}";
            item.SetSelected(false);

            if (item.vehicleName != null)
            {
                item.vehicleName.text = node.name;
            }

            if (item.level != null)
            {
                item.level.text = node.level.ToString();
            }

            if (item.image != null)
            {
                item.image.sprite = GameResourceManager.GetIcon(node.code);
            }

            VehicleResearchProgress resolvedProgress = progressResolver.Resolve(node);
            progress = resolvedProgress;
            ApplyTreeItemState(item, resolvedProgress);

            if (item.button != null)
            {
                item.button.onClick.AddListener(() => SelectVehicle(item, node));
            }

            if (item.rectTransform != null)
            {
                nodeMap[node.id] = item.rectTransform;
            }

            itemMap[node.id] = item;

            return item;
        }

        private static void ApplyTreeItemState(TreeItem item, VehicleResearchProgress progress)
        {
            if (item == null || progress == null)
            {
                return;
            }

            bool isLocked = progress.Status == VehicleResearchStatus.Hidden
                || progress.Status == VehicleResearchStatus.LockedByResearch;

            item.ApplyVisualState(
                progress.PurchaseCost,
                progress.RequiredXp,
                progress.IsOwned,
                progress.IsResearched,
                isLocked,
                progress.Status == VehicleResearchStatus.Hidden);
        }

        private void SelectVehicle(TreeItem item, VehicleResearchProgress progress)
        {
            if (progress == null || progress.Node == null)
            {
                return;
            }

            _selectedVehicleId = progress.Node.id;
            SetSelectedTreeItem(item);
            detailsPanel.Show(progress, TryBuy, TryResearch);
        }

        private void SelectVehicle(TreeItem item, VehicleNode node)
        {
            SelectVehicle(item, ResolveProgress(node));
        }

        public void RefreshProgressionStates()
        {
            if (!isInitialized || _views.Count == 0)
            {
                return;
            }

            VehicleResearchProgress selectedProgress = null;

            for (int viewIndex = 0; viewIndex < _views.Count; viewIndex++)
            {
                FactionView view = _views[viewIndex];
                if (view == null || view.Graph == null || view.Graph.nodes == null)
                {
                    continue;
                }

                view.ProgressResolver = new VehicleResearchProgressResolver(
                    GetCurrentProfile(),
                    view.Graph,
                    _vehicleLites);

                for (int nodeIndex = 0; nodeIndex < view.Graph.nodes.Length; nodeIndex++)
                {
                    VehicleNode node = view.Graph.nodes[nodeIndex];
                    if (node == null || !view.ItemMap.TryGetValue(node.id, out TreeItem item))
                    {
                        continue;
                    }

                    VehicleResearchProgress progress = view.ProgressResolver.Resolve(node);
                    ApplyTreeItemState(item, progress);

                    if (view.DefaultTreeItem == item)
                    {
                        view.DefaultProgress = progress;
                    }

                    if (node.id == _selectedVehicleId)
                    {
                        selectedProgress = progress;
                    }
                }
            }

            if (selectedProgress != null && _selectedTreeItem != null)
            {
                detailsPanel.Show(selectedProgress, TryBuy, TryResearch);
            }
        }

        private VehicleResearchProgress ResolveProgress(VehicleNode node)
        {
            if (node == null)
            {
                return null;
            }

            for (int viewIndex = 0; viewIndex < _views.Count; viewIndex++)
            {
                FactionView view = _views[viewIndex];
                if (view != null
                    && view.ProgressResolver != null
                    && string.Equals(view.Code, node.factionCode, StringComparison.OrdinalIgnoreCase))
                {
                    return view.ProgressResolver.Resolve(node);
                }
            }

            return null;
        }

        private async UniTask RebuildLayoutsAndDrawArrows()
        {
            Canvas.ForceUpdateCanvases();

            for (int i = 0; i < _views.Count; i++)
            {
                FactionView view = _views[i];
                LayoutRebuilder.ForceRebuildLayoutImmediate(view.Root.RowsContainer);
                arrowDrawer.Draw(view.Edges, view.NodeMap, view.Root.ArrowsLayer);
            }

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        }

        private async UniTask RedrawActive(int index)
        {
            if (index < 0 || index >= _views.Count)
            {
                return;
            }

            FactionView view = _views[index];
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(view.Root.RowsContainer);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            arrowDrawer.Draw(view.Edges, view.NodeMap, view.Root.ArrowsLayer);
        }

        private static Dictionary<int, List<VehicleNode>> GroupNodesByLevel(VehicleNode[] nodes)
        {
            Dictionary<int, List<VehicleNode>> nodesByLevel = new Dictionary<int, List<VehicleNode>>();

            for (int i = 0; i < nodes.Length; i++)
            {
                VehicleNode node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                int level = Mathf.Max(1, node.level);
                if (!nodesByLevel.TryGetValue(level, out List<VehicleNode> levelNodes))
                {
                    levelNodes = new List<VehicleNode>();
                    nodesByLevel.Add(level, levelNodes);
                }

                levelNodes.Add(node);
            }

            return nodesByLevel;
        }

        private static List<int> GetSortedLevels(Dictionary<int, List<VehicleNode>> nodesByLevel)
        {
            List<int> levels = new List<int>(nodesByLevel.Keys);
            levels.Sort();
            return levels;
        }

        private static int CompareNodesByCode(VehicleNode left, VehicleNode right)
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

            int codeCompare = string.Compare(left.code, right.code, StringComparison.Ordinal);
            if (codeCompare != 0)
            {
                return codeCompare;
            }

            return left.id.CompareTo(right.id);
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

                bool exists = false;
                for (int factionIndex = 0; factionIndex < factionCodes.Count; factionIndex++)
                {
                    if (string.Equals(factionCodes[factionIndex], item.factionCode, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    factionCodes.Add(item.factionCode);
                }
            }

            return factionCodes.ToArray();
        }

        private PlayerProfile GetCurrentProfile()
        {
            IPlayerClientInfo clientInfo = ServiceLocator.Get<IPlayerClientInfo>();
            return clientInfo != null ? clientInfo.Profile : null;
        }

        private bool HasRuntimeTreeConfiguration()
        {
            return treeHost != null
                && factionTreePrefab != null
                && levelRowPrefab != null
                && treeItemPrefab != null
                && arrowDrawer != null
                && buttonsContainer != null
                && factionButtonPrefab != null
                && detailsPanel != null;
        }

        private void ClearRuntimeTree()
        {
            SetSelectedTreeItem(null);
            ClearChildren(treeHost);
            _views.Clear();
            ClearButtons();
        }

        private void SetSelectedTreeItem(TreeItem item)
        {
            if (_selectedTreeItem != null)
            {
                _selectedTreeItem.SetSelected(false);
            }

            _selectedTreeItem = item;
            if (_selectedTreeItem == null)
            {
                _selectedVehicleId = 0;
            }

            if (_selectedTreeItem != null)
            {
                _selectedTreeItem.SetSelected(true);
            }
        }

        private void ClearButtons()
        {
            ClearChildren(buttonsContainer);
            _factionButtons.Clear();
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                child.gameObject.SetActive(false);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        public void ReturnToMainMenu()
        {
            if (treeMenuRoot != null)
            {
                treeMenuRoot.SetActive(false);
            }

            MenuManager.OpenMenu(MenuType.MainMenu);
            RobotView.UpdateUI();
        }

        private void TryResearch(VehicleResearchProgress progress)
        {
            if (progress == null || progress.Node == null || progress.Status != VehicleResearchStatus.AvailableToResearch)
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

            Popup.ShowText(
                "Research vehicle?\n" + predecessorName + ": -" + progress.RequiredXp + " XP",
                Color.green,
                () =>
                {
                    Helpers.StandardLoadingOverlay.Show();
                    ResearchVehicleServerRpc(progress.Node.id, progress.PredecessorVehicleId);
                },
                PopupType.Confirm);
        }

        private void TryBuy(VehicleResearchProgress progress)
        {
            if (progress == null || progress.Node == null || progress.Status != VehicleResearchStatus.AvailableToBuy)
            {
                return;
            }

            IPlayerClientInfo info = ServiceLocator.Get<IPlayerClientInfo>();
            if (info == null || info.Profile == null)
            {
                Popup.ShowText("Profile is not loaded.", Color.red);
                return;
            }

            VehicleLite vehicle = progress.Vehicle ?? FindVehicleLite(progress.Node.id);
            if (vehicle == null)
            {
                Popup.ShowText("Vehicle data is not loaded.", Color.red);
                return;
            }

            if (info.Profile.bolts < vehicle.purchaseCost)
            {
                Popup.ShowText("Not enough bolts.\nprice: " + vehicle.purchaseCost, Color.red);
                return;
            }

            Popup.ShowText(
                "Do you want buy?\nprice: " + vehicle.purchaseCost,
                Color.green,
                () =>
                {
                    Helpers.StandardLoadingOverlay.Show();
                    BuyVehicleServerRpc(vehicle.code);
                },
                PopupType.Confirm);
        }

        [ServerRpc(RequireOwnership = false)]
        private void BuyVehicleServerRpc(string code, NetworkConnection sender = null)
        {
            if (sender != null)
            {
                BuyVehicleAsync(sender, code).Forget();
            }
        }

        private async UniTask BuyVehicleAsync(NetworkConnection sender, string code)
        {
            string token = ServerPlayerSessions.GetToken(sender.ClientId);
            if (string.IsNullOrEmpty(token))
            {
                TargetRpcBuy(sender, false, "Not logged in.");
                return;
            }

            (bool ok, string message) validation = await ValidateVehiclePurchaseAsync(token, code);
            if (!validation.ok)
            {
                TargetRpcBuy(sender, false, validation.message);
                return;
            }

            (bool ok, string message, BuyVehicleResult _) result = await UserVehiclesManager.Buy(code, token);
            TargetRpcBuy(sender, result.ok, result.message);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ResearchVehicleServerRpc(int vehicleId, int predecessorVehicleId, NetworkConnection sender = null)
        {
            if (sender != null)
            {
                ResearchVehicleAsync(sender, vehicleId, predecessorVehicleId).Forget();
            }
        }

        private async UniTask ResearchVehicleAsync(NetworkConnection sender, int vehicleId, int predecessorVehicleId)
        {
            string token = ServerPlayerSessions.GetToken(sender.ClientId);
            if (string.IsNullOrEmpty(token))
            {
                TargetRpcResearch(sender, false, "Not logged in.");
                return;
            }

            (bool ok, string message, ResearchVehicleResult _) result = await UserVehiclesManager.Research(vehicleId, predecessorVehicleId, token);
            TargetRpcResearch(sender, result.ok, result.message);
        }

        [TargetRpc]
        private void TargetRpcResearch(NetworkConnection target, bool success, string errorMessage)
        {
            if (success)
            {
                ProfileServer.UpdateProfile();
                return;
            }

            Popup.ShowText(errorMessage, Color.red);
            Helpers.StandardLoadingOverlay.Hide();
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

            Helpers.StandardLoadingOverlay.Hide();
        }

        private async UniTask<(bool ok, string message)> ValidateVehiclePurchaseAsync(string token, string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return (false, "Vehicle code is empty.");
            }

            (bool profileOk, string profileMessage, PlayerProfile profile) profileResult = await PlayersManager.GetMyProfile(token);
            if (!profileResult.profileOk || profileResult.profile == null)
            {
                return (false, string.IsNullOrEmpty(profileResult.profileMessage)
                    ? "Failed to load profile."
                    : profileResult.profileMessage);
            }

            (bool vehicleOk, string vehicleMessage, VehicleLite vehicle) vehicleResult = await VehiclesManager.GetByCode(code);
            if (!vehicleResult.vehicleOk || vehicleResult.vehicle == null)
            {
                return (false, string.IsNullOrEmpty(vehicleResult.vehicleMessage)
                    ? "Vehicle data is not loaded."
                    : vehicleResult.vehicleMessage);
            }

            if (profileResult.profile.IsHave(vehicleResult.vehicle.id))
            {
                return (false, "Vehicle already owned.");
            }

            (bool vehiclesOk, string vehiclesMessage, VehicleLite[] vehicles) vehiclesResult = await VehiclesManager.GetAll(vehicleResult.vehicle.factionCode);
            if (!vehiclesResult.vehiclesOk || vehiclesResult.vehicles == null || vehiclesResult.vehicles.Length == 0)
            {
                return (false, string.IsNullOrEmpty(vehiclesResult.vehiclesMessage)
                    ? "Failed to load vehicle list."
                    : vehiclesResult.vehiclesMessage);
            }

            (bool graphOk, string graphMessage, VehicleGraph graph) graphResult = await VehiclesManager.GetGraph(vehicleResult.vehicle.factionCode);
            if (!graphResult.graphOk || graphResult.graph == null || graphResult.graph.nodes == null)
            {
                return (false, string.IsNullOrEmpty(graphResult.graphMessage)
                    ? "Failed to load research tree."
                    : graphResult.graphMessage);
            }

            VehicleNode node = FindNode(graphResult.graph.nodes, vehicleResult.vehicle.id);
            if (node == null)
            {
                return (false, "Vehicle is missing from research tree.");
            }

            VehicleResearchProgress progress = new VehicleResearchProgressResolver(
                profileResult.profile,
                graphResult.graph,
                vehiclesResult.vehicles).Resolve(node);

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

            if (profileResult.profile.bolts < vehicleResult.vehicle.purchaseCost)
            {
                return (false, "Not enough bolts.\nprice: " + vehicleResult.vehicle.purchaseCost);
            }

            return (true, string.Empty);
        }

        private VehicleLite FindVehicleLite(int vehicleId)
        {
            if (_vehicleLites == null)
            {
                return null;
            }

            for (int i = 0; i < _vehicleLites.Length; i++)
            {
                VehicleLite vehicle = _vehicleLites[i];
                if (vehicle != null && vehicle.id == vehicleId)
                {
                    return vehicle;
                }
            }

            return null;
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

        private static string BuildResearchLockedMessage(VehicleResearchProgress progress)
        {
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

        private void OnDestroy()
        {
            Singleton<DevelopmentTree>.Unregister(this);
        }
    }
}
