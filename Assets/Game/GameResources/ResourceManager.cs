using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager _in;
    public RobotRegistry registry;
    private void Awake() => _in = this;
    public static TankRoot GetPrefab(int id) => _in.registry.GetPrefab(id);
    public static Sprite GetIcon(int id) => _in.registry.GetIcon(id);
}