using UnityEngine;

namespace OceanFactory.Saves
{
    public static class HighScoreStorage
    {
        private const string KeyWeeks = "OF_BestWeeks";
        private const string KeyResources = "OF_BestResources";

        public static int BestWeeks => PlayerPrefs.GetInt(KeyWeeks, 0);
        public static int BestResources => PlayerPrefs.GetInt(KeyResources, 0);

        public static bool SaveIfBetter(int weeks, int resources)
        {
            bool improved = false;
            if (weeks > BestWeeks)
            {
                PlayerPrefs.SetInt(KeyWeeks, weeks);
                improved = true;
            }
            if (resources > BestResources)
            {
                PlayerPrefs.SetInt(KeyResources, resources);
                improved = true;
            }
            if (improved) PlayerPrefs.Save();
            return improved;
        }
    }
}
