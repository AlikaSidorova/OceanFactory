using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.UI
{
    public class ItemCounterPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private ItemCatalogSO catalog;
        [SerializeField] private bool hideZeros = false;

        private readonly Dictionary<ItemTypeSO, int> counts = new();
        private readonly StringBuilder builder = new();

        private void Awake()
        {
            ApplyLabelSafety();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ItemDeliveredEvent>(OnDelivered);
            ApplyLabelSafety();
            Refresh();
        }

        private void ApplyLabelSafety()
        {
            if (label == null) return;
            // Prevent TMP from word-wrapping long item names into a second line with hanging indent.
#pragma warning disable CS0618
            label.enableWordWrapping = false;
#pragma warning restore CS0618
            label.overflowMode = TextOverflowModes.Overflow;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ItemDeliveredEvent>(OnDelivered);
        }

        private void OnDelivered(ItemDeliveredEvent e)
        {
            if (e.Item == null) return;
            counts.TryGetValue(e.Item, out int n);
            counts[e.Item] = n + 1;
            Refresh();
        }

        private void Refresh()
        {
            if (label == null) return;
            builder.Clear();
            if (catalog != null && catalog.all != null && catalog.all.Count > 0)
            {
                for (int i = 0; i < catalog.all.Count; i++)
                {
                    var item = catalog.all[i];
                    if (item == null) continue;
                    int c = counts.TryGetValue(item, out var v) ? v : 0;
                    if (hideZeros && c == 0) continue;
                    builder.Append(item.displayName);
                    builder.Append(": ");
                    builder.Append(c);
                    builder.Append('\n');
                }
            }
            else
            {
                foreach (var kvp in counts)
                {
                    if (kvp.Key == null) continue;
                    builder.Append(kvp.Key.displayName);
                    builder.Append(": ");
                    builder.Append(kvp.Value);
                    builder.Append('\n');
                }
            }
            label.text = builder.ToString();
        }
    }
}
