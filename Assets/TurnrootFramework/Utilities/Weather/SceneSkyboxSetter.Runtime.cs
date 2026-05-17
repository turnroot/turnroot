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

        private readonly System.Collections.Generic.Dictionary<
            Renderer,
            Material[]
        > _celRendererOriginalMaterials = new();
        private readonly System.Collections.Generic.Dictionary<
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
            Material[] selectedSkyboxes = SelectSkyboxes(weatherType);

            if (_instantiatedSkyboxMaterial != null)
            {
                Destroy(_instantiatedSkyboxMaterial);
                _instantiatedSkyboxMaterial = null;
            }

            AssignSkybox(selectedSkyboxes, forcedIndex);

            UpdateAmbientAudio();
            ResetEventTimers();
        }

        private Material[] SelectSkyboxes(WeatherType weatherType)
        {
            return weatherType switch
            {
                WeatherType.Sunny => SunnySkyboxes,
                WeatherType.Cloudy => CloudySkyboxes,
                WeatherType.Rainy => RainySkyboxes,
                WeatherType.Snowy => SnowySkyboxes,
                WeatherType.Stormy => StormySkyboxes,
                WeatherType.Volcanic => VolcanicSkyboxes,
                _ => null,
            };
        }

        private void AssignSkybox(Material[] selectedSkyboxes, int? forcedIndex)
        {
            if (selectedSkyboxes != null && selectedSkyboxes.Length > 0)
            {
                int chosenIndex = forcedIndex ?? HubDayRandom.Range(0, selectedSkyboxes.Length);
                _instantiatedSkyboxMaterial = Instantiate(selectedSkyboxes[chosenIndex]);
                currentSkybox = _instantiatedSkyboxMaterial;
                RenderSettings.skybox = currentSkybox;
            }
            else
            {
                currentSkybox = DefaultSkybox;
                RenderSettings.skybox = currentSkybox;
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

        public void SetActiveParticles(int month)
        {
            DeactivateAllParticles();
            ActivateWeatherSpecificParticles(month);
        }

        private void DeactivateAllParticles()
        {
            SetParticlesActive(HeavyRainParticles, false);
            SetParticlesActive(DrizzleParticles, false);
            SetParticlesActive(SnowParticles, false);
            SetParticlesActive(VolcanicAshParticles, false);
        }

        private void ActivateWeatherSpecificParticles(int month)
        {
            if (CurrentWeatherType == WeatherType.Rainy)
            {
                SetParticlesActive(HeavyRainParticles, true);
            }
            else if (CurrentWeatherType == WeatherType.Cloudy)
            {
                SetParticlesActive(DrizzleParticles, true);
            }
            else if (CurrentWeatherType == WeatherType.Snowy)
            {
                SetParticlesActive(SnowParticles, true);
            }
            else if (CurrentWeatherType == WeatherType.Volcanic)
            {
                SetParticlesActive(VolcanicAshParticles, true);
            }
            else if (CurrentWeatherType == WeatherType.Stormy)
            {
                if ((month <= 2 || month >= 10) && SnowsHere)
                {
                    SetParticlesActive(SnowParticles, true);
                }
                else
                {
                    SetParticlesActive(HeavyRainParticles, true);
                }
            }
        }

        public void Awake()
        {
            InitializeTimeOfDay();
            SetupDirectionalLight();
            SetupWaterMaterial();
            SetupAudioSources();
            CacheCelMaterials();

            _brain = FindFirstObjectByType<Brain>();
            if (_brain != null)
            {
                _brain.OnSceneChanged += HandleSceneChanged;
            }
        }

        private void InitializeTimeOfDay()
        {
            if (RandomizeStartTimeOfDay)
            {
                float min = Mathf.Clamp(StartTimeOfDayMin, 0f, 24f);
                float max = Mathf.Clamp(StartTimeOfDayMax, 0f, 24f);
                if (max < min)
                {
                    (min, max) = (max, min);
                }

                TimeOfDay = HubDayRandom.Range(min, max);
            }
        }

        private void SetupDirectionalLight()
        {
            if (DirectionalLight != null)
            {
                DirectionalLight.transform.rotation = Quaternion.Euler(
                    ConvertTimeOfDayAndYearToDirectionalLightRotation(TimeOfDay, TimeOfYear)
                );
            }
        }

        private void SetupWaterMaterial()
        {
            if (WaterMaterial != null)
            {
                _waterMaterialSource = WaterMaterial;
                _waterMaterialInstance = Instantiate(_waterMaterialSource);
            }
        }

        private void SetupAudioSources()
        {
            if (EventAudioSource != null)
            {
                EventAudioSource.spatialBlend = 1f;
            }

            if (AmbientAudioSource != null)
            {
                AmbientAudioSource.loop = true;
            }
        }

        private void CacheCelMaterials()
        {
            if (CelMaterials != null && CelMaterials.Length > 0)
            {
                InstantiateCelMaterialsForRenderers();
            }
        }

        private void SetupForScene(string sceneName)
        {
            ClearParticles();
            InitializeWeather(sceneName);
            SetupDirectionalLight();
            ActivateSceneParticles(sceneName);
            CacheCelMaterials();
        }

        private void ClearParticles()
        {
            SetParticlesActive(HeavyRainParticles, false);
            SetParticlesActive(DrizzleParticles, false);
            SetParticlesActive(SnowParticles, false);
            SetParticlesActive(VolcanicAshParticles, false);
        }

        private void InitializeWeather(string sceneName)
        {
            if (sceneName != _lastSceneName)
            {
                _lastSceneName = sceneName;
                CurrentWeatherType =
                    PossibleWeatherTypes.Length > 0
                        ? PossibleWeatherTypes[HubDayRandom.Range(0, PossibleWeatherTypes.Length)]
                        : WeatherType.Sunny;
            }
        }

        private void ActivateSceneParticles(string sceneName)
        {
            var ltm = FindFirstObjectByType<LongTermMemory>();
            if (ltm != null)
            {
                var gd = ltm.GetGameDate();
                SetActiveParticles(gd.month);
            }
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

        public void SetupForScenePublic(string sceneName) => SetupForScene(sceneName);

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

            SetParticlesActive(HeavyRainParticles, false);
            SetParticlesActive(DrizzleParticles, false);
            SetParticlesActive(SnowParticles, false);
            SetParticlesActive(VolcanicAshParticles, false);
        }
    }
}
