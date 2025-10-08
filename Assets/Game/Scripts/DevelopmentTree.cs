using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Scripts.API; // для Vehicle enum, якщо він у тебе там. Якщо ні — дивись локальний enum VehicleClass нижче.
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevelopmentTree : MonoBehaviour
{
    [Header("Roots & Prefabs")]
    public Transform animationPanel;              // Головний контейнер для всіх фракцій
    public Transform fractionTreePrefab;          // Кореневий об'єкт для однієї фракції (контейнер вкладки фракції)
    public Transform startVehicleContainerPrefab; // Контейнер для стартера зверху
    public FactionContainer factionContainerPrefab; // Контейнер для колонок (3 вертикальні гріди)
    public TreeGrid treeGrid;                     // Вертикальний грід однієї колонки
    public TreeItem treeItemPrefab;               // Звичайний айтем (L2..L4)

    [Header("Tabs UI")]
    public Transform buttonsContainer;            // Куди ставити кнопки фракцій
    public Button factionButtonPrefab;            // Префаб кнопки фракції
    public Button buttonBack;                     // Кнопка Назад

    // Для прибирання при перевідбудові
    private readonly List<Transform> _fractionRoots = new();
    private readonly List<Button> _factionButtons = new();

    #region Test Data Structures

    [Serializable]
    public class VehicleNodeSpec
    {
        public string spriteName;
        public string displayName;
        public Vehicle vehicleType; // або VehicleClass, якщо використовуєш локальний enum
        public int level;
    }

    [Serializable]
    public class FactionSpec
    {
        public string factionCode;
        public string factionDisplayName;

        public VehicleNodeSpec starter;                     // L1
        public List<VehicleNodeSpec> scoutLine = new();     // L2,L3,L4
        public List<VehicleNodeSpec> guardianLine = new();  // L2,L3,L4
        public List<VehicleNodeSpec> colossusLine = new();  // L2,L3,L4
    }

    #endregion

    #region Public API

    // Підстав свій пошук спрайтів. Зараз заглушка.
    public Sprite GetSpriteByName(string spriteName)
    {
        // Наприклад: return Resources.Load<Sprite>($"UI/Vehicles/{spriteName}");
        return null;
    }

    public void SetActiveContainer(int number)
    {
        for (int i = 0; i < _fractionRoots.Count; i++)
        {
            var go = _fractionRoots[i]?.gameObject;
            if (go != null) go.SetActive(i == number);
        }
    }
    
    private async void RebuildAllLayouts()
    {
        // Дочекайся завершення всіх етапів кадру, щоб лейаути мали валідні розміри контенту
        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

        Canvas.ForceUpdateCanvases();

        // 1) Спочатку діти (TreeGrid та їх айтеми), потім контейнери колонок, потім кожен корінь фракції.
        foreach (var root in _fractionRoots.Where(r => r != null))
        {
            // Форснемо дочірні гріди
            ForceRebuildInChildren(root);

            // І нарешті сам корінь
            var rect = root as RectTransform ?? root.GetComponent<RectTransform>();
            if (rect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void ForceRebuildInChildren(Transform t)
    {
        // Обов'язково викликати для кожного RectTransform знизу догори
        for (int i = 0; i < t.childCount; i++)
            ForceRebuildInChildren(t.GetChild(i));

        var rt = t as RectTransform ?? t.GetComponent<RectTransform>();
        if (rt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    [Button]
    public async void Init()
    {
        // 1) Прибрати попередній UI
        WipeUI();

        // 2) Зібрати тестові дані (дві фракції)
        List<FactionSpec> factions = BuildTestFactions();

        // 3) Створити кнопки фракцій
        for (int i = 0; i < factions.Count; i++)
        {
            var idx = i;
            var btn = Instantiate(factionButtonPrefab, buttonsContainer);
            _factionButtons.Add(btn);
            btn.GetComponentInChildren<TMP_Text>().text = factions[i].factionDisplayName;
            btn.onClick.AddListener(() =>
            {
                SetActiveContainer(idx);
                RebuildAllLayouts();
            });
        }

        // 4) Побудувати кожну фракцію
        for (int i = 0; i < factions.Count; i++)
        {
            BuildFactionTree(factions[i], i);
        }

        // 5) Відкрити першу вкладку (якщо є)
        if (_fractionRoots.Count > 0)
            SetActiveContainer(0);

        RebuildAllLayouts();
    }

    private void Awake()
    {
        if (buttonBack != null)
            buttonBack.onClick.AddListener(() =>
            {
                // твій менеджер меню
                Game.Scripts.MenuController.MenuManager.OpenMenu(Game.Scripts.MenuController.MenuType.MainMenu);
            });
    }

    #endregion

    #region Build Logic

    private void BuildFactionTree(FactionSpec spec, int index)
    {
        // Кореневий контейнер вкладки фракції
        Transform factionRoot = Instantiate(fractionTreePrefab, animationPanel);
        factionRoot.name = $"FactionRoot_{spec.factionCode}";
        _fractionRoots.Add(factionRoot);

        // Стартовий контейнер + айтем
        Transform starterContainer = Instantiate(startVehicleContainerPrefab, factionRoot);
        starterContainer.name = "StarterContainer";

        CreateTreeItem(starterContainer, spec.starter, isStarter: true);

        // Контейнер колонок
        FactionContainer columnsContainer = Instantiate(factionContainerPrefab, factionRoot);
        columnsContainer.name = "ColumnsContainer";

        // 3 вертикальні гріди: Scout / Guardian / Colossus
        BuildColumn(columnsContainer.transform, spec.scoutLine, Vehicle.Scout, "Scout_Column");
        BuildColumn(columnsContainer.transform, spec.guardianLine, Vehicle.Guardian, "Guardian_Column");
        BuildColumn(columnsContainer.transform, spec.colossusLine, Vehicle.Colossus, "Colossus_Column");
    }

    private void BuildColumn(Transform parent, List<VehicleNodeSpec> line, Vehicle expectedType, string name)
    {
        TreeGrid grid = Instantiate(treeGrid, parent);
        grid.name = name;

        foreach (var node in line)
        {
            // На випадок помилок у даних — попередимо різний тип
            if (node.vehicleType != expectedType)
                Debug.LogWarning($"[{name}] Node '{node.displayName}' має type {node.vehicleType}, очікувався {expectedType}");

            CreateTreeItem(grid.transform, node, isStarter: false);
        }
    }

    private void CreateTreeItem(Transform parent, VehicleNodeSpec spec, bool isStarter)
    {
        TreeItem item = Instantiate(treeItemPrefab, parent);
        item.image.sprite = GetSpriteByName(spec.spriteName);
        item.vehicleName.text = spec.displayName;
        item.vehicleType = spec.vehicleType; // твій enum Vehicle (Scout/Guardian/Colossus)
        item.level.text = spec.level.ToString();
    }

    #endregion

    #region Helpers

    private void WipeUI()
    {
        // видалити попередні корені фракцій
        foreach (var t in _fractionRoots)
        {
            if (t != null) DestroyImmediate(t.gameObject);
        }
        _fractionRoots.Clear();

        // видалити кнопки
        foreach (var b in _factionButtons)
        {
            if (b != null) DestroyImmediate(b.gameObject);
        }
        _factionButtons.Clear();
    }

    private List<FactionSpec> BuildTestFactions()
    {
        // ФРАКЦІЯ 1 — Iron Alliance (приклад)
        var iron = new FactionSpec
        {
            factionCode = "iron_alliance",
            factionDisplayName = "Iron Alliance",
            starter = new VehicleNodeSpec { spriteName = "ia_l1", displayName = "IA Starter L1", vehicleType = Vehicle.Scout, level = 1 },
            scoutLine = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "ia_scout_l2", displayName = "IA Scout L2", vehicleType = Vehicle.Scout, level = 2 },
                new VehicleNodeSpec { spriteName = "ia_scout_l3", displayName = "IA Scout L3", vehicleType = Vehicle.Scout, level = 3 },
                new VehicleNodeSpec { spriteName = "ia_scout_l4", displayName = "IA Scout L4", vehicleType = Vehicle.Scout, level = 4 }
            },
            guardianLine = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "ia_guard_l2", displayName = "IA Guardian L2", vehicleType = Vehicle.Guardian, level = 2 },
                new VehicleNodeSpec { spriteName = "ia_guard_l3", displayName = "IA Guardian L3", vehicleType = Vehicle.Guardian, level = 3 },
                new VehicleNodeSpec { spriteName = "ia_guard_l4", displayName = "IA Guardian L4", vehicleType = Vehicle.Guardian, level = 4 }
            },
            colossusLine = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "ia_col_l2", displayName = "IA Colossus L2", vehicleType = Vehicle.Colossus, level = 2 },
                new VehicleNodeSpec { spriteName = "ia_col_l3", displayName = "IA Colossus L3", vehicleType = Vehicle.Colossus, level = 3 },
                new VehicleNodeSpec { spriteName = "ia_col_l4", displayName = "IA Colossus L4", vehicleType = Vehicle.Colossus, level = 4 }
            }
        };

        // ФРАКЦІЯ 2 — Nova Syndicate (приклад)
        var nova = new FactionSpec
        {
            factionCode = "nova_syndicate",
            factionDisplayName = "Nova Syndicate",
            starter = new VehicleNodeSpec { spriteName = "nv_l1", displayName = "NS Starter L1", vehicleType = Vehicle.Scout, level = 1 },
            scoutLine = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "nv_scout_l2", displayName = "NS Scout L2", vehicleType = Vehicle.Scout, level = 2 },
                new VehicleNodeSpec { spriteName = "nv_scout_l3", displayName = "NS Scout L3", vehicleType = Vehicle.Scout, level = 3 },
                new VehicleNodeSpec { spriteName = "nv_scout_l4", displayName = "NS Scout L4", vehicleType = Vehicle.Scout, level = 4 }
            },
            guardianLine = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "nv_guard_l2", displayName = "NS Guardian L2", vehicleType = Vehicle.Guardian, level = 2 },
                new VehicleNodeSpec { spriteName = "nv_guard_l3", displayName = "NS Guardian L3", vehicleType = Vehicle.Guardian, level = 3 },
                new VehicleNodeSpec { spriteName = "nv_guard_l4", displayName = "NS Guardian L4", vehicleType = Vehicle.Guardian, level = 4 }
            },
            colossusLine = new List<VehicleNodeSpec>
            {
                new VehicleNodeSpec { spriteName = "nv_col_l2", displayName = "NS Colossus L2", vehicleType = Vehicle.Colossus, level = 2 },
                new VehicleNodeSpec { spriteName = "nv_col_l3", displayName = "NS Colossus L3", vehicleType = Vehicle.Colossus, level = 3 },
                new VehicleNodeSpec { spriteName = "nv_col_l4", displayName = "NS Colossus L4", vehicleType = Vehicle.Colossus, level = 4 }
            }
        };

        return new List<FactionSpec> { iron, nova };
    }

    #endregion
}
