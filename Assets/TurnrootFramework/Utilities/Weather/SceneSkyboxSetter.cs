using NaughtyAttributes;
using UnityEngine;

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

    public partial class SceneSkyboxSetter : MonoBehaviour
    {
        [HorizontalLine(2, EColor.Blue)]
        [Tooltip("Default skybox material used when no weather-specific skybox is available.")]
        public Material DefaultSkybox;

        [BoxGroup("Skyboxes")]
        [Tooltip("Skyboxes used when the weather is Sunny.")]
        public Material[] SunnySkyboxes;

        [BoxGroup("Skyboxes")]
        [Tooltip("Skyboxes used when the weather is Cloudy.")]
        public Material[] CloudySkyboxes;

        [BoxGroup("Skyboxes")]
        [Tooltip("Skyboxes used when the weather is Rainy.")]
        public Material[] RainySkyboxes;

        [BoxGroup("Skyboxes")]
        [Tooltip("Skyboxes used when the weather is Snowy.")]
        public Material[] SnowySkyboxes;

        [BoxGroup("Skyboxes")]
        [Tooltip("Skyboxes used when the weather is Stormy.")]
        public Material[] StormySkyboxes;

        [BoxGroup("Skyboxes")]
        [Tooltip("Skyboxes used when the weather is Volcanic.")]
        public Material[] VolcanicSkyboxes;

        [HorizontalLine(2, EColor.Green)]
        [Header("Skybox Capture")]
        [Tooltip(
            "RenderTexture used for sampling the sky color (assign a camera's Target Texture)"
        )]
        public RenderTexture SkyboxCaptureTexture;

        [HorizontalLine(2, EColor.Orange)]
        [Header("Skybox Tinting")]
        [BoxGroup("Skybox Tinting")]
        [Tooltip("Multiplier applied to sampled sky saturation before using it as a tint")]
        [Range(0, 4)]
        public float SkyboxTintSaturationBoost = 2f;

        [BoxGroup("Skybox Tinting")]
        [Tooltip("Night tint color applied during evening/night")]
        public Color NightTintColor = new Color(0.1f, 0.13f, 0.25f, 1f);

        [BoxGroup("Skybox Tinting")]
        [Tooltip("Strength of the night tint (0 = off, 1 = full)")]
        [Range(0, 1)]
        public float NightTintIntensity = 0.5f;

        [HorizontalLine(2, EColor.Blue)]
        [Header("Night Timing")]
        [BoxGroup("Night Timing")]
        [Tooltip(
            "Hour at which night begins (0-24). This is the point where the night tint begins rising."
        )]
        [Range(0, 24)]
        public float NightStartHour = 16f;

        [BoxGroup("Night Timing")]
        [Tooltip(
            "Hour at which night ends (0-24). This is the point where the night tint reaches zero again."
        )]
        [Range(0, 24)]
        public float NightEndHour = 6f;

        [HorizontalLine(2, EColor.Orange)]
        [Header("Scene Lights")]
        [BoxGroup("Scene Lights")]
        [Tooltip("Directional light used for the sky / sun.")]
        public Light DirectionalLight;

        [HorizontalLine(2, EColor.Yellow)]
        [Header("Screen Space Weather Overlay")]
        [BoxGroup("Weather Overlay")]
        [Tooltip("Optional weather overlay controller on a fullscreen quad.")]
        public WeatherOverlayController WeatherOverlayController;

        [BoxGroup("Weather Overlay")]
        [Tooltip("Whether snow should be used for stormy conditions during cold months.")]
        public bool SnowsHere = true;

        [HorizontalLine(2, EColor.Green)]
        [Header("Water Material & Colors")]
        [BoxGroup("Water")]
        [Tooltip("Water material used for tinting based on sky conditions.")]
        public Material WaterMaterial;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to shallow water at sunrise.")]
        public Color WaterShallowSunrise;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to deep water at sunrise.")]
        public Color WaterDeepSunrise;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to shallow water at noon.")]
        public Color WaterShallowNoon;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to deep water at noon.")]
        public Color WaterDeepNoon;

        [HorizontalLine(2, EColor.Pink)]
        [Header("Overcast / Midnight Colors")]
        [BoxGroup("Water")]
        [Tooltip("Tint applied to shallow water at sunrise when overcast.")]
        public Color WaterShallowSunriseOvercast;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to deep water at sunrise when overcast.")]
        public Color WaterDeepSunriseOvercast;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to shallow water at noon when overcast.")]
        public Color WaterShallowNoonOvercast;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to deep water at noon when overcast.")]
        public Color WaterDeepNoonOvercast;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to shallow water at midnight.")]
        public Color WaterShallowMidnight;

        [BoxGroup("Water")]
        [Tooltip("Tint applied to deep water at midnight.")]
        public Color WaterDeepMidnight;

        [HorizontalLine(2, EColor.Green)]
        [Header("Water Blending")]
        [BoxGroup("Water")]
        [Range(0, 1)]
        [Tooltip("Blend between original water colors and the configured sky-based colors.")]
        public float WaterColorBlendFactor = 0.5f; // 0 = use original water color, 1 = use these colors, blend in between for values in between

        [BoxGroup("Water")]
        [Range(0, 1)]
        [Tooltip("Blend specular color toward shallow water color")]
        public float SpecularColorBlend = 0f;

        [BoxGroup("Water")]
        [Range(0, 1)]
        [Tooltip("Blend fresnel colour toward shallow water color")]
        public float FresnelColorBlend = 0f;

        [HorizontalLine(2, EColor.Pink)]
        [Header("Audio")]
        [BoxGroup("Audio")]
        [Tooltip("Ambient audio source (looping ambience, rain, wind, etc.)")]
        public AudioSource AmbientAudioSource;

        [BoxGroup("Audio")]
        [Tooltip("3D audio source for singular events (lightning strikes, volcanic rumbles, etc.)")]
        public AudioSource EventAudioSource;

        [Header("Ambient Audio Clips")]
        [BoxGroup("Ambient Audio Clips")]
        [Tooltip("Looping ambient clips for sunny weather.")]
        public AudioClip[] SunnyAmbientClips;

        [BoxGroup("Ambient Audio Clips")]
        [Tooltip("Looping ambient clips for cloudy weather.")]
        public AudioClip[] CloudyAmbientClips;

        [BoxGroup("Ambient Audio Clips")]
        [Tooltip("Looping ambient clips for rainy weather.")]
        public AudioClip[] RainyAmbientClips;

        [BoxGroup("Ambient Audio Clips")]
        [Tooltip("Looping ambient clips for stormy weather.")]
        public AudioClip[] StormyAmbientClips;

        [BoxGroup("Ambient Audio Clips")]
        [Tooltip("Looping ambient clips for volcanic weather.")]
        public AudioClip[] VolcanicAmbientClips;

        [HorizontalLine(2, EColor.Blue)]
        [Header("Night Ambient Overlay")]
        [BoxGroup("Night Ambient Overlay")]
        [Tooltip(
            "Audio source used for night overlay sounds layered on top of the weather ambience."
        )]
        public AudioSource NightAmbientAudioSource;

        [BoxGroup("Night Ambient Overlay")]
        [Tooltip("Looping overlay clips for nighttime (crickets, owls, wind, etc.).")]
        public AudioClip[] NightAmbientClips;

        [BoxGroup("Night Ambient Overlay")]
        [Range(0, 1)]
        [Tooltip(
            "Maximum volume for the night overlay when fully night. Weather ambience stays at its current level."
        )]
        public float NightAmbientMaxVolume = 1f;

        [HorizontalLine(2, EColor.Pink)]
        [Header("Event Audio Clips")]
        [BoxGroup("Event Audio Clips")]
        [Tooltip("Thunder sound effects used during storms.")]
        public AudioClip[] ThunderClips;

        [BoxGroup("Event Audio Clips")]
        [Tooltip("Volcanic rumble sound effects used during volcanic weather.")]
        public AudioClip[] VolcanicRumbleClips;

        [HorizontalLine(2, EColor.Red)]
        [Header("Event Audio Settings")]
        [BoxGroup("Event Audio")]
        [Tooltip("Minimum distance from the listener for event sounds (thunder, rumble).")]
        public float EventSoundMinDistance = 10f;

        [BoxGroup("Event Audio")]
        [Tooltip("Maximum distance from the listener for event sounds (thunder, rumble).")]
        public float EventSoundMaxDistance = 80f;

        [BoxGroup("Event Audio")]
        [Tooltip("Minimum delay between lightning sound triggers.")]
        public float MinLightningInterval = 2f;

        [BoxGroup("Event Audio")]
        [Tooltip("Maximum delay between lightning sound triggers.")]
        public float MaxLightningInterval = 8f;

        [BoxGroup("Event Audio")]
        [Tooltip("Minimum delay between volcanic rumble sound triggers.")]
        public float MinVolcanicRumbleInterval = 10f;

        [BoxGroup("Event Audio")]
        [Tooltip("Maximum delay between volcanic rumble sound triggers.")]
        public float MaxVolcanicRumbleInterval = 30f;

        [HorizontalLine(2, EColor.Orange)]
        [Header("Light Intensities")]
        [BoxGroup("Lighting")]
        [Tooltip("Intensity of the directional light when the sky is sunny.")]
        public float SunnyLightIntensity = 1f;

        [BoxGroup("Lighting")]
        [Tooltip("Intensity of the directional light when the sky is overcast.")]
        public float OvercastLightIntensity = 0.5f;

        [HorizontalLine(2, EColor.Orange)]
        [Header("Water Specular")]
        [BoxGroup("Water")]
        [Tooltip("Specular strength applied to water when the sky is sunny.")]
        public float SunnySpecularStrength = 0.8f;

        [BoxGroup("Water")]
        [Tooltip("Specular strength applied to water when the sky is overcast.")]
        public float OvercastSpecularStrength = 0.4f;

        [HorizontalLine(2, EColor.Blue)]
        [Header("Cel Shader Tinting")]
        [BoxGroup("Cel Shader")]
        [Tooltip("Materials using the Cel/Godot shader that will be tinted for lighting.")]
        public Material[] CelMaterials;

        [HorizontalLine(2, EColor.Green)]
        [Header("Grass Tinting")]
        [BoxGroup("Grass Tinting")]
        [Tooltip("Grass material (uses Grass.shader) to apply cel/night tinting")]
        public Material GrassMaterial;

        [BoxGroup("Grass Tinting")]
        [Tooltip("GrassExtras materials (uses GrassExtras.shader) to apply cel/night tinting")]
        public Material[] GrassExtrasMaterials;

        [HorizontalLine(2, EColor.Yellow)]
        [Header("Weather Settings")]
        [BoxGroup("Weather")]
        [Tooltip("The currently active weather type.")]
        public WeatherType CurrentWeatherType = WeatherType.Sunny;

        [BoxGroup("Weather")]
        [Tooltip("Possible weather types that can be randomly selected when the scene loads.")]
        public WeatherType[] PossibleWeatherTypes;

        [HorizontalLine(2, EColor.Gray)]
        [Header("Time of Day")]
        [BoxGroup("Time")]
        [Tooltip(
            "If enabled, the starting time of day will be randomized between Min and Max at scene start."
        )]
        public bool RandomizeStartTimeOfDay = true;

        [BoxGroup("Time")]
        [Tooltip("Minimum time of day (0-24) to randomize from.")]
        [Range(0f, 24f)]
        public float StartTimeOfDayMin = 8f;

        [BoxGroup("Time")]
        [Tooltip("Maximum time of day (0-24) to randomize to.")]
        [Range(0f, 24f)]
        public float StartTimeOfDayMax = 18f;

        [BoxGroup("Time")]
        [Tooltip(
            "Current time of day (0-24). Updated automatically if ProgressTimeOfDay is enabled."
        )]
        [Range(0f, 24f)]
        public float TimeOfDay = 12f;

        [Tooltip("Whether time of day should automatically progress.")]
        public bool ProgressTimeOfDay = true;

        [Tooltip("Length of a full day/night cycle in seconds.")]
        public float DayLength = 600f; // Length of a full day in seconds
    }
}
