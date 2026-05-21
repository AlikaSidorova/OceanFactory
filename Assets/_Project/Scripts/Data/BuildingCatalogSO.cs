using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "BuildingCatalog", menuName = "OceanFactory/Building Catalog")]
    public class BuildingCatalogSO : ScriptableObject
    {
        public BuildingDefinitionSO extractor;
        public BuildingDefinitionSO conveyor;
        public BuildingDefinitionSO assembler;
        public BuildingDefinitionSO hub;
    }
}
