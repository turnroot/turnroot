using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Utilities.Weather
{
    [ExecuteAlways]
    public class WeatherOverlayController : MonoBehaviour
    {
        public enum ShaderValueType
        {
            Float,
            Color,
            Vector,
        }

        [Serializable]
        public class ShaderPropertyOverride
        {
            [Tooltip("Shader property name, e.g. _RainEnabled")]
            public string Property = "_RainEnabled";

            [Tooltip("Value type for this shader property.")]
            public ShaderValueType ValueType = ShaderValueType.Float;

            [Tooltip("Float value when ValueType is Float.")]
            public float FloatValue;

            [Tooltip("Color value when ValueType is Color.")]
            public Color ColorValue = Color.white;

            [Tooltip("Vector value when ValueType is Vector.")]
            public Vector4 VectorValue;
        }

        [Serializable]
        public class WeatherOverlayPreset
        {
            [Tooltip("Weather type this preset should be used for.")]
            public WeatherType WeatherType;

            [Tooltip("Property overrides applied to the shared weather material.")]
            public ShaderPropertyOverride[] Overrides;
        }

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

            ApplyOverrides(targetMat, preset);

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

        public bool HasPreset(WeatherType weatherType)
        {
            var preset = GetPreset(weatherType);
            return preset != null;
        }

        private void Awake()
        {
            EnsureMaterialBinding();
        }

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

        private static void SetFloatOverride(
            List<ShaderPropertyOverride> list,
            string property,
            float value
        )
        {
            list.Add(
                new ShaderPropertyOverride
                {
                    Property = property,
                    ValueType = ShaderValueType.Float,
                    FloatValue = value,
                }
            );
        }

        private static void SetColorOverride(
            List<ShaderPropertyOverride> list,
            string property,
            Color value
        )
        {
            list.Add(
                new ShaderPropertyOverride
                {
                    Property = property,
                    ValueType = ShaderValueType.Color,
                    ColorValue = value,
                }
            );
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
            if (SharedWeatherMaterial != null)
            {
                return SharedWeatherMaterial;
            }

            if (OverlayRenderer != null)
            {
                return OverlayRenderer.sharedMaterial;
            }

            return null;
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

        private void ApplyOverrides(Material targetMat, WeatherOverlayPreset preset)
        {
            if (targetMat == null || preset == null || preset.Overrides == null)
            {
                return;
            }

            for (int i = 0; i < preset.Overrides.Length; i++)
            {
                var ov = preset.Overrides[i];
                if (ov == null || string.IsNullOrWhiteSpace(ov.Property))
                {
                    continue;
                }

                if (!targetMat.HasProperty(ov.Property))
                {
                    continue;
                }

                switch (ov.ValueType)
                {
                    case ShaderValueType.Float:
                        targetMat.SetFloat(ov.Property, ov.FloatValue);
                        break;
                    case ShaderValueType.Color:
                        targetMat.SetColor(ov.Property, ov.ColorValue);
                        break;
                    case ShaderValueType.Vector:
                        targetMat.SetVector(ov.Property, ov.VectorValue);
                        break;
                }
            }
        }

        private void EnsureDefaultPresets()
        {
            var byType = new Dictionary<WeatherType, WeatherOverlayPreset>();
            if (Presets != null)
            {
                for (int i = 0; i < Presets.Length; i++)
                {
                    var p = Presets[i];
                    if (p == null)
                    {
                        continue;
                    }

                    if (!byType.ContainsKey(p.WeatherType))
                    {
                        byType.Add(p.WeatherType, p);
                    }
                }
            }

            var types = (WeatherType[])Enum.GetValues(typeof(WeatherType));
            bool changed = false;

            for (int i = 0; i < types.Length; i++)
            {
                var weatherType = types[i];
                if (!byType.TryGetValue(weatherType, out var preset) || preset == null)
                {
                    byType[weatherType] = CreateTestPreset(weatherType);
                    changed = true;
                    continue;
                }

                if (preset.Overrides == null || preset.Overrides.Length == 0)
                {
                    preset.Overrides = CreateTestPreset(weatherType).Overrides;
                    changed = true;
                }
            }

            if (changed || Presets == null || Presets.Length != types.Length)
            {
                var rebuilt = new WeatherOverlayPreset[types.Length];
                for (int i = 0; i < types.Length; i++)
                {
                    rebuilt[i] = byType[types[i]];
                }

                Presets = rebuilt;
            }
        }

        private WeatherOverlayPreset CreateTestPreset(WeatherType weatherType)
        {
            var overrides = new List<ShaderPropertyOverride>();

            SetFloatOverride(overrides, "_GlobalOpacity", 1f);
            SetFloatOverride(overrides, "_ParallaxEnabled", 1f);
            SetFloatOverride(overrides, "_ParallaxAmount", 0.08f);

            // Disable all weather channels first.
            SetFloatOverride(overrides, "_RainEnabled", 0f);
            SetFloatOverride(overrides, "_DrizzleEnabled", 0f);
            SetFloatOverride(overrides, "_SnowEnabled", 0f);
            SetFloatOverride(overrides, "_AshEnabled", 0f);
            SetFloatOverride(overrides, "_FogEnabled", 0f);

            // Stylized flat palette.
            SetColorOverride(overrides, "_RainColor", new Color(0.24f, 0.46f, 0.88f, 1f));
            SetColorOverride(overrides, "_DrizzleColor", new Color(0.42f, 0.63f, 0.94f, 1f));
            SetColorOverride(overrides, "_FogColor", new Color(0.70f, 0.78f, 0.84f, 1f));
            SetColorOverride(overrides, "_SnowColor", new Color(0.93f, 0.96f, 1.0f, 1f));
            SetColorOverride(overrides, "_AshColor", new Color(0.40f, 0.36f, 0.34f, 1f));

            // Stylized shape defaults.
            SetFloatOverride(overrides, "_RainFlatBody", 1f);
            SetFloatOverride(overrides, "_DrizzleFlatBody", 1f);
            SetFloatOverride(overrides, "_SnowDotEdgeSoftness", 0.06f);
            SetFloatOverride(overrides, "_AshDotEdgeSoftness", 0.08f);
            SetFloatOverride(overrides, "_FogBrushEnabled", 1f);
            SetFloatOverride(overrides, "_FogBrushAngle", 22f);
            SetFloatOverride(overrides, "_FogBrushDensity", 20f);
            SetFloatOverride(overrides, "_FogBrushWidth", 0.24f);
            SetFloatOverride(overrides, "_FogBrushJitter", 0.75f);
            SetFloatOverride(overrides, "_FogBrushBreakup", 0.45f);
            SetFloatOverride(overrides, "_FogBrushSoftness", 0.08f);

            switch (weatherType)
            {
                case WeatherType.Sunny:
                    SetFloatOverride(overrides, "_GlobalOpacity", 0f);
                    break;

                case WeatherType.Cloudy:
                    SetFloatOverride(overrides, "_DrizzleEnabled", 1f);
                    SetFloatOverride(overrides, "_DrizzleIntensity", 0.65f);
                    SetFloatOverride(overrides, "_DrizzleOpacity", 0.4f);
                    SetFloatOverride(overrides, "_DrizzleDensity", 120f);
                    SetFloatOverride(overrides, "_DrizzleWidth", 0.03f);
                    SetFloatOverride(overrides, "_DrizzleLength", 0.72f);
                    SetFloatOverride(overrides, "_DrizzleStreakTiling", 0.28f);
                    SetFloatOverride(overrides, "_DrizzleSoftness", 0.045f);
                    SetFloatOverride(overrides, "_DrizzleFallAngle", 14f);
                    SetFloatOverride(overrides, "_FogEnabled", 1f);
                    SetFloatOverride(overrides, "_FogIntensity", 0.24f);
                    SetFloatOverride(overrides, "_FogOpacity", 0.24f);
                    break;

                case WeatherType.Rainy:
                    SetFloatOverride(overrides, "_RainEnabled", 1f);
                    SetFloatOverride(overrides, "_RainIntensity", 1.15f);
                    SetFloatOverride(overrides, "_RainOpacity", 0.62f);
                    SetFloatOverride(overrides, "_RainSpeed", 11f);
                    SetFloatOverride(overrides, "_RainDensity", 140f);
                    SetFloatOverride(overrides, "_RainWidth", 0.04f);
                    SetFloatOverride(overrides, "_RainLength", 0.86f);
                    SetFloatOverride(overrides, "_RainStreakTiling", 0.22f);
                    SetFloatOverride(overrides, "_RainSoftness", 0.05f);
                    SetFloatOverride(overrides, "_RainFallAngle", 22f);
                    break;

                case WeatherType.Snowy:
                    SetFloatOverride(overrides, "_SnowEnabled", 1f);
                    SetFloatOverride(overrides, "_SnowIntensity", 0.9f);
                    SetFloatOverride(overrides, "_SnowOpacity", 0.8f);
                    SetFloatOverride(overrides, "_SnowDensity", 55f);
                    SetFloatOverride(overrides, "_SnowDriftAmount", 0.5f);
                    SetFloatOverride(overrides, "_SnowFallAngle", 6f);
                    SetFloatOverride(overrides, "_SnowDotEdgeSoftness", 0.05f);
                    break;

                case WeatherType.Stormy:
                    SetFloatOverride(overrides, "_RainEnabled", 1f);
                    SetFloatOverride(overrides, "_RainIntensity", 1.5f);
                    SetFloatOverride(overrides, "_RainOpacity", 0.78f);
                    SetFloatOverride(overrides, "_RainSpeed", 14.5f);
                    SetFloatOverride(overrides, "_RainDensity", 180f);
                    SetFloatOverride(overrides, "_RainWidth", 0.06f);
                    SetFloatOverride(overrides, "_RainLength", 0.92f);
                    SetFloatOverride(overrides, "_RainStreakTiling", 0.16f);
                    SetFloatOverride(overrides, "_RainSoftness", 0.065f);
                    SetFloatOverride(overrides, "_RainFallAngle", 30f);
                    SetFloatOverride(overrides, "_FogEnabled", 1f);
                    SetFloatOverride(overrides, "_FogIntensity", 0.33f);
                    SetFloatOverride(overrides, "_FogOpacity", 0.24f);
                    break;

                case WeatherType.Volcanic:
                    SetFloatOverride(overrides, "_AshEnabled", 1f);
                    SetFloatOverride(overrides, "_AshIntensity", 1f);
                    SetFloatOverride(overrides, "_AshOpacity", 0.72f);
                    SetFloatOverride(overrides, "_AshDensity", 95f);
                    SetFloatOverride(overrides, "_AshDriftAmount", 0.78f);
                    SetFloatOverride(overrides, "_AshFallAngle", 14f);
                    SetFloatOverride(overrides, "_AshDotEdgeSoftness", 0.07f);
                    SetFloatOverride(overrides, "_FogEnabled", 1f);
                    SetFloatOverride(overrides, "_FogIntensity", 0.4f);
                    SetFloatOverride(overrides, "_FogOpacity", 0.3f);
                    SetFloatOverride(overrides, "_FogBrushDensity", 16f);
                    SetFloatOverride(overrides, "_FogBrushWidth", 0.28f);
                    break;
            }

            return new WeatherOverlayPreset
            {
                WeatherType = weatherType,
                Overrides = overrides.ToArray(),
            };
        }
    }
}
