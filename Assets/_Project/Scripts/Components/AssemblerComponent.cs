using UnityEngine;
using OceanFactory.Conveyors;
using OceanFactory.Core;
using OceanFactory.Data;
using OceanFactory.Grid;

namespace OceanFactory.Components
{
    public class AssemblerComponent : BuildingComponent, IItemSink
    {
        [SerializeField] private RecipeBookSO recipeBook;
        [SerializeField] private SpriteRenderer statusRenderer;
        [SerializeField] private SpriteRenderer outputIconRenderer;
        [SerializeField] private RecipeSO selectedRecipe;

        [Header("Diagnostics")]
        [SerializeField, Tooltip("Print state transitions (item in/out, craft start/complete, blocked output) to console.")]
        private bool verboseDiagnostics = true;

        private ItemTypeSO slotA;
        private ItemTypeSO slotB;
        private bool crafting;
        private float craftTimer;
        private ItemTypeSO outputBuffer;
        private int outputCursor;
        private float lastNoRecipeLog = -10f;
        private float lastWrongItemLog = -10f;

        // 4 cardinal dirs used for permissive output scanning.
        private static readonly Direction[] AllDirs =
        {
            Direction.North, Direction.East, Direction.South, Direction.West
        };

        public RecipeSO SelectedRecipe => selectedRecipe;
        public RecipeBookSO RecipeBook => recipeBook;

        public void SetRecipe(RecipeSO recipe)
        {
            var previous = selectedRecipe;
            selectedRecipe = recipe;
            // Strictly type the slots to the new recipe. slotA must hold inputA, slotB must hold
            // inputB — anything else is junk (possibly from a prior recipe or a prior buggy state)
            // and gets dropped so future crafts can validate cleanly.
            if (selectedRecipe != null)
            {
                if (slotA != null && slotA != selectedRecipe.inputA) slotA = null;
                if (slotB != null && slotB != selectedRecipe.inputB) slotB = null;
                if (outputBuffer != null && outputBuffer != selectedRecipe.output) outputBuffer = null;
            }
            else
            {
                slotA = null;
                slotB = null;
                outputBuffer = null;
            }
            crafting = false;
            craftTimer = 0f;
            RefreshOutputIcon();
            if (verboseDiagnostics)
            {
                string from = previous != null ? previous.name : "<none>";
                string to   = selectedRecipe != null ? selectedRecipe.name : "<none>";
                Debug.Log($"[ASM @{Cell}] recipe '{from}' -> '{to}'. slotA={Name(slotA)} slotB={Name(slotB)} output={Name(outputBuffer)}");
            }
        }

        public struct Ports
        {
            public Vector2Int InputA;
            public Vector2Int InputB;
            public Vector2Int OutputCell;
            public Vector2Int OutputTargetCell;
        }

        // PERMISSIVE INPUT: items can enter ANY cell of the footprint. The recipe filter already
        // gates by item type, so accepting from any side just lets the player wire belts however
        // makes sense for their layout.
        public bool TryAcceptItem(ItemTypeSO item, Vector2Int intoCell, Direction fromDir)
        {
            if (item == null) return false;
            if (selectedRecipe == null)
            {
                // Throttled high-visibility warning: items are clearly arriving but nothing was picked.
                if (Time.unscaledTime - lastNoRecipeLog > 1f)
                {
                    lastNoRecipeLog = Time.unscaledTime;
                    Debug.LogWarning($"[ASM @{Cell}] STUCK: items are arriving but NO RECIPE IS SELECTED. " +
                                     $"Click this assembler to pick a recipe.");
                }
                return false;
            }
            if (!IsInsideFootprint(intoCell))
            {
                if (verboseDiagnostics) Debug.LogWarning($"[ASM @{Cell}] reject {item.name}: intoCell {intoCell} outside footprint {Cell}+{Size}");
                return false;
            }

            bool isA = item == selectedRecipe.inputA;
            bool isB = item == selectedRecipe.inputB;
            if (!isA && !isB)
            {
                // Throttled — items pile up by the dozens otherwise.
                if (Time.unscaledTime - lastWrongItemLog > 1f)
                {
                    lastWrongItemLog = Time.unscaledTime;
                    Debug.LogWarning($"[ASM @{Cell}] WRONG ITEM: got '{item.name}' but recipe '{selectedRecipe.name}' needs " +
                                     $"'{selectedRecipe.inputA.name}' + '{selectedRecipe.inputB.name}'. " +
                                     $"Check what your extractors are producing, or pick a different recipe.");
                }
                return false;
            }

            // For recipes with two DIFFERENT inputs (e.g. Ruby Shard + Iron Ingot), slotA is
            // strictly reserved for inputA, slotB strictly for inputB. Without this, two of the
            // same item arriving back-to-back would fall through to "fill whatever's empty" and
            // jam the assembler with duplicates that no craft check can validate.
            // For recipes with IDENTICAL inputs (Iron Ore + Iron Ore), either slot accepts the item.
            bool identicalInputs = selectedRecipe.inputA == selectedRecipe.inputB;
            if (identicalInputs)
            {
                if (slotA == null) { slotA = item; LogAccept(item, intoCell, "A"); return true; }
                if (slotB == null) { slotB = item; LogAccept(item, intoCell, "B"); return true; }
                if (verboseDiagnostics) Debug.Log($"[ASM @{Cell}] both slots full ({Name(slotA)}/{Name(slotB)}) — back-pressuring conveyor");
                return false;
            }

            if (isA)
            {
                if (slotA == null) { slotA = item; LogAccept(item, intoCell, "A"); return true; }
                if (verboseDiagnostics) Debug.Log($"[ASM @{Cell}] slot A already holds {Name(slotA)} — back-pressuring (waiting for {selectedRecipe.inputB.name})");
                return false;
            }
            // isB == true
            if (slotB == null) { slotB = item; LogAccept(item, intoCell, "B"); return true; }
            if (verboseDiagnostics) Debug.Log($"[ASM @{Cell}] slot B already holds {Name(slotB)} — back-pressuring (waiting for {selectedRecipe.inputA.name})");
            return false;
        }

        public override void OnPlaced()
        {
            base.OnPlaced();
            RefreshOutputIcon();
            if (verboseDiagnostics)
            {
                var ports = ComputePorts();
                Debug.Log($"[ASM @{Cell}] placed, facing={Facing}, footprint={Size}, primary output -> {ports.OutputTargetCell}. " +
                          "Permissive mode: any conveyor adjacent to any footprint cell can receive output, items can enter from any side.");
            }
        }

        private void Update()
        {
            if (!Services.TryGet<GameStateManager>(out var gsm) || !gsm.IsPlaying) return;

            if (crafting && selectedRecipe != null)
            {
                craftTimer += Time.deltaTime;
                if (craftTimer >= selectedRecipe.craftTime)
                {
                    outputBuffer = selectedRecipe.output;
                    crafting = false;
                    craftTimer = 0f;
                    if (verboseDiagnostics) Debug.Log($"[ASM @{Cell}] craft complete -> outputBuffer={Name(outputBuffer)}");
                }
            }

            if (outputBuffer != null)
            {
                TryEmitOutput();
            }

            if (!crafting && outputBuffer == null && selectedRecipe != null && slotA != null && slotB != null)
            {
                bool haveA = slotA == selectedRecipe.inputA || slotB == selectedRecipe.inputA;
                bool haveB = slotA == selectedRecipe.inputB || slotB == selectedRecipe.inputB;
                if (haveA && haveB)
                {
                    crafting = true;
                    craftTimer = 0f;
                    slotA = null;
                    slotB = null;
                    if (verboseDiagnostics) Debug.Log($"[ASM @{Cell}] craft start: {selectedRecipe.name} ({selectedRecipe.craftTime:0.#}s)");
                }
            }

            UpdateStatusVisual();
        }

        // PERMISSIVE OUTPUT: pushes onto any empty conveyor adjacent to ANY footprint cell.
        // Preferred port (ComputePorts.OutputTargetCell) is tried first so single-output recipes
        // keep their visually-intended exit when available. Round-robin keeps things fair otherwise.
        private void TryEmitOutput()
        {
            if (!Services.TryGet<ConveyorNetwork>(out var network)) return;
            if (!Services.TryGet<ConveyorSimulation>(out var sim)) return;
            if (!Services.TryGet<GridSystem>(out var grid)) return;

            var ports = ComputePorts();

            if (TryEmitTo(ports.OutputTargetCell, network, sim, grid)) return;

            var size = Size;
            int totalSides = size.x * 2 + size.y * 2;
            for (int i = 0; i < totalSides; i++)
            {
                int idx = (outputCursor + i) % totalSides;
                var target = PerimeterCell(idx, size);
                if (target == ports.OutputTargetCell) continue; // already tried
                if (TryEmitTo(target, network, sim, grid))
                {
                    outputCursor = (idx + 1) % totalSides;
                    return;
                }
            }

            if (verboseDiagnostics)
            {
                // Only log this once a second or so to avoid spam — gate by a coarse timer.
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning($"[ASM @{Cell}] output {Name(outputBuffer)} STUCK — no empty conveyor on any of the {totalSides} adjacent cells. " +
                                     $"Place a conveyor adjacent to the assembler (any side).");
                }
            }
        }

        private bool TryEmitTo(Vector2Int target, ConveyorNetwork network, ConveyorSimulation sim, GridSystem grid)
        {
            if (!grid.IsInside(target)) return false;
            var conveyor = network.GetAt(target);
            if (conveyor == null) return false;
            if (conveyor.HasItem) return false;

            // Direction items would enter this conveyor from if we pushed onto it (from the
            // assembler side back into the conveyor cell).
            var emitInDir = SideFromFootprintTo(target);

            // If this conveyor is feeding us — its flow points back into our footprint — it is
            // an INPUT belt. Never push outputs onto it: when it momentarily empties between
            // hand-offs, we would otherwise dump an output item backwards into the input chain
            // (e.g. iron ingots appearing on the iron-ore conveyors that feed the assembler).
            if (conveyor.OutDir == emitInDir)
            {
                if (verboseDiagnostics && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[ASM @{Cell}] skip emit to conveyor @ {target}: it feeds us (OutDir={conveyor.OutDir})");
                }
                return false;
            }

            var visual = sim.AcquireVisual(outputBuffer, grid.CellToWorld(target));
            if (conveyor.TryPlaceItem(outputBuffer, visual))
            {
                // Only mutate the receiving conveyor's flow AFTER a successful hand-off, so
                // failed attempts don't leave belts with wrong sprites.
                conveyor.SetInDir(emitInDir);
                if (verboseDiagnostics) Debug.Log($"[ASM @{Cell}] emitted {Name(outputBuffer)} -> conveyor @ {target}");
                outputBuffer = null;
                return true;
            }
            sim.ReleaseVisual(visual);
            return false;
        }

        // For a target cell sitting just outside our footprint, returns the direction back into
        // the footprint — i.e., "items enter the conveyor from this side".
        private Direction SideFromFootprintTo(Vector2Int target)
        {
            var s = Size;
            if (target.y < Cell.y)           return Direction.North; // target is south of us; items come from the north
            if (target.y >= Cell.y + s.y)    return Direction.South;
            if (target.x < Cell.x)           return Direction.East;
            if (target.x >= Cell.x + s.x)    return Direction.West;
            return Direction.South; // shouldn't happen — target was supposed to be on the perimeter
        }

        // Translate a perimeter index 0..(2*sx + 2*sy)-1 to a world cell just outside the footprint.
        // Walks bottom edge (south), right edge (east), top edge (north), left edge (west).
        private Vector2Int PerimeterCell(int index, Vector2Int size)
        {
            int sx = size.x, sy = size.y;
            int bottom = sx, right = sy, top = sx, left = sy;
            int i = index;
            if (i < bottom)       return new Vector2Int(Cell.x + i,                Cell.y - 1);
            i -= bottom;
            if (i < right)        return new Vector2Int(Cell.x + sx,               Cell.y + i);
            i -= right;
            if (i < top)          return new Vector2Int(Cell.x + (sx - 1 - i),     Cell.y + sy);
            i -= top;
            /* left */            return new Vector2Int(Cell.x - 1,                Cell.y + (sy - 1 - i));
        }

        private bool IsInsideFootprint(Vector2Int cell)
        {
            var s = Size;
            return cell.x >= Cell.x && cell.x < Cell.x + s.x &&
                   cell.y >= Cell.y && cell.y < Cell.y + s.y;
        }

        // Three signal lights:
        //   GREEN  = working (currently crafting an item)
        //   YELLOW = awaiting (recipe set, waiting for inputs or for the output conveyor to free up)
        //   RED    = problem (no recipe chosen — the assembler does nothing)
        private static readonly Color StatusGreen  = new Color(0.35f, 0.95f, 0.40f, 1f);
        private static readonly Color StatusYellow = new Color(1.00f, 0.85f, 0.20f, 1f);
        private static readonly Color StatusRed    = new Color(0.95f, 0.30f, 0.30f, 1f);

        private void UpdateStatusVisual()
        {
            if (statusRenderer == null) return;
            if (selectedRecipe == null)        statusRenderer.color = StatusRed;
            else if (crafting)                 statusRenderer.color = StatusGreen;
            else                               statusRenderer.color = StatusYellow;
        }

        private void RefreshOutputIcon()
        {
            if (outputIconRenderer == null) return;
            if (selectedRecipe == null || selectedRecipe.output == null)
            {
                outputIconRenderer.enabled = false;
                return;
            }
            outputIconRenderer.enabled = true;
            outputIconRenderer.color = selectedRecipe.output.color;
            var outputSprite = selectedRecipe.output.ItemOrFallback;
            if (outputSprite != null) outputIconRenderer.sprite = outputSprite;
        }

        private void LogAccept(ItemTypeSO item, Vector2Int intoCell, string slot)
        {
            if (!verboseDiagnostics) return;
            Debug.Log($"[ASM @{Cell}] accepted {item.name} into slot {slot} via {intoCell}. slots=({Name(slotA)}, {Name(slotB)})");
        }

        private static string Name(ItemTypeSO s) => s != null ? s.name : "<empty>";

        private Ports ComputePorts() => ComputePorts(Cell, Facing);

        public static Ports ComputePortsFor(Vector2Int origin, Direction facing) => ComputePorts(origin, facing);

        private static Ports ComputePorts(Vector2Int origin, Direction facing)
        {
            Vector2Int p00 = origin;
            Vector2Int p10 = origin + new Vector2Int(1, 0);
            Vector2Int p01 = origin + new Vector2Int(0, 1);
            Vector2Int p11 = origin + new Vector2Int(1, 1);
            Vector2Int fwd = facing.ToVector();

            switch (facing)
            {
                case Direction.North:
                    return new Ports { InputA = p00, InputB = p10, OutputCell = p01, OutputTargetCell = p01 + fwd };
                case Direction.East:
                    return new Ports { InputA = p00, InputB = p01, OutputCell = p11, OutputTargetCell = p11 + fwd };
                case Direction.South:
                    return new Ports { InputA = p01, InputB = p11, OutputCell = p10, OutputTargetCell = p10 + fwd };
                case Direction.West:
                    return new Ports { InputA = p10, InputB = p11, OutputCell = p00, OutputTargetCell = p00 + fwd };
                default:
                    return new Ports { InputA = p00, InputB = p10, OutputCell = p01, OutputTargetCell = p01 + fwd };
            }
        }
    }
}
