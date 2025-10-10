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
        public int id;
        public TankRoot prefab;
        [ShowAssetPreview(64, 64)]
        public Sprite icon;
    }

    public List<Item> items = new ();

    public TankRoot GetPrefab(int id)
    {
        foreach (Item it in items)
        {
            if (it.id == id)
            {
                return it.prefab;
            }
        }
        
        return null;
    }
    
    public Sprite GetIcon(int id)
    {
        foreach (Item it in items)
        {
            if (it.id == id)
            {
                return it.icon;
            }
        }
        
        return null;
    }
}