using System.Collections;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.NonCombatScenes.Hub;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Utilities.Weather
{
    public partial class SceneSkyboxSetter : MonoBehaviour
    {
        public Vector3 ConvertTimeOfDayAndYearToDirectionalLightRotation(
            float timeOfDay,
            float timeOfYear
        )
        {
            // Calculate the sun's position based on time of day and time of year
            float dayProgress = timeOfDay / 24f; // Normalize to 0-1
            float yearProgress = timeOfYear; // Already normalized to 0-1

            // Calculate the sun's angle in the sky
            float sunAngle = dayProgress * 360f; // Full rotation over a day
            // seasonalTilt varies between -23.5 and +23.5 degrees over the year
            float seasonalTilt = Mathf.Sin(yearProgress * Mathf.PI * 2f) * 23.5f;

            // Apply tilt to both X and Y axes so the light not only rises/sets
            // but also shifts along the horizon with the seasons
            float xRot = sunAngle;
            float yRot = DirectionalLightRotation.y + seasonalTilt;

            return new Vector3(xRot, yRot, DirectionalLightRotation.z);
        }

        public void SetSkybox(WeatherType weatherType) => SetSkybox(weatherType, null);

        private Material[] GetSkyboxesForWeather(WeatherType weatherType) =>
            weatherType switch
            {
                WeatherType.Sunny => SunnySkyboxes,
                WeatherType.Cloudy => CloudySkyboxes,
                WeatherType.Rainy => RainySkyboxes,
                WeatherType.Snowy => SnowySkyboxes,
                WeatherType.Stormy => StormySkyboxes,
                WeatherType.Volcanic => VolcanicSkyboxes,
                _ => null,
            };

        private void ApplyWeatherOverlayPreset(int month)
        {
            if (WeatherOverlayController == null)
            {
                return;
            }

            WeatherType overlayWeather = CurrentWeatherType;
            if (CurrentWeatherType == WeatherType.Stormy)
            {
                bool snowStorm = (month <= 2 || month >= 10) && SnowsHere;
                overlayWeather = snowStorm ? WeatherType.Snowy : WeatherType.Rainy;
            }

            WeatherOverlayController.ApplyPreset(overlayWeather);
        }

        private void ApplyDirectionalLightRotation()
        {
            if (DirectionalLight == null)
            {
                return;
            }

            DirectionalLight.transform.rotation = Quaternion.Euler(
                ConvertTimeOfDayAndYearToDirectionalLightRotation(TimeOfDay, TimeOfYear)
            );
        }

        private void CacheWaterBaseColors()
        {
            var mat = GetActiveWaterMaterial();
            if (mat == null)
            {
                return;
            }

            baseShallowColor = mat.GetColor("_ShallowColor");
            baseDeepColor = mat.GetColor("_DeepColor");
            baseSpecColor = mat.GetColor("_SpecularColor");
            baseFresnelColor = mat.GetColor("_FresnelColor");
        }

        private void CacheCelMaterialBaseColors()
        {
            if (CelMaterials == null || CelMaterials.Length == 0)
            {
                return;
            }

            int len = CelMaterials.Length;
            baseCelLight = new Color[len];
            baseCelBaseTint = new Color[len];
            for (int i = 0; i < len; i++)
            {
                var m = CelMaterials[i];
                baseCelLight[i] =
                    m != null && m.HasProperty("_light") ? m.GetColor("_light") : Color.white;
                baseCelBaseTint[i] =
                    m != null && m.HasProperty("_BaseTint") ? m.GetColor("_BaseTint") : Color.white;
            }

            InstantiateCelMaterialsForRenderers();
        }

        private void InitializeWater()
        {
            if (WaterMaterial != null)
            {
                // Instantiate the material so runtime tinting does not persist to the source asset.
                _waterMaterialSource = WaterMaterial;
                _waterMaterialInstance = Instantiate(_waterMaterialSource);
                _waterMaterialInstance.hideFlags = HideFlags.DontSave;
                CacheWaterBaseColors();

                // Swap the material slot on the renderer so SetColor calls are visible in-scene.
                if (WaterRenderer != null)
                {
                    var slots = WaterRenderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == _waterMaterialSource)
                        {
                            slots[i] = _waterMaterialInstance;
                        }
                    }
                    WaterRenderer.sharedMaterials = slots;
                }
            }
        }

        private void RandomizeTimeOfDay()
        {
            if (RandomizeStartTimeOfDay)
            {
                float min = Mathf.Clamp(StartTimeOfDayMin, 0f, 24f);
                float max = Mathf.Clamp(StartTimeOfDayMax, 0f, 24f);
                if (max < min)
                {
                    float temp = min;
                    min = max;
                    max = temp;
                }

                TimeOfDay = HubDayRandom.Range(min, max);
            }
        }

        private void InitializeAudio()
        {
            _audioListenerTransform =
                FindFirstObjectByType<AudioListener>()?.transform ?? Camera.main?.transform;
            if (EventAudioSource != null)
            {
                EventAudioSource.spatialBlend = 1f;
                EventAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;

                // Use a min/max distance so thunder can sound both close and distant.
                float minDist = EventSoundMinDistance;
                float maxDist = EventSoundMaxDistance;

                minDist = Mathf.Max(0.01f, minDist);
                maxDist = Mathf.Max(minDist, maxDist);

                EventAudioSource.minDistance = minDist;
                EventAudioSource.maxDistance = maxDist;
            }

            if (AmbientAudioSource != null)
            {
                AmbientAudioSource.loop = true;
            }

            UpdateAmbientAudio();
        }

        private void SetTimeOfYear()
        {
            var ltm = Brain.Instance?.ltm;

            var gd = ltm.GetGameDate();
            float mfrac = (gd.month - 1) / 12f;
            float dfrac = (gd.day - 1) / (30f * 12f);
            TimeOfYear = Mathf.Clamp01(mfrac + dfrac);
        }

        public void SetSkybox(WeatherType weatherType, int? forcedIndex)
        {
            var selectedSkyboxes = GetSkyboxesForWeather(weatherType);
            if (selectedSkyboxes == null)
            {
                currentSkybox = DefaultSkybox;
                RenderSettings.skybox = currentSkybox;
                return;
            }

            if (_instantiatedSkyboxMaterial != null)
            {
                Destroy(_instantiatedSkyboxMaterial);
                _instantiatedSkyboxMaterial = null;
            }

            if (selectedSkyboxes.Length > 0)
            {
                int chosenIndex = -1;

                // Prefer a forced index (from saved state) if valid
                if (
                    forcedIndex != null
                    && forcedIndex >= 0
                    && forcedIndex < selectedSkyboxes.Length
                )
                {
                    chosenIndex = forcedIndex.Value;
                }
                else
                {
                    chosenIndex = HubDayRandom.Range(0, selectedSkyboxes.Length);

                    // Persist the chosen skybox index so it stays consistent across exits
                    HubDayStateStore.SetSkyboxIndex(Brain.Instance, chosenIndex);
                }

                _instantiatedSkyboxMaterial = Instantiate(selectedSkyboxes[chosenIndex]);
                currentSkybox = _instantiatedSkyboxMaterial;
                RenderSettings.skybox = currentSkybox;
            }
            else
            {
                currentSkybox = DefaultSkybox;
                RenderSettings.skybox = currentSkybox;
            }

            ApplyLightningSpriteSettings(currentSkybox);

            // Refresh audio / event timing when skybox changes
            UpdateAmbientAudio();
            ResetEventTimers();
        }

        private void ApplyLightningSpriteSettings(Material mat)
        {
            if (mat == null)
            {
                return;
            }

            if (LightningSpriteAtlas != null && mat.HasProperty("_LightningSpriteAtlas"))
            {
                mat.SetTexture("_LightningSpriteAtlas", LightningSpriteAtlas.texture);
            }

            if (mat.HasProperty("_LightningSpriteSize"))
            {
                mat.SetFloat("_LightningSpriteSize", LightningSpriteSize);
            }

            if (mat.HasProperty("_LightningGridCount"))
            {
                mat.SetFloat("_LightningGridCount", LightningGridCount);
            }
        }

        // Returns true when time advances from 'from' to 'to' (with optional midnight wrap) and crosses targetHour.
        private static bool HourCrossed(float from, float to, bool wrapped, float targetHour)
        {
            if (!wrapped)
            {
                return from < targetHour && targetHour <= to;
            }
            // Wrapped: effective range is [from, 24) ∪ [0, to]
            return targetHour > from || targetHour <= to;
        }

        private IEnumerator UpdateWorld()
        {
            if (ProgressTimeOfDay)
            {
                ApplyDirectionalLightRotation();
                if (DirectionalLight != null)
                {
                    bool overcast = CurrentWeatherType != WeatherType.Sunny;
                    DirectionalLight.intensity = overcast
                        ? OvercastLightIntensity
                        : SunnyLightIntensity;
                }

                // keep skybox set in case lighting settings asset overrides it
                if (currentSkybox != null && RenderSettings.skybox != currentSkybox)
                {
                    RenderSettings.skybox = currentSkybox;
                }

                UpdateWaterColors();
            }

            yield return _waitForSeconds0_5;
        }
    }
}
