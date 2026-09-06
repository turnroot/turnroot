using UnityEngine;

namespace Turnroot.Graphics3D
{
    public partial class GrassRenderer
    {
        // helper for indirect args array
        private void SetArgsFromMesh(Mesh mesh)
        {
            _args[0] = mesh.GetIndexCount(0);
            _args[2] = mesh.GetIndexStart(0);
            _args[3] = mesh.GetBaseVertex(0);
            _args[4] = 0;
        }

        private void SetInstanceCount(uint count) => _args[1] = count;

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

            // Prevent Unity from rendering the mesh via a MeshRenderer, which would
            // invoke the grass shader without supplying the required _VisibleBlades buffer.
            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer != null)
            {
                _meshRendererWasEnabled = _meshRenderer.enabled;
                _meshRenderer.enabled = false;
            }

            ReleaseBuffers();
            DestroyMesh(ref _bladeMesh);
            DestroyMesh(ref _planeMesh);

            BuildBladeMesh();
            BuildPlaneMesh();
            UploadMeshData(mf.sharedMesh);
            BuildExtraGroups(mf.sharedMesh);

            _generateKernel = computeShader.FindKernel("GenerateAndCull");
            _cullKernel = computeShader.FindKernel("CullGrass");
        }

        // ── Main grass: GPU procedural generate + cull in one pass ────────────────
        private void DrawMainGrass(UnityEngine.Camera cam)
        {
            if (
                _visibleBladesBuffer == null
                || _indirectArgsBuffer == null
                || _bladeMesh == null
                || grassMaterial == null
                || cam == null
            )
            {
                return;
            }

            _visibleBladesBuffer.SetCounterValue(0);
            DispatchGenerate(cam);

            SetArgsFromMesh(_bladeMesh);
            SetInstanceCount(0);
            _indirectArgsBuffer.SetData(_args);
            ComputeBuffer.CopyCount(_visibleBladesBuffer, _indirectArgsBuffer, sizeof(uint));

            grassMaterial.SetBuffer("_VisibleBlades", _visibleBladesBuffer);
            grassMaterial.SetVector("_CameraPosition", cam.transform.position);
            grassMaterial.SetFloat("_MaxDistance", maxDistance);
            grassMaterial.SetFloat("_FadeStartDistance", fadeStartDistance);

            Graphics.DrawMeshInstancedIndirect(
                _bladeMesh,
                0,
                grassMaterial,
                _drawBounds,
                _indirectArgsBuffer,
                0,
                null,
                shadowCasting,
                true
            );
        }

        private void DispatchGenerate(UnityEngine.Camera cam)
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

            computeShader.SetVectorArray("_FrustumPlanes", planeV4);
            computeShader.SetVector("_CameraPos", cam.transform.position);
            computeShader.SetFloat("_MaxDistance", maxDistance);
            computeShader.SetInt("_TriCount", _triCount);
            computeShader.SetMatrix("_LocalToWorld", transform.localToWorldMatrix);
            computeShader.SetFloat("_Density", density);
            computeShader.SetFloat("_MinHeight", minHeight);
            computeShader.SetFloat("_MaxHeight", maxHeight);
            computeShader.SetFloat("_MinWidth", minWidth);
            computeShader.SetFloat("_MaxWidth", maxWidth);

            bool hasMask = maskTexture != null && maskTexture.isReadable;
            computeShader.SetInt("_HasMask", hasMask ? 1 : 0);
            computeShader.SetFloat("_MaskFloor", maskFloor);
            computeShader.SetTexture(
                _generateKernel,
                "_MaskTex",
                hasMask ? maskTexture : _whiteTex
            );

            computeShader.SetBuffer(_generateKernel, "_MeshVerts", _meshVertsBuffer);
            computeShader.SetBuffer(_generateKernel, "_MeshTris", _meshTrisBuffer);
            computeShader.SetBuffer(_generateKernel, "_MeshNormals", _meshNormalsBuffer);
            computeShader.SetBuffer(_generateKernel, "_MeshUVs", _meshUVsBuffer);
            computeShader.SetBuffer(_generateKernel, "_VisibleBlades", _visibleBladesBuffer);

            computeShader.Dispatch(_generateKernel, Mathf.CeilToInt(_triCount / 64f), 1, 1);
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
            // Guard against missing GPU resources. If buffers are not valid, skip drawing.
            if (
                all == null
                || visible == null
                || argsBuffer == null
                || mesh == null
                || mat == null
                || cam == null
            )
            {
                return;
            }

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
            if (_meshRenderer != null)
            {
                _meshRenderer.enabled = _meshRendererWasEnabled;
            }

            _visibleBladesBuffer?.Release();
            _visibleBladesBuffer = null;
            _indirectArgsBuffer?.Release();
            _indirectArgsBuffer = null;
            _readbackBuffer?.Release();
            _readbackBuffer = null;

            _meshVertsBuffer?.Release();
            _meshVertsBuffer = null;
            _meshTrisBuffer?.Release();
            _meshTrisBuffer = null;
            _meshNormalsBuffer?.Release();
            _meshNormalsBuffer = null;
            _meshUVsBuffer?.Release();
            _meshUVsBuffer = null;

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
