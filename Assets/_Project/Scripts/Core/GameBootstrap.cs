using UnityEngine;
using OceanFactory.Buildings;
using OceanFactory.CameraControl;
using OceanFactory.Data;
using OceanFactory.Generation;
using OceanFactory.Grid;

namespace OceanFactory.Core
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfigSO gameConfig;
        [SerializeField] private BuildingCatalogSO buildingCatalog;
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private RandomProvider randomProvider;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private ResourcePatchVisualizer patchVisualizer;
        [SerializeField] private GridBackground gridBackground;
        [SerializeField, Tooltip("Auto-place a Hub at the center of the grid on game start (Shapez-style).")]
        private bool autoPlaceCenterHub = true;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            if (gameConfig == null)
            {
                Debug.LogError("GameBootstrap: GameConfig is not assigned");
                return;
            }
            if (mapGenerator == null) { Debug.LogError("GameBootstrap: MapGenerator missing"); return; }
            if (randomProvider == null) { Debug.LogError("GameBootstrap: RandomProvider missing"); return; }

            Services.Register(gameConfig);
            randomProvider.Seed(gameConfig.randomSeed);
            mapGenerator.GenerateInitial();

            if (gridBackground != null) gridBackground.Build();
            if (patchVisualizer != null) patchVisualizer.RebuildFromGrid();

            if (Services.TryGet<GameStateManager>(out var gsm))
            {
                gsm.TransitionTo(GameState.Playing);
            }
            else
            {
                Debug.LogError("GameBootstrap: GameStateManager not in scene — gameplay will be locked. Add it.");
            }

            if (autoPlaceCenterHub) TryPlaceCenterHub();
            if (cameraController != null) cameraController.CenterOnGrid();
        }

        private void TryPlaceCenterHub()
        {
            if (buildingCatalog == null || buildingCatalog.hub == null)
            {
                Debug.LogWarning("GameBootstrap: BuildingCatalog or hub definition missing — center hub not placed.");
                return;
            }
            if (!Services.TryGet<GridSystem>(out var grid)) return;
            if (!Services.TryGet<BuildingFactory>(out var factory))
            {
                Debug.LogWarning("GameBootstrap: BuildingFactory not registered yet — center hub not placed.");
                return;
            }

            var hubDef = buildingCatalog.hub;
            var hubSize = hubDef.size.x > 0 && hubDef.size.y > 0 ? hubDef.size : Vector2Int.one;
            var center = new Vector2Int(grid.Width / 2, grid.Height / 2);
            // Origin is bottom-left; shift so the hub is centred on grid center.
            var preferredOrigin = new Vector2Int(center.x - hubSize.x / 2, center.y - hubSize.y / 2);

            int maxRadius = Mathf.Max(grid.Width, grid.Height);
            for (int radius = 0; radius <= maxRadius; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (radius > 0 && Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;
                        var origin = preferredOrigin + new Vector2Int(dx, dy);
                        if (!grid.IsRectEmpty(origin, hubSize)) continue;
                        var spawned = factory.Spawn(hubDef, origin, Core.Direction.North);
                        if (spawned != null) return;
                    }
                }
            }
            Debug.LogWarning("GameBootstrap: could not find an empty area for center hub.");
        }

        private void OnDestroy()
        {
            Services.Unregister<GameConfigSO>();
        }

        private void ResolveReferences()
        {
            if (mapGenerator == null) mapGenerator = FindFirstObjectByType<MapGenerator>();
            if (randomProvider == null) randomProvider = FindFirstObjectByType<RandomProvider>();
            if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();
            if (patchVisualizer == null) patchVisualizer = FindFirstObjectByType<ResourcePatchVisualizer>();
            if (gridBackground == null) gridBackground = FindFirstObjectByType<GridBackground>();
        }
    }
}
