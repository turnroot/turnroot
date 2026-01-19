using UnityEngine;

namespace Turnroot.Utilities.AbstractScripts
{
    [RequireComponent(typeof(Renderer))]
    public class SineMaterialColors : MonoBehaviour
    {
        public float Speed = 1.0f;
        public Material TargetMaterial;

        public Color startColor;
        public Color endColor;

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
                Color newColor = Color.Lerp(startColor, endColor, sineValue);
                _materialInstance.color = newColor;
            }
        }
    }
}
