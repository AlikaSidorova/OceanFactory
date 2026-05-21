using UnityEngine;
using OceanFactory.Data;

namespace OceanFactory.Core
{
    public readonly struct BuildingPlacedEvent
    {
        public readonly Vector2Int Cell;
        public readonly BuildingKind Kind;

        public BuildingPlacedEvent(Vector2Int cell, BuildingKind kind)
        {
            Cell = cell;
            Kind = kind;
        }
    }

    public readonly struct BuildingRemovedEvent
    {
        public readonly Vector2Int Cell;
        public readonly BuildingKind Kind;

        public BuildingRemovedEvent(Vector2Int cell, BuildingKind kind)
        {
            Cell = cell;
            Kind = kind;
        }
    }

    public readonly struct ConveyorPlacedEvent
    {
        public readonly Vector2Int Cell;

        public ConveyorPlacedEvent(Vector2Int cell)
        {
            Cell = cell;
        }
    }

    public readonly struct ConveyorRemovedEvent
    {
        public readonly Vector2Int Cell;

        public ConveyorRemovedEvent(Vector2Int cell)
        {
            Cell = cell;
        }
    }

    public readonly struct ItemDeliveredEvent
    {
        public readonly ItemTypeSO Item;
        public readonly Vector2Int HubCell;

        public ItemDeliveredEvent(ItemTypeSO item, Vector2Int hubCell)
        {
            Item = item;
            HubCell = hubCell;
        }
    }

    public readonly struct GoalCompletedEvent
    {
        public readonly int GoalIndex;
        public readonly ItemTypeSO Item;

        public GoalCompletedEvent(int goalIndex, ItemTypeSO item)
        {
            GoalIndex = goalIndex;
            Item = item;
        }
    }

    public readonly struct GameStateChangedEvent
    {
        public readonly GameState From;
        public readonly GameState To;

        public GameStateChangedEvent(GameState from, GameState to)
        {
            From = from;
            To = to;
        }
    }
}
