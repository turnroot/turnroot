using System.Collections;
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
        private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
        public UnityEvent NightStart;
        public UnityEvent NightEnd;

        private readonly Dictionary<Renderer, Material[]> _celRendererOriginalMaterials = new();
        private readonly Dictionary<Material, Material> _celMaterialInstances = new();

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
        private Texture2D _skyboxSampleTexture;

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

        private string _lastSceneName;

        private bool _handledSceneChange = false;

        public void Update()
        {
            if (ProgressTimeOfDay)
            {
                float prevTimeOfDay = TimeOfDay;
                TimeOfDay += (Time.deltaTime / DayLength) * 24f;
                bool wrapped = TimeOfDay >= 24f;
                if (wrapped)
                {
                    TimeOfDay -= 24f;
                }

                if (HourCrossed(prevTimeOfDay, TimeOfDay, wrapped, NightStartHour))
                {
                    NightStart.Invoke();
                }
                else if (HourCrossed(prevTimeOfDay, TimeOfDay, wrapped, NightEndHour))
                {
                    NightEnd.Invoke();
                }
            }

            UpdateAmbientAudio();
            UpdateNightAmbientAudio();
            UpdateEventAudio();
        }

        public void Awake()
        {
            // one time inits
            RandomizeTimeOfDay();
            ApplyDirectionalLightRotation();
            InitializeWater();
            InitializeAudio();
            ResetEventTimers();
            CacheCelMaterialBaseColors();
            SetTimeOfYear();

            // constant update
            StartCoroutine(UpdateWorld());

            Brain.Instance.OnSceneChanged += HandleSceneChanged;
        }

        public void OnDestroy()
        {
            Brain.Instance.OnSceneChanged -= HandleSceneChanged;

            CurrentWeatherType = WeatherType.Sunny;

            // restore material colours (if we instantiated an instance)
            if (_waterMaterialInstance != null)
            {
                // Restore original material on the renderer before destroying the instance.
                if (WaterRenderer != null && _waterMaterialSource != null)
                {
                    var slots = WaterRenderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == _waterMaterialInstance)
                        {
                            slots[i] = _waterMaterialSource;
                        }
                    }
                    WaterRenderer.sharedMaterials = slots;
                }

                if (_waterMaterialSource != null)
                {
                    WaterMaterial = _waterMaterialSource;
                }

                Destroy(_waterMaterialInstance);
                _waterMaterialInstance = null;
            }

            RestoreCelMaterials();

            if (_skyboxSampleTexture != null)
            {
                Destroy(_skyboxSampleTexture);
                _skyboxSampleTexture = null;
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
            Brain.Instance.OnSceneChanged -= HandleSceneChanged;
            SetupForScene(sceneName);
        }

        public void SetupForScene(string sceneName)
        {
            bool newScene = sceneName != _lastSceneName;
            if (newScene)
            {
                _lastSceneName = sceneName;

                // Make sure the day's state is loaded for the current date before deciding weather
                var brain = Brain.Instance;
                var date = brain.ltm.GetGameDate();
                if (date != GameDate.Default)
                {
                    HubDayStateStore.Initialize(brain, date);
                }

                // Prefer persisted weather for this date (so it's the same across play sessions)
                if (HubDayStateStore.HasWeather)
                {
                    CurrentWeatherType = HubDayStateStore.Weather;
                }
                else if (
                    PossibleWeatherTypes.Length > 0
                    && HubDayStateStore.IsInitializedForDate(date)
                )
                {
                    int index = HubDayRandom.Range(0, PossibleWeatherTypes.Length);
                    CurrentWeatherType = PossibleWeatherTypes[index];
                    HubDayStateStore.SetWeather(brain, CurrentWeatherType);
                }
            }

            ApplyDirectionalLightRotation();
            SetSkybox(
                CurrentWeatherType,
                HubDayStateStore.HasSkyboxIndex ? HubDayStateStore.SkyboxIndex : null
            );

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

            CacheWaterBaseColors();
            CacheCelMaterialBaseColors();
        }
    }
}
