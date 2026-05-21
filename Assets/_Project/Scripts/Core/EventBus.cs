using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanFactory.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            var key = typeof(T);
            if (handlers.TryGetValue(key, out var existing))
            {
                handlers[key] = Delegate.Combine(existing, handler);
            }
            else
            {
                handlers[key] = handler;
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null) return;
            var key = typeof(T);
            if (!handlers.TryGetValue(key, out var existing)) return;
            var updated = Delegate.Remove(existing, handler);
            if (updated == null)
            {
                handlers.Remove(key);
            }
            else
            {
                handlers[key] = updated;
            }
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (!handlers.TryGetValue(typeof(T), out var del)) return;
            var invocationList = del.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                var typed = (Action<T>)invocationList[i];
                try
                {
                    typed.Invoke(evt);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static void Clear()
        {
            handlers.Clear();
        }
    }
}
