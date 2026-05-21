using System;
using System.Collections.Generic;
using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.Economy
{
    public class DeliveryLedger : MonoBehaviour
    {
        private readonly Dictionary<ItemTypeSO, int> counts = new();
        private int total;

        public int Total => total;
        public IReadOnlyDictionary<ItemTypeSO, int> Counts => counts;

        public event Action<ItemTypeSO, int> OnItemDelivered;

        private void Awake()
        {
            Services.Register(this);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ItemDeliveredEvent>(OnDeliveredEvent);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ItemDeliveredEvent>(OnDeliveredEvent);
        }

        private void OnDestroy()
        {
            Services.Unregister<DeliveryLedger>();
        }

        public int GetCount(ItemTypeSO item)
        {
            if (item == null) return 0;
            return counts.TryGetValue(item, out var n) ? n : 0;
        }

        public void Reset()
        {
            counts.Clear();
            total = 0;
        }

        private void OnDeliveredEvent(ItemDeliveredEvent e)
        {
            if (e.Item == null) return;
            counts.TryGetValue(e.Item, out var n);
            counts[e.Item] = n + 1;
            total++;
            OnItemDelivered?.Invoke(e.Item, n + 1);
        }
    }
}
