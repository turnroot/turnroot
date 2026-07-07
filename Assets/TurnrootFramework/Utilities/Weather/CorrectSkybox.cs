using UnityEngine;

namespace Turnroot.Utilities.Weather
{
    public class CorrectSkybox : MonoBehaviour
    {
        public Material SkyboxMaterial;

        private void Update()
        {
            if (RenderSettings.skybox != SkyboxMaterial)
            {
                ApplySkyboxMaterial();
            }
        }

        public void ApplySkyboxMaterial()
        {
            if (SkyboxMaterial == null)
            {
                return;
            }

            RenderSettings.skybox = SkyboxMaterial;
            DynamicGI.UpdateEnvironment();
        }
    }
}
