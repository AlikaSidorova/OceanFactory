using System;
using System.Collections.Generic;

namespace OceanFactory.Core
{
    public static class Services
    {
        private static readonly Dictionary<Type, object> map = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) return;
            map[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (map.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            throw new InvalidOperationException($"Service {typeof(T).Name} not registered");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (map.TryGetValue(typeof(T), out var raw))
            {
                service = (T)raw;
                return true;
            }
            service = null;
            return false;
        }

        public static void Unregister<T>() where T : class
        {
            map.Remove(typeof(T));
        }

        public static void Clear()
        {
            map.Clear();
        }
    }
}
