using System;
using System.Collections.Generic;

namespace Game.Scripts.Core.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new();

        public static void Register<T>(T service)
        {
            Type type = typeof(T);
            if (!Services.TryAdd(type, service))
            {
                throw new Exception($"Service of type {type} is already registered.");
            }
        }

        public static T Get<T>()
        {
            Type type = typeof(T);
            if (Services.TryGetValue(type, out var service))
            {
                return (T)service;
            }

            throw new Exception($"Service of type {type} not registered.");
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}