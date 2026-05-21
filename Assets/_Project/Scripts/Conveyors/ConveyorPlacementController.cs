using System;
using System.Collections.Generic;
using UnityEngine;
using OceanFactory.Buildings;
using OceanFactory.Core;
using OceanFactory.Data;
using OceanFactory.Grid;
using OceanFactory.Input;
using OceanFactory.Utils;

namespace OceanFactory.Conveyors
{
    public class ConveyorPlacementController : MonoBehaviour
    {
        [SerializeField] private BuildingCatalogSO catalog;
        [SerializeField] private Transform conveyorsRoot;
        [SerializeField] private SpriteRenderer ghostRenderer;
        [SerializeField] private Sprite ghostFallbackSprite;
        [SerializeField] private Color allowedColor = new Color(0.4f, 0.9f, 1f, 0.5f);
        [SerializeField] private Color deniedColor  = new Color(1f, 0.4f, 0.4f, 0.5f);

        private InputReader      input;
        private BuildModeService modeService;
        private GridSystem       grid;
        private ConveyorNetwork  network;
        private Direction        facing = Direction.North;
        private bool             isDragging;
        private Vector2Int       lastCell;
        private bool             needsInputBind;

        // Cells placed during the current drag. Only these cells can have their outDir auto-rewritten;
        // pre-existing conveyors are left alone so items in flight don't get redirected mid-flight.
        private readonly HashSet<Vector2Int> placedThisDrag = new();

        private void OnEnable()
        {
            if (ghostRenderer != null) ghostRenderer.enabled = false;
            if (Services.TryGet(out input))
            {
                input.OnPointerPrimaryDown += HandlePrimaryDown;
                input.OnCancel             += HandleCancel;
                input.OnRotatePressed      += HandleRotate;
            }
            else
            {
                needsInputBind = true;
            }
        }

        private void OnDisable()
        {
            needsInputBind = false;
            isDragging = false;
            placedThisDrag.Clear();
            if (input != null)
            {
                input.OnPointerPrimaryDown -= HandlePrimaryDown;
                input.OnCancel             -= HandleCancel;
                input.OnRotatePressed      -= HandleRotate;
            }
            if (ghostRenderer != null) ghostRenderer.enabled = false;
        }

        private void Update()
        {
            if (needsInputBind && Services.TryGet(out input))
            {
                input.OnPointerPrimaryDown += HandlePrimaryDown;
                input.OnCancel             += HandleCancel;
                input.OnRotatePressed      += HandleRotate;
                needsInputBind = false;
            }
            if (modeService == null) Services.TryGet(out modeService);
            if (grid        == null) Services.TryGet(out grid);
            if (network     == null) Services.TryGet(out network);

            if (input == null || modeService == null || grid == null)
            {
                isDragging = false;
                if (ghostRenderer != null) ghostRenderer.enabled = false;
                return;
            }

            if (modeService.Current != BuildMode.Conveyor)
            {
                isDragging = false;
                placedThisDrag.Clear();
                if (ghostRenderer != null) ghostRenderer.enabled = false;
                return;
            }

            if (PointerUtility.IsOverUI())
            {
                if (ghostRenderer != null) ghostRenderer.enabled = false;
                return;
            }

            UpdateGhost();

            if (!input.PointerPrimaryHeld)
            {
                if (isDragging) placedThisDrag.Clear();
                isDragging = false;
                return;
            }
            if (!isDragging) return;

            var currentCell = grid.WorldToCell(input.PointerWorld);
            if (!grid.IsInside(currentCell)) return;
            if (currentCell == lastCell) return;

            var path = StepLine(lastCell, currentCell);
            for (int i = 1; i < path.Count; i++)
            {
                var prev = path[i - 1];
                var cur  = path[i];
                var dir  = VectorToDirection(cur - prev);

                // Hard stop on building cells — never bridge or auto-rewrite through them.
                if (grid.GetCellType(cur) == CellType.Building) break;

                // Only redirect conveyors that were created during this drag.
                // Existing conveyors keep their direction, so any items mid-flight aren't yanked sideways.
                if (placedThisDrag.Contains(prev))
                {
                    UpdateOutDir(prev, dir);
                }

                if (PlaceConveyor(cur, dir))
                {
                    placedThisDrag.Add(cur);
                }
            }
            lastCell = currentCell;
        }

        // Returns any *other* conveyor whose OutDir points into `cell`. Used to enforce the
        // "1 input per conveyor" rule by rejecting placements/redirects that would create a merge.
        private ConveyorComponent FindAnyUpstream(Vector2Int cell, ConveyorComponent ignore)
        {
            if (network == null) return null;
            foreach (var kvp in network.All)
            {
                var c = kvp.Value;
                if (c == null || c == ignore) continue;
                if (c.Cell + c.OutDir.ToVector() == cell) return c;
            }
            return null;
        }

        private void UpdateGhost()
        {
            if (ghostRenderer == null) return;
            var cell = grid.WorldToCell(input.PointerWorld);
            if (!grid.IsInside(cell)) { ghostRenderer.enabled = false; return; }

            // Don't preview a conveyor over an already-built object.
            if (grid.GetCellType(cell) != CellType.Empty)
            {
                ghostRenderer.enabled = false;
                return;
            }

            ghostRenderer.enabled = true;
            ghostRenderer.transform.position = grid.CellToWorld(cell);
            ghostRenderer.transform.localScale = new Vector3(0.135f, 0.135f, 1f);
            ghostRenderer.color = allowedColor;

            ResolveGhostFlow(cell, out var inDir, out var outDir);
            var straight = catalog != null && catalog.conveyor != null ? catalog.conveyor.bodySprite : null;
            var turn = ghostFallbackSprite;
            if (catalog?.conveyor?.prefab != null)
            {
                var prefabComp = catalog.conveyor.prefab.GetComponent<ConveyorComponent>();
                if (prefabComp != null)
                {
                    if (prefabComp.StraightSprite != null) straight = prefabComp.StraightSprite;
                    if (prefabComp.TurnSprite != null) turn = prefabComp.TurnSprite;
                }
            }
            ConveyorComponent.ApplyVisual(ghostRenderer, ghostRenderer.transform, inDir, outDir, straight, turn);
        }

        private void ResolveGhostFlow(Vector2Int cell, out Direction inDir, out Direction outDir)
        {
            outDir = facing;
            inDir = facing.Opposite();

            var upstream = FindAnyUpstream(cell, ignore: null);
            if (upstream != null)
            {
                inDir = upstream.OutDir.Opposite();
                return;
            }

            if (!isDragging || cell == lastCell) return;
            var step = cell - lastCell;
            if (Mathf.Abs(step.x) + Mathf.Abs(step.y) != 1) return;
            inDir = VectorToDirection(step).Opposite();
            outDir = VectorToDirection(step);
        }

        private void HandlePrimaryDown()
        {
            if (modeService == null) Services.TryGet(out modeService);
            if (grid        == null) Services.TryGet(out grid);
            if (network     == null) Services.TryGet(out network);

            if (modeService == null || modeService.Current != BuildMode.Conveyor) return;
            if (PointerUtility.IsOverUI()) return;
            if (!Services.TryGet<GameStateManager>(out var gsm) || !gsm.IsPlaying) return;
            if (grid == null || input == null) return;

            var cell = grid.WorldToCell(input.PointerWorld);
            if (!grid.IsInside(cell)) return;
            placedThisDrag.Clear();
            if (PlaceConveyor(cell, facing))
            {
                placedThisDrag.Add(cell);
            }
            lastCell   = cell;
            isDragging = true;
        }

        private void HandleCancel()
        {
            if (modeService == null) Services.TryGet(out modeService);
            modeService?.SetMode(BuildMode.None);
            isDragging = false;
            placedThisDrag.Clear();
        }

        private void HandleRotate() => facing = facing.RotateCW();

        private bool PlaceConveyor(Vector2Int cell, Direction outDir)
        {
            if (catalog == null || catalog.conveyor == null || catalog.conveyor.prefab == null) return false;
            if (grid == null || !grid.IsInside(cell)) return false;
            if (grid.GetCellType(cell) != CellType.Empty) return false;

            var target = cell + outDir.ToVector();
            if (network != null)
            {
                var targetConveyor = network.GetAt(target);
                if (targetConveyor != null)
                {
                    // Ping-pong guard: refuse if the target belt points back at us.
                    if (targetConveyor.OutDir == outDir.Opposite()) return false;
                    // Merge guard #1: target already has an upstream — we'd be a 2nd input.
                    if (FindAnyUpstream(target, ignore: null) != null) return false;
                }
            }

            var root = conveyorsRoot != null ? conveyorsRoot : transform;
            var go   = Instantiate(catalog.conveyor.prefab, grid.CellToWorld(cell), Quaternion.identity, root);
            var comp = go.GetComponent<ConveyorComponent>();
            if (comp == null)
            {
                Debug.LogError("ConveyorPlacement: prefab missing ConveyorComponent");
                Destroy(go);
                return false;
            }
            comp.Configure(cell, outDir);
            grid.TryOccupyForConveyor(cell);
            network?.Register(comp);

            // Pick the visual InDir from whichever existing conveyor (if any) points at us.
            // No upstream conveyor -> stay straight (in = opposite of out) until something connects.
            var upstream = FindAnyUpstream(cell, ignore: comp);
            if (upstream != null) comp.SetInDir(upstream.OutDir.Opposite());

            EventBus.Publish(new ConveyorPlacedEvent(cell));
            return true;
        }

        private void UpdateOutDir(Vector2Int cell, Direction newOutDir)
        {
            if (network == null) return;
            var c = network.GetAt(cell);
            if (c == null) return;
            if (c.OutDir == newOutDir) return;

            var newTarget = cell + newOutDir.ToVector();
            var targetConveyor = network.GetAt(newTarget);

            // Ping-pong guard: refuse to point at a belt that points back at us.
            if (targetConveyor != null && targetConveyor.OutDir == newOutDir.Opposite()) return;

            // Merge guard #2: refuse to redirect into a conveyor that already has a *different*
            // upstream than us. Without this, drag-bending could quietly fork two streams together.
            if (targetConveyor != null)
            {
                var existingUpstream = FindAnyUpstream(newTarget, ignore: c);
                if (existingUpstream != null) return;
            }

            var prevOut = c.OutDir;
            var upstreamOfC = FindAnyUpstream(cell, ignore: c);
            if (upstreamOfC != null)
            {
                c.SetInDir(upstreamOfC.OutDir.Opposite());
            }
            else if (prevOut != newOutDir)
            {
                // Bend with no external upstream: items were arriving from behind our old output.
                c.SetInDir(prevOut.Opposite());
            }

            c.SetOutDir(newOutDir);
            if (targetConveyor != null) targetConveyor.SetInDir(newOutDir.Opposite());
        }

        private static List<Vector2Int> StepLine(Vector2Int from, Vector2Int to)
        {
            var result = new List<Vector2Int> { from };
            int dx = Math.Sign(to.x - from.x);
            int dy = Math.Sign(to.y - from.y);
            var cur = from;
            while (cur.x != to.x) { cur.x += dx; result.Add(cur); }
            while (cur.y != to.y) { cur.y += dy; result.Add(cur); }
            return result;
        }

        private static Direction VectorToDirection(Vector2Int v)
        {
            if (v.x > 0) return Direction.East;
            if (v.x < 0) return Direction.West;
            if (v.y > 0) return Direction.North;
            return Direction.South;
        }
    }
}
