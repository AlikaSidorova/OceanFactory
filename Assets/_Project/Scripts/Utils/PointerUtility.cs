using UnityEngine.EventSystems;

namespace OceanFactory.Utils
{
    public static class PointerUtility
    {
        public static bool IsOverUI()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            return es.IsPointerOverGameObject();
        }
    }
}
