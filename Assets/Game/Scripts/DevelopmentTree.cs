// DevelopmentTree.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Scripts.API;

public class DevelopmentTree : MonoBehaviour
{
    [Header("Prefabs and References")]
    public Transform animationPanel;
    public Transform fractionTreePrefab;
    public Transform starterContainerPrefab;
    public FactionContainer factionContainerPrefab;
    public TreeGrid treeGridPrefab;
    public TreeItem treeItemPrefab;

    [Header("UI Buttons")]
    public Transform buttonsContainer;
    public Button factionButtonPrefab;
    public Button buttonBack;

    [Header("Arrows")]
    public ArrowDrawer arrowDrawer;
    public RectTransform arrowsLayerPrefab;

    // збережені дані з сервера
    private VehicleGraph[] _graphs;
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

    private void Awake()
    {
        // кнопка "Назад" повертає в головне меню
        if (buttonBack != null)
        {
            buttonBack.onClick.AddListener(() =>
            {
                Game.Scripts.MenuController.MenuManager.OpenMenu(Game.Scripts.MenuController.MenuType.MainMenu);
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
        
        if (!ok || items == null || items.Length == 0)
        {
            Debug.LogWarning("No vehicle data received from server!");
            _graphs = Array.Empty<VehicleGraph>();
            _factionCodes = Array.Empty<string>();
            return ok;
        }

        // формуємо словник назв фракцій
        foreach (var v in items)
        {
            if (!string.IsNullOrWhiteSpace(v.factionCode) && !_factionNameByCode.ContainsKey(v.factionCode))
            {
                _factionNameByCode[v.factionCode] = string.IsNullOrWhiteSpace(v.factionName)
                    ? v.factionCode
                    : v.factionName;
            }
        }

        // отримуємо список кодів фракцій
        _factionCodes = items
            .Select(v => v.factionCode)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

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
                Debug.LogWarning($"Failed to get graph for faction: {faction}");
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
            Debug.LogWarning("No data to build UI!");
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

        await RebuildAllLayouts();
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

    public Sprite GetSpriteByName(string spriteName)
    {
        return null; // TODO: реалізувати
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
        item.image.sprite = GetSpriteByName(node.code);
        item.vehicleName.text = node.name;
        item.vehicleType = ParseVehicleClass(node.@class);
        item.level.text = node.level.ToString();
        item.isClose.SetActive(!node.isVisible);

        RectTransform rt = item.GetComponent<RectTransform>();
        nodeMap[node.id] = rt;
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
        await RebuildAllLayouts();
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

    // примусове оновлення LayoutGroup'ів
    private async UniTask RebuildAllLayouts()
    {
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
        Canvas.ForceUpdateCanvases();

        foreach (Transform root in _fractionRoots)
        {
            ForceRebuildRecursive(root);
        }

        Canvas.ForceUpdateCanvases();
    }

    // рекурсивне оновлення LayoutRebuilder
    private void ForceRebuildRecursive(Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
            ForceRebuildRecursive(t.GetChild(i));

        RectTransform rt = t as RectTransform ?? t.GetComponent<RectTransform>();
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
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
