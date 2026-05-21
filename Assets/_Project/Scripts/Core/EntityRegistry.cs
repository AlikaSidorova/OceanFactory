// ECS-style registry: components self-register by concrete type for system queries.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanFactory.Core
{
    public static class EntityRegistry
    {
        private static readonly Dictionary<Type, List<MonoBehaviour>> map = new Dictionary<Type, List<MonoBehaviour>>();
        private static readonly Dictionary<Type, object> typedCache = new Dictionary<Type, object>();

        public static void Register<T>(T component) where T : MonoBehaviour
        {
            if (component == null) return;
            var key = typeof(T);
            if (!map.TryGetValue(key, out var raw))
            {
                raw = new List<MonoBehaviour>();
                map[key] = raw;
            }
            if (raw.Contains(component)) return;
            raw.Add(component);
            if (typedCache.TryGetValue(key, out var cachedObj))
            {
                ((List<T>)cachedObj).Add(component);
            }
        }

        public static void Unregister<T>(T component) where T : MonoBehaviour
        {
            if (component == null) return;
            var key = typeof(T);
            if (map.TryGetValue(key, out var raw))
            {
                raw.Remove(component);
            }
            if (typedCache.TryGetValue(key, out var cachedObj))
            {
                ((List<T>)cachedObj).Remove(component);
            }
        }

        public static IReadOnlyList<T> GetAll<T>() where T : MonoBehaviour
        {
            var key = typeof(T);
            if (!map.TryGetValue(key, out var raw) || raw.Count == 0)
            {
                return Array.Empty<T>();
            }
            if (!typedCache.TryGetValue(key, out var cachedObj))
            {
                var typed = new List<T>(raw.Count);
                for (int i = 0; i < raw.Count; i++)
                {
                    typed.Add((T)raw[i]);
                }
                typedCache[key] = typed;
                return typed;
            }
            return (List<T>)cachedObj;
        }

        public static void Clear()
        {
            map.Clear();
            typedCache.Clear();
        }
    }
}
