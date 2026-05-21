using UnityEngine;
using OceanFactory.Components;
using OceanFactory.Conveyors;
using OceanFactory.Core;
using OceanFactory.Data;
using OceanFactory.Grid;
using OceanFactory.Input;
using OceanFactory.UI;
using OceanFactory.Utils;

namespace OceanFactory.Buildings
{
    public class BuildPlacementController : MonoBehaviour
    {
        [SerializeField] private BuildingCatalogSO catalog;
        [Header("Ghosts")]
        [SerializeField] private SpriteRenderer extractorGhostRenderer;
        [SerializeField] private SpriteRenderer assemblerGhostRenderer;
        [SerializeField] private SpriteRenderer removeGhostRenderer;
        [Header("Fallback")]
        [SerializeField] private SpriteRenderer ghostRenderer;
        [SerializeField] private Sprite ghostFallbackSprite;
        [SerializeField] private Color allowedColor = new Color(0.4f, 1f, 0.6f, 0.45f);
        [SerializeField] private Color deniedColor  = new Color(1f, 0.4f, 0.4f, 0.45f);
        [SerializeField] private bool verboseRejects = true;

        private Direction facing = Direction.North;

        // Cached service refs — resolved lazily in Update/handlers.
        private InputReader      input;
        private BuildModeService modeService;
        private GridSystem       grid;
        private BuildingFactory  factory;
        private ConveyorNetwork  network;
        private ConveyorSimulation sim;

        private void OnEnable()
        {
            HideAllGhosts();
            // Bind directly — no coroutine. InputReader must be in scene and register in Awake.
            if (Services.TryGet(out input))
            {
                input.OnPointerPrimaryDown += HandlePrimaryDown;
                input.OnCancel             += HandleCancel;
                input.OnRotatePressed      += HandleRotate;
            }
            else
            {
                // Fall back: retry next frame via Update flag.
                needsInputBind = true;
            }
        }

        private bool needsInputBind;

        private void OnDisable()
        {
            needsInputBind = false;
            if (input != null)
            {
                input.OnPointerPrimaryDown -= HandlePrimaryDown;
                input.OnCancel             -= HandleCancel;
                input.OnRotatePressed      -= HandleRotate;
            }
            HideAllGhosts();
        }

        private void Update()
        {
            // Lazy bind input if it wasn't available in OnEnable.
            if (needsInputBind && Services.TryGet(out input))
            {
                input.OnPointerPrimaryDown += HandlePrimaryDown;
                input.OnCancel             += HandleCancel;
                input.OnRotatePressed      += HandleRotate;
                needsInputBind = false;
            }

            // Lazy-resolve remaining services.
            if (modeService == null) Services.TryGet(out modeService);
            if (grid       == null) Services.TryGet(out grid);
            if (factory    == null) Services.TryGet(out factory);
            if (network    == null) Services.TryGet(out network);
            if (sim        == null) Services.TryGet(out sim);

            UpdateGhost();
        }

        private void UpdateGhost()
        {
            if (input == null || modeService == null || grid == null)
            {
                HideAllGhosts();
                return;
            }

            var activeGhost = ResolveGhostRenderer(modeService.Current);
            if (activeGhost == null)
            {
                HideAllGhosts();
                return;
            }

            if (!IsPlacementMode(modeService.Current))
            {
                HideAllGhosts();
                return;
            }
            if (PointerUtility.IsOverUI())
            {
                HideAllGhosts();
                return;
            }

            var cell = grid.WorldToCell(input.PointerWorld);
            if (!grid.IsInside(cell))
            {
                HideAllGhosts();
                return;
            }

            HideAllGhostsExcept(activeGhost);

            if (modeService.Current == BuildMode.Remove)
            {
                activeGhost.enabled = true;
                activeGhost.transform.position = grid.CellToWorld(cell);
                activeGhost.transform.localScale = Vector3.one;
                activeGhost.transform.rotation = Quaternion.identity;
                activeGhost.sprite = ghostFallbackSprite;
                var ct = grid.GetCellType(cell);
                activeGhost.color = (ct == CellType.Building || ct == CellType.Conveyor) ? allowedColor : deniedColor;
                return;
            }

            var def = ResolveDef(modeService.Current);
            if (def == null || def.prefab == null)
            {
                HideAllGhosts();
                return;
            }

            var size = def.size.x > 0 && def.size.y > 0 ? def.size : Vector2Int.one;

            // Hide ghost entirely if the cell rect is already occupied — no preview over existing objects.
            if (!grid.IsRectEmpty(cell, size))
            {
                HideAllGhosts();
                return;
            }

            Vector3 worldPos = grid.CellToWorld(cell);
            worldPos.x += (size.x - 1) * 0.5f;
            worldPos.y += (size.y - 1) * 0.5f;
            activeGhost.enabled = true;
            activeGhost.transform.position = worldPos;
            // Match the actual prefab scale so the ghost occupies exactly the same footprint.
            // Formula: finalScale = size * spriteScale  →  spriteScale = prefabScale / size
            // Extractor prefab scale 0.25, size 1x1 → spriteScale = 0.25 / 1 = 0.25
            // Assembler prefab scale 0.20, size 2x2 → spriteScale = 0.20 / 2 = 0.10
            float spriteScale = modeService.Current == BuildMode.Assembler ? 0.10f : 0.25f;
            activeGhost.transform.localScale = new Vector3(size.x * spriteScale, size.y * spriteScale, 1f);
            activeGhost.transform.rotation = Quaternion.Euler(0f, 0f, facing.ToZAngleDeg());

            // Dedicated ghost renderers (extractor/assembler) already have their sprite set in the
            // Inspector — don't overwrite it. Only assign a sprite when using the generic fallback.
            bool usingFallback = activeGhost == ghostRenderer;
            if (usingFallback)
                activeGhost.sprite = def.bodySprite != null ? def.bodySprite : ghostFallbackSprite;

            bool canPlace = true;
            if (def.kind == BuildingKind.Extractor)
                canPlace = def.requiredResource == null ? grid.HasResource(cell) : grid.GetResourceAt(cell) == def.requiredResource;
            activeGhost.color = canPlace ? allowedColor : deniedColor;
        }

        private SpriteRenderer ResolveGhostRenderer(BuildMode mode)
        {
            return mode switch
            {
                BuildMode.Extractor => extractorGhostRenderer != null ? extractorGhostRenderer : ghostRenderer,
                BuildMode.Assembler => assemblerGhostRenderer != null ? assemblerGhostRenderer : ghostRenderer,
                BuildMode.Remove => removeGhostRenderer != null ? removeGhostRenderer : ghostRenderer,
                _ => ghostRenderer
            };
        }

        private void HideAllGhosts() => HideAllGhostsExcept(null);

        private void HideAllGhostsExcept(SpriteRenderer active)
        {
            DisableIfInactive(extractorGhostRenderer, active);
            DisableIfInactive(assemblerGhostRenderer, active);
            DisableIfInactive(removeGhostRenderer, active);
            DisableIfInactive(ghostRenderer, active);
        }

        private static void DisableIfInactive(SpriteRenderer renderer, SpriteRenderer active)
        {
            if (renderer != null && renderer != active) renderer.enabled = false;
        }

        private void HandlePrimaryDown()
        {
            if (modeService == null) Services.TryGet(out modeService);
            if (grid        == null) Services.TryGet(out grid);
            if (factory     == null) Services.TryGet(out factory);
            if (network     == null) Services.TryGet(out network);
            if (sim         == null) Services.TryGet(out sim);

            if (modeService == null || grid == null)
            {
                if (verboseRejects) Debug.LogWarning("[BuildPlacement] click ignored: services not bound yet (modeService or grid null)");
                return;
            }
            if (PointerUtility.IsOverUI())
            {
                if (verboseRejects) Debug.LogWarning("[BuildPlacement] click ignored: pointer over UI — disable Raycast Target on background Images in Canvas");
                return;
            }
            if (!Services.TryGet<GameStateManager>(out var gsm) || !gsm.IsPlaying)
            {
                if (verboseRejects) Debug.LogWarning("[BuildPlacement] click ignored: GameState is not Playing");
                return;
            }

            var mode = modeService.Current;
            if (input == null) return;
            var cell = grid.WorldToCell(input.PointerWorld);
            if (!grid.IsInside(cell))
            {
                if (verboseRejects) Debug.LogWarning($"[BuildPlacement] click ignored: cell {cell} outside grid");
                return;
            }

            var hitType = grid.GetCellType(cell);
            var hitBuilding = hitType == CellType.Building ? grid.GetBuildingAt(cell) : null;
            if (verboseRejects)
                Debug.Log($"[BuildPlacement] click @ cell={cell} mode={mode} cellType={hitType} building={(hitBuilding != null ? hitBuilding.GetType().Name : "null")}");

            // Clicking an existing assembler is always interaction, except in Remove mode.
            if (mode != BuildMode.Remove && hitBuilding is AssemblerComponent asm)
            {
                if (verboseRejects) Debug.Log("[BuildPlacement] -> opening recipe picker");
                RecipePickerController.OpenFor(asm);
                return;
            }

            // None mode = interaction. Other cells do nothing.
            if (mode == BuildMode.None)
            {
                if (verboseRejects) Debug.LogWarning("[BuildPlacement] click ignored: mode is None — press 1/2/3/4 or click a build button. Click an assembler to pick its recipe.");
                return;
            }

            if (!IsPlacementMode(mode))
            {
                if (verboseRejects) Debug.LogWarning($"[BuildPlacement] click ignored: mode is {mode} — press 1/2/3/4 or click a build button first");
                return;
            }

            if (mode == BuildMode.Remove) { TryRemoveAt(cell); return; }

            var def = ResolveDef(mode);
            if (def == null)
            {
                if (verboseRejects) Debug.LogWarning($"[BuildPlacement] click ignored: no definition for {mode} in BuildingCatalog");
                return;
            }
            var spawned = factory?.Spawn(def, cell, facing);

            // Assembler is useless until a recipe is picked. Open the picker right after placement
            // so the player can't forget — this is the #1 reason items pile up on conveyors.
            if (spawned is AssemblerComponent asmJustPlaced)
            {
                RecipePickerController.OpenFor(asmJustPlaced);
            }
        }

        private void HandleCancel()
        {
            if (modeService == null) Services.TryGet(out modeService);
            modeService?.SetMode(BuildMode.None);
        }

        private void HandleRotate() => facing = facing.RotateCW();

        private void TryRemoveAt(Vector2Int cell)
        {
            if (grid == null) return;
            var ct = grid.GetCellType(cell);
            if (ct == CellType.Building)
            {
                var bldg = grid.GetBuildingAt(cell);
                if (bldg is HubComponent)
                {
                    if (verboseRejects) Debug.LogWarning("[BuildPlacement] cannot remove the main Hub");
                    return;
                }
                if (bldg != null) factory?.Despawn(bldg);
            }
            else if (ct == CellType.Conveyor)
            {
                if (network == null || sim == null) return;
                var c = network.GetAt(cell);
                if (c != null)
                {
                    if (c.HasItem && c.Visual != null) sim.ReleaseVisual(c.Visual);
                    c.ClearItem();
                    network.Unregister(cell);
                    Destroy(c.gameObject);
                }
                grid.FreeConveyor(cell);
                EventBus.Publish(new ConveyorRemovedEvent(cell));
            }
        }

        private static bool IsPlacementMode(BuildMode m) =>
            m == BuildMode.Extractor || m == BuildMode.Assembler || m == BuildMode.Remove;

        private BuildingDefinitionSO ResolveDef(BuildMode mode)
        {
            if (catalog == null) return null;
            return mode switch
            {
                BuildMode.Extractor => catalog.extractor,
                BuildMode.Assembler => catalog.assembler,
                _ => null
            };
        }
    }
}
