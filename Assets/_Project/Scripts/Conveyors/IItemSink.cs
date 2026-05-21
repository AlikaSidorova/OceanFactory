using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Data;

namespace OceanFactory.Conveyors
{
    public interface IItemSink
    {
        bool TryAcceptItem(ItemTypeSO item, Vector2Int intoCell, Direction fromDir);
    }
}
