using System;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Utilities.Weather
{
    [ExecuteAlways]
    public class WeatherOverlayController : MonoBehaviour
    {
        [Serializable]
        public class WeatherOverlayPreset
        {
            [Tooltip("Weather type this preset should be used for.")]
            public WeatherType WeatherType;

            [Tooltip("Material containing tuned shader parameters for this weather type.")]
            public Material PresetMaterial;
        }

        [Header("Target")]
        [Tooltip("Renderer on the fullscreen quad that displays the weather overlay material.")]
        public Renderer OverlayRenderer;

        [Tooltip(
            "Optional explicit runtime material instance. If null, one is created from the renderer material."
        )]
        public Material RuntimeMaterial;

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
        public void ApplyPreviewPreset() => ApplyPreset(PreviewPreset, true);

        [Button("Clear Overlay")]
        public void ClearOverlay()
        {
            if (OverlayRenderer != null)
            {
                OverlayRenderer.enabled = false;
            }
        }

        public bool ApplyPreset(WeatherType weatherType, bool allowSharedMaterialInEditMode = false)
        {
            var preset = GetPreset(weatherType);
            if (preset == null || preset.PresetMaterial == null)
            {
                if (HideRendererWhenNoPreset && OverlayRenderer != null)
                {
                    OverlayRenderer.enabled = false;
                }

                return false;
            }

            var targetMat = GetTargetMaterial(allowSharedMaterialInEditMode);
            if (targetMat == null)
            {
                return false;
            }

            targetMat.CopyPropertiesFromMaterial(preset.PresetMaterial);

            // Keep the intended weather shader if the runtime material has the expected shader.
            if (
                targetMat.shader == null
                || targetMat.shader.name != preset.PresetMaterial.shader.name
            )
            {
                targetMat.shader = preset.PresetMaterial.shader;
            }

            if (OverlayRenderer != null)
            {
                OverlayRenderer.enabled = true;
            }

            return true;
        }

        public bool HasPreset(WeatherType weatherType)
        {
            var preset = GetPreset(weatherType);
            return preset != null && preset.PresetMaterial != null;
        }

        private void Awake()
        {
            EnsureRuntimeMaterial();
        }

        private void OnEnable()
        {
            EnsureRuntimeMaterial();

            if (!Application.isPlaying && AutoPreviewInEditMode)
            {
                ApplyPreviewPreset();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && AutoPreviewInEditMode)
            {
                ApplyPreviewPreset();
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && RuntimeMaterial != null)
            {
                Destroy(RuntimeMaterial);
                RuntimeMaterial = null;
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

        private Material GetTargetMaterial(bool allowSharedMaterialInEditMode)
        {
            if (Application.isPlaying)
            {
                EnsureRuntimeMaterial();
                return RuntimeMaterial;
            }

            if (allowSharedMaterialInEditMode && OverlayRenderer != null)
            {
                return OverlayRenderer.sharedMaterial;
            }

            return RuntimeMaterial;
        }

        private void EnsureRuntimeMaterial()
        {
            if (OverlayRenderer == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                if (RuntimeMaterial == null)
                {
                    var src = OverlayRenderer.sharedMaterial;
                    if (src == null)
                    {
                        return;
                    }

                    RuntimeMaterial = new Material(src) { name = src.name + " (Runtime)" };
                }

                if (OverlayRenderer.sharedMaterial != RuntimeMaterial)
                {
                    OverlayRenderer.sharedMaterial = RuntimeMaterial;
                }
            }
            else if (RuntimeMaterial == null)
            {
                // In edit mode, default to the renderer's shared material for preview.
                RuntimeMaterial = OverlayRenderer.sharedMaterial;
            }
        }
    }
}
