using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "Building_New", menuName = "OceanFactory/Building Definition")]
    public class BuildingDefinitionSO : ScriptableObject
    {
        public BuildingKind kind;
        public string displayName;
        public GameObject prefab;
        public Sprite bodySprite;

        [Header("Footprint")]
        public Vector2Int size = new Vector2Int(1, 1);

        [Header("Extractor")]
        public ItemTypeSO requiredResource;
        public float extractInterval = 1.5f;

        [Header("Assembler")]
        public float craftTime = 2f;
    }
}
