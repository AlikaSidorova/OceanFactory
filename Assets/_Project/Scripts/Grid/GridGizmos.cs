using UnityEngine;

namespace OceanFactory.Grid
{
    public class GridGizmos : MonoBehaviour
    {
        [SerializeField] private bool draw = true;
        [SerializeField] private Color color = new Color(0.2f, 0.4f, 0.6f, 0.3f);
        [SerializeField] private GridSystem gridSystem;

        private void OnDrawGizmos()
        {
            if (!draw || gridSystem == null || !Application.isPlaying) return;
            int w = gridSystem.Width;
            int h = gridSystem.Height;
            if (w <= 0 || h <= 0) return;

            Gizmos.color = color;
            for (int x = 0; x <= w; x++)
            {
                Gizmos.DrawLine(new Vector3(x, 0f, 0f), new Vector3(x, h, 0f));
            }
            for (int y = 0; y <= h; y++)
            {
                Gizmos.DrawLine(new Vector3(0f, y, 0f), new Vector3(w, y, 0f));
            }
        }
    }
}
