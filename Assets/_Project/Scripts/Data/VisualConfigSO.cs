using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "VisualConfig", menuName = "OceanFactory/Visual Config")]
    public class VisualConfigSO : ScriptableObject
    {
        public GameObject itemVisualPrefab;
        public Sprite itemSpriteFallback;
        public int itemPoolDefaultSize = 200;
        public int itemPoolMaxSize = 2000;
    }
}
