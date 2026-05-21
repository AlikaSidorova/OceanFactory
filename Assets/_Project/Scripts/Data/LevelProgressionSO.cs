using System.Collections.Generic;
using UnityEngine;

namespace OceanFactory.Data
{
    /// <summary>
    /// Ordered list of level goals. The hub references this asset and walks the list as
    /// the player completes each level. Drop new LevelGoalSOs in to extend the game.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelProgression", menuName = "OceanFactory/Level Progression")]
    public class LevelProgressionSO : ScriptableObject
    {
        public List<LevelGoalSO> levels = new();
    }
}
