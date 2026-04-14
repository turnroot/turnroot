using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Turnroot.Graphics3D
{
    public partial class GrassRenderer
    {
        /// <summary>
        /// Sets _GroundTex_ST so world-space UVs span 0–1 over the mesh footprint.
        /// Called automatically on Init; call manually after rescaling.
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

            Transform t = mf.transform;
            Bounds local = mf.sharedMesh.bounds;
            Vector3 lMin = local.min;
            Vector3 lMax = local.max;
            Vector3[] corners = new Vector3[8]
            {
                t.TransformPoint(new Vector3(lMin.x, lMin.y, lMin.z)),
                t.TransformPoint(new Vector3(lMax.x, lMin.y, lMin.z)),
                t.TransformPoint(new Vector3(lMin.x, lMax.y, lMin.z)),
                t.TransformPoint(new Vector3(lMax.x, lMax.y, lMin.z)),
                t.TransformPoint(new Vector3(lMin.x, lMin.y, lMax.z)),
                t.TransformPoint(new Vector3(lMax.x, lMin.y, lMax.z)),
                t.TransformPoint(new Vector3(lMin.x, lMax.y, lMax.z)),
                t.TransformPoint(new Vector3(lMax.x, lMax.y, lMax.z)),
            };
            Vector3 worldMin = corners[0];
            Vector3 worldMax = corners[0];
            for (int i = 1; i < 8; i++)
            {
                worldMin = Vector3.Min(worldMin, corners[i]);
                worldMax = Vector3.Max(worldMax, corners[i]);
            }

            float w = Mathf.Max(Mathf.Abs(worldMax.x - worldMin.x), 0.001f);
            float d = Mathf.Max(Mathf.Abs(worldMax.z - worldMin.z), 0.001f);

            Vector2 scale = new Vector2(1f / w, 1f / d);
            Vector2 offset = new Vector2(-worldMin.x * scale.x, -worldMin.z * scale.y);
            grassMaterial.SetVector(
                "_GroundTex_ST",
                new Vector4(scale.x, scale.y, offset.x, offset.y)
            );
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
