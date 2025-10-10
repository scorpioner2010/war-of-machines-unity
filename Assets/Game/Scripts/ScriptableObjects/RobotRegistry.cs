// RobotRegistry.cs (лінійний пошук)
using System;
using System.Collections.Generic;
using Game.Scripts.Gameplay.Robots;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "RobotRegistry", menuName = "WOM/Robot Registry")]
public class RobotRegistry : ScriptableObject
{
    [Serializable]
    public class Item
    {
        public string code;
        public TankRoot prefab;
        [ShowAssetPreview(64, 64)]
        public Sprite icon;
    }

    public List<Item> items = new ();

    public TankRoot GetPrefab(string code)
    {
        foreach (Item it in items)
        {
            if (it.code == code)
            {
                return it.prefab;
            }
        }
        
        return null;
    }
    
    public Sprite GetIcon(string code)
    {
        foreach (Item it in items)
        {
            if (it.code == code)
            {
                return it.icon;
            }
        }
        
        return null;
    }
}