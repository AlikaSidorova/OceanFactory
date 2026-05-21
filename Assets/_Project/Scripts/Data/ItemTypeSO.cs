using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "Item_New", menuName = "OceanFactory/Item Type")]
    public class ItemTypeSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public Color color = Color.white;
        [Tooltip("Shown on resource deposit tiles on the map.")]
        public Sprite icon;
        [Tooltip("Shown on conveyor items, UI panels, assembler output, and hub goals. Falls back to 'icon' if not set.")]
        public Sprite itemSprite;
        public bool isBaseResource;
        [Tooltip("For deposit types (BaseRes*): the actual Item_* that extractors drop on conveyors when mining this deposit. Leave null to use this item itself.")]
        public ItemTypeSO producedItem;

        /// <summary>Returns itemSprite if assigned, otherwise falls back to icon.</summary>
        public Sprite ItemOrFallback => itemSprite != null ? itemSprite : icon;

        /// <summary>What an extractor actually places on the belt. Deposit types redirect to producedItem.</summary>
        public ItemTypeSO ExtractedItem => producedItem != null ? producedItem : this;
    }
}
