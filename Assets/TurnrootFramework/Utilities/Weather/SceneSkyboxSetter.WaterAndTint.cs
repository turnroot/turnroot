using Turnroot.Gameplay.PlayerSettings;
using UnityEngine;

namespace Turnroot.Utilities.Weather
{
    public partial class SceneSkyboxSetter : MonoBehaviour
    {
        #region Water & Tint

        private Color GetSkyboxAverageColor()
        {
            if (SkyboxCaptureTexture == null)
            {
                return Color.white;
            }

            int qualityStep = GameplayPlayerSettings.Instance?.QualityStep ?? 0;
            if (qualityStep <= 0)
            {
                return Color.white;
            }

            int sampleCount = GetSkyboxSampleCount(qualityStep);
            float cooldown = GetSkyboxSampleCooldown(qualityStep);

            bool shouldResample =
                SkyboxCaptureTexture != _lastSkyboxForSampling
                || Time.time >= _lastSkyboxSampleTime + cooldown;

            if (shouldResample)
            {
                _lastSkyboxForSampling = SkyboxCaptureTexture;
                _lastSkyboxSampleTime = Time.time;
                _previousSkyboxTint = _currentSkyboxTint;
                _currentSkyboxTint = SampleSkyboxAverageColor(SkyboxCaptureTexture, sampleCount);
                _tintLerpStartTime = Time.time;
            }

            float t = Mathf.Clamp01((Time.time - _tintLerpStartTime) / TintLerpDuration);
            return Color.Lerp(_previousSkyboxTint, _currentSkyboxTint, t);
        }

        private Color SampleSkyboxAverageColor(RenderTexture rt, int sampleCount)
        {
            if (rt == null || rt.width <= 0 || rt.height <= 0)
            {
                return Color.white;
            }

            // Reuse the cached texture when dimensions match; recreate only when the RT is resized.
            if (
                _skyboxSampleTexture == null
                || _skyboxSampleTexture.width != rt.width
                || _skyboxSampleTexture.height != rt.height
            )
            {
                if (_skyboxSampleTexture != null)
                {
                    Destroy(_skyboxSampleTexture);
                }

                _skyboxSampleTexture = new Texture2D(
                    rt.width,
                    rt.height,
                    TextureFormat.RGBA32,
                    false
                );
                _skyboxSampleTexture.hideFlags = HideFlags.DontSave;
            }

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            _skyboxSampleTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            // No Apply() needed: ReadPixels populates the CPU buffer directly,
            // and GetPixel reads from the CPU buffer. Apply() would only upload to GPU.

            RenderTexture.active = prev;

            Color sum = Color.black;
            for (int i = 0; i < sampleCount; i++)
            {
                int x = Random.Range(0, rt.width);
                int y = Random.Range(0, rt.height);
                sum += _skyboxSampleTexture.GetPixel(x, y);
            }

            var avg = sum / sampleCount;
            if (avg.maxColorComponent <= 0.01f)
            {
                return Color.white;
            }

            // boost saturation so tint is more noticeable
            Color.RGBToHSV(avg, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * SkyboxTintSaturationBoost);
            return Color.HSVToRGB(h, s, v);
        }

        private int GetSkyboxSampleCount(int qualityStep)
        {
            return qualityStep switch
            {
                3 => 20,
                2 => 10,
                1 => 5,
                _ => 0,
            };
        }

        private float GetSkyboxSampleCooldown(int qualityStep)
        {
            return qualityStep switch
            {
                3 => 0.5f, // fastest updates
                2 => 1.0f,
                1 => 2.0f,
                _ => 3.0f,
            };
        }

        private float _appliedNightFactor = -1f;
        private float _nightLerpStartFactor = -1f;
        private float _nightLerpTargetFactor = -1f;
        private float _nightTintLerpStartTime = -Mathf.Infinity;
        private const float NightTintLerpDuration = 0.5f;
        private const float NightFactorChangeThreshold = 0.02f;

        // Returns a [0,1] curve applied to the raw night factor (0 = dusk/dawn, 1 = midnight).
        // The exponent keeps the tint soft at dusk/dawn and concentrates it near midnight.
        // Intensity exactly reaches NightTintIntensity at rawNightFactor = 1 (midnight).
        private float GetNightBlendFactor(float rawNightFactor) => Mathf.Pow(Mathf.Clamp01(rawNightFactor), NightTintCurveExponent);

        private float GetAppliedNightFactor() => _appliedNightFactor < 0f ? 0f : Mathf.Clamp01(_appliedNightFactor);

        private float GetNightTintIntensityFromFactor(float rawNightFactor) => NightTintIntensity * GetNightBlendFactor(rawNightFactor);

        private void ApplyNightTintToMaterial(Material material, float intensity)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_NightTintColor"))
            {
                material.SetColor("_NightTintColor", NightTintColor);
            }

            if (material.HasProperty("_NightTintIntensity"))
            {
                material.SetFloat("_NightTintIntensity", intensity);
            }
        }

        private void UpdateNightTint(float nightFactor)
        {
            float clampedFactor = Mathf.Clamp01(nightFactor);

            if (_appliedNightFactor < 0f)
            {
                // Initialize to avoid snapping from an uninitialized value.
                _appliedNightFactor = clampedFactor;
                _nightLerpStartFactor = clampedFactor;
                _nightLerpTargetFactor = clampedFactor;
                ApplyNightTintImmediate(GetNightTintIntensityFromFactor(_appliedNightFactor));
                return;
            }

            if (Mathf.Abs(clampedFactor - _nightLerpTargetFactor) < NightFactorChangeThreshold)
            {
                return; // no meaningful change to target
            }

            // Start from the currently applied value so retargeting during a transition is smooth.
            _nightLerpStartFactor = _appliedNightFactor;
            _nightLerpTargetFactor = clampedFactor;
            _nightTintLerpStartTime = Time.time;
        }

        private void ApplyNightTintLerp()
        {
            // Do nothing until initialized.
            if (_appliedNightFactor < 0f)
            {
                return;
            }

            // If target equals applied, nothing to do.
            if (Mathf.Approximately(_appliedNightFactor, _nightLerpTargetFactor))
            {
                return;
            }

            float t = Mathf.Clamp01((Time.time - _nightTintLerpStartTime) / NightTintLerpDuration);
            float newFactor = Mathf.Lerp(_nightLerpStartFactor, _nightLerpTargetFactor, t);

            // If we haven't moved yet, skip the expensive update.
            if (Mathf.Abs(newFactor - _appliedNightFactor) < 0.0001f)
            {
                return;
            }

            // Snap exactly at the end to avoid accumulating tiny residual differences.
            _appliedNightFactor = t >= 1f ? _nightLerpTargetFactor : newFactor;

            ApplyNightTintImmediate(GetNightTintIntensityFromFactor(_appliedNightFactor));
        }

        private void ApplyNightTintImmediate(float intensity)
        {
            ApplyNightTintToMaterial(GrassMaterial, intensity);

            if (GrassExtrasMaterials != null)
            {
                foreach (var mat in GrassExtrasMaterials)
                {
                    ApplyNightTintToMaterial(mat, intensity);
                }
            }

            foreach (var runtimeMat in _celMaterialInstances.Values)
            {
                ApplyNightTintToMaterial(runtimeMat, intensity);
            }
        }

        private void UpdateCelTintIfNeeded(bool force = false)
        {
            int qualityStep = GameplayPlayerSettings.Instance?.QualityStep ?? 0;
            float cooldown = GetCelTintCooldown(qualityStep);

            bool shouldUpdate = force;
            if (!shouldUpdate)
            {
                // If we're in the middle of a skybox tint lerp, keep updating so
                // cel tint can transition smoothly rather than stepping.
                if (Time.time < _tintLerpStartTime + TintLerpDuration)
                {
                    shouldUpdate = true;
                }
                else if (qualityStep != _lastCelTintQualityStep)
                {
                    shouldUpdate = true;
                }
                else if (qualityStep > 0)
                {
                    if (Time.time >= _lastCelTintUpdateTime + cooldown)
                    {
                        shouldUpdate = true;
                    }
                }
            }

            if (!shouldUpdate)
            {
                return;
            }

            bool shouldUpdateBaseTint = qualityStep > 0;
            Color skyTint = shouldUpdateBaseTint ? GetSkyboxAverageColor() : Color.white;

            foreach (var kvp in _celMaterialInstances)
            {
                var originalMat = kvp.Key;
                var runtimeMat = kvp.Value;
                if (runtimeMat == null)
                {
                    continue;
                }

                if (shouldUpdateBaseTint)
                {
                    // Determine the original base tint for this material
                    Color baseTint = Color.white;
                    if (originalMat != null && CelMaterials != null)
                    {
                        int idx = System.Array.IndexOf(CelMaterials, originalMat);
                        if (idx >= 0 && baseCelBaseTint != null && idx < baseCelBaseTint.Length)
                        {
                            baseTint = baseCelBaseTint[idx];
                        }
                    }

                    // Blend hue and saturation from the sky tint but preserve the base tint's brightness (value).
                    Color.RGBToHSV(baseTint, out float baseH, out float baseS, out float baseV);
                    Color.RGBToHSV(skyTint, out float skyH, out float skyS, out float _);
                    float blendedH =
                        Mathf.LerpAngle(baseH * 360f, skyH * 360f, CelBlendFactor) / 360f;
                    float blendedS = Mathf.Lerp(baseS, skyS, CelBlendFactor);
                    Color desiredBaseTint = Color.HSVToRGB(blendedH, blendedS, baseV);
                    desiredBaseTint.a = baseTint.a;

                    if (runtimeMat.HasProperty("_BaseTint"))
                    {
                        runtimeMat.SetColor("_BaseTint", desiredBaseTint);
                    }
                }

                // Apply night tint on the same update (for quality updates and forced updates)
                // Use the same curved night intensity path as grass tint so all night tinting stays in sync.
                float nightIntensity = GetNightTintIntensityFromFactor(GetAppliedNightFactor());
                ApplyNightTintToMaterial(runtimeMat, nightIntensity);
            }

            _lastCelTintUpdateTime = Time.time;
            _lastCelTintQualityStep = qualityStep;
        }

        private float GetCelTintCooldown(int qualityStep)
        {
            return qualityStep switch
            {
                3 => 0.5f,
                2 => 1.0f,
                1 => 2.0f,
                _ => float.MaxValue,
            };
        }

        private Material GetActiveWaterMaterial() => _waterMaterialInstance ?? WaterMaterial;

        // Returns [0, 1]: rises from 0 to 1 between SunriseStartHour and NightEndHour (peak),
        // then falls back to 0 by SunriseEndHour. Triangle curve — no discontinuity.
        // Returns 0 if the sunrise window is degenerate (start >= end).
        private float GetSunriseFactor(float timeOfDay)
        {
            if (SunriseStartHour >= SunriseEndHour)
            {
                return 0f;
            }

            // Rising phase: SunriseStartHour → NightEndHour
            if (timeOfDay >= SunriseStartHour && timeOfDay < NightEndHour)
            {
                return Mathf.InverseLerp(SunriseStartHour, NightEndHour, timeOfDay);
            }

            // Falling phase: NightEndHour → SunriseEndHour
            return timeOfDay >= NightEndHour && timeOfDay < SunriseEndHour ? 1f - Mathf.InverseLerp(NightEndHour, SunriseEndHour, timeOfDay) : 0f;
        }

        private float GetNightFactor(float timeOfDay)
        {
            // We're expecting timeOfDay in [0, 24]. Clamp (but do not wrap) so 24 remains 24.
            timeOfDay = Mathf.Clamp(timeOfDay, 0f, 24f);

            float start = Mathf.Clamp(NightStartHour, 0f, 24f);
            float end = Mathf.Clamp(NightEndHour, 0f, 24f);

            // If start == end, treat as always night
            if (Mathf.Approximately(start, end))
            {
                return 1f;
            }

            bool wrapsMidnight = end < start;

            if (!wrapsMidnight)
            {
                // Straight interval: night is between start and end
                return timeOfDay < start || timeOfDay > end
                    ? 0f
                    : Mathf.InverseLerp(start, end, timeOfDay);
            }

            // Wraps past midnight: use two segments (start..24 and 0..end)
            if (timeOfDay >= start)
            {
                return Mathf.InverseLerp(start, 24f, timeOfDay);
            }
            else if (timeOfDay <= end)
            {
                // Keep 0/24 both mapping to 1 (midnight)
                return 1f - Mathf.InverseLerp(0f, end, timeOfDay);
            }

            return 0f;
        }

        private void UpdateWaterColors()
        {
            var mat = GetActiveWaterMaterial();
            if (mat == null)
            {
                return;
            }

            // Calculate day/night/sunrise blending based on inspector-configured timing.
            float rawNightFactor = GetNightFactor(TimeOfDay);
            float tNight = GetNightBlendFactor(rawNightFactor);
            // 3-way blend: tNight + tSunrise + tDay always sums to 1.
            // Sunrise phase fills the gap between night ending and full day.
            float remaining = 1f - tNight;
            float tSunrise = remaining * GetSunriseFactor(TimeOfDay);
            float tDay = remaining - tSunrise;
            bool overcast = CurrentWeatherType != WeatherType.Sunny;

            // Update night tint based on time of day — only when the factor changes meaningfully.
            if (Mathf.Abs(rawNightFactor - _lastNightFactor) >= 0.01f)
            {
                UpdateNightTint(rawNightFactor);
                _lastNightFactor = rawNightFactor;
            }
            ApplyNightTintLerp();

            Color nightShallow = WaterShallowMidnight;
            Color nightDeep = WaterDeepMidnight;

            Color riseShallow = overcast ? WaterShallowSunriseOvercast : WaterShallowSunrise;
            Color riseDeep = overcast ? WaterDeepSunriseOvercast : WaterDeepSunrise;

            Color noonShallow = overcast ? WaterShallowNoonOvercast : WaterShallowNoon;
            Color noonDeep = overcast ? WaterDeepNoonOvercast : WaterDeepNoon;

            Color targetShallow =
                nightShallow * tNight + riseShallow * tSunrise + noonShallow * tDay;
            Color targetDeep = nightDeep * tNight + riseDeep * tSunrise + noonDeep * tDay;

            Color finalShallow = Color.Lerp(baseShallowColor, targetShallow, WaterColorBlendFactor);
            Color finalDeep = Color.Lerp(baseDeepColor, targetDeep, WaterColorBlendFactor);
            finalShallow.a = baseShallowColor.a;
            finalDeep.a = baseDeepColor.a;

            mat.SetColor("_ShallowColor", finalShallow);
            mat.SetColor("_DeepColor", finalDeep);
            float specStrength = overcast ? OvercastSpecularStrength : SunnySpecularStrength;
            mat.SetFloat("_SpecularStrength", specStrength);
            Color specNew = Color.Lerp(baseSpecColor, finalShallow, SpecularColorBlend);
            mat.SetColor("_SpecularColor", specNew);
            Color fresnelNew = Color.Lerp(baseFresnelColor, finalShallow, FresnelColorBlend);
            mat.SetColor("_FresnelColor", fresnelNew);

            // Periodically update cel material base tint (and night tint) based on quality settings.
            UpdateCelTintIfNeeded();
        }

        #endregion
    }
}
