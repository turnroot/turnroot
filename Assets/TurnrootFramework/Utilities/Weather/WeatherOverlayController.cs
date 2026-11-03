using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Utilities.Weather
{
    [ExecuteAlways]
    public class WeatherOverlayController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Renderer on the fullscreen quad that displays the weather overlay material.")]
        public Renderer OverlayRenderer;

        [Tooltip("Single shared weather material used by all presets.")]
        public Material SharedWeatherMaterial;

        [Tooltip("If enabled, this component disables the quad renderer when no preset exists.")]
        public bool HideRendererWhenNoPreset = true;

        [Header("Presets")]
        [Tooltip("Preset lookup table. One entry per weather type is recommended.")]
        public WeatherOverlayPreset[] Presets;

        [Header("Inspector Preview")]
        [Tooltip("Select a weather type to preview this preset from the inspector.")]
        [OnValueChanged(nameof(OnPreviewChanged))]
        public WeatherType PreviewPreset = WeatherType.Sunny;

        [Tooltip("Auto-apply the selected preview preset while editing.")]
        public bool AutoPreviewInEditMode = true;

        [Button("Apply Preview Preset")]
        public void ApplyPreviewPreset() => ApplyPreset(PreviewPreset);

        [Button("Rebuild Missing Presets")]
        public void RebuildMissingPresets() => EnsureDefaultPresets();

        [Button("Clear Overlay")]
        public void ClearOverlay()
        {
            if (OverlayRenderer != null)
            {
                OverlayRenderer.enabled = false;
            }
        }

        public bool ApplyPreset(WeatherType weatherType)
        {
            var preset = GetPreset(weatherType);
            if (preset == null)
            {
                if (HideRendererWhenNoPreset && OverlayRenderer != null)
                {
                    OverlayRenderer.enabled = false;
                }

                return false;
            }

            var targetMat = GetTargetMaterial();
            if (targetMat == null)
            {
                return false;
            }

            preset.ApplyToMaterial(targetMat);

            if (OverlayRenderer != null && OverlayRenderer.sharedMaterial != targetMat)
            {
                OverlayRenderer.sharedMaterial = targetMat;
            }

            if (OverlayRenderer != null)
            {
                OverlayRenderer.enabled = true;
            }

            return true;
        }

        public bool HasPreset(WeatherType weatherType) => GetPreset(weatherType) != null;

        private void Awake() => EnsureMaterialBinding();

        private void OnEnable()
        {
            EnsureMaterialBinding();

            if (!Application.isPlaying && AutoPreviewInEditMode)
            {
                ApplyPreviewPreset();
            }
        }

        private void OnValidate()
        {
            EnsureMaterialBinding();
            EnsureDefaultPresets();

            if (!Application.isPlaying && AutoPreviewInEditMode)
            {
                ApplyPreviewPreset();
            }
        }

        private void OnPreviewChanged()
        {
            if (!Application.isPlaying && AutoPreviewInEditMode)
            {
                ApplyPreviewPreset();
            }
        }

        private WeatherOverlayPreset GetPreset(WeatherType weatherType)
        {
            if (Presets == null)
            {
                return null;
            }

            for (int i = 0; i < Presets.Length; i++)
            {
                var preset = Presets[i];
                if (preset != null && preset.WeatherType == weatherType)
                {
                    return preset;
                }
            }

            return null;
        }

        private Material GetTargetMaterial()
        {
            return SharedWeatherMaterial != null
                ? SharedWeatherMaterial
                : OverlayRenderer?.sharedMaterial;
        }

        private void EnsureMaterialBinding()
        {
            if (SharedWeatherMaterial == null && OverlayRenderer != null)
            {
                SharedWeatherMaterial = OverlayRenderer.sharedMaterial;
            }

            if (OverlayRenderer != null && SharedWeatherMaterial != null)
            {
                OverlayRenderer.sharedMaterial = SharedWeatherMaterial;
            }
        }

        private void EnsureDefaultPresets()
        {
            var byType = new Dictionary<WeatherType, WeatherOverlayPreset>();
            if (Presets != null)
            {
                for (int i = 0; i < Presets.Length; i++)
                {
                    var preset = Presets[i];
                    if (preset == null)
                    {
                        continue;
                    }

                    if (!byType.ContainsKey(preset.WeatherType))
                    {
                        byType.Add(preset.WeatherType, preset);
                    }
                }
            }

            var types = (WeatherType[])Enum.GetValues(typeof(WeatherType));
            bool changed = false;
            var rebuilt = new WeatherOverlayPreset[types.Length];

            for (int i = 0; i < types.Length; i++)
            {
                var weatherType = types[i];
                if (!byType.TryGetValue(weatherType, out var preset) || preset == null)
                {
                    preset = WeatherOverlayPreset.CreateDefault(weatherType);
                    changed = true;
                }

                rebuilt[i] = preset;
            }

            if (changed || Presets == null || Presets.Length != rebuilt.Length)
            {
                Presets = rebuilt;
            }
        }
    }
}
