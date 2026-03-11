using UnityEngine;

namespace Turnroot.Graphics3D
{
    public partial class GrassRenderer
    {
        // helper for indirect args array
        private void SetArgsFromMesh(Mesh mesh)
        {
            _args[0] = (uint)mesh.GetIndexCount(0);
            _args[2] = (uint)mesh.GetIndexStart(0);
            _args[3] = (uint)mesh.GetBaseVertex(0);
            _args[4] = 0;
        }

        private void SetInstanceCount(uint count)
        {
            _args[1] = count;
        }

        // ── Init ──────────────────────────────────────────────────────────────────
        private void Init()
        {
            if (computeShader == null || grassMaterial == null)
            {
                return;
            }

            var mf = GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                return;
            }

            ReleaseBuffers();
            DestroyMesh(ref _bladeMesh);
            DestroyMesh(ref _planeMesh);

            BuildBladeMesh();
            BuildPlaneMesh();
            BuildBladeData(mf.sharedMesh);
            BuildExtraGroups(mf.sharedMesh);
            AlignGroundTexture();

            _cullKernel = computeShader.FindKernel("CullGrass");
        }

        // ── Per-frame GPU work ────────────────────────────────────────────────────
        private void DrawGroup(
            ComputeBuffer all,
            ComputeBuffer visible,
            ComputeBuffer argsBuffer,
            int totalCount,
            Mesh mesh,
            Material mat,
            UnityEngine.Camera cam
        )
        {
            if (disableCulling)
            {
                // Render directly from the full blade buffer; write count into args slot 1.
                mat.SetBuffer("_VisibleBlades", all);
                SetArgsFromMesh(mesh);
                SetInstanceCount((uint)totalCount);
                argsBuffer.SetData(_args);
            }
            else
            {
                visible.SetCounterValue(0);
                DispatchCull(all, visible, totalCount, cam);
                // Slots 0,2,3,4 are constant; CopyCount writes the visible count into slot 1.
                SetArgsFromMesh(mesh);
                SetInstanceCount(0);
                argsBuffer.SetData(_args);
                ComputeBuffer.CopyCount(visible, argsBuffer, sizeof(uint));
                mat.SetBuffer("_VisibleBlades", visible);
            }

            mat.SetVector("_CameraPosition", cam.transform.position);
            mat.SetFloat("_MaxDistance", maxDistance);
            mat.SetFloat("_FadeStartDistance", fadeStartDistance);
            Graphics.DrawMeshInstancedIndirect(
                mesh,
                0,
                mat,
                _drawBounds,
                argsBuffer,
                0,
                null,
                shadowCasting,
                true
            );
        }

        private void DispatchCull(
            ComputeBuffer all,
            ComputeBuffer visible,
            int totalCount,
            UnityEngine.Camera cam
        )
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var planeV4 = new Vector4[6];
            for (int i = 0; i < 6; i++)
            {
                planeV4[i] = new Vector4(
                    planes[i].normal.x,
                    planes[i].normal.y,
                    planes[i].normal.z,
                    planes[i].distance
                );
            }

            computeShader.SetInt("_TotalBladeCount", totalCount);
            computeShader.SetVectorArray("_FrustumPlanes", planeV4);
            computeShader.SetVector("_CameraPos", cam.transform.position);
            computeShader.SetFloat("_MaxDistance", maxDistance);
            computeShader.SetBuffer(_cullKernel, "_AllBlades", all);
            computeShader.SetBuffer(_cullKernel, "_VisibleBlades", visible);
            computeShader.Dispatch(_cullKernel, Mathf.CeilToInt(totalCount / 64f), 1, 1);
        }

        // ── Cleanup ───────────────────────────────────────────────────────────────
        private void ReleaseBuffers()
        {
            _allBladesBuffer?.Release();
            _allBladesBuffer = null;
            _visibleBladesBuffer?.Release();
            _visibleBladesBuffer = null;
            _indirectArgsBuffer?.Release();
            _indirectArgsBuffer = null;
            _readbackBuffer?.Release();
            _readbackBuffer = null;

            if (_extraGroups != null)
            {
                foreach (var g in _extraGroups)
                {
                    g.all?.Release();
                    g.visible?.Release();
                    g.args?.Release();
                }
                _extraGroups = null;
            }
        }

        private void DestroyMesh(ref Mesh mesh)
        {
            if (mesh == null)
            {
                return;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(mesh);
            }
            else
#endif
                Destroy(mesh);
            mesh = null;
        }
    }
}
