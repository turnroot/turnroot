using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Brain.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Utilities.Weather
{
    public enum WeatherType
    {
        Sunny,
        Cloudy,
        Rainy,
        Snowy,
        Stormy,
        Volcanic,
    }

    public class SceneSkyboxSetter : MonoBehaviour
    {
        public Material DefaultSkybox;
        public Material[] SunnySkyboxes;
        public Material[] CloudySkyboxes;
        public Material[] RainySkyboxes;
        public Material[] SnowySkyboxes;
        public Material[] StormySkyboxes;
        public Material[] VolcanicSkyboxes;

        [Header("Scene Lights")]
        public Light DirectionalLight;

        [Header("Scene Particles")]
        public GameObject HeavyRainParticles;
        public GameObject DrizzleParticles;
        public GameObject SnowParticles;
        public GameObject VolcanicAshParticles;
        public bool SnowsHere = true;

        [Header("Water Material & Colors")]
        public Material WaterMaterial;
        public Color WaterShallowSunrise;
        public Color WaterDeepSunrise;
        public Color WaterShallowNoon;
        public Color WaterDeepNoon;

        [Header("Overcast / Midnight Colors")]
        public Color WaterShallowSunriseOvercast;
        public Color WaterDeepSunriseOvercast;
        public Color WaterShallowNoonOvercast;
        public Color WaterDeepNoonOvercast;
        public Color WaterShallowMidnight;
        public Color WaterDeepMidnight;

        [Header("Water Blending")]
        [Range(0, 1)]
        public float WaterColorBlendFactor = 0.5f; // 0 = use original water color, 1 = use these colors, blend in between for values in between

        [Range(0, 1)]
        [Tooltip("Blend specular color toward shallow water color")]
        public float SpecularColorBlend = 0f;

        [Range(0, 1)]
        [Tooltip("Blend fresnel colour toward shallow water color")]
        public float FresnelColorBlend = 0f;

        [Header("Light Intensities")]
        // simple sunny/overcast light intensities
        public float SunnyLightIntensity = 1f;
        public float OvercastLightIntensity = 0.5f;

        [Header("Water Specular")]
        public float SunnySpecularStrength = 0.8f;
        public float OvercastSpecularStrength = 0.4f;

        [Header("Cel Shader Tinting")]
        public Material[] CelMaterials;

        [Range(0, 1)]
        public float CelBlendFactor = 0.0f; // 0 = no tint, 1 = full shallow colour
        private Color[] baseCelLight;
        private Color[] baseCelBaseTint;

        private Color baseShallowColor;
        private Color baseDeepColor;
        private Color baseSpecColor;
        private Color baseFresnelColor;

        public WeatherType CurrentWeatherType = WeatherType.Sunny;

        public WeatherType[] PossibleWeatherTypes;
        private Vector3 DirectionalLightRotation = new Vector3(50f, -30f, 0f);
        private Material currentSkybox;

        [Range(0f, 24f)]
        public float TimeOfDay = 12f;
        private float TimeOfYear = 0f;

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

        public bool ProgressTimeOfDay = true;
        public float DayLength = 600f; // Length of a full day in seconds

        public void SetSkybox(WeatherType weatherType)
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

            if (selectedSkyboxes != null && selectedSkyboxes.Length > 0)
            {
                int randomIndex = Random.Range(0, selectedSkyboxes.Length);
                currentSkybox = selectedSkyboxes[randomIndex];
                RenderSettings.skybox = currentSkybox;
                $"Set skybox to {RenderSettings.skybox.name} for weather {weatherType}".LogInfo();
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
        }

        public void SetActiveParticles(int month)
        {
            if (CurrentWeatherType == WeatherType.Rainy && HeavyRainParticles != null)
            {
                HeavyRainParticles.SetActive(true);
                if (HeavyRainParticles.TryGetComponent<ParticleSystem>(out var ps1))
                {
                    ps1.Play();
                }
            }
            else if (CurrentWeatherType == WeatherType.Cloudy && DrizzleParticles != null)
            {
                DrizzleParticles.SetActive(true);
                if (DrizzleParticles.TryGetComponent<ParticleSystem>(out var ps2))
                {
                    ps2.Play();
                }
            }
            else if (CurrentWeatherType == WeatherType.Snowy && SnowParticles != null)
            {
                SnowParticles.SetActive(true);
                if (SnowParticles.TryGetComponent<ParticleSystem>(out var ps3))
                {
                    ps3.Play();
                }
            }
            else if (CurrentWeatherType == WeatherType.Volcanic && VolcanicAshParticles != null)
            {
                VolcanicAshParticles.SetActive(true);
                if (VolcanicAshParticles.TryGetComponent<ParticleSystem>(out var ps4))
                {
                    ps4.Play();
                }
            }

            if (CurrentWeatherType == WeatherType.Stormy)
            {
                if ((month <= 2 || month >= 10) && SnowsHere && SnowParticles != null)
                {
                    SnowParticles?.SetActive(true);
                    if (SnowParticles.TryGetComponent<ParticleSystem>(out var ps5))
                    {
                        ps5.Play();
                    }
                }
                else if (HeavyRainParticles != null)
                {
                    HeavyRainParticles.SetActive(true);
                    if (HeavyRainParticles.TryGetComponent<ParticleSystem>(out var ps6))
                    {
                        ps6.Play();
                    }
                }
            }
        }

        private Brain _brain;
        private string _lastSceneName;

        private bool _handledSceneChange = false;

        public void Awake()
        {
            if (DirectionalLight != null)
            {
                DirectionalLight.transform.rotation = Quaternion.Euler(
                    ConvertTimeOfDayAndYearToDirectionalLightRotation(TimeOfDay, TimeOfYear)
                );
            }

            if (WaterMaterial != null)
            {
                baseShallowColor = WaterMaterial.GetColor("_ShallowColor");
                baseDeepColor = WaterMaterial.GetColor("_DeepColor");
                baseSpecColor = WaterMaterial.GetColor("_SpecularColor");
                baseFresnelColor = WaterMaterial.GetColor("_FresnelColor");
            }

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
            }

            _brain = FindFirstObjectByType<Brain>();
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
            // restore material colours
            if (WaterMaterial != null)
            {
                WaterMaterial.SetColor("_ShallowColor", baseShallowColor);
                WaterMaterial.SetColor("_DeepColor", baseDeepColor);
                WaterMaterial.SetColor("_SpecularColor", baseSpecColor);
                WaterMaterial.SetColor("_FresnelColor", baseFresnelColor);
            }
            HeavyRainParticles?.SetActive(false);
            DrizzleParticles?.SetActive(false);
            SnowParticles?.SetActive(false);
            VolcanicAshParticles?.SetActive(false);
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

        private void SetupForScene(string sceneName)
        {
            // clear any leftover particles before applying the new weather
            if (HeavyRainParticles != null)
            {
                HeavyRainParticles.SetActive(false);
            }

            if (DrizzleParticles != null)
            {
                DrizzleParticles.SetActive(false);
            }

            if (SnowParticles != null)
            {
                SnowParticles.SetActive(false);
            }

            if (VolcanicAshParticles != null)
            {
                VolcanicAshParticles.SetActive(false);
            }

            // if we already processed this scene once we don't reseed the weather
            bool newScene = sceneName != _lastSceneName;
            if (newScene)
            {
                _lastSceneName = sceneName;
                if (PossibleWeatherTypes.Length > 0)
                {
                    int index = Random.Range(0, PossibleWeatherTypes.Length);
                    CurrentWeatherType = PossibleWeatherTypes[index];
                    $"Selected Weather: {CurrentWeatherType}".LogInfo();
                }
            }

            // orientation may not be correct until light exists but set anyway
            if (DirectionalLight != null)
            {
                DirectionalLight.transform.rotation = Quaternion.Euler(
                    ConvertTimeOfDayAndYearToDirectionalLightRotation(TimeOfDay, TimeOfYear)
                );
            }

            SetSkybox(CurrentWeatherType);

            // always refresh particles after scene change
            var ltm2 = FindFirstObjectByType<LongTermMemory>();
            if (ltm2 != null)
            {
                var gd2 = ltm2.GetGameDate();
                SetActiveParticles(gd2.month);
            }

            // cache base water colors so we don't drift when blending
            if (WaterMaterial != null)
            {
                baseShallowColor = WaterMaterial.GetColor("_ShallowColor");
                baseDeepColor = WaterMaterial.GetColor("_DeepColor");
                baseSpecColor = WaterMaterial.GetColor("_SpecularColor");
                baseFresnelColor = WaterMaterial.GetColor("_FresnelColor");
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
            }
        }

        private void UpdateWaterColors()
        {
            if (WaterMaterial == null)
            {
                return;
            }

            // determine sun height (-1..1)
            float dayProgress = TimeOfDay / 24f;
            float sunHeight = Mathf.Sin(dayProgress * Mathf.PI * 2f);
            bool overcast = CurrentWeatherType != WeatherType.Sunny;

            float tNight = Mathf.SmoothStep(0.0f, 1.0f, -sunHeight); // 0 at horizon, 1 at midnight
            float tDay = Mathf.SmoothStep(0.0f, 1.0f, sunHeight); // 0 at horizon, 1 at noon
            float tSunrise = 1.0f - tNight - tDay;
            tSunrise = Mathf.Clamp01(tSunrise);

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

            WaterMaterial.SetColor("_ShallowColor", finalShallow);
            WaterMaterial.SetColor("_DeepColor", finalDeep);
            float specStrength = overcast ? OvercastSpecularStrength : SunnySpecularStrength;
            WaterMaterial.SetFloat("_SpecularStrength", specStrength);
            Color specNew = Color.Lerp(baseSpecColor, finalShallow, SpecularColorBlend);
            WaterMaterial.SetColor("_SpecularColor", specNew);
            Color fresnelNew = Color.Lerp(baseFresnelColor, finalShallow, FresnelColorBlend);
            WaterMaterial.SetColor("_FresnelColor", fresnelNew);

            // apply shallow tint to any cel materials
            if (CelMaterials != null && baseCelLight != null)
            {
                for (int i = 0; i < CelMaterials.Length; i++)
                {
                    var m = CelMaterials[i];
                    if (m == null)
                    {
                        continue;
                    }

                    Color lightBase = baseCelLight[i];
                    Color lightNew = Color.Lerp(lightBase, finalShallow, CelBlendFactor);
                    m.SetColor("_light", lightNew);

                    if (m.HasProperty("_BaseTint") && baseCelBaseTint != null)
                    {
                        Color tintNew = finalShallow;
                        tintNew.a = CelBlendFactor;
                        m.SetColor("_BaseTint", tintNew);
                    }
                }
            }
        }
    }
}
