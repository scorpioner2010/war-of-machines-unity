// DevelopmentTree.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevelopmentTree : MonoBehaviour
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

    private readonly List<Transform> _fractionRoots = new();
    private readonly List<Button> _factionButtons = new();

    [Serializable]
    private class VehicleNodeSpec
    {
        public string spriteName;
        public string displayName;
        public Vehicle vehicleType;
        public int level;
    }

    [Serializable]
    private class FactionSpec
    {
        public string code;
        public string displayName;
        public VehicleNodeSpec starter;
        public List<VehicleNodeSpec> scout = new();
        public List<VehicleNodeSpec> guardian = new();
        public List<VehicleNodeSpec> colossus = new();
    }

    private void Awake()
    {
        if (buttonBack != null)
        {
            buttonBack.onClick.AddListener(() =>
            {
                Game.Scripts.MenuController.MenuManager.OpenMenu(Game.Scripts.MenuController.MenuType.MainMenu);
            });
        }
    }

    [Button]
    public void Init()
    {
        WipeUI();

        List<FactionSpec> factions = BuildTestFactions();

        for (int i = 0; i < factions.Count; i++)
        {
            int idx = i;
            Button btn = Instantiate(factionButtonPrefab, buttonsContainer);
            _factionButtons.Add(btn);
            btn.GetComponentInChildren<TMP_Text>().text = factions[i].displayName;
            btn.onClick.AddListener(() =>
            {
                SetActiveContainer(idx);
                _ = RebuildAllLayouts();
            });
        }

        for (int i = 0; i < factions.Count; i++)
        {
            BuildFactionTree(factions[i]);
        }

        if (_fractionRoots.Count > 0)
        {
            SetActiveContainer(0);
        }

        _ = RebuildAllLayouts();
    }

    public void SetActiveContainer(int number)
    {
        for (int i = 0; i < _fractionRoots.Count; i++)
        {
            GameObject go = _fractionRoots[i].gameObject;
            go.SetActive(i == number);
        }
    }

    public Sprite GetSpriteByName(string spriteName)
    {
        return null;
    }

    private void BuildFactionTree(FactionSpec spec)
    {
        Transform root = Instantiate(fractionTreePrefab, animationPanel);
        root.name = $"FactionRoot_{spec.code}";
        _fractionRoots.Add(root);

        Transform starterContainer = Instantiate(starterContainerPrefab, root);
        starterContainer.name = "StarterContainer";
        CreateTreeItem(starterContainer, spec.starter, true);

        FactionContainer columns = Instantiate(factionContainerPrefab, root);
        columns.name = "ColumnsContainer";

        BuildColumn(columns.transform, spec.scout, Vehicle.Scout, "Scout_Column");
        BuildColumn(columns.transform, spec.guardian, Vehicle.Guardian, "Guardian_Column");
        BuildColumn(columns.transform, spec.colossus, Vehicle.Colossus, "Colossus_Column");
    }

    private void BuildColumn(Transform parent, List<VehicleNodeSpec> line, Vehicle type, string name)
    {
        TreeGrid grid = Instantiate(treeGridPrefab, parent);
        grid.name = name;
        grid.Init(type);

        foreach (VehicleNodeSpec node in line)
        {
            if (node.vehicleType != type)
            {
                Debug.LogWarning($"[{name}] '{node.displayName}' type {node.vehicleType}, expected {type}");
            }

            CreateTreeItem(grid.transform, node, false);
        }
    }

    private void CreateTreeItem(Transform parent, VehicleNodeSpec spec, bool isStarter)
    {
        TreeItem item = Instantiate(treeItemPrefab, parent);
        item.image.sprite = GetSpriteByName(spec.spriteName);
        item.vehicleName.text = spec.displayName;
        item.vehicleType = spec.vehicleType;
        item.level.text = spec.level.ToString();
    }

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

    private void ForceRebuildRecursive(Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            ForceRebuildRecursive(t.GetChild(i));
        }

        RectTransform rt = t as RectTransform ?? t.GetComponent<RectTransform>();
        if (rt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        }
    }

    private List<FactionSpec> BuildTestFactions()
    {
        FactionSpec iron = new FactionSpec
        {
            code = "iron_alliance",
            displayName = "Iron Alliance",
            starter = new VehicleNodeSpec { spriteName = "ia_l1", displayName = "IA Starter L1", vehicleType = Vehicle.Scout, level = 1 },
            scout = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "ia_scout_l2", displayName = "IA Scout L2", vehicleType = Vehicle.Scout, level = 2 },
                new VehicleNodeSpec { spriteName = "ia_scout_l3", displayName = "IA Scout L3", vehicleType = Vehicle.Scout, level = 3 },
                new VehicleNodeSpec { spriteName = "ia_scout_l4", displayName = "IA Scout L4", vehicleType = Vehicle.Scout, level = 4 }
            },
            guardian = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "ia_guard_l2", displayName = "IA Guardian L2", vehicleType = Vehicle.Guardian, level = 2 },
                new VehicleNodeSpec { spriteName = "ia_guard_l3", displayName = "IA Guardian L3", vehicleType = Vehicle.Guardian, level = 3 },
                new VehicleNodeSpec { spriteName = "ia_guard_l4", displayName = "IA Guardian L4", vehicleType = Vehicle.Guardian, level = 4 }
            },
            colossus = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "ia_col_l2", displayName = "IA Colossus L2", vehicleType = Vehicle.Colossus, level = 2 },
                new VehicleNodeSpec { spriteName = "ia_col_l3", displayName = "IA Colossus L3", vehicleType = Vehicle.Colossus, level = 3 },
                new VehicleNodeSpec { spriteName = "ia_col_l4", displayName = "IA Colossus L4", vehicleType = Vehicle.Colossus, level = 4 }
            }
        };

        FactionSpec nova = new FactionSpec
        {
            code = "nova_syndicate",
            displayName = "Nova Syndicate",
            starter = new VehicleNodeSpec { spriteName = "nv_l1", displayName = "NS Starter L1", vehicleType = Vehicle.Scout, level = 1 },
            scout = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "nv_scout_l2", displayName = "NS Scout L2", vehicleType = Vehicle.Scout, level = 2 },
                new VehicleNodeSpec { spriteName = "nv_scout_l3", displayName = "NS Scout L3", vehicleType = Vehicle.Scout, level = 3 },
                new VehicleNodeSpec { spriteName = "nv_scout_l4", displayName = "NS Scout L4", vehicleType = Vehicle.Scout, level = 4 }
            },
            guardian = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "nv_guard_l2", displayName = "NS Guardian L2", vehicleType = Vehicle.Guardian, level = 2 },
                new VehicleNodeSpec { spriteName = "nv_guard_l3", displayName = "NS Guardian L3", vehicleType = Vehicle.Guardian, level = 3 },
                new VehicleNodeSpec { spriteName = "nv_guard_l4", displayName = "NS Guardian L4", vehicleType = Vehicle.Guardian, level = 4 }
            },
            colossus = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "nv_col_l2", displayName = "NS Colossus L2", vehicleType = Vehicle.Colossus, level = 2 },
                new VehicleNodeSpec { spriteName = "nv_col_l3", displayName = "NS Colossus L3", vehicleType = Vehicle.Colossus, level = 3 },
                new VehicleNodeSpec { spriteName = "nv_col_l4", displayName = "NS Colossus L4", vehicleType = Vehicle.Colossus, level = 4 }
            }
        };

        return new List<FactionSpec> { iron, nova };
    }
}
