using System.Collections.Generic;
using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Data;
using OceanFactory.Grid;

namespace OceanFactory.Generation
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private GameConfigSO gameConfig;
        [SerializeField] private ItemCatalogSO itemCatalog;
        [SerializeField] private int minSeedSpacing = 6;
        [SerializeField] private int border = 3;
        [SerializeField, Tooltip("No resource patches will spawn within this Manhattan radius of grid center (reserved for Hub).")]
        private int centerSafeRadius = 5;
        [SerializeField, Range(0f, 1f), Tooltip("Tie-break randomness when picking next frontier cell. Lower = rounder blobs.")]
        private float growthJitter = 0.15f;

        public void GenerateInitial()
        {
            var grid = Services.Get<GridSystem>();
            grid.Initialize();

            if (gameConfig == null)
            {
                Debug.LogError("MapGenerator: GameConfig is not assigned");
                return;
            }
            if (itemCatalog == null)
            {
                Debug.LogError("MapGenerator: ItemCatalog is not assigned");
                return;
            }
            if (!Services.TryGet<RandomProvider>(out var rngp) || rngp.Rng == null)
            {
                Debug.LogError("MapGenerator: RandomProvider not ready");
                return;
            }
            var rng = rngp.Rng;

            var resources = ResolveBaseResources();
            if (resources.Count == 0)
            {
                Debug.LogError("MapGenerator: no base resources found in ItemCatalog (set isBaseResource=true on the 4 base items, or populate baseResources list).");
                return;
            }

            var placedSeeds = new List<Vector2Int>();
            int patchesPerResource = Mathf.Max(1, gameConfig.patchesPerResource);
            int patchMin = Mathf.Max(1, gameConfig.patchMinCells);
            int patchMax = Mathf.Max(patchMin, gameConfig.patchMaxCells);
            var center = new Vector2Int(grid.Width / 2, grid.Height / 2);

            for (int r = 0; r < resources.Count; r++)
            {
                var item = resources[r];
                if (item == null) continue;
                for (int p = 0; p < patchesPerResource; p++)
                {
                    if (!TryFindSeed(grid, placedSeeds, center, rng, out var seed)) continue;
                    placedSeeds.Add(seed);
                    int target = rng.Next(patchMin, patchMax + 1);
                    GrowPatch(grid, seed, item, target, center, rng);
                }
            }
        }

        private List<ItemTypeSO> ResolveBaseResources()
        {
            var result = new List<ItemTypeSO>();
            if (itemCatalog.all != null)
            {
                for (int i = 0; i < itemCatalog.all.Count; i++)
                {
                    var it = itemCatalog.all[i];
                    if (it != null && it.isBaseResource) result.Add(it);
                }
            }
            if (result.Count == 0 && itemCatalog.baseResources != null)
            {
                for (int i = 0; i < itemCatalog.baseResources.Count; i++)
                {
                    var it = itemCatalog.baseResources[i];
                    if (it != null) result.Add(it);
                }
            }
            return result;
        }

        private bool TryFindSeed(GridSystem grid, List<Vector2Int> placedSeeds, Vector2Int center, System.Random rng, out Vector2Int seed)
        {
            int w = grid.Width;
            int h = grid.Height;
            int minX = border;
            int minY = border;
            int maxX = Mathf.Max(border + 1, w - border);
            int maxY = Mathf.Max(border + 1, h - border);
            for (int attempt = 0; attempt < 400; attempt++)
            {
                var c = new Vector2Int(rng.Next(minX, maxX), rng.Next(minY, maxY));
                if (!grid.IsInside(c)) continue;
                if (grid.HasResource(c)) continue;
                if (ManhattanDistance(c, center) <= centerSafeRadius) continue;
                bool tooClose = false;
                for (int i = 0; i < placedSeeds.Count; i++)
                {
                    if (ManhattanDistance(placedSeeds[i], c) < minSeedSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;
                seed = c;
                return true;
            }
            seed = default;
            return false;
        }

        private void GrowPatch(GridSystem grid, Vector2Int seed, ItemTypeSO item, int targetSize, Vector2Int center, System.Random rng)
        {
            var patch = new List<Vector2Int> { seed };
            grid.SetResourceAt(seed, item);
            var frontier = new List<Vector2Int>();
            AppendFrontier(grid, seed, frontier, center);

            while (patch.Count < targetSize && frontier.Count > 0)
            {
                int pickIdx = PickFrontierIndex(grid, frontier, item, rng);
                var c = frontier[pickIdx];
                frontier.RemoveAt(pickIdx);
                if (grid.HasResource(c)) continue;
                grid.SetResourceAt(c, item);
                patch.Add(c);
                AppendFrontier(grid, c, frontier, center);
            }
        }

        private int PickFrontierIndex(GridSystem grid, List<Vector2Int> frontier, ItemTypeSO item, System.Random rng)
        {
            // Prefer cells with more same-resource neighbours -> rounded blobs.
            // Occasional random pick (growthJitter) keeps shapes organic.
            if (frontier.Count == 1) return 0;
            if (rng.NextDouble() < growthJitter) return rng.Next(frontier.Count);

            int bestIdx = 0;
            int bestScore = -1;
            int tieCount = 0;
            for (int i = 0; i < frontier.Count; i++)
            {
                int score = 0;
                foreach (var n in grid.Neighbours4(frontier[i]))
                {
                    if (grid.GetResourceAt(n) == item) score++;
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                    tieCount = 1;
                }
                else if (score == bestScore)
                {
                    tieCount++;
                    // Reservoir sampling so direction bias doesn't form.
                    if (rng.Next(tieCount) == 0) bestIdx = i;
                }
            }
            return bestIdx;
        }

        private void AppendFrontier(GridSystem grid, Vector2Int origin, List<Vector2Int> frontier, Vector2Int center)
        {
            foreach (var n in grid.Neighbours4(origin))
            {
                if (!grid.IsInside(n)) continue;
                if (grid.HasResource(n)) continue;
                if (ManhattanDistance(n, center) <= centerSafeRadius) continue;
                if (!frontier.Contains(n)) frontier.Add(n);
            }
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
