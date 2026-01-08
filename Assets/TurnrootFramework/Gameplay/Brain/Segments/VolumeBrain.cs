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
                    var globalVolume = rootGameObject.GetComponentInChildren<Volume>();
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

            // Apply Brightness and Gamma via Lift Gamma Gain
            if (profile.TryGet<LiftGammaGain>(out var liftGammaGain))
            {
                // Keep existing lift, modify gamma and gain
                var currentLift = liftGammaGain.lift.value;
                var currentGamma = liftGammaGain.gamma.value;
                var currentGain = liftGammaGain.gain.value;

                // Apply gamma and brightness (gain) together
                liftGammaGain.gamma.value = new Vector4(
                    settings.Gamma,
                    settings.Gamma,
                    settings.Gamma,
                    currentGamma.w
                );

                liftGammaGain.gain.value = new Vector4(
                    settings.Brightness - 1f,
                    settings.Brightness - 1f,
                    settings.Brightness - 1f,
                    currentGain.w
                );
            }

#if UNITY_EDITOR
            Debug.Log(
                $"VolumeBrain: Applied graphics settings - Bloom: {settings.Bloom}, DepthOfField: {settings.DepthOfField}, Brightness: {settings.Brightness}, Gamma: {settings.Gamma}, LensFlare: {settings.LensFlare}"
            );
#endif
        }
    }
}
