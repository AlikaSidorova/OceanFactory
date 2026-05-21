using UnityEngine;
using OceanFactory.Components;
using OceanFactory.Core;
using OceanFactory.Data;
using OceanFactory.Grid;

namespace OceanFactory.Buildings
{
    public class BuildingFactory : MonoBehaviour
    {
        [SerializeField] private Transform buildingsRoot;
        [SerializeField, Tooltip("Print a warning every time Spawn() rejects a placement attempt and why.")]
        private bool verboseRejects = true;

        private void Awake()
        {
            Services.Register(this);
        }

        private void OnDestroy()
        {
            Services.Unregister<BuildingFactory>();
        }

        public BuildingComponent Spawn(BuildingDefinitionSO def, Vector2Int origin, Direction facing)
        {
            if (def == null)
            {
                Debug.LogError("BuildingFactory.Spawn: definition is null");
                return null;
            }
            if (def.prefab == null)
            {
                Debug.LogError($"BuildingFactory.Spawn: '{def.name}' has no prefab assigned");
                return null;
            }

            var grid = Services.Get<GridSystem>();
            var size = def.size.x > 0 && def.size.y > 0 ? def.size : Vector2Int.one;

            if (!grid.IsRectEmpty(origin, size))
            {
                if (verboseRejects) Debug.LogWarning($"[BuildingFactory] {def.kind} rejected at {origin}: rect {size} not empty");
                return null;
            }

            if (def.kind == BuildingKind.Extractor)
            {
                if (def.requiredResource == null)
                {
                    if (!grid.HasResource(origin))
                    {
                        if (verboseRejects) Debug.LogWarning($"[BuildingFactory] Extractor rejected at {origin}: no resource under cell");
                        return null;
                    }
                }
                else
                {
                    if (grid.GetResourceAt(origin) != def.requiredResource)
                    {
                        if (verboseRejects) Debug.LogWarning($"[BuildingFactory] Extractor rejected at {origin}: required resource '{def.requiredResource.name}' not present");
                        return null;
                    }
                }
            }

            Vector3 worldPos = grid.CellToWorld(origin);
            if (size.x > 1 || size.y > 1)
            {
                worldPos.x += (size.x - 1) * 0.5f;
                worldPos.y += (size.y - 1) * 0.5f;
            }

            var go = Instantiate(def.prefab, worldPos, Quaternion.identity, buildingsRoot);
            var comp = go.GetComponent<BuildingComponent>();
            if (comp == null)
            {
                Debug.LogError($"BuildingFactory.Spawn: prefab '{def.prefab.name}' missing BuildingComponent");
                Destroy(go);
                return null;
            }

            comp.SetCell(origin);
            comp.SetFacing(facing);

            if (!grid.TryOccupyRectForBuilding(origin, size, comp))
            {
                Destroy(go);
                return null;
            }

            comp.OnPlaced();
            EventBus.Publish(new BuildingPlacedEvent(origin, def.kind));
            return comp;
        }

        public bool Despawn(BuildingComponent building)
        {
            if (building == null) return false;
            var grid = Services.Get<GridSystem>();
            var origin = building.Cell;
            var size = building.Size;
            var kind = building.Definition != null ? building.Definition.kind : BuildingKind.Extractor;
            grid.FreeRect(origin, size);
            EventBus.Publish(new BuildingRemovedEvent(origin, kind));
            Destroy(building.gameObject);
            return true;
        }
    }
}
