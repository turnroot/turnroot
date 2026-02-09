using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manage global volume settings
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    public class VolumeBrain : BrainComponent
    {
        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void SubscribeToBrainEvents() => Brain.OnPlayerSettingsChanged += ApplySettingsToVolumes;

        protected override void UnsubscribeFromBrainEvents()
        {
            if (Brain != null)
            {
                Brain.OnPlayerSettingsChanged -= ApplySettingsToVolumes;
            }
        }

        protected override void Awake() => base.Awake();

        public void ApplySettingsToVolumes(PlayerSettings.GameplayPlayerSettings playerSettings)
        {
            var settings = playerSettings;

            // Find the GlobalVolume component in all scenes
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                foreach (var rootGameObject in scene.GetRootGameObjects())
                {
                    var globalVolume = rootGameObject.GetComponentInChildren<Volume>(true);
                    if (globalVolume != null && globalVolume.profile != null)
                    {
                        ApplyGraphicsSettings(globalVolume, settings);
                        break;
                    }
                }
            }
        }

        private void ApplyGraphicsSettings(
            Volume globalVolume,
            PlayerSettings.GameplayPlayerSettings settings
        )
        {
            var profile = globalVolume.profile;

            // Apply Bloom setting
            if (profile.TryGet<Bloom>(out var bloom))
            {
                bloom.active = settings.Bloom;
            }

            // Apply Depth of Field setting
            if (profile.TryGet<DepthOfField>(out var depthOfField))
            {
                depthOfField.active = settings.DepthOfField;
            }

            // Apply Lens Flare setting
            if (profile.TryGet<ScreenSpaceLensFlare>(out var lensFlare))
            {
                lensFlare.active = settings.LensFlare;
            }

            // Apply Brightness and Contrast via URP Color Adjustments (postExposure and contrast)
            if (profile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                // Map brightness to postExposure (-2..2) and contrast to contrast (-50..50)
                var postExposure = Mathf.Clamp(settings.Brightness, -2f, 2f);
                var contrast = Mathf.Clamp(settings.Contrast, -50f, 50f);

                colorAdjustments.postExposure.value = postExposure;
                colorAdjustments.contrast.value = contrast;

                // Ensure overrides are set so the volume system uses our values
                try
                {
                    colorAdjustments.postExposure.overrideState = true;
                    colorAdjustments.contrast.overrideState = true;
                }
                catch { }

                // Enable the effect only if settings modify it from neutral
                colorAdjustments.active =
                    Mathf.Abs(postExposure) > 0.000001f || Mathf.Abs(contrast) > 0.000001f;
            }

            // Apply URP quality settings (shadows, cascades, etc.)
            ApplyQualitySettings(settings);
        }

        private void ApplyQualitySettings(PlayerSettings.GameplayPlayerSettings settings)
        {
            var rpAsset =
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline
                as UniversalRenderPipelineAsset;
            if (rpAsset == null)
            {
                return;
            }

            // Map float quality to 4 discrete steps (0..3)
            int step = settings.QualityStep;

            switch (step)
            {
                case 0: // Low
                    rpAsset.mainLightShadowmapResolution = 512;
                    rpAsset.additionalLightsShadowmapResolution = 256;
                    rpAsset.shadowDistance = 30f;
                    rpAsset.shadowCascadeCount = 1;
                    break;
                case 1: // Medium
                    rpAsset.mainLightShadowmapResolution = 1024;
                    rpAsset.additionalLightsShadowmapResolution = 512;
                    rpAsset.shadowDistance = 80f;
                    rpAsset.shadowCascadeCount = 2;
                    break;
                case 2: // High
                    rpAsset.mainLightShadowmapResolution = 2048;
                    rpAsset.additionalLightsShadowmapResolution = 1024;
                    rpAsset.shadowDistance = 150f;
                    rpAsset.shadowCascadeCount = 4;
                    break;
                case 3: // Ultra
                    rpAsset.mainLightShadowmapResolution = 4096;
                    rpAsset.additionalLightsShadowmapResolution = 4096;
                    rpAsset.shadowDistance = 300f;
                    rpAsset.shadowCascadeCount = 4;
                    break;
            }
        }
    }
}
