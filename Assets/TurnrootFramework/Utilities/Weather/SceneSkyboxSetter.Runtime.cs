using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.NonCombatScenes.Hub;
using UnityEngine;
using UnityEngine.Events;

namespace Turnroot.Utilities.Weather
{
    public partial class SceneSkyboxSetter : MonoBehaviour
    {
        public UnityEvent NightStart;
        public UnityEvent NightEnd;

        private readonly Dictionary<
            Renderer,
            Material[]
        > _celRendererOriginalMaterials = new();
        private readonly Dictionary<
            Material,
            Material
        > _celMaterialInstances = new();

        [Range(0, 1)]
        [Tooltip(
            "Blend factor applied to the cel shaders for soft lighting effects (0 = none, 1 = full)."
        )]
        public float CelBlendFactor = 0.0f; // 0 = no tint, 1 = full shallow colour
        private Color[] baseCelLight;
        private Color[] baseCelBaseTint;

        private Color baseShallowColor = new(.4f, 0.6470588f, 0.7647059f, .1333f);
        private Color baseDeepColor = new(0.05098037f, 0.1803921f, 0.3490196f, 0.9019608f);
        private Color baseSpecColor = new(1f, 0.9743333f, 0.78f, 0.1333333f);
        private Color baseFresnelColor;

        // cached skybox sampling results for tinting
        private Color _currentSkyboxTint = Color.white;
        private Color _previousSkyboxTint = Color.white;
        private float _tintLerpStartTime;
        private const float TintLerpDuration = 0.25f; // smooth transition between samples

        private RenderTexture _lastSkyboxForSampling;
        private float _lastSkyboxSampleTime;

        // Night tint state (for updating grass tint only when night changes meaningfully)
        private float _lastNightFactor = -1f;

        // Cel tint update timing (driven by quality settings and night changes)
        private float _lastCelTintUpdateTime = -Mathf.Infinity;
        private int _lastCelTintQualityStep = -1;

        // We create a runtime instance of the assigned water material so that
        // runtime color tweaks do not modify the source asset.
        private Material _waterMaterialSource;
        private Material _waterMaterialInstance;

        private Vector3 DirectionalLightRotation = new(50f, -30f, 0f);
        private Material currentSkybox;
        private Material _instantiatedSkyboxMaterial;

        private WeatherType _lastAmbientWeatherType = (WeatherType)(-1);

        private Transform _audioListenerTransform;
        private float _nextLightningTime;
        private float _nextVolcanicRumbleTime;
        private float _lightningDuration = 0.3f;

        private float TimeOfYear = 0f;

        private Brain _brain;
        private string _lastSceneName;

        private bool _handledSceneChange = false;

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

        public void SetSkybox(WeatherType weatherType, int? forcedIndex)
        {
            Material[] selectedSkyboxes = null;

            switch (weatherType)
            {
                case WeatherType.Sunny:
                    selectedSkyboxes = SunnySkyboxes;
                    break;
                case WeatherType.Cloudy:
                    selectedSkyboxes = CloudySkyboxes;
                    break;
                case WeatherType.Rainy:
                    selectedSkyboxes = RainySkyboxes;
                    break;
                case WeatherType.Snowy:
                    selectedSkyboxes = SnowySkyboxes;
                    break;
                case WeatherType.Stormy:
                    selectedSkyboxes = StormySkyboxes;
                    break;
                case WeatherType.Volcanic:
                    selectedSkyboxes = VolcanicSkyboxes;
                    break;
                default:
                    RenderSettings.skybox = DefaultSkybox;
                    return;
            }

            if (_instantiatedSkyboxMaterial != null)
            {
                Destroy(_instantiatedSkyboxMaterial);
                _instantiatedSkyboxMaterial = null;
            }

            if (selectedSkyboxes != null && selectedSkyboxes.Length > 0)
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
                    var brain = GetAndCacheBrain.GetBrain();
                    if (brain != null)
                    {
                        HubDayStateStore.SetSkyboxIndex(brain, chosenIndex);
                    }
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

        public void Update()
        {
            if (ProgressTimeOfDay)
            {
                TimeOfDay += (Time.deltaTime / DayLength) * 24f;
                if (TimeOfDay >= 24f)
                {
                    TimeOfDay = 0f;
                }

                if (Mathf.Approximately(TimeOfDay, NightStartHour))
                {
                    NightStart.Invoke();
                }
                else if (Mathf.Approximately(TimeOfDay, NightEndHour))
                {
                    NightEnd.Invoke();
                }
            }

            if (DirectionalLight != null)
            {
                DirectionalLight.transform.rotation = Quaternion.Euler(
                    ConvertTimeOfDayAndYearToDirectionalLightRotation(TimeOfDay, TimeOfYear)
                );

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

            // Update audio / event-driven effects (lightning, volcanic rumble, etc.)
            UpdateAmbientAudio();
            UpdateNightAmbientAudio();
            UpdateEventAudio();
        }

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

        public void Awake()
        {
            if (RandomizeStartTimeOfDay)
            {
                float min = Mathf.Clamp(StartTimeOfDayMin, 0f, 24f);
                float max = Mathf.Clamp(StartTimeOfDayMax, 0f, 24f);
                if (max < min)
                {
                    // Prevent inverted ranges
                    float temp = min;
                    min = max;
                    max = temp;
                }

                TimeOfDay = HubDayRandom.Range(min, max);
            }

            if (DirectionalLight != null)
            {
                DirectionalLight.transform.rotation = Quaternion.Euler(
                    ConvertTimeOfDayAndYearToDirectionalLightRotation(TimeOfDay, TimeOfYear)
                );
            }

            if (WaterMaterial != null)
            {
                // Instantiate the material so runtime tinting does not persist to the source asset.
                _waterMaterialSource = WaterMaterial;
                _waterMaterialInstance = Instantiate(_waterMaterialSource);

                baseShallowColor = _waterMaterialInstance.GetColor("_ShallowColor");
                baseDeepColor = _waterMaterialInstance.GetColor("_DeepColor");
                baseSpecColor = _waterMaterialInstance.GetColor("_SpecularColor");
                baseFresnelColor = _waterMaterialInstance.GetColor("_FresnelColor");
            }

            // Configure audio sources for ambience and 3D event sounds
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

            // Ensure audio starts correctly
            UpdateAmbientAudio();
            ResetEventTimers();

            // set TimeOfYear based on game date
            var ltm = FindFirstObjectByType<LongTermMemory>();
            if (ltm != null)
            {
                var gd = ltm.GetGameDate();
                float mfrac = (gd.month - 1) / 12f;
                float dfrac = (gd.day - 1) / (30f * 12f);
                TimeOfYear = Mathf.Clamp01(mfrac + dfrac);
            }

            // cache cel shader base light & base-tint colours
            if (CelMaterials != null && CelMaterials.Length > 0)
            {
                int len = CelMaterials.Length;
                baseCelLight = new Color[len];
                baseCelBaseTint = new Color[len];
                for (int i = 0; i < len; i++)
                {
                    var m = CelMaterials[i];
                    if (m != null)
                    {
                        baseCelLight[i] = m.HasProperty("_light")
                            ? m.GetColor("_light")
                            : Color.white;
                        baseCelBaseTint[i] = m.HasProperty("_BaseTint")
                            ? m.GetColor("_BaseTint")
                            : Color.white;
                    }
                    else
                    {
                        baseCelLight[i] = Color.white;
                        baseCelBaseTint[i] = Color.white;
                    }
                }

                // Ensure we modify runtime instances rather than the shared assets.
                InstantiateCelMaterialsForRenderers();
            }

            _brain = GetAndCacheBrain.GetBrain();
            if (_brain != null)
            {
                // only the generic change event is needed; it fires after the new
                // scene has been fully established and avoids double-randomisation.
                _brain.OnSceneChanged += HandleSceneChanged;
            }
        }

        public void OnDestroy()
        {
            if (_brain != null)
            {
                _brain.OnSceneChanged -= HandleSceneChanged;
            }

            CurrentWeatherType = WeatherType.Sunny;

            // restore material colours (if we instantiated an instance)
            if (_waterMaterialInstance != null)
            {
                _waterMaterialInstance.SetColor("_ShallowColor", baseShallowColor);
                _waterMaterialInstance.SetColor("_DeepColor", baseDeepColor);
                _waterMaterialInstance.SetColor("_SpecularColor", baseSpecColor);
                _waterMaterialInstance.SetColor("_FresnelColor", baseFresnelColor);

                if (_waterMaterialSource != null)
                {
                    WaterMaterial = _waterMaterialSource;
                }

                Destroy(_waterMaterialInstance);
                _waterMaterialInstance = null;
            }

            RestoreCelMaterials();
        }

        private void HandleSceneChanged(string sceneName, string displayName)
        {
            if (_handledSceneChange)
            {
                return;
            }

            _handledSceneChange = true;
            // unsubscribe since we don't want to run again
            if (_brain != null)
            {
                _brain.OnSceneChanged -= HandleSceneChanged;
            }
            SetupForScene(sceneName);
        }

        public void SetupForScene(string sceneName)
        {
            bool newScene = sceneName != _lastSceneName;
            if (newScene)
            {
                _lastSceneName = sceneName;

                // Prefer persisted weather for this date (so it's the same across play sessions)
                var brain = GetAndCacheBrain.GetBrain();
                if (brain != null && HubDayStateStore.HasWeather)
                {
                    CurrentWeatherType = HubDayStateStore.Weather;
                }
                else if (PossibleWeatherTypes.Length > 0)
                {
                    int index = HubDayRandom.Range(0, PossibleWeatherTypes.Length);
                    CurrentWeatherType = PossibleWeatherTypes[index];

                    if (brain != null)
                    {
                        HubDayStateStore.SetWeather(brain, CurrentWeatherType);
                    }
                }
            }

            if (DirectionalLight != null)
            {
                DirectionalLight.transform.rotation = Quaternion.Euler(
                    ConvertTimeOfDayAndYearToDirectionalLightRotation(TimeOfDay, TimeOfYear)
                );
            }

            SetSkybox(CurrentWeatherType);

            // Always refresh overlay weather after scene change
            var ltm2 = FindFirstObjectByType<LongTermMemory>();
            if (ltm2 != null)
            {
                var gd2 = ltm2.GetGameDate();
                ApplyWeatherOverlayPreset(gd2.month);
            }
            else
            {
                // Fall back to the current season estimate when date memory is unavailable
                int month = Mathf.Clamp(Mathf.FloorToInt(TimeOfYear * 12f) + 1, 1, 12);
                ApplyWeatherOverlayPreset(month);
            }

            var mat = GetActiveWaterMaterial();
            if (mat != null)
            {
                baseShallowColor = mat.GetColor("_ShallowColor");
                baseDeepColor = mat.GetColor("_DeepColor");
                baseSpecColor = mat.GetColor("_SpecularColor");
                baseFresnelColor = mat.GetColor("_FresnelColor");
            }

            // cache cel shader base light & base-tint colours
            if (CelMaterials != null && CelMaterials.Length > 0)
            {
                int len = CelMaterials.Length;
                baseCelLight = new Color[len];
                baseCelBaseTint = new Color[len];
                for (int i = 0; i < len; i++)
                {
                    var m = CelMaterials[i];
                    if (m != null)
                    {
                        baseCelLight[i] = m.HasProperty("_light")
                            ? m.GetColor("_light")
                            : Color.white;
                        baseCelBaseTint[i] = m.HasProperty("_BaseTint")
                            ? m.GetColor("_BaseTint")
                            : Color.white;
                    }
                    else
                    {
                        baseCelLight[i] = Color.white;
                        baseCelBaseTint[i] = Color.white;
                    }
                }

                // Ensure we modify runtime instances rather than the shared assets
                InstantiateCelMaterialsForRenderers();
            }
        }
    }
}
