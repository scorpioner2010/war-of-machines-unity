using System;
using UnityEngine;

namespace Game.Scripts.Core.Services
{
    public class Singleton<TSingleton> : MonoBehaviour where TSingleton : MonoBehaviour
    {
        [Obsolete("Obsolete")] public static TSingleton Instance => GetNotNull();
        private static TSingleton _cachedInstance;

        [Obsolete("Obsolete")]
        public static TSingleton GetCanBeNull()
        {
            return GetInstance(true);
        }

        [Obsolete("Obsolete")]
        public static TSingleton GetNotNull()
        {
            return GetInstance(false);
        }

        [Obsolete("Obsolete")]
        private static TSingleton GetInstance(bool canBeNull)
        {
            if (_cachedInstance != null)
            {
                return _cachedInstance;
            }
            
            var allInstances = FindObjectsOfType<TSingleton>();
            var instance = allInstances.Length > 0
                ? allInstances[0]
                : GetInstanceIfNotFound(canBeNull);
            
            if (allInstances.Length > 1)
            {
                Debug.LogError($"The number of {typeof(TSingleton).Name} on the scene is greater than one!");
            }
            
            for (var i = 1; i < allInstances.Length; i++)
            {
                Destroy(allInstances[i]);
            }

            return _cachedInstance = instance;
        }

        private static TSingleton GetInstanceIfNotFound(bool canBeNull)
        {
            return canBeNull ? null : new GameObject($"[Singleton] {typeof(TSingleton).Name}").AddComponent<TSingleton>();
        }
    }
}
