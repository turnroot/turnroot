using UnityEngine;

namespace Turnroot.Utilities
{
    public class PulseLightIntensityOrColor : MonoBehaviour
    {
        [SerializeField]
        private Light lightToPulse;

        [SerializeField]
        private float pulseSpeed = 1f;

        [SerializeField]
        private float minIntensity = 0f;

        [SerializeField]
        private float maxIntensity = 1f;

        [SerializeField]
        private Color minColor = Color.black;

        [SerializeField]
        private Color maxColor = Color.white;

        private void Update()
        {
            if (lightToPulse != null)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
                lightToPulse.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
                lightToPulse.color = Color.Lerp(minColor, maxColor, pulse);
            }
        }
    }
}
