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
        public VehicleRoot prefab;
        [ShowAssetPreview(64, 64)]
        public Sprite icon;
    }

    public List<Item> items = new ();

    public VehicleRoot GetPrefab(string code)
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

    public string GetFirstCode()
    {
        if (items == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item != null && !string.IsNullOrEmpty(item.code) && item.prefab != null)
            {
                return item.code;
            }
        }

        return string.Empty;
    }

    public void FillValidCodes(List<string> results)
    {
        if (results == null)
        {
            return;
        }

        if (items == null)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (item == null || string.IsNullOrEmpty(item.code) || item.prefab == null)
            {
                continue;
            }

            results.Add(item.code);
        }
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
