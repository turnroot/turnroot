using UnityEngine;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Animates a material's color between two colors using a sine wave interpolation.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class SineMaterialColors : MonoBehaviour
    {
        public float Speed = 1.0f;
        public Material TargetMaterial;

        public Color startColor;
        public Color endColor;

        public bool HoldAtCycleMidpoint = false;

        [Range(0f, 1f)]
        public float HoldAmount = 0f;

        private Material _materialInstance;
        private Renderer _renderer;

        private void Start()
        {
            _renderer = GetComponent<Renderer>();

            _materialInstance = _renderer.material;
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                _materialInstance.color = startColor;
                if (Application.isPlaying)
                {
                    Destroy(_materialInstance);
                }
                else
                {
                    DestroyImmediate(_materialInstance);
                }
            }
        }

        private void Update()
        {
            if (_materialInstance != null)
            {
                float sineValue = (Mathf.Sin(Time.time * Speed) + 1) / 2; // Normalize sine to [0,1]
                if (HoldAtCycleMidpoint && HoldAmount > 0f)
                {
                    float half = HoldAmount * 0.5f;
                    float lo = half;
                    float hi = 1f - half;
                    sineValue =
                        lo >= hi
                            ? (sineValue >= 0.5f ? 1f : 0f)
                            : Mathf.Clamp01(Mathf.InverseLerp(lo, hi, sineValue));
                }
                Color newColor = Color.Lerp(startColor, endColor, sineValue);
                _materialInstance.color = newColor;
            }
        }
    }
}
