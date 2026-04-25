using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Animates the color of a UI <see cref="Graphic"/> (Image, RawImage, Text, TMP_Text, etc.)
    /// between two colors using a sine wave. Works with Canvas Renderer — no world-space
    /// Renderer or material instance needed.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class SineMaterialColorsCanvas : MonoBehaviour
    {
        public float Speed = 1.0f;

        public Color StartColor = Color.white;
        public Color EndColor = Color.white;

        public bool HoldAtCycleMidpoint = false;

        [Range(0f, 1f)]
        public float HoldAmount = 0f;

        private Graphic _graphic;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
        }

        private void Update()
        {
            if (_graphic == null)
            {
                return;
            }

            float t = (Mathf.Sin(Time.time * Speed) + 1f) * 0.5f; // normalised to [0, 1]
            if (HoldAtCycleMidpoint && HoldAmount > 0f)
            {
                float half = HoldAmount * 0.5f;
                float lo = half;
                float hi = 1f - half;
                t = lo >= hi ? (t >= 0.5f ? 1f : 0f) : Mathf.Clamp01(Mathf.InverseLerp(lo, hi, t));
            }
            _graphic.color = Color.Lerp(StartColor, EndColor, t);
        }

        private void OnDestroy()
        {
            // Restore the start color so the graphic doesn't stay mid-tween in the editor.
            if (_graphic != null)
            {
                _graphic.color = StartColor;
            }
        }
    }
}
