using UnityEngine;
using System;
#if UNITY_EDITOR
#endif

namespace Turnroot.Graphics3D
{
    public partial class GrassRenderer
    {
        /// <summary>
        /// Sets _GroundTex_ST by fitting a linear world-XZ → UV transform to the ground mesh's
        /// own vertex UVs. This correctly handles any UV flip or scale the artist baked in.
        /// Called automatically on Init; call manually after rescaling or reassigning groundSource.
        /// </summary>
        [ContextMenu("Align Ground Texture")]
        public void AlignGroundTexture()
        {
            if (grassMaterial == null)
            {
                return;
            }

            // Use the assigned ground source; fall back to this object's own mesh.
            MeshFilter mf = groundSource != null ? groundSource : GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                return;
            }

            if (!TryFitWorldToUV(mf, out Vector4 st))
            {
                Debug.LogWarning("GrassRenderer: Ground mesh has no UVs or too few vertices to fit UV transform.", this);
                return;
            }

            grassMaterial.SetVector("_GroundTex_ST", st);
        }

        // Fits a linear transform  uv = (sx * world.x + ox,  sy * world.z + oy)
        // to the ground mesh's vertex UVs using ordinary least-squares per axis.
        // This correctly recovers flips and axis-proportional scales.
        private static bool TryFitWorldToUV(MeshFilter mf, out Vector4 st)
        {
            st = new Vector4(1, 1, 0, 0);
            Mesh mesh = mf.sharedMesh;
            Vector3[] verts = mesh.vertices;
            Vector2[] uvs   = mesh.uv;

            if (uvs == null || uvs.Length < 2 || uvs.Length != verts.Length)
            {
                return false;
            }

            Transform tr = mf.transform;
            int n = verts.Length;

            // Accumulate sums for two independent 2-parameter least-squares fits:
            //   U = sx * wx + ox   (world X  → texture U)
            //   V = sy * wz + oy   (world Z  → texture V)
            double swx2 = 0, swx = 0, swxu = 0, su = 0;
            double swz2 = 0, swz = 0, swzv = 0, sv = 0;

            for (int i = 0; i < n; i++)
            {
                Vector3 wp = tr.TransformPoint(verts[i]);
                double wx = wp.x, wz = wp.z;
                double u  = uvs[i].x, v = uvs[i].y;

                swx2 += wx * wx;  swx  += wx;  swxu += wx * u;  su += u;
                swz2 += wz * wz;  swz  += wz;  swzv += wz * v;  sv += v;
            }

            double detX = swx2 * n - swx * swx;
            double detZ = swz2 * n - swz * swz;

            if (Math.Abs(detX) < 1e-6 || Math.Abs(detZ) < 1e-6)
            {
                return false;
            }

            float sx = (float)((swxu * n - swx * su) / detX);
            float ox = (float)((swx2 * su - swx * swxu) / detX);
            float sy = (float)((swzv * n - swz * sv) / detZ);
            float oy = (float)((swz2 * sv - swz * swzv) / detZ);

            st = new Vector4(sx, sy, ox, oy);
            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("Regenerate Grass")]
        public void RegenerateGrass() => Init();

        private void OnValidate()
        {
            minHeight = Mathf.Min(minHeight, maxHeight);
            minWidth = Mathf.Min(minWidth, maxWidth);
            maxDistance = Mathf.Max(0f, maxDistance);
            fadeStartDistance = Mathf.Clamp(fadeStartDistance, 0f, maxDistance);
            unmaskedExtraDensity = Mathf.Clamp01(unmaskedExtraDensity);
            maskedExtraDensity = Mathf.Clamp01(maskedExtraDensity);
            maskedExtraThreshold = Mathf.Clamp01(maskedExtraThreshold);
            unmaskedExtraSize = Vector2.Max(unmaskedExtraSize, Vector2.zero);
            maskedExtraSize = Vector2.Max(maskedExtraSize, Vector2.zero);
        }
#endif
    }
}
