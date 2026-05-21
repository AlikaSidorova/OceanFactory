using System;
using System.Collections.Generic;
using UnityEngine;

namespace OceanFactory.Data
{
    /// <summary>
    /// One level objective: a player-facing title and one or more item delivery requirements.
    /// When every requirement's target is met, the hub advances to the next level.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_New", menuName = "OceanFactory/Level Goal")]
    public class LevelGoalSO : ScriptableObject
    {
        [Serializable]
        public struct Requirement
        {
            public ItemTypeSO item;
            public int targetCount;
        }

        [Tooltip("Display number for this level (1, 2, 3...). Purely cosmetic; ordering comes from LevelProgression.")]
        public int levelNumber = 1;
        [Tooltip("User-facing title shown in the HUD, e.g. 'Bootstrap the factory'.")]
        public string title;
        public List<Requirement> requirements = new();
    }
}
