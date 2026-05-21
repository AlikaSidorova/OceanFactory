using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Data;
using OceanFactory.Grid;

namespace OceanFactory.Generation
{
    public class ResourcePatchVisualizer : MonoBehaviour
    {
        [SerializeField] private Sprite patchSprite;
        [SerializeField] private Transform patchesRoot;
        [SerializeField] private int sortingOrder = -5;
        [SerializeField, Range(0.05f, 1f)] private float baseAlpha = 0.55f;
        [SerializeField, Range(0.1f, 1f)] private float iconScale = 0.65f;
        [SerializeField, Range(0.05f, 1f)] private float iconAlpha = 0.95f;
        [SerializeField] private bool drawBackdrop = true;

        public void RebuildFromGrid()
        {
            if (patchesRoot == null)
            {
                Debug.LogError("ResourcePatchVisualizer: patchesRoot is not assigned");
                return;
            }
            for (int i = patchesRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(patchesRoot.GetChild(i).gameObject);
            }

            if (!Services.TryGet<GridSystem>(out var grid)) return;
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    var c = new Vector2Int(x, y);
                    var res = grid.GetResourceAt(c);
                    if (res == null) continue;
                    SpawnCell(c, res, grid);
                }
            }
        }

        private void SpawnCell(Vector2Int cell, ItemTypeSO item, GridSystem grid)
        {
            var center = grid.CellToWorld(cell);

            if (drawBackdrop && patchSprite != null)
            {
                var bg = new GameObject($"Patch_{cell.x}_{cell.y}_{item.displayName}");
                bg.transform.SetParent(patchesRoot, false);
                bg.transform.position = center;
                var sr = bg.AddComponent<SpriteRenderer>();
                sr.sprite = patchSprite;
                sr.color = new Color(item.color.r, item.color.g, item.color.b, baseAlpha);
                sr.sortingOrder = sortingOrder;
            }

            if (item.icon != null)
            {
                var ic = new GameObject($"Icon_{cell.x}_{cell.y}_{item.displayName}");
                ic.transform.SetParent(patchesRoot, false);
                ic.transform.position = center;
                ic.transform.localScale = new Vector3(iconScale, iconScale, 1f);
                var sr = ic.AddComponent<SpriteRenderer>();
                sr.sprite = item.icon;
                sr.color = new Color(1f, 1f, 1f, iconAlpha);
                sr.sortingOrder = sortingOrder + 1;
            }
        }
    }
}
