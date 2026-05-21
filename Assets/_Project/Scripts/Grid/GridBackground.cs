using System.Collections.Generic;
using UnityEngine;
using OceanFactory.Core;

namespace OceanFactory.Grid
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridBackground : MonoBehaviour
    {
        [Header("Dark Tile Grid")]
        [SerializeField] private Color outsideColor    = new Color(0.028f, 0.038f, 0.075f, 1f);
        [SerializeField] private Color gapColor        = new Color(0.040f, 0.055f, 0.105f, 1f);
        [SerializeField] private Color tileColor       = new Color(0.055f, 0.075f, 0.140f, 1f);
        [SerializeField] private Color tileBorderColor = new Color(0.075f, 0.100f, 0.170f, 1f);
        [SerializeField, Range(0.01f, 0.25f), Tooltip("Empty space between cell panels. Larger values create thicker grid gaps.")]
        private float cellGap = 0.075f;
        [SerializeField, Range(0f, 0.12f), Tooltip("Subtle inner rim around each cell. Set to 0 to disable.")]
        private float tileBorderThickness = 0.025f;
        [SerializeField] private int sortingOrder = -10;
        [SerializeField] private string sortingLayerName = "Default";

        private MeshFilter mf;
        private MeshRenderer mr;
        private Material runtimeMaterial;

        private void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name == "GridBackgroundMesh")
            {
                Destroy(mf.sharedMesh);
            }
        }

        public void Build()
        {
            if (!Services.TryGet<GridSystem>(out var grid))
            {
                Debug.LogError("GridBackground: GridSystem not registered yet");
                return;
            }
            EnsureComponents();

            var mesh = mf.sharedMesh != null && mf.sharedMesh.name == "GridBackgroundMesh"
                ? mf.sharedMesh
                : new Mesh { name = "GridBackgroundMesh", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.Clear();

            int cellCount = grid.Width * grid.Height;
            int quadsPerCell = tileBorderThickness > 0f ? 2 : 1;
            var vertices = new List<Vector3>(4 + cellCount * quadsPerCell * 4);
            var colors = new List<Color>(vertices.Capacity);
            var triangles = new List<int>(vertices.Capacity * 3 / 2);

            // A slightly larger outside plate prevents camera-edge white flashes when panning.
            AddQuad(vertices, triangles, colors,
                new Vector2(-1f, -1f), new Vector2(grid.Width + 1f, grid.Height + 1f), outsideColor);

            // The base layer is the visible grid gap. Each cell is an inset dark panel on top.
            AddQuad(vertices, triangles, colors,
                new Vector2(0f, 0f), new Vector2(grid.Width, grid.Height), gapColor);

            float inset = cellGap * 0.5f;
            float borderInset = Mathf.Max(inset + tileBorderThickness, inset);
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var borderBl = new Vector2(x + inset, y + inset);
                    var borderTr = new Vector2(x + 1f - inset, y + 1f - inset);
                    if (borderBl.x >= borderTr.x || borderBl.y >= borderTr.y) continue;

                    if (tileBorderThickness > 0f)
                    {
                        AddQuad(vertices, triangles, colors, borderBl, borderTr, tileBorderColor);
                    }

                    var tileBl = new Vector2(x + borderInset, y + borderInset);
                    var tileTr = new Vector2(x + 1f - borderInset, y + 1f - borderInset);
                    if (tileBl.x >= tileTr.x || tileBl.y >= tileTr.y) continue;
                    AddQuad(vertices, triangles, colors, tileBl, tileTr, tileColor);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;

            if (runtimeMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                runtimeMaterial = new Material(shader) { name = "GridBackgroundMat" };
            }
            mr.sharedMaterial = runtimeMaterial;
            mr.sortingOrder = sortingOrder;
            mr.sortingLayerName = sortingLayerName;
            transform.position = new Vector3(0f, 0f, 0.01f);

            if (Camera.main != null)
            {
                Camera.main.backgroundColor = outsideColor;
            }
        }

        private void EnsureComponents()
        {
            if (mf == null) mf = GetComponent<MeshFilter>();
            if (mr == null) mr = GetComponent<MeshRenderer>();
        }

        private static void AddQuad(List<Vector3> verts, List<int> tris, List<Color> cols, Vector2 bl, Vector2 tr, Color c)
        {
            int b = verts.Count;
            verts.Add(new Vector3(bl.x, bl.y, 0f));
            verts.Add(new Vector3(tr.x, bl.y, 0f));
            verts.Add(new Vector3(tr.x, tr.y, 0f));
            verts.Add(new Vector3(bl.x, tr.y, 0f));
            cols.Add(c); cols.Add(c); cols.Add(c); cols.Add(c);
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 1);
            tris.Add(b + 0); tris.Add(b + 3); tris.Add(b + 2);
        }
    }
}
