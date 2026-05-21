using UnityEngine;
using OceanFactory.Components;
using OceanFactory.Core;
using OceanFactory.Grid;
using OceanFactory.Input;
using OceanFactory.Utils;

namespace OceanFactory.UI
{
    /// <summary>
    /// Lightweight hover effect for buildings: when the pointer hovers a building's footprint,
    /// the whole prefab nudges slightly larger (button-style). No prefab wiring required —
    /// the controller auto-creates itself after scene load.
    /// </summary>
    public class BuildingHoverHighlighter : MonoBehaviour
    {
        [SerializeField, Range(1.0f, 1.25f)] private float hoverScale = 1.06f;
        [SerializeField, Range(0f, 30f)] private float lerpSpeed = 18f;

        private InputReader input;
        private GridSystem grid;
        private BuildingComponent currentHover;
        private Vector3 currentBaseScale;
        private float currentBlend;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
            // Idempotent: don't add a second one if the scene already provides it.
            if (FindFirstObjectByType<BuildingHoverHighlighter>() != null) return;
            var go = new GameObject("BuildingHoverHighlighter");
            go.AddComponent<BuildingHoverHighlighter>();
        }

        private void Update()
        {
            if (input == null) Services.TryGet(out input);
            if (grid  == null) Services.TryGet(out grid);
            if (input == null || grid == null) return;

            BuildingComponent hovered = null;
            if (!PointerUtility.IsOverUI())
            {
                var cell = grid.WorldToCell(input.PointerWorld);
                if (grid.IsInside(cell)) hovered = grid.GetBuildingAt(cell);
            }

            if (hovered != currentHover)
            {
                // Restore the previous building to its natural scale.
                if (currentHover != null) currentHover.transform.localScale = currentBaseScale;
                currentHover = hovered;
                currentBlend = 0f;
                if (currentHover != null) currentBaseScale = currentHover.transform.localScale;
            }

            if (currentHover == null) return;

            currentBlend = Mathf.MoveTowards(currentBlend, 1f, Time.unscaledDeltaTime * lerpSpeed);
            float scale = Mathf.Lerp(1f, hoverScale, currentBlend);
            currentHover.transform.localScale = currentBaseScale * scale;
        }

        private void OnDisable()
        {
            if (currentHover != null)
            {
                currentHover.transform.localScale = currentBaseScale;
                currentHover = null;
            }
        }
    }
}
