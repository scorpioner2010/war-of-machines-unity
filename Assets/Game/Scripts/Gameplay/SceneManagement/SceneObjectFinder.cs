using System;
using System.Collections.Generic;
using UnityEngine;
using UEScene = UnityEngine.SceneManagement.Scene;

namespace Game.Scripts.Gameplay.SceneManagement
{
    public static class SceneObjectFinder
    {
        public static List<T> FindInScene<T>(UEScene scene, bool includeInactive = true) where T : Component
        {
            List<T> results = new List<T>();
            if (!scene.IsValid())
            {
                return results;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T[] components = root.GetComponentsInChildren<T>(includeInactive);
                for (int i = 0; i < components.Length; i++)
                {
                    results.Add(components[i]);
                }
            }

            return results;
        }

        public static List<Component> FindInHierarchy(GameObject root, Type componentType, bool includeInactive = true)
        {
            List<Component> results = new List<Component>();
            if (root == null || componentType == null)
            {
                return results;
            }

            Component[] components = root.GetComponentsInChildren(componentType, includeInactive);
            for (int i = 0; i < components.Length; i++)
            {
                results.Add(components[i]);
            }

            return results;
        }

        public static List<T> FindInHierarchy<T>(GameObject root, bool includeInactive = true) where T : Component
        {
            List<T> results = new List<T>();
            if (root == null)
            {
                return results;
            }

            T[] components = root.GetComponentsInChildren<T>(includeInactive);
            for (int i = 0; i < components.Length; i++)
            {
                results.Add(components[i]);
            }

            return results;
        }
    }
}
