using UnityEngine;

namespace OceanFactory.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "OceanFactory/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Map")]
        public int mapWidth = 60;
        public int mapHeight = 40;

        [Header("Resource Patches")]
        public int patchesPerResource = 6;
        public int patchMinCells = 12;
        public int patchMaxCells = 24;

        [Header("Simulation")]
        public float simTickHz = 6f;

        [Header("Seed")]
        public int randomSeed = 0;
    }
}
