using System.Collections.Generic;
using UnityEngine;
using OceanFactory.Components;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.Grid
{
    public class GridSystem : MonoBehaviour
    {
        [SerializeField] private GameConfigSO gameConfig;

        private CellType[,] cells;
        private ItemTypeSO[,] resources;
        private Dictionary<Vector2Int, BuildingComponent> buildingAt;

        public int Width { get; private set; }
        public int Height { get; private set; }

        private void Awake()
        {
            Services.Register(this);
        }

        private void OnDestroy()
        {
            Services.Unregister<GridSystem>();
        }

        public void Initialize()
        {
            Width = gameConfig.mapWidth;
            Height = gameConfig.mapHeight;
            cells = new CellType[Width, Height];
            resources = new ItemTypeSO[Width, Height];
            buildingAt = new Dictionary<Vector2Int, BuildingComponent>(Width * Height / 4);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    cells[x, y] = CellType.Empty;
                    resources[x, y] = null;
                }
            }
        }

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        public CellType GetCellType(Vector2Int cell)
        {
            if (!IsInside(cell)) return CellType.Blocked;
            return cells[cell.x, cell.y];
        }

        public BuildingComponent GetBuildingAt(Vector2Int cell)
        {
            if (buildingAt != null && buildingAt.TryGetValue(cell, out var b))
            {
                return b;
            }
            return null;
        }

        public ItemTypeSO GetResourceAt(Vector2Int cell)
        {
            if (!IsInside(cell) || resources == null) return null;
            return resources[cell.x, cell.y];
        }

        public bool HasResource(Vector2Int cell)
        {
            return GetResourceAt(cell) != null;
        }

        public void SetResourceAt(Vector2Int cell, ItemTypeSO item)
        {
            if (!IsInside(cell) || resources == null) return;
            resources[cell.x, cell.y] = item;
        }

        public bool IsRectEmpty(Vector2Int origin, Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0) return false;
            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    var c = new Vector2Int(origin.x + dx, origin.y + dy);
                    if (!IsInside(c)) return false;
                    if (cells[c.x, c.y] != CellType.Empty) return false;
                }
            }
            return true;
        }

        public bool TryOccupyRectForBuilding(Vector2Int origin, Vector2Int size, BuildingComponent building)
        {
            if (!IsRectEmpty(origin, size)) return false;
            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    var c = new Vector2Int(origin.x + dx, origin.y + dy);
                    cells[c.x, c.y] = CellType.Building;
                    buildingAt[c] = building;
                }
            }
            return true;
        }

        public bool FreeRect(Vector2Int origin, Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0) return false;
            for (int dx = 0; dx < size.x; dx++)
            {
                for (int dy = 0; dy < size.y; dy++)
                {
                    var c = new Vector2Int(origin.x + dx, origin.y + dy);
                    if (!IsInside(c)) continue;
                    if (cells[c.x, c.y] == CellType.Building)
                    {
                        cells[c.x, c.y] = CellType.Empty;
                        buildingAt.Remove(c);
                    }
                }
            }
            return true;
        }

        public bool TryOccupyForBuilding(Vector2Int cell, BuildingComponent building)
        {
            return TryOccupyRectForBuilding(cell, Vector2Int.one, building);
        }

        public bool FreeBuilding(Vector2Int cell)
        {
            var comp = GetBuildingAt(cell);
            if (comp == null) return false;
            FreeRect(comp.Cell, comp.Size);
            return true;
        }

        public bool TryOccupyForConveyor(Vector2Int cell)
        {
            if (!IsInside(cell)) return false;
            if (cells[cell.x, cell.y] != CellType.Empty) return false;
            cells[cell.x, cell.y] = CellType.Conveyor;
            return true;
        }

        public bool FreeConveyor(Vector2Int cell)
        {
            if (!IsInside(cell)) return false;
            if (cells[cell.x, cell.y] != CellType.Conveyor) return false;
            cells[cell.x, cell.y] = CellType.Empty;
            return true;
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            return new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
        }

        public IEnumerable<Vector2Int> Neighbours4(Vector2Int cell)
        {
            var right = new Vector2Int(cell.x + 1, cell.y);
            if (IsInside(right)) yield return right;
            var left = new Vector2Int(cell.x - 1, cell.y);
            if (IsInside(left)) yield return left;
            var up = new Vector2Int(cell.x, cell.y + 1);
            if (IsInside(up)) yield return up;
            var down = new Vector2Int(cell.x, cell.y - 1);
            if (IsInside(down)) yield return down;
        }
    }
}
