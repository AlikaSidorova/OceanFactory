using System.Collections.Generic;
using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "RecipeBook", menuName = "OceanFactory/Recipe Book")]
    public class RecipeBookSO : ScriptableObject
    {
        public List<RecipeSO> recipes = new();

        private Dictionary<(ItemTypeSO, ItemTypeSO), RecipeSO> map;

        private void OnEnable()
        {
            BuildMap();
        }

        public void BuildMap()
        {
            map = new Dictionary<(ItemTypeSO, ItemTypeSO), RecipeSO>(recipes.Count * 2);
            for (int i = 0; i < recipes.Count; i++)
            {
                var r = recipes[i];
                if (r == null || r.inputA == null || r.inputB == null || r.output == null) continue;
                map[(r.inputA, r.inputB)] = r;
                map[(r.inputB, r.inputA)] = r;
            }
        }

        public bool TryGetRecipe(ItemTypeSO a, ItemTypeSO b, out RecipeSO recipe)
        {
            if (map == null) BuildMap();
            if (a == null || b == null)
            {
                recipe = null;
                return false;
            }
            return map.TryGetValue((a, b), out recipe);
        }
    }
}
