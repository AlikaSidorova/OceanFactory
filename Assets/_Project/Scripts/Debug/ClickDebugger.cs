using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using OceanFactory.Core;
using OceanFactory.Grid;

namespace OceanFactory.Diagnostics
{
    /// <summary>
    /// Attach to any active GameObject in Game scene.
    /// On every left-click prints:
    ///   - All UI objects under the pointer (EventSystem raycasts)
    ///   - Whether EventSystem.IsPointerOverGameObject() is true
    ///   - All Physics2D colliders under the pointer
    ///   - Grid cell at the pointer
    ///   - Current BuildMode
    /// Remove or disable this component once debugging is done.
    /// </summary>
    public class ClickDebugger : MonoBehaviour
    {
        [SerializeField] private bool enabled2DPhysics = true;
        [SerializeField] private bool enabledUIRaycast = true;
        [SerializeField] private float physicsCheckRadius = 0.1f;

        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (cam == null) cam = Camera.main;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            var screenPos = mouse.position.ReadValue();
            var worldPos = cam != null
                ? cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z))
                : Vector3.zero;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== ClickDebugger @ screen({screenPos.x:F0},{screenPos.y:F0}) world({worldPos.x:F2},{worldPos.y:F2}) ===");

            // --- EventSystem UI raycast ---
            if (enabledUIRaycast)
            {
                var es = EventSystem.current;
                bool overUI = es != null && es.IsPointerOverGameObject();
                sb.AppendLine($"[UI] IsPointerOverGameObject = {overUI}");

                if (es != null)
                {
                    var pointerData = new PointerEventData(es) { position = screenPos };
                    var results = new List<RaycastResult>();
                    es.RaycastAll(pointerData, results);
                    if (results.Count == 0)
                    {
                        sb.AppendLine("[UI] No UI objects hit by EventSystem raycast");
                    }
                    else
                    {
                        sb.AppendLine($"[UI] {results.Count} object(s) hit:");
                        for (int i = 0; i < results.Count; i++)
                        {
                            var r = results[i];
                            var go = r.gameObject;
                            var components = go.GetComponents<Component>();
                            var compNames = new System.Text.StringBuilder();
                            foreach (var c in components)
                                if (c != null) compNames.Append(c.GetType().Name).Append(", ");
                            sb.AppendLine($"  [{i}] '{go.name}' (layer:{LayerMask.LayerToName(go.layer)}) depth:{r.depth} sortOrder:{r.sortingOrder} | components: {compNames}");
                            if (go.TryGetComponent<UnityEngine.UI.Image>(out var img))
                                sb.AppendLine($"       Image: color={img.color} raycastTarget={img.raycastTarget} enabled={img.enabled}");
                            if (go.TryGetComponent<TMPro.TMP_Text>(out var tmp))
                                sb.AppendLine($"       TMP_Text: raycastTarget={tmp.raycastTarget} enabled={tmp.enabled}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine("[UI] EventSystem.current is NULL — add EventSystem to scene!");
                }
            }

            // --- Physics2D ---
            if (enabled2DPhysics)
            {
                var hits = Physics2D.OverlapCircleAll(worldPos, physicsCheckRadius);
                if (hits.Length == 0)
                    sb.AppendLine("[2D] No Physics2D colliders at cursor");
                else
                    for (int i = 0; i < hits.Length; i++)
                        sb.AppendLine($"[2D] '{hits[i].gameObject.name}' layer:{LayerMask.LayerToName(hits[i].gameObject.layer)}");
            }

            // --- Grid cell ---
            if (Services.TryGet<GridSystem>(out var grid))
            {
                var cell = grid.WorldToCell(worldPos);
                bool inside = grid.IsInside(cell);
                var ct = inside ? grid.GetCellType(cell).ToString() : "OUTSIDE";
                var res = inside ? (grid.GetResourceAt(cell)?.displayName ?? "none") : "-";
                sb.AppendLine($"[Grid] cell={cell} inside={inside} cellType={ct} resource={res}");
            }
            else sb.AppendLine("[Grid] GridSystem not in Services yet");

            // --- Current build mode ---
            if (Services.TryGet<OceanFactory.Buildings.BuildModeService>(out var bms))
                sb.AppendLine($"[Mode] Current BuildMode = {bms.Current}");
            else
                sb.AppendLine("[Mode] BuildModeService not in Services yet");

            sb.AppendLine("=== end ===");
            UnityEngine.Debug.Log(sb.ToString());
        }
    }
}
