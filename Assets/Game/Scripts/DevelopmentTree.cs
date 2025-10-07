using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.API;
using UnityEngine;
using UnityEngine.UI;
using Game.Scripts.API.ServerManagers;
using Game.Scripts.Audio;
using Game.Scripts.Core.Services;
using Game.Scripts.MenuController;
using Game.Scripts.Player.Data;
using TMPro;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class DevelopmentTree : MonoBehaviour
{
    public Button buttonBack;
    public Transform buttonsContainer;

    public Button factionButtonPrefab;
    public FactionContainer factionContainer;
    public TreeGrid  treeGrid;
    public TreeItem treeItemPrefab;
    
    private void Awake()
    {
        buttonBack.onClick.AddListener(() =>
        {
            MenuManager.OpenMenu(MenuType.MainMenu);
        });
    }

    public void Init()
    {
        ddd
    }
    
    public string factionCode = "iron_alliance";


    private async UniTask LoadGraph()
    {
        Debug.Log("Loading vehicle graph for faction: " + factionCode);

        var (ok, msg, graph) = await VehiclesManager.GetGraph(factionCode);
        if (!ok || graph.nodes == null)
        {
            Debug.LogError("Failed to load graph: " + msg);
            return;
        }

        // побудова словників
        var nodes = new Dictionary<int, VehicleNode>();
        foreach (var n in graph.nodes)
            nodes[n.id] = n;

        var edgesByFrom = new Dictionary<int, List<VehicleEdge>>();
        foreach (var e in graph.edges)
        {
            if (!edgesByFrom.ContainsKey(e.fromId))
                edgesByFrom[e.fromId] = new List<VehicleEdge>();
            edgesByFrom[e.fromId].Add(e);
        }

        // приклад виводу у консоль:
        foreach (var node in graph.nodes)
        {
            Debug.Log($"[{node.level}] {node.name} ({node.@class})");

            if (edgesByFrom.TryGetValue(node.id, out var links))
            {
                foreach (var l in links)
                {
                    if (nodes.TryGetValue(l.toId, out var child))
                        Debug.Log($"   └→ {child.name} ({child.@class})  XP: {l.requiredXp}");
                }
            }
        }

        Debug.Log($"Loaded {graph.nodes.Length} vehicles, {graph.edges.Length} links total.");
    }
}