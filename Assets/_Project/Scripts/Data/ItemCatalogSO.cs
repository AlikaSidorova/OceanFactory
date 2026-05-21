using System.Collections.Generic;
using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "ItemCatalog", menuName = "OceanFactory/Item Catalog")]
    public class ItemCatalogSO : ScriptableObject
    {
        public List<ItemTypeSO> all = new();
        public List<ItemTypeSO> baseResources = new();
    }
}
