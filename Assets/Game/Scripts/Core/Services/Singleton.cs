using System;
using UnityEngine;

namespace Game.Scripts.Core.Services
{
    public class Singleton<TSingleton> : MonoBehaviour where TSingleton : MonoBehaviour
    {
        [Obsolete("Obsolete")] public static TSingleton Instance => GetNotNull();
        private static TSingleton _cachedInstance;

        public static TSingleton Current => GetRequired();
        public static TSingleton CurrentOrNull => GetOptional();

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

        public static TSingleton GetOptional()
        {
            return GetInstance(true);
        }

        public static TSingleton GetRequired()
        {
            return GetInstance(false);
        }

        public static void Register(TSingleton instance)
        {
            if (instance == null)
            {
                return;
            }

            _cachedInstance = instance;
        }

        public static void Unregister(TSingleton instance)
        {
            if (_cachedInstance == instance)
            {
                _cachedInstance = null;
            }
        }

        private static TSingleton GetInstance(bool canBeNull)
        {
            if (_cachedInstance != null)
            {
                return _cachedInstance;
            }

            if (!canBeNull)
            {
                Debug.LogError($"Singleton<{typeof(TSingleton).Name}> is not registered. Configure it in the scene and call Singleton.Register from the component.");
            }

            return null;
        }
    }
}
