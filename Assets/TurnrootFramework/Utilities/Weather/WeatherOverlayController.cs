using System;
using System.Collections.Generic;
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
            [Header("Preset")]
            public WeatherType WeatherType;

            [Header("Global")]
            [Range(0f, 1f)]
            public float GlobalOpacity = 1f;

            [Range(0f, 3f)]
            public float Brightness = 1f;

            [Range(0f, 3f)]
            public float Contrast = 1f;
            public Vector4 WorldForwardXZ = new(1f, 0f, 0f, 0f);

            [Range(-180f, 180f)]
            public float GlobalWindAngle = 0f;

            [Header("Parallax")]
            public bool ParallaxEnabled = true;

            [Range(0f, 12f)]
            public float ParallaxAmount = 0.75f;

            [Range(0f, 2f)]
            public float ParallaxYawAmount = 1f;

            [Range(0f, 2f)]
            public float ParallaxPitchAmount = 1f;

            [Range(0f, 4f)]
            public float ParallaxRain = 1.2f;

            [Range(0f, 4f)]
            public float ParallaxDrizzle = 1f;

            [Range(0f, 4f)]
            public float ParallaxSnow = 0.65f;

            [Range(0f, 4f)]
            public float ParallaxAsh = 0.55f;

            [Header("Layer Parallax")]
            [Range(0f, 6f)]
            public float LayerBackParallax = 0.35f;

            [Range(0f, 6f)]
            public float LayerMidParallax = 1f;

            [Range(0f, 6f)]
            public float LayerForeParallax = 2.1f;

            [Header("Layer Density")]
            [Range(0.1f, 4f)]
            public float LayerBackDensity = 1.6f;

            [Range(0.1f, 4f)]
            public float LayerMidDensity = 1f;

            [Range(0.1f, 4f)]
            public float LayerForeDensity = 0.65f;

            [Header("Layer Size")]
            [Range(0.1f, 4f)]
            public float LayerBackSize = 0.7f;

            [Range(0.1f, 4f)]
            public float LayerMidSize = 1f;

            [Range(0.1f, 4f)]
            public float LayerForeSize = 1.45f;

            [Header("Rain")]
            public bool RainEnabled = false;

            [Range(0f, 2f)]
            public float RainIntensity = 1f;

            [Range(0f, 1f)]
            public float RainOpacity = 0.62f;
            public Color RainColor = new(0.24f, 0.46f, 0.88f, 1f);

            [Range(10f, 900f)]
            public float RainDensity = 140f;

            [Range(0f, 20f)]
            public float RainSpeed = 11f;

            [Range(0.0002f, 0.25f)]
            public float RainWidth = 0.04f;

            [Range(0.02f, 0.99f)]
            public float RainLength = 0.86f;

            [Range(0f, 1f)]
            public float RainWidthRandomness = 0.35f;

            [Range(0f, 1f)]
            public float RainLengthRandomness = 0.4f;

            [Range(0.05f, 2f)]
            public float RainStreakTiling = 0.22f;

            [Range(0f, 1f)]
            public float RainFlatBody = 1f;

            [Range(-80f, 80f)]
            public float RainFallAngle = 22f;

            [Range(-1f, 1f)]
            public float RainCameraYawInfluence = 0.45f;

            [Range(0f, 1f)]
            public float RainJitter = 0.35f;

            [Range(0f, 1f)]
            public float RainSpawn = 0.82f;

            [Range(0.0005f, 0.25f)]
            public float RainSoftness = 0.05f;

            [Header("Drizzle")]
            public bool DrizzleEnabled = false;

            [Range(0f, 2f)]
            public float DrizzleIntensity = 0.65f;

            [Range(0f, 1f)]
            public float DrizzleOpacity = 0.4f;
            public Color DrizzleColor = new(0.42f, 0.63f, 0.94f, 1f);

            [Range(10f, 900f)]
            public float DrizzleDensity = 120f;

            [Range(0f, 20f)]
            public float DrizzleSpeed = 4f;

            [Range(0.0002f, 0.2f)]
            public float DrizzleWidth = 0.03f;

            [Range(0.02f, 0.99f)]
            public float DrizzleLength = 0.72f;

            [Range(0f, 1f)]
            public float DrizzleWidthRandomness = 0.25f;

            [Range(0f, 1f)]
            public float DrizzleLengthRandomness = 0.3f;

            [Range(0.05f, 2f)]
            public float DrizzleStreakTiling = 0.28f;

            [Range(0f, 1f)]
            public float DrizzleFlatBody = 1f;

            [Range(-80f, 80f)]
            public float DrizzleFallAngle = 14f;

            [Range(-1f, 1f)]
            public float DrizzleCameraYawInfluence = 0.35f;

            [Range(0f, 1f)]
            public float DrizzleJitter = 0.4f;

            [Range(0f, 1f)]
            public float DrizzleSpawn = 0.68f;

            [Range(0.0005f, 0.25f)]
            public float DrizzleSoftness = 0.045f;

            [Header("Snow")]
            public bool SnowEnabled = false;

            [Range(0f, 2f)]
            public float SnowIntensity = 0.9f;

            [Range(0f, 1f)]
            public float SnowOpacity = 0.8f;
            public Color SnowColor = new(0.93f, 0.96f, 1f, 1f);

            [Range(2f, 250f)]
            public float SnowDensity = 55f;

            [Range(0f, 10f)]
            public float SnowSpeed = 1.3f;

            [Range(0.001f, 0.25f)]
            public float SnowSize = 0.08f;

            [Range(0f, 1f)]
            public float SnowSizeRandomness = 0.65f;

            [Range(0f, 1f)]
            public float SnowDriftAmount = 0.5f;

            [Range(0f, 8f)]
            public float SnowDriftSpeed = 1.4f;

            [Range(-80f, 80f)]
            public float SnowFallAngle = 6f;

            [Range(-1f, 1f)]
            public float SnowCameraYawInfluence = 0.2f;

            [Range(0f, 1f)]
            public float SnowSpawn = 0.86f;

            [Range(0.001f, 1f)]
            public float SnowDotEdgeSoftness = 0.05f;

            [Header("Ash")]
            public bool AshEnabled = false;

            [Range(0f, 2f)]
            public float AshIntensity = 1f;

            [Range(0f, 1f)]
            public float AshOpacity = 0.72f;
            public Color AshColor = new(0.40f, 0.36f, 0.34f, 1f);

            [Range(2f, 250f)]
            public float AshDensity = 95f;

            [Range(0f, 10f)]
            public float AshSpeed = 0.9f;

            [Range(0.001f, 0.25f)]
            public float AshSize = 0.07f;

            [Range(0f, 1f)]
            public float AshSizeRandomness = 0.6f;

            [Range(0f, 1f)]
            public float AshDriftAmount = 0.78f;

            [Range(0f, 8f)]
            public float AshDriftSpeed = 2.2f;

            [Range(-80f, 80f)]
            public float AshFallAngle = 14f;

            [Range(-1f, 1f)]
            public float AshCameraYawInfluence = 0.3f;

            [Range(0f, 1f)]
            public float AshSpawn = 0.72f;

            [Range(0.001f, 1f)]
            public float AshDotEdgeSoftness = 0.07f;

            [Header("Depth Fade")]
            [Range(0f, 1f)]
            public float VerticalFadeTop = 0f;

            [Range(0f, 1f)]
            public float VerticalFadeBottom = 0f;

            [Range(0f, 1f)]
            public float HorizontalFadeLeft = 0f;

            [Range(0f, 1f)]
            public float HorizontalFadeRight = 0f;

            public void ApplyToMaterial(Material material)
            {
                if (material == null)
                {
                    return;
                }

                SetFloat(material, "_GlobalOpacity", GlobalOpacity);
                SetFloat(material, "_Brightness", Brightness);
                SetFloat(material, "_Contrast", Contrast);
                SetVector(material, "_WorldForwardXZ", WorldForwardXZ);
                SetFloat(material, "_GlobalWindAngle", GlobalWindAngle);

                SetFloat(material, "_ParallaxEnabled", ParallaxEnabled ? 1f : 0f);
                SetFloat(material, "_ParallaxAmount", ParallaxAmount);
                SetFloat(material, "_ParallaxYawAmount", ParallaxYawAmount);
                SetFloat(material, "_ParallaxPitchAmount", ParallaxPitchAmount);
                SetFloat(material, "_ParallaxRain", ParallaxRain);
                SetFloat(material, "_ParallaxDrizzle", ParallaxDrizzle);
                SetFloat(material, "_ParallaxSnow", ParallaxSnow);
                SetFloat(material, "_ParallaxAsh", ParallaxAsh);

                SetFloat(material, "_LayerBackParallax", LayerBackParallax);
                SetFloat(material, "_LayerMidParallax", LayerMidParallax);
                SetFloat(material, "_LayerForeParallax", LayerForeParallax);
                SetFloat(material, "_LayerBackDensity", LayerBackDensity);
                SetFloat(material, "_LayerMidDensity", LayerMidDensity);
                SetFloat(material, "_LayerForeDensity", LayerForeDensity);
                SetFloat(material, "_LayerBackSize", LayerBackSize);
                SetFloat(material, "_LayerMidSize", LayerMidSize);
                SetFloat(material, "_LayerForeSize", LayerForeSize);

                SetFloat(material, "_RainEnabled", RainEnabled ? 1f : 0f);
                SetFloat(material, "_RainIntensity", RainIntensity);
                SetFloat(material, "_RainOpacity", RainOpacity);
                SetColor(material, "_RainColor", RainColor);
                SetFloat(material, "_RainDensity", RainDensity);
                SetFloat(material, "_RainSpeed", RainSpeed);
                SetFloat(material, "_RainWidth", RainWidth);
                SetFloat(material, "_RainLength", RainLength);
                SetFloat(material, "_RainWidthRandomness", RainWidthRandomness);
                SetFloat(material, "_RainLengthRandomness", RainLengthRandomness);
                SetFloat(material, "_RainStreakTiling", RainStreakTiling);
                SetFloat(material, "_RainFlatBody", RainFlatBody);
                SetFloat(material, "_RainFallAngle", RainFallAngle);
                SetFloat(material, "_RainCameraYawInfluence", RainCameraYawInfluence);
                SetFloat(material, "_RainJitter", RainJitter);
                SetFloat(material, "_RainSpawn", RainSpawn);
                SetFloat(material, "_RainSoftness", RainSoftness);

                SetFloat(material, "_DrizzleEnabled", DrizzleEnabled ? 1f : 0f);
                SetFloat(material, "_DrizzleIntensity", DrizzleIntensity);
                SetFloat(material, "_DrizzleOpacity", DrizzleOpacity);
                SetColor(material, "_DrizzleColor", DrizzleColor);
                SetFloat(material, "_DrizzleDensity", DrizzleDensity);
                SetFloat(material, "_DrizzleSpeed", DrizzleSpeed);
                SetFloat(material, "_DrizzleWidth", DrizzleWidth);
                SetFloat(material, "_DrizzleLength", DrizzleLength);
                SetFloat(material, "_DrizzleWidthRandomness", DrizzleWidthRandomness);
                SetFloat(material, "_DrizzleLengthRandomness", DrizzleLengthRandomness);
                SetFloat(material, "_DrizzleStreakTiling", DrizzleStreakTiling);
                SetFloat(material, "_DrizzleFlatBody", DrizzleFlatBody);
                SetFloat(material, "_DrizzleFallAngle", DrizzleFallAngle);
                SetFloat(material, "_DrizzleCameraYawInfluence", DrizzleCameraYawInfluence);
                SetFloat(material, "_DrizzleJitter", DrizzleJitter);
                SetFloat(material, "_DrizzleSpawn", DrizzleSpawn);
                SetFloat(material, "_DrizzleSoftness", DrizzleSoftness);

                SetFloat(material, "_SnowEnabled", SnowEnabled ? 1f : 0f);
                SetFloat(material, "_SnowIntensity", SnowIntensity);
                SetFloat(material, "_SnowOpacity", SnowOpacity);
                SetColor(material, "_SnowColor", SnowColor);
                SetFloat(material, "_SnowDensity", SnowDensity);
                SetFloat(material, "_SnowSpeed", SnowSpeed);
                SetFloat(material, "_SnowSize", SnowSize);
                SetFloat(material, "_SnowSizeRandomness", SnowSizeRandomness);
                SetFloat(material, "_SnowDriftAmount", SnowDriftAmount);
                SetFloat(material, "_SnowDriftSpeed", SnowDriftSpeed);
                SetFloat(material, "_SnowFallAngle", SnowFallAngle);
                SetFloat(material, "_SnowCameraYawInfluence", SnowCameraYawInfluence);
                SetFloat(material, "_SnowSpawn", SnowSpawn);
                SetFloat(material, "_SnowDotEdgeSoftness", SnowDotEdgeSoftness);

                SetFloat(material, "_AshEnabled", AshEnabled ? 1f : 0f);
                SetFloat(material, "_AshIntensity", AshIntensity);
                SetFloat(material, "_AshOpacity", AshOpacity);
                SetColor(material, "_AshColor", AshColor);
                SetFloat(material, "_AshDensity", AshDensity);
                SetFloat(material, "_AshSpeed", AshSpeed);
                SetFloat(material, "_AshSize", AshSize);
                SetFloat(material, "_AshSizeRandomness", AshSizeRandomness);
                SetFloat(material, "_AshDriftAmount", AshDriftAmount);
                SetFloat(material, "_AshDriftSpeed", AshDriftSpeed);
                SetFloat(material, "_AshFallAngle", AshFallAngle);
                SetFloat(material, "_AshCameraYawInfluence", AshCameraYawInfluence);
                SetFloat(material, "_AshSpawn", AshSpawn);
                SetFloat(material, "_AshDotEdgeSoftness", AshDotEdgeSoftness);

                SetFloat(material, "_VerticalFadeTop", VerticalFadeTop);
                SetFloat(material, "_VerticalFadeBottom", VerticalFadeBottom);
                SetFloat(material, "_HorizontalFadeLeft", HorizontalFadeLeft);
                SetFloat(material, "_HorizontalFadeRight", HorizontalFadeRight);
            }

            public static WeatherOverlayPreset CreateDefault(WeatherType weatherType)
            {
                var preset = new WeatherOverlayPreset
                {
                    WeatherType = weatherType,
                    RainEnabled = false,
                    DrizzleEnabled = false,
                    SnowEnabled = false,
                    AshEnabled = false,
                };

                switch (weatherType)
                {
                    case WeatherType.Sunny:
                        preset.GlobalOpacity = 0f;
                        break;

                    case WeatherType.Cloudy:
                        preset.DrizzleEnabled = true;
                        preset.DrizzleIntensity = 0.65f;
                        preset.DrizzleOpacity = 0.4f;
                        preset.DrizzleDensity = 120f;
                        preset.DrizzleWidth = 0.03f;
                        preset.DrizzleLength = 0.72f;
                        preset.DrizzleLengthRandomness = 0.35f;
                        preset.DrizzleWidthRandomness = 0.25f;
                        preset.DrizzleStreakTiling = 0.28f;
                        break;

                    case WeatherType.Rainy:
                        preset.RainEnabled = true;
                        preset.RainIntensity = 1.15f;
                        preset.RainOpacity = 0.62f;
                        preset.RainDensity = 140f;
                        preset.RainWidth = 0.04f;
                        preset.RainLength = 0.86f;
                        preset.RainLengthRandomness = 0.45f;
                        preset.RainWidthRandomness = 0.35f;
                        preset.RainStreakTiling = 0.22f;
                        break;

                    case WeatherType.Snowy:
                        preset.SnowEnabled = true;
                        preset.SnowIntensity = 0.9f;
                        preset.SnowOpacity = 0.85f;
                        preset.SnowDensity = 55f;
                        preset.SnowSize = 0.085f;
                        preset.SnowSizeRandomness = 0.8f;
                        break;

                    case WeatherType.Stormy:
                        preset.RainEnabled = true;
                        preset.RainIntensity = 1.5f;
                        preset.RainOpacity = 0.78f;
                        preset.RainSpeed = 14.5f;
                        preset.RainDensity = 180f;
                        preset.RainWidth = 0.06f;
                        preset.RainLength = 0.92f;
                        preset.RainLengthRandomness = 0.5f;
                        preset.RainWidthRandomness = 0.4f;
                        preset.RainStreakTiling = 0.16f;
                        break;

                    case WeatherType.Volcanic:
                        preset.AshEnabled = true;
                        preset.AshIntensity = 1f;
                        preset.AshOpacity = 0.72f;
                        preset.AshDensity = 95f;
                        preset.AshSize = 0.075f;
                        preset.AshSizeRandomness = 0.75f;
                        preset.AshDriftAmount = 0.82f;
                        break;
                }

                return preset;
            }

            private static void SetFloat(Material material, string property, float value)
            {
                if (material.HasProperty(property))
                {
                    material.SetFloat(property, value);
                }
            }

            private static void SetColor(Material material, string property, Color value)
            {
                if (material.HasProperty(property))
                {
                    material.SetColor(property, value);
                }
            }

            private static void SetVector(Material material, string property, Vector4 value)
            {
                if (material.HasProperty(property))
                {
                    material.SetVector(property, value);
                }
            }
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
