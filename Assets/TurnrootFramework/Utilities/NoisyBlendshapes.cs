using UnityEngine;

namespace Turnroot.Utilities
{
    public class NoisyBlendshapes : MonoBehaviour
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public int[] blendshapeIndices;

        [Range(0f, 1f)]
        public float noiseScale = 1f;
        private float _scale;

        [Range(0f, 5f)]
        public float noiseSpeed = 1f;
        public float[] seeds;

        private void Awake() => _scale = 1f / noiseScale; // cache expensive division

        private void Update()
        {
            if (skinnedMeshRenderer == null)
            {
                return;
            }

            for (int i = 0; i < blendshapeIndices.Length; i++)
            {
                float noiseValue = Mathf.PerlinNoise(Time.time * noiseSpeed, seeds[i]);
                noiseValue *= 100f * _scale;
                skinnedMeshRenderer.SetBlendShapeWeight(blendshapeIndices[i], noiseValue);
            }
        }
    }
}
