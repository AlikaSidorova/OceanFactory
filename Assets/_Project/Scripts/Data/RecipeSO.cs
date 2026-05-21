using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "Recipe_New", menuName = "OceanFactory/Recipe")]
    public class RecipeSO : ScriptableObject
    {
        public ItemTypeSO inputA;
        public ItemTypeSO inputB;
        public ItemTypeSO output;
        public float craftTime = 2f;
    }
}
