using UnityEngine;

namespace Turnroot.Graphics3D
{
    public partial class GrassRenderer
    {
        // ── Mesh construction ─────────────────────────────────────────────────────
        // Blade: 5-vertex tapered quad. vertex.x = side (–0.5..0.5), vertex.y = t (0..1).
        // The shader reconstructs world positions per instance from BladeData.
        //     [4] tip
        //    [2][3] mid
        //    [0][1] base
        private void BuildBladeMesh()
        {
            _bladeMesh = new Mesh { name = "GrassBlade" };
            _bladeMesh.SetVertices(
                new Vector3[]
                {
                    new(-0.5f, 0.00f, 0),
                    new(0.5f, 0.00f, 0),
                    new(-0.5f, 0.55f, 0),
                    new(0.5f, 0.55f, 0),
                    new(0.0f, 1.00f, 0),
                }
            );
            _bladeMesh.SetUVs(
                0,
                new Vector2[]
                {
                    new(0f, 0.00f),
                    new(1f, 0.00f),
                    new(0f, 0.55f),
                    new(1f, 0.55f),
                    new(0.5f, 1f),
                }
            );
            _bladeMesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3, 2, 4, 3 }, 0);
            _bladeMesh.RecalculateNormals();
            // Oversized bounds prevent Unity's per-mesh frustum cull from firing;
            // per-blade visibility is handled by the compute shader.
            _bladeMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100_000f);
        }

        // Extra: vertical unit quad, x = –0.5..0.5, y = 0..1.
        private void BuildPlaneMesh()
        {
            _planeMesh = new Mesh { name = "GrassExtraPlane" };
            _planeMesh.SetVertices(
                new Vector3[]
                {
                    new(-0.5f, 0, 0),
                    new(0.5f, 0, 0),
                    new(-0.5f, 1, 0),
                    new(0.5f, 1, 0),
                }
            );
            _planeMesh.SetUVs(0, new Vector2[] { new(0, 0), new(1, 0), new(0, 1), new(1, 1) });
            _planeMesh.SetTriangles(new[] { 0, 1, 2, 2, 1, 3 }, 0);
            _planeMesh.RecalculateNormals();
            _planeMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100_000f);
        }
    }
}
