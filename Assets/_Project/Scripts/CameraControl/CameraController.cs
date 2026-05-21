using UnityEngine;
using OceanFactory.Core;
using OceanFactory.Grid;
using OceanFactory.Input;
using OceanFactory.Utils;

namespace OceanFactory.CameraControl
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float zoomMin = 5f;
        [SerializeField] private float zoomMax = 40f;
        [SerializeField] private float zoomStep = 1.5f;
        [SerializeField, Tooltip("Right-mouse drag pan sensitivity (multiplier).")]
        private float dragPanSensitivity = 1f;

        public void CenterOnGrid()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;
            if (!Services.TryGet<GridSystem>(out var grid)) return;
            var p = mainCamera.transform.position;
            p.x = grid.Width * 0.5f;
            p.y = grid.Height * 0.5f;
            mainCamera.transform.position = p;
            float aspect = mainCamera.aspect > 0.01f ? mainCamera.aspect : (16f / 9f);
            float fitByHeight = grid.Height * 0.5f + 1f;
            float fitByWidth = (grid.Width * 0.5f + 1f) / aspect;
            float fitSize = Mathf.Max(fitByHeight, fitByWidth);
            mainCamera.orthographicSize = Mathf.Clamp(fitSize, zoomMin, zoomMax);
        }

        private void Update()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            var input = Services.Get<InputReader>();
            if (input == null) return;

            Vector3 pos = mainCamera.transform.position;

            // WASD pan
            Vector2 axis = input.MoveAxis;
            pos.x += axis.x * panSpeed * Time.unscaledDeltaTime;
            pos.y += axis.y * panSpeed * Time.unscaledDeltaTime;

            // Right-mouse-drag pan. Convert pixel delta to world units using current ortho size.
            if (input.PointerSecondaryHeld && mainCamera.pixelHeight > 0)
            {
                float worldPerPixel = (2f * mainCamera.orthographicSize) / mainCamera.pixelHeight;
                Vector2 d = input.PointerDelta;
                pos.x -= d.x * worldPerPixel * dragPanSensitivity;
                pos.y -= d.y * worldPerPixel * dragPanSensitivity;
            }

            var grid = Services.TryGet<GridSystem>(out var g) ? g : null;
            if (grid != null && grid.Width > 0)
            {
                pos.x = Mathf.Clamp(pos.x, 0f, grid.Width);
                pos.y = Mathf.Clamp(pos.y, 0f, grid.Height);
            }
            mainCamera.transform.position = pos;

            // Scroll-wheel zoom — ignore when pointer hovers UI (scrolling a panel must not zoom).
            float zoom = input.ZoomDelta;
            if (Mathf.Abs(zoom) > 0.01f && !PointerUtility.IsOverUI())
            {
                float size = mainCamera.orthographicSize - Mathf.Sign(zoom) * zoomStep;
                mainCamera.orthographicSize = Mathf.Clamp(size, zoomMin, zoomMax);
            }
        }
    }
}
