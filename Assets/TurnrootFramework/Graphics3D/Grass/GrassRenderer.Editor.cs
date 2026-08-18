using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Turnroot.Graphics3D
{
    public partial class GrassRenderer
    {
#if UNITY_EDITOR
        [ContextMenu("Regenerate Grass")]
        public void RegenerateGrass() => Init();

        private void OnValidate()
        {
            minHeight = Mathf.Min(minHeight, maxHeight);
            minWidth = Mathf.Min(minWidth, maxWidth);
            density = Mathf.Clamp(density, 1f, 500f);
            maxDistance = Mathf.Max(0f, maxDistance);
            fadeStartDistance = Mathf.Clamp(fadeStartDistance, 0f, maxDistance);
            grassMixinDensity = Mathf.Clamp01(grassMixinDensity);
            maxGrassMixinSize = Mathf.Max(0.1f, maxGrassMixinSize);
            grassMixinSize = Vector2.Min(
                Vector2.Max(grassMixinSize, Vector2.zero),
                Vector2.one * maxGrassMixinSize
            );
        }
#endif
    }
}
