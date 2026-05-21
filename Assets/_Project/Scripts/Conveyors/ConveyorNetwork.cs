using System.Collections.Generic;
using UnityEngine;
using OceanFactory.Core;

namespace OceanFactory.Conveyors
{
    public class ConveyorNetwork : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, ConveyorComponent> map = new();

        public IReadOnlyDictionary<Vector2Int, ConveyorComponent> All => map;

        private void Awake()
        {
            Services.Register(this);
        }

        private void OnDestroy()
        {
            Services.Unregister<ConveyorNetwork>();
        }

        public void Register(ConveyorComponent c)
        {
            if (c == null) return;
            map[c.Cell] = c;
        }

        public void Unregister(Vector2Int cell)
        {
            map.Remove(cell);
        }

        public ConveyorComponent GetAt(Vector2Int cell)
        {
            return map.TryGetValue(cell, out var c) ? c : null;
        }
    }
}
