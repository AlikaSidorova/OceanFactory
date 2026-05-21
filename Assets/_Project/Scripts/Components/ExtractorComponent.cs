using UnityEngine;
using OceanFactory.Conveyors;
using OceanFactory.Core;
using OceanFactory.Data;
using OceanFactory.Grid;

namespace OceanFactory.Components
{
    public class ExtractorComponent : BuildingComponent
    {
        [SerializeField] private SpriteRenderer iconRenderer;

        private static readonly Direction[] AllDirs =
        {
            Direction.North, Direction.East, Direction.South, Direction.West
        };

        private float accumulator;
        private int outputCursor;

        public override void OnPlaced()
        {
            base.OnPlaced();
            if (iconRenderer == null) return;
            ItemTypeSO displayed = null;
            if (Definition != null) displayed = Definition.requiredResource;
            if (displayed == null && Services.TryGet<GridSystem>(out var grid)) displayed = grid.GetResourceAt(Cell);
            if (displayed != null)
            {
                iconRenderer.color = displayed.color;
                if (displayed.icon != null) iconRenderer.sprite = displayed.icon;
            }
        }

        private void Update()
        {
            if (!Services.TryGet<GameStateManager>(out var gsm) || !gsm.IsPlaying) return;
            if (Definition == null) return;
            if (!Services.TryGet<GridSystem>(out var grid)) return;

            var deposit = Definition.requiredResource != null ? Definition.requiredResource : grid.GetResourceAt(Cell);
            if (deposit == null) return;
            // Deposits (BaseRes*) redirect to their configured Item_*; non-deposits use themselves.
            var resourceUnder = deposit.ExtractedItem;

            float interval = Mathf.Max(0.05f, Definition.extractInterval);
            accumulator += Time.deltaTime;
            if (accumulator < interval) return;

            if (!Services.TryGet<ConveyorNetwork>(out var network)) return;
            if (!Services.TryGet<ConveyorSimulation>(out var sim)) return;

            // Round-robin search across 4 sides. Picks first conveyor that exists and is empty.
            for (int i = 0; i < AllDirs.Length; i++)
            {
                var dir = AllDirs[(outputCursor + i) % AllDirs.Length];
                var targetCell = Cell + dir.ToVector();
                var conveyor = network.GetAt(targetCell);
                if (conveyor == null || conveyor.HasItem) continue;

                // If the conveyor's flow points BACK into the extractor cell, it's part of an
                // unrelated chain that delivers items here — pushing onto it would send our
                // output backwards through that chain. Skip and try a different side.
                var emitInDir = dir.Opposite();
                if (conveyor.OutDir == emitInDir) continue;

                accumulator = 0f;
                outputCursor = (outputCursor + i + 1) % AllDirs.Length;
                var visual = sim.AcquireVisual(resourceUnder, grid.CellToWorld(targetCell));
                if (conveyor.TryPlaceItem(resourceUnder, visual))
                {
                    // Only mutate the receiving conveyor's flow after a successful hand-off.
                    conveyor.SetInDir(emitInDir);
                }
                else
                {
                    sim.ReleaseVisual(visual);
                }
                return;
            }
            // No conveyor accepted item this tick — keep the accumulator pinned so we can output as soon as possible.
            accumulator = interval;
        }
    }
}
