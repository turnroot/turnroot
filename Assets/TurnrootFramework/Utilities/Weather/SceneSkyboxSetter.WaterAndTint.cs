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

            var prev = RenderTexture.active;
            RenderTexture.active = rt;

            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            int minY = rt.height / 2;
            Color sum = Color.black;
            for (int i = 0; i < sampleCount; i++)
            {
                int x = Random.Range(0, rt.width);
                int y = Random.Range(minY, rt.height);
                sum += tex.GetPixel(x, y);
            }

            RenderTexture.active = prev;
            Destroy(tex);

            var avg = sum / sampleCount;
            if (avg.maxColorComponent <= 0.01f)
            {
                return Color.white;
            }

            // boost saturation so tint is more noticeable
            Color.RGBToHSV(avg, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * SkyboxTintSaturationBoost);
            avg = Color.HSVToRGB(h, s, v);

            return avg;
        }

        private int GetSkyboxSampleCount(int qualityStep)
        {
            switch (qualityStep)
            {
                case 3:
                    return 20;
                case 2:
                    return 10;
                case 1:
                    return 5;
                default:
                    return 0;
            }
        }

        private float GetSkyboxSampleCooldown(int qualityStep)
        {
            switch (qualityStep)
            {
                case 3:
                    return 0.5f; // fastest updates
                case 2:
                    return 1.0f;
                case 1:
                    return 2.0f;
                default:
                    return 3.0f;
            }
        }

        private float _targetNightFactor = -1f;
        private float _appliedNightFactor = -1f;
        private float _nightTintLerpStartTime = -Mathf.Infinity;
        private const float NightTintLerpDuration = 0.5f;

        private void UpdateNightTint(float nightFactor)
        {
            float clampedFactor = Mathf.Clamp01(nightFactor);
            if (Mathf.Abs(clampedFactor - _targetNightFactor) < 0.02f)
            {
                return; // no meaningful change to target
            }

            _targetNightFactor = clampedFactor;
            _nightTintLerpStartTime = Time.time;

            // If this is the first time, initialize applied value to the current target so we don't
            // immediately snap from -1 (uninitialized) to the first target value.
            if (_appliedNightFactor < 0f)
            {
                _appliedNightFactor = _targetNightFactor;
                ApplyNightTintImmediate(_appliedNightFactor);
                return;
            }

            // Ensure we always start lerping from the current applied value.
            // (Do not reset _appliedNightFactor to the target here, or we'd never lerp.)
        }

        private void ApplyNightTintLerp()
        {
            // If we haven't initialized yet, do nothing.
            if (_appliedNightFactor < 0f)
            {
                return;
            }

            // If target equals applied, nothing to do.
            if (Mathf.Approximately(_appliedNightFactor, _targetNightFactor))
            {
                return;
            }

            float t = Mathf.Clamp01((Time.time - _nightTintLerpStartTime) / NightTintLerpDuration);
            float newFactor = Mathf.Lerp(_appliedNightFactor, _targetNightFactor, t);

            // If we haven't moved yet, skip the expensive update.
            if (Mathf.Abs(newFactor - _appliedNightFactor) < 0.0001f)
            {
                return;
            }

            _appliedNightFactor = newFactor;
            ApplyNightTintImmediate(_appliedNightFactor);
        }

        private void ApplyNightTintImmediate(float factor)
        {
            // Use a curved falloff so tint ramps in stronger near midnight and is softer during dusk/dawn.
            float curvedFactor = Mathf.Pow(factor, 2f);
            float intensity = NightTintIntensity * curvedFactor;

            // Grass material (optional): update when night tint changes.
            if (GrassMaterial != null)
            {
                GrassMaterial.SetColor("_NightTintColor", NightTintColor);
                GrassMaterial.SetFloat("_NightTintIntensity", intensity);
            }

            // GrassExtras materials (optional): same night tint.
            if (GrassExtrasMaterials != null)
            {
                foreach (var mat in GrassExtrasMaterials)
                {
                    if (mat != null)
                    {
                        mat.SetColor("_NightTintColor", NightTintColor);
                        mat.SetFloat("_NightTintIntensity", intensity);
                    }
                }
            }

            // Also update cel materials (night tint portion only).
            foreach (var runtimeMat in _celMaterialInstances.Values)
            {
                if (runtimeMat == null)
                {
                    continue;
                }

                if (runtimeMat.HasProperty("_NightTintColor"))
                {
                    runtimeMat.SetColor("_NightTintColor", NightTintColor);
                }
                if (runtimeMat.HasProperty("_NightTintIntensity"))
                {
                    runtimeMat.SetFloat("_NightTintIntensity", intensity);
                }
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
                if (runtimeMat.HasProperty("_NightTintColor"))
                {
                    runtimeMat.SetColor("_NightTintColor", NightTintColor);
                }
                if (runtimeMat.HasProperty("_NightTintIntensity"))
                {
                    // Use the applied night factor so cel and grass tint match
                    float nightIntensity = NightTintIntensity * Mathf.Pow(_appliedNightFactor, 2f);
                    runtimeMat.SetFloat("_NightTintIntensity", nightIntensity);
                }
            }

            _lastCelTintUpdateTime = Time.time;
            _lastCelTintQualityStep = qualityStep;
        }

        private float GetCelTintCooldown(int qualityStep)
        {
            switch (qualityStep)
            {
                case 3:
                    return 0.5f;
                case 2:
                    return 1.0f;
                case 1:
                    return 2.0f;
                default:
                    return float.MaxValue;
            }
        }

        private Material GetActiveWaterMaterial() => _waterMaterialInstance ?? WaterMaterial;

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

            // Calculate day/night blending based on the desired transition times.
            // Night starts at 16:00 and ends at 06:00, with midnight being full night.
            float tNight = GetNightFactor(TimeOfDay);
            float tDay = 1f - tNight;
            float tSunrise = 0f;
            bool overcast = CurrentWeatherType != WeatherType.Sunny;

            // Update night tint based on time of day — only when the factor changes meaningfully.
            if (Mathf.Abs(tNight - _lastNightFactor) >= 0.01f)
            {
                UpdateNightTint(tNight);
                _lastNightFactor = tNight;
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

        private void SetParticlesActive(GameObject[] particleObjects, bool active)
        {
            if (particleObjects == null)
            {
                return;
            }

            foreach (var go in particleObjects)
            {
                if (go == null)
                {
                    continue;
                }

                go.SetActive(active);
                if (active && go.TryGetComponent<ParticleSystem>(out var ps))
                {
                    ps.Play();
                }
            }
        }

        #endregion
    }
}
