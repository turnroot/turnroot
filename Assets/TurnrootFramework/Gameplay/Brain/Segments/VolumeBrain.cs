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

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }

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
#if UNITY_EDITOR
                        Debug.Log(
                            $"VolumeBrain: Found global volume in scene '{scene.name}' on root '{rootGameObject.name}' (profile: {globalVolume.profile.name})"
                        );
#endif
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

            // Apply Brightness and Gamma via Lift Gamma Gain
            if (profile.TryGet<LiftGammaGain>(out var liftGammaGain))
            {
                // Preserve existing alpha/w components where applicable
                var currentGamma = liftGammaGain.gamma.value;
                var currentGain = liftGammaGain.gain.value;

                // Defensive mapping: gamma must be > 0; brightness uses 1.0 as neutral
                var gamma = Mathf.Max(0.01f, settings.Gamma);
                var brightness = settings.Brightness;

                var gammaW = settings.Gamma - 1f; // neutral = 0
                var gainW = settings.Brightness - 1f; // neutral = 0

                liftGammaGain.gamma.value = new Vector4(
                    currentGamma.x,
                    currentGamma.y,
                    currentGamma.z,
                    gammaW
                );

                liftGammaGain.gain.value = new Vector4(
                    currentGain.x,
                    currentGain.y,
                    currentGain.z,
                    gainW
                );

                // Ensure the parameter overrides are set so the volume system uses our values
                try
                {
                    // Explicitly set override state for lift/gamma/gain where possible
                    liftGammaGain.lift.overrideState = liftGammaGain.lift.overrideState || false;
                    liftGammaGain.gamma.overrideState = true;
                    liftGammaGain.gain.overrideState = true;
                }
                catch { }

                // Enable the effect only if settings modify it from neutral (using W components)
                bool isModified = Mathf.Abs(gainW) > 0.000001f || Mathf.Abs(gammaW) > 0.000001f;
                liftGammaGain.active = isModified;
            }
        }
    }
}
