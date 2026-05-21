using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using OceanFactory.Core;
using OceanFactory.Data;
using OceanFactory.Grid;

namespace OceanFactory.Conveyors
{
    public class ConveyorSimulation : MonoBehaviour
    {
        [SerializeField] private GameConfigSO gameConfig;
        [SerializeField] private VisualConfigSO visualConfig;
        [SerializeField] private Transform itemsRoot;

        private float accumulator;
        private float tickInterval;

        private ObjectPool<ItemVisual> visualPool;
        private readonly HashSet<ItemVisual> activeVisuals = new();

        private readonly List<ConveyorComponent> processOrder = new();
        private readonly HashSet<ConveyorComponent> visited = new();
        private readonly HashSet<ConveyorComponent> onStack = new();

        private void Awake()
        {
            Services.Register(this);
            if (gameConfig != null)
            {
                tickInterval = gameConfig.simTickHz > 0f ? 1f / gameConfig.simTickHz : 0.1666f;
            }
            else
            {
                tickInterval = 0.1666f;
            }
            visualPool = new ObjectPool<ItemVisual>(
                createFunc: CreateVisual,
                actionOnGet: OnGetVisual,
                actionOnRelease: OnReleaseVisual,
                actionOnDestroy: OnDestroyVisual,
                collectionCheck: false,
                defaultCapacity: visualConfig != null ? visualConfig.itemPoolDefaultSize : 200,
                maxSize: visualConfig != null ? visualConfig.itemPoolMaxSize : 2000);
        }

        private void OnDestroy()
        {
            Services.Unregister<ConveyorSimulation>();
            visualPool?.Clear();
        }

        public ItemVisual AcquireVisual(ItemTypeSO item, Vector3 worldPos)
        {
            var v = visualPool.Get();
            v.Setup(item, visualConfig != null ? visualConfig.itemSpriteFallback : null);
            v.SetStatic(worldPos);
            activeVisuals.Add(v);
            return v;
        }

        public void ReleaseVisual(ItemVisual visual)
        {
            if (visual == null) return;
            activeVisuals.Remove(visual);
            visualPool.Release(visual);
        }

        private void Update()
        {
            if (!Services.TryGet<GameStateManager>(out var gsm) || !gsm.IsPlaying) return;
            if (tickInterval <= 0f) return;

            accumulator += Time.deltaTime / tickInterval;
            int safety = 4;
            while (accumulator >= 1f && safety-- > 0)
            {
                accumulator -= 1f;
                Tick();
            }
            if (accumulator < 0f) accumulator = 0f;

            float t01 = Mathf.Clamp01(accumulator);
            foreach (var v in activeVisuals)
            {
                if (v != null) v.RenderAt(t01);
            }
        }

        private void Tick()
        {
            if (!Services.TryGet<ConveyorNetwork>(out var network)) return;
            if (!Services.TryGet<GridSystem>(out var grid)) return;

            BuildProcessOrder(network);

            for (int i = 0; i < processOrder.Count; i++)
            {
                var c = processOrder[i];
                if (c == null || c.HeldItem == null) continue;

                var nextCell = c.Cell + c.OutDir.ToVector();
                var nextBelt = network.GetAt(nextCell);
                if (nextBelt != null)
                {
                    // Ping-pong guard: refuse to push into a belt whose output points back at us.
                    // Without this, two facing conveyors trade the same item forever.
                    if (nextBelt.OutDir == c.OutDir.Opposite())
                    {
                        if (c.Visual != null)
                        {
                            var p = grid.CellToWorld(c.Cell);
                            c.Visual.SetMotion(p, p);
                        }
                        continue;
                    }
                    if (nextBelt.HeldItem == null)
                    {
                        var item = c.HeldItem;
                        var visual = c.Visual;
                        c.ClearItem();
                        // Receiving belt should visually reflect "items flow in from where C is"
                        // (opposite of C's outDir). Keeps straight/turn sprites correct after drag bends.
                        nextBelt.SetInDir(c.OutDir.Opposite());
                        nextBelt.TryPlaceItem(item, visual);
                        if (visual != null)
                        {
                            visual.SetMotion(grid.CellToWorld(c.Cell), grid.CellToWorld(nextBelt.Cell));
                        }
                        continue;
                    }
                }
                else
                {
                    var building = grid.GetBuildingAt(nextCell);
                    if (building is IItemSink sink)
                    {
                        if (sink.TryAcceptItem(c.HeldItem, nextCell, c.OutDir.Opposite()))
                        {
                            ReleaseVisual(c.Visual);
                            c.ClearItem();
                            continue;
                        }
                    }
                }

                if (c.Visual != null)
                {
                    var p = grid.CellToWorld(c.Cell);
                    c.Visual.SetMotion(p, p);
                }
            }
        }

        private void BuildProcessOrder(ConveyorNetwork network)
        {
            processOrder.Clear();
            visited.Clear();
            onStack.Clear();
            foreach (var kvp in network.All)
            {
                Visit(kvp.Value, network);
            }
        }

        private void Visit(ConveyorComponent c, ConveyorNetwork network)
        {
            if (c == null) return;
            if (visited.Contains(c)) return;
            if (onStack.Contains(c)) return;
            onStack.Add(c);
            var next = network.GetAt(c.Cell + c.OutDir.ToVector());
            if (next != null) Visit(next, network);
            onStack.Remove(c);
            visited.Add(c);
            processOrder.Add(c);
        }

        private ItemVisual CreateVisual()
        {
            if (visualConfig == null || visualConfig.itemVisualPrefab == null)
            {
                var go = new GameObject("Item");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 5;
                var iv = go.AddComponent<ItemVisual>();
                if (itemsRoot != null) go.transform.SetParent(itemsRoot, false);
                return iv;
            }
            var inst = Instantiate(visualConfig.itemVisualPrefab, itemsRoot);
            var visual = inst.GetComponent<ItemVisual>();
            if (visual == null) visual = inst.AddComponent<ItemVisual>();
            return visual;
        }

        private void OnGetVisual(ItemVisual v)
        {
            if (v != null) v.gameObject.SetActive(true);
        }

        private void OnReleaseVisual(ItemVisual v)
        {
            if (v != null)
            {
                v.Hide();
                v.gameObject.SetActive(false);
            }
        }

        private void OnDestroyVisual(ItemVisual v)
        {
            if (v != null) Destroy(v.gameObject);
        }
    }
}
