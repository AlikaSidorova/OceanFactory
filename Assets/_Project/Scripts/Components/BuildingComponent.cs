using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.Components
{
    public abstract class BuildingComponent : MonoBehaviour
    {
        [SerializeField] protected BuildingDefinitionSO definition;

        public BuildingDefinitionSO Definition => definition;
        public Vector2Int Cell { get; private set; }
        public Direction Facing { get; private set; } = Direction.North;

        public Vector2Int Size => definition != null ? definition.size : Vector2Int.one;
        public Vector2Int Forward => Facing.ToVector();

        public void SetCell(Vector2Int cell)
        {
            Cell = cell;
        }

        public void SetFacing(Direction facing)
        {
            Facing = facing;
            transform.rotation = Quaternion.Euler(0f, 0f, facing.ToZAngleDeg());
        }

        // Called by BuildingFactory after SetCell/SetFacing/grid occupy have completed.
        // Use this instead of OnEnable for any logic depending on Cell, Facing, or grid state.
        public virtual void OnPlaced() { }

        protected virtual void OnEnable()
        {
            EntityRegistry.Register(this);
        }

        protected virtual void OnDisable()
        {
            EntityRegistry.Unregister(this);
        }
    }
}
