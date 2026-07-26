Shader "Turnroot/StylizedSkybox"
{
    Properties
    {
        [Header(Procedural Stars Settings)]
        _StarsDensity("Stars Density",  Range(0, 1)) = 0.5
        _StarsSize("Stars Size",  Range(0, 0.001)) = 0.002
        _StarsSizeVariation("Stars Size Variation",  Range(0, 1)) = 0.5
        _StarsMinSize("Stars Minimum Size Multiplier",  Range(0.1, 1.0)) = 0.3
        _StarsBrightness("Stars Brightness",  Range(1, 30)) = 10.0
        _StarsBrightnessVariation("Stars Brightness Variation",  Range(0, 1)) = 0.5
        _StarsColorVariation("Stars Color Variation",  Range(0, 1)) = 0.3
        _StarsSpeed("Stars Move Speed",  Range(0, 1)) = 0.01 
        [Toggle(STARS_ALWAYS_VISIBLE)] _StarsAlwaysVisible("Stars Always Visible", Float) = 0
        _StarsSkyColor("Stars Sky Color", Color) = (0.0,0.2,0.1,1)
        
        [Header(Shooting Stars Settings)]
        [Toggle(SHOOTING_STARS)] _ShootingStarsEnabled("Enable Shooting Stars", Float) = 0
        _ShootingStarFrequency("Shooting Star Frequency",  Range(0.1, 10)) = 2.0
        _ShootingStarSpeed("Shooting Star Speed",  Range(0.1, 5)) = 1.0
        _ShootingStarTrailLength("Trail Length",  Range(0.01, 0.5)) = 0.15
        _ShootingStarBrightness("Shooting Star Brightness",  Range(0, 5)) = 2.0
        _ShootingStarSize("Shooting Star Size Multiplier",  Range(2.5, 10)) = 2.5
 
         [Header(Horizon Settings)]
        _OffsetHorizon("Horizon Offset",  Range(-1, 1)) = 0
        _HorizonIntensity("Horizon Intensity",  Range(0, 10)) = 3.3
        // two colors to interpolate between as sun moves off the horizon
        _SunsetColorStart("Sunset/Rise Color (horizon)", Color) = (1,0.8,1,1)
        _SunsetColorEnd("Sunset/Rise Color (above)", Color) = (1,0.5,0.2,1)
        _ZenithFade("Zenith Fade", Range(0,1)) = 1
        _HorizonColorDay("Day Horizon Color", Color) = (0,0.8,1,1)
        _HorizonColorNight("Night Horizon Color", Color) = (0,0.8,1,1)
 
         [Header(Sun Settings)]
         _SunColor("Sun Color", Color) = (1,1,1,1)
        _SunRadius("Sun Radius",  Range(0, 2)) = 0.1
 
        [Header(Moon Settings)]
        _MoonColor("Moon Color", Color) = (1,1,1,1)
        _MoonRadius("Moon Radius",  Range(0, 2)) = 0.15
        _MoonOffset("Moon Crescent",  Range(-1, 1)) = -0.1
        _MoonTextureIntensity("Moon Dark Patches Intensity",  Range(0, 1)) = 0.4
        _MoonTextureScale("Moon Texture Scale",  Range(1, 60)) = 8.0
        _MoonTextureContrast("Moon Texture Contrast",  Range(0.1, 2.0)) = 1.0
        _MoonTextureRotation("Moon Texture Rotation",  Range(0, 6.28)) = 0.0
        _MoonTextureSeed("Moon Texture Pattern Seed",  Range(0, 100)) = 0.0
        
        [Header(Moon Halo Settings)]
        _HaloIntensity("Halo Intensity",  Range(0, 2)) = 0.5
        _HaloInnerRadius("Halo Inner Radius",  Range(0.1, 2)) = 0.18
        _HaloOuterRadius("Halo Outer Radius",  Range(0.1, 4)) = 0.35
        _HaloColor("Halo Color", Color) = (0.8,0.9,1,1)
        _HaloNoiseScale("Halo Noise Scale",  Range(0, 50)) = 10
        _HaloNoiseSpeed("Halo Noise Speed",  Range(0, 2)) = 0.5
        _HaloNoiseAmount("Halo Noise Amount",  Range(0, 1)) = 0.3
        _HaloSoftness("Halo Edge Softness",  Range(0.1, 2)) = 0.5

        [Header(Day Sky Settings)]
        _DayTopColor("Day Sky Color Top", Color) = (0.4,1,1,1)
        _DayBottomColor("Day Sky Color Bottom", Color) = (0,0.8,1,1)

        [Header(Main Cloud Layer Settings)]
        _BaseNoise("Base Noise", 2D) = "black" {}
        _Distort("Distort", 2D) = "black" {}
        _SecNoise("Secondary Noise", 2D) = "black" {}
        _BaseNoiseScale("Base Noise Scale",  Range(0, 1)) = 0.2
        _DistortScale("Distort Noise Scale",  Range(0, 1)) = 0.06
        _SecNoiseScale("Secondary Noise Scale",  Range(0, 1)) = 0.05
        _Distortion("Extra Distortion",  Range(0, 1)) = 0.1
        _Speed("Movement Speed",  Range(-2, 10)) = 1.4
        _DetailSpeed("Detail Movement Speed",  Range(0, 5)) = 1.0
        _CloudCutoff("Cloud Cutoff",  Range(0, 1)) = 0.3
        _CloudCoverage("Cloud Coverage",  Range(0, 1)) = 0.5
        _Fuzziness("Cloud Fuzziness",  Range(0, 1)) = 0.04
        _FuzzinessUnder("Cloud Fuzziness Under",  Range(0, 1)) = 0.01
        [Toggle(FUZZY)] _FUZZY("Extra Fuzzy clouds", Float) = 1
        
        [Header(Second Cloud Layer Settings)]
        [Toggle(CLOUD_LAYER_2)] _CloudLayer2Enabled("Enable Second Cloud Layer", Float) = 0
        _BaseNoiseScale2("Base Noise Scale 2",  Range(0, 1)) = 0.15
        _DistortScale2("Distort Noise Scale 2",  Range(0, 1)) = 0.04
        _SecNoiseScale2("Secondary Noise Scale 2",  Range(0, 1)) = 0.08
        _Distortion2("Extra Distortion 2",  Range(0, 1)) = 0.15
        _Speed2("Movement Speed 2",  Range(-2, 10)) = 2.0
        _DetailSpeed2("Detail Movement Speed 2",  Range(0, 5)) = 1.5
        _CloudCutoff2("Cloud Cutoff 2",  Range(0, 1)) = 0.4
        _CloudCoverage2("Cloud Coverage 2",  Range(0, 1)) = 0.3
        _Fuzziness2("Cloud Fuzziness 2",  Range(0, 1)) = 0.06
        _FuzzinessUnder2("Cloud Fuzziness Under 2",  Range(0, 1)) = 0.02
 
        [Header(Day Clouds Settings)]
        _CloudColorDayEdge("Clouds Edge Day", Color) = (1,1,1,1)
        _CloudColorDayMain("Clouds Main Day", Color) = (0.8,0.9,0.8,1)
        _CloudColorDayUnder("Clouds Under Day", Color) = (0.6,0.7,0.6,1)
        _CloudDayTint("Cloud Day Tint", Color) = (1,1,1,1)
        _Brightness("Cloud Brightness (noon)",  Range(1, 10)) = 2.5
        _BrightnessSunrise("Cloud Brightness (sunrise)",  Range(1, 10)) = 1.5
        
        [Header(Night Sky Settings)]
        _NightTopColor("Night Sky Color Top", Color) = (0,0,0,1)
        _NightBottomColor("Night Sky Color Bottom", Color) = (0,0,0.2,1)
 
        [Header(Night Clouds Settings)]
        _CloudColorNightEdge("Clouds Edge Night", Color) = (0,1,1,1)
        _CloudColorNightMain("Clouds Main Night", Color) = (0,0.2,0.8,1)
        _CloudColorNightUnder("Clouds Under Night", Color) = (0,0.2,0.6,1)
        _CloudNightTint("Cloud Night Tint", Color) = (1,1,1,1)
        
        [Header(Aurora Settings)]
        [Toggle(AURORA)] _AuroraEnabled("Enable Aurora", Float) = 0
        _AuroraIntensity("Aurora Intensity",  Range(0, 3)) = 1.0
        _AuroraHeight("Aurora Height",  Range(0, 1)) = 0.4
        _AuroraSpeed("Aurora Speed",  Range(0, 2)) = 0.5
        _AuroraColor1("Aurora Color 1", Color) = (0.0,1.0,0.5,1)
        _AuroraColor2("Aurora Color 2", Color) = (0.3,0.5,1.0,1)
        _AuroraColor3("Aurora Color 3", Color) = (1.0,0.2,0.8,1)
        
        [Header(Celestial Glow Settings)]
        _CelestialGlowIntensity("Celestial Glow Intensity",  Range(0, 2)) = 0.5
        _CelestialGlowSize("Celestial Glow Size",  Range(0.1, 2)) = 0.8
        _SunGlowColor("Sun Glow Color", Color) = (1.0,0.9,0.7,1)
        _MoonGlowColor("Moon Glow Color", Color) = (0.7,0.8,1.0,1)
        
        [Header(Milky Way Settings)]
        [Toggle(MILKY_WAY)] _MilkyWayEnabled("Enable Milky Way", Float) = 0
        _MilkyWayIntensity("Milky Way Intensity",  Range(0, 2)) = 1.0
        _MilkyWayColor("Milky Way Color", Color) = (0.8,0.85,1.0,1)
        _MilkyWayAngle("Milky Way Rotation",  Range(0, 6.28)) = 0.785
        _MilkyWayWidth("Milky Way Width",  Range(0.1, 1.0)) = 0.4
        
        [Header(Lightning Settings)]
        [Toggle(LIGHTNING)] _LightningEnabled("Enable Lightning", Float) = 0
        _LightningFrequency("Lightning Frequency",  Range(0.05,10)) = 0.5
        _LightningIntensity("Lightning Intensity",  Range(0, 1)) = 0.3
        _LightningLocalization("Lightning Localization",  Range(0, 1)) = 0.7
        _LightningColor("Lightning Color", Color) = (0.9,0.95,1.0,1)
        _LightningEventStartTime("Lightning Event Start Time", Float) = 0
        _LightningEventDuration("Lightning Event Duration", Float) = 0.4
        _LightningEventIntensity("Lightning Event Intensity", Float) = 1
        _LightningEventDirection("Lightning Event Direction", Vector) = (0,1,0,0)
        [Toggle(DISTANT_LIGHTNING)] _DistantLightningEnabled("Enable Distant Lightning Bolts", Float) = 1
        _DistantLightningBrightness("Distant Lightning Brightness",  Range(0, 2)) = 1.0
        _DistantLightningGlow("Distant Lightning Glow Strength",  Range(0, 2)) = 0.5
        [NoScaleOffset] _LightningSpriteAtlas("Lightning Sprite Atlas (white on black)", 2D) = "black" {}
        _LightningSpriteSize("Lightning Sprite Reference Size (px)",  Range(32, 512)) = 512
        _LightningGridCount("Lightning Sprite Grid Count", Range(1, 64)) = 16
        
        [Header(Atmospheric Effects)]
        _HazeIntensity("Horizon Haze Intensity",  Range(0, 2)) = 0.5
        _HazeHeight("Haze Height",  Range(0, 1)) = 0.3
        _HazeDayColor("Haze Day Color", Color) = (0.8,0.85,1.0,1)
        _HazeNightColor("Haze Night Color", Color) = (0.1,0.15,0.3,1)
        _CloudShadowIntensity("Cloud Shadow Intensity",  Range(0, 1)) = 0.3
        
        [Header(Rainbow Settings)]
        [Toggle(RAINBOW)] _RainbowEnabled("Enable Rainbow", Float) = 0
        _RainbowIntensity("Rainbow Intensity",  Range(0, 2)) = 1.0
        _RainbowWidth("Rainbow Width",  Range(0.01, 0.2)) = 0.05
        _RainbowRadius("Rainbow Radius",  Range(0.2, 1.0)) = 0.5
        _RainbowAngleOffset("Rainbow Angle Offset",  Range(-3.14, 3.14)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" "RenderPipeline"="UniversalPipeline" }
        //LOD 100

        Pass
        {
            Name "Skybox"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature FUZZY
            #pragma shader_feature CLOUD_LAYER_2
            #pragma shader_feature STARS_ALWAYS_VISIBLE
            #pragma shader_feature SHOOTING_STARS
            #pragma shader_feature AURORA
            #pragma shader_feature MILKY_WAY
            #pragma shader_feature LIGHTNING
            #pragma shader_feature DISTANT_LIGHTNING
            #pragma shader_feature RAINBOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 viewDir : TEXCOORD1; // View direction for stars
            };

            CBUFFER_START(UnityPerMaterial)
                float _SunRadius, _MoonRadius, _MoonOffset, _OffsetHorizon;
                float4 _SunColor, _MoonColor;
                float _MoonTextureIntensity, _MoonTextureScale, _MoonTextureContrast, _MoonTextureRotation, _MoonTextureSeed;
                float4 _DayTopColor, _DayBottomColor, _NightBottomColor, _NightTopColor;
                float4 _HorizonColorDay, _HorizonColorNight;
                float4 _SunsetColorStart, _SunsetColorEnd;
                float _ZenithFade;
                
                // Procedural stars
                float _StarsDensity, _StarsSize, _StarsBrightness, _StarsColorVariation;
                float _StarsSizeVariation, _StarsBrightnessVariation, _StarsMinSize;
                float _StarsSpeed, _HorizonIntensity;
                float4 _StarsSkyColor;
                
                // Shooting stars
                float _ShootingStarFrequency, _ShootingStarSpeed, _ShootingStarTrailLength, _ShootingStarBrightness, _ShootingStarSize;
                
                // Moon halo
                float _HaloIntensity, _HaloInnerRadius, _HaloOuterRadius;
                float4 _HaloColor;
                float _HaloNoiseScale, _HaloNoiseSpeed, _HaloNoiseAmount, _HaloSoftness;
                
                // Cloud layer 1
                float _BaseNoiseScale, _DistortScale, _SecNoiseScale, _Distortion;
                float _Speed, _DetailSpeed, _CloudCutoff, _CloudCoverage, _Fuzziness, _FuzzinessUnder;
                float _BrightnessSunrise, _Brightness; // sunrise and noon brightness
                float4 _CloudColorDayEdge, _CloudColorDayMain, _CloudColorDayUnder, _CloudDayTint;
                float4 _CloudColorNightEdge, _CloudColorNightMain, _CloudColorNightUnder, _CloudNightTint;
                
                // Cloud layer 2
                float _CloudLayer2Enabled;
                float _BaseNoiseScale2, _DistortScale2, _SecNoiseScale2, _Distortion2;
                float _Speed2, _DetailSpeed2, _CloudCutoff2, _CloudCoverage2, _Fuzziness2, _FuzzinessUnder2;
                
                // Aurora
                float _AuroraIntensity, _AuroraHeight, _AuroraSpeed;
                float4 _AuroraColor1, _AuroraColor2, _AuroraColor3;
                
                // Celestial Glow
                float _CelestialGlowIntensity, _CelestialGlowSize;
                float4 _SunGlowColor, _MoonGlowColor;
                
                // Milky Way
                float _MilkyWayIntensity, _MilkyWayAngle, _MilkyWayWidth;
                float4 _MilkyWayColor;
                
                // Lightning
                float _LightningFrequency, _LightningIntensity, _LightningLocalization, _DistantLightningBrightness;
                float _DistantLightningWidth, _DistantLightningGlow;
                float _LightningSpriteSize, _LightningGridCount;
                float4 _LightningColor;

                // Lightning event (script-driven)
                float _LightningEventStartTime;
                float _LightningEventDuration;
                float _LightningEventIntensity;
                float4 _LightningEventDirection;
                
                // Atmospheric effects
                float _HazeIntensity, _HazeHeight, _CloudShadowIntensity;
                float4 _HazeDayColor, _HazeNightColor;
                
                // Rainbow
                float _RainbowIntensity, _RainbowWidth, _RainbowRadius, _RainbowAngleOffset;
            CBUFFER_END

            TEXTURE2D(_BaseNoise);
            SAMPLER(sampler_BaseNoise);
            TEXTURE2D(_Distort);
            SAMPLER(sampler_Distort);
            TEXTURE2D(_SecNoise);
            SAMPLER(sampler_SecNoise);
            TEXTURE2D(_LightningSpriteAtlas);
            SAMPLER(sampler_LightningSpriteAtlas);
            
            // Hash functions for procedural generation
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 19.19);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float3 generateProceduralStars(float3 viewDir, float time)
            {
                float3 stars = float3(0, 0, 0);
                
                // Use 3D grid directly on view direction (like sun/moon do)
                // This avoids UV mapping precision issues
                float gridScale = 20.0;
                float3 gridPos = viewDir * gridScale;
                int3 cellID = int3(floor(gridPos));
                
                // Star rotation for movement
                float rotationAngle = time * _StarsSpeed * 0.001;
                float cosAngle = cos(rotationAngle);
                float sinAngle = sin(rotationAngle);
                
                // Check neighboring cells
                for(int z = -1; z <= 1; z++)
                {
                    for(int y = -1; y <= 1; y++)
                    {
                        for(int x = -1; x <= 1; x++)
                        {
                            int3 neighborCell = cellID + int3(x, y, z);
                            
                            // Hash the cell ID to get random seed
                            float cellHash = hash(float2(
                                hash(float2(neighborCell.x, neighborCell.y)),
                                neighborCell.z
                            ));
                            
                            // Star density control
                            if(cellHash > _StarsDensity)
                                continue;
                            
                            // Random star direction within cell
                            float3 starOffset = float3(
                                hash(float2(cellHash, 1.1)),
                                hash(float2(cellHash, 2.2)),
                                hash(float2(cellHash, 3.3))
                            );
                            starOffset = (starOffset - 0.5) / gridScale;
                            
                            float3 starDir = normalize(float3(neighborCell) / gridScale + starOffset);
                            
                            // Rotate star direction around Y axis for movement
                            float3 rotatedStarDir = float3(
                                starDir.x * cosAngle - starDir.z * sinAngle,
                                starDir.y,
                                starDir.x * sinAngle + starDir.z * cosAngle
                            );
                            
                            // Distance to star (use original view dir, rotated star dir)
                            float distToStar = distance(viewDir, rotatedStarDir);
                            
                            // Star size variation
                            float sizeRandomness = hash(float2(cellHash, 4.4));
                            float sizeMultiplier = lerp(1.0, _StarsMinSize + sizeRandomness * (1.7 - _StarsMinSize), _StarsSizeVariation);
                            float starSize = _StarsSize * sizeMultiplier;
                            float star = smoothstep(starSize, 0, distToStar);
                            
                            // Star brightness variation
                            float brightnessRandomness = hash(float2(cellHash, 5.5));
                            float brightnessMultiplier = lerp(1.0, 0.3 + brightnessRandomness * 1.4, _StarsBrightnessVariation);
                            
                            // Blackbody color variation (stellar temperature)
                            float colorTemp = hash(float2(cellHash, 6.6));
                            float3 starColorVaried;
                            
                            // Map temperature to stellar colors
                            if(colorTemp < 0.2) {
                                starColorVaried = float3(1.0, 0.4, 0.2);
                            } else if(colorTemp < 0.4) {
                                starColorVaried = float3(1.0, 0.7, 0.4);
                            } else if(colorTemp < 0.6) {
                                starColorVaried = float3(1.0, 0.95, 0.8);
                            } else if(colorTemp < 0.8) {
                                starColorVaried = float3(1.0, 1.0, 1.0);
                            } else {
                                starColorVaried = float3(0.7, 0.85, 1.0);
                            }
                            
                            float3 starColor = lerp(float3(1, 1, 1), starColorVaried, _StarsColorVariation);
                            
                            stars += star * starColor * _StarsBrightness * brightnessMultiplier;
                        }
                    }
                }
                
                return stars;
            }
            
            float3 generateShootingStars(float3 worldDir, float time)
            {
                float3 shootingStars = float3(0, 0, 0);
                
                // Create multiple shooting stars at different times
                for(int i = 0; i < 3; i++)
                {
                    float timeOffset = i * 123.456;
                    float cycleTime = time * _ShootingStarFrequency + timeOffset;
                    float shootingStarCycle = frac(cycleTime * 0.1);
                    
                    // Only show shooting star during brief window
                    if(shootingStarCycle < 0.05)
                    {
                        // Generate shooting star direction and position based on cycle
                        float t = shootingStarCycle / 0.05;
                        float2 seed = float2(floor(cycleTime * 0.1) + i, i * 7.89);
                        
                        // Random start position on sky hemisphere
                        float startAngleH = hash(seed) * 6.28;
                        float startAngleV = hash(seed * 1.3) * 1.5 - 0.5;
                        float3 startDir = normalize(float3(
                            cos(startAngleH) * cos(startAngleV),
                            sin(startAngleV) + 0.3,
                            sin(startAngleH) * cos(startAngleV)
                        ));
                        
                        // Shooting star direction (mostly horizontal)
                        float shootAngle = hash(seed * 1.7) * 6.28;
                        float3 shootDir = normalize(float3(
                            cos(shootAngle),
                            -0.3 - hash(seed * 2.1) * 0.2,
                            sin(shootAngle)
                        ));
                        
                        // Current position along path
                        float3 currentPos = startDir + shootDir * t * _ShootingStarSpeed * 0.5;
                        currentPos = normalize(currentPos);
                        
                        // Distance from view direction to shooting star position
                        float distToShootingStar = distance(worldDir, currentPos);
                        
                        // Create elongated trail
                        float trailFade = 0.0;
                        for(float j = 0; j < 5; j++)
                        {
                            float trailOffset = j * 0.2;
                            float3 trailPos = normalize(startDir + shootDir * (t - trailOffset * _ShootingStarTrailLength) * _ShootingStarSpeed * 0.5);
                            float trailDist = distance(worldDir, trailPos);
                            // Use star size for the point size, trail length controls spacing
                            float trailContribution = smoothstep(_StarsSize * _ShootingStarSize, 0, trailDist) * (1.0 - trailOffset * 0.15);
                            trailFade = max(trailFade, trailContribution);
                        }
                        
                        // Fade in and out
                        float fade = smoothstep(0, 0.1, t) * smoothstep(1, 0.7, t);
                        shootingStars += trailFade * fade * _ShootingStarBrightness;
                    }
                }
                
                return shootingStars;
            }
            
            // Aurora (Northern Lights) - Stylized wavy bands
            float3 generateAurora(float3 worldDir, float time)
            {
                // Only show aurora in upper hemisphere
                if(worldDir.y < _AuroraHeight - 0.2)
                    return float3(0, 0, 0);
                
                // Height fade
                float heightFade = smoothstep(_AuroraHeight - 0.2, _AuroraHeight + 0.3, worldDir.y);
                heightFade *= smoothstep(1.0, 0.7, worldDir.y); // Fade at zenith
                
                // Create wavy curtain pattern
                float2 auroraUV = float2(atan2(worldDir.z, worldDir.x) * 3.0, worldDir.y * 5.0);
                
                // Multiple layers of waves for curtain effect
                float wave1 = sin(auroraUV.x + time * _AuroraSpeed * 0.5 + sin(auroraUV.y * 2.0) * 0.5) * 0.5 + 0.5;
                float wave2 = sin(auroraUV.x * 1.7 - time * _AuroraSpeed * 0.3 + cos(auroraUV.y * 1.5) * 0.7) * 0.5 + 0.5;
                float wave3 = sin(auroraUV.x * 2.3 + time * _AuroraSpeed * 0.7 + sin(auroraUV.y * 3.0) * 0.3) * 0.5 + 0.5;
                
                // Combine waves with noise for organic look
                float auroraPattern = wave1 * 0.5 + wave2 * 0.3 + wave3 * 0.2;
                
                // Add vertical streaks
                float streaks = sin(auroraUV.y * 15.0 + auroraPattern * 3.0) * 0.5 + 0.5;
                auroraPattern *= lerp(1.0, streaks, 0.3);
                
                // Smooth cutoff for painterly ribbons
                auroraPattern = smoothstep(0.3, 0.7, auroraPattern);
                
                // Color variation across the aurora
                float colorVar = sin(auroraUV.x * 0.7 + time * _AuroraSpeed * 0.2) * 0.5 + 0.5;
                float3 auroraColor;
                if(colorVar < 0.33) {
                    auroraColor = lerp(_AuroraColor1.rgb, _AuroraColor2.rgb, colorVar * 3.0);
                } else if(colorVar < 0.66) {
                    auroraColor = lerp(_AuroraColor2.rgb, _AuroraColor3.rgb, (colorVar - 0.33) * 3.0);
                } else {
                    auroraColor = lerp(_AuroraColor3.rgb, _AuroraColor1.rgb, (colorVar - 0.66) * 3.0);
                }
                
                return auroraColor * auroraPattern * heightFade * _AuroraIntensity;
            }
            
            // Milky Way band - Dense starfield across sky
            float3 generateMilkyWay(float3 worldDir)
            {
                // Create a band across the sky with controllable angle
                float bandAngle = atan2(worldDir.z, worldDir.x) + worldDir.y * 0.5 + _MilkyWayAngle;
                float bandDist = abs(sin(bandAngle * 0.7) - 0.3);
                
                // Smooth band shape with controllable width
                float bandWidth = _MilkyWayWidth * 0.5;
                float band = smoothstep(bandWidth + 0.1, bandWidth - 0.1, bandDist);
                band *= smoothstep(-0.2, 0.3, worldDir.y); // Fade below horizon
                
                // Add noise for cloud-like density variation
                float2 milkyWayUV = float2(bandAngle * 2.0, worldDir.y * 3.0);
                float density = noise(milkyWayUV * 3.0) * noise(milkyWayUV * 7.0);
                density = smoothstep(0.2, 0.8, density);
                
                // Add some sparkle variation
                float sparkle = hash(floor(milkyWayUV * 50.0)) * hash(floor(milkyWayUV * 50.0 + 1.5));
                sparkle = smoothstep(0.8, 1.0, sparkle);
                
                return _MilkyWayColor.rgb * band * density * (1.0 + sparkle * 0.5) * _MilkyWayIntensity;
            }
            
            // Lightning flash effect - Returns intensity and position
            void generateLightning(float time, out float intensity, out float3 position)
            {
                intensity = 0.0;
                position = float3(0, 0, 0);

                // Script-driven lightning event (overrides procedural lightning)
                float eventAge = time - _LightningEventStartTime;
                if (_LightningEventIntensity > 0.001 && eventAge >= 0.0 && eventAge < _LightningEventDuration)
                {
                    float t = saturate(eventAge / max(0.0001, _LightningEventDuration));
                    float flash = smoothstep(0.0, 0.2, t) * smoothstep(1.0, 0.8, t);
                    intensity = flash * _LightningEventIntensity;
                    position = normalize(_LightningEventDirection.xyz);
                    return;
                }

                float lightningCycle = time * _LightningFrequency * 0.2; // Slowed down for photosensitivity
                float flashChance = hash(float2(floor(lightningCycle), 0.123));
                
                // Only flash occasionally (reduced frequency)
                if(flashChance < 0.85)
                    return;
                
                float flashTime = frac(lightningCycle);
                
                // Slower, gentler flash patterns
                float flash = 0.0;
                if(flashTime < 0.1) {
                    // Primary flash - slower rise and fall
                    flash = smoothstep(0.0, 0.04, flashTime) * smoothstep(0.1, 0.06, flashTime);
                } else if(flashTime < 0.25 && flashTime > 0.15) {
                    // Secondary flash - softer
                    float t = (flashTime - 0.15) / 0.1;
                    flash = smoothstep(0.0, 0.4, t) * smoothstep(1.0, 0.7, t) * 0.4;
                }
                
                // Generate lightning bolt position (line between two points)
                float2 seed = float2(floor(lightningCycle), 0.456);
                
                // Random position in sky (avoid zenith and below horizon)
                float posAngle = hash(seed) * 6.28;
                float posHeight = hash(seed * 1.3) * 0.4 + 0.2; // Between 0.2 and 0.6 height
                
                position = normalize(float3(
                    cos(posAngle) * sqrt(1.0 - posHeight * posHeight),
                    posHeight,
                    sin(posAngle) * sqrt(1.0 - posHeight * posHeight)
                ));
                
                intensity = flash * _LightningIntensity;
            }
            
            // Distant Lightning Bolts - Visible sprite-based bolt on horizon
            // Picks a random cell from a square sprite-sheet (white bolt on black) each flash
            // cycle and projects it onto the same angular footprint the old procedural bolt used.
            float3 generateDistantLightning(float3 worldDir, float time, float3 lightningPosition, float lightningIntensity)
            {
                if(lightningIntensity < 0.001)
                    return float3(0, 0, 0);
                
                // Horizontal angles for view direction and lightning position
                float viewAngle = atan2(worldDir.z, worldDir.x);
                float lightningAngle = atan2(lightningPosition.z, lightningPosition.x);
                
                // Signed angular difference (preserve direction)
                float angleDiff = viewAngle - lightningAngle;
                // Handle wraparound
                if(angleDiff > 3.14159) angleDiff -= 6.28318;
                if(angleDiff < -3.14159) angleDiff += 6.28318;
                
                float widthMult = 1;
                float angularHalfWidth = 0.3 * max(0.01, widthMult);
                
                // Check if we're in viewing range
                if(abs(angleDiff) > angularHalfWidth)
                    return float3(0, 0, 0);
                
                // Height of current view direction
                float viewHeight = worldDir.y;
                
                // Bolt extends from lightning position down to below horizon
                float boltTopHeight = lightningPosition.y;
                float boltBottomHeight = -0.15; // Extend below horizon for visibility
                
                // Check if we're within the vertical range of the bolt
                if(viewHeight < boltBottomHeight || viewHeight > boltTopHeight + 0.05)
                    return float3(0, 0, 0);
                
                // Normalize height within bolt range (0 = bottom, 1 = top)
                float heightInBolt = saturate((viewHeight - boltBottomHeight) / (boltTopHeight - boltBottomHeight));
                
                // Same flash-cycle seed the old procedural bolt used, so the sprite only
                // changes once per flash rather than every frame.
                float2 boltSeed = float2(floor(time * _LightningFrequency * 0.2), 0.789);
                
                // Local UV within the chosen sprite cell (0-1 across the bolt's footprint)
                float u = saturate(angleDiff / angularHalfWidth * 0.5 + 0.5);
                float v = heightInBolt;
                
                // Pick a random cell out of the grid (e.g. 16 => 4x4 sheet)
                float gridDim = max(1.0, round(sqrt(max(1.0, _LightningGridCount))));
                float cellCount = gridDim * gridDim;
                float cellIndex = min(floor(hash(boltSeed + float2(3.71, 8.42)) * cellCount), cellCount - 1.0);
                float cellX = fmod(cellIndex, gridDim);
                float cellY = floor(cellIndex / gridDim);
                
                float2 atlasUV = (float2(cellX, cellY) + float2(u, v)) / gridDim;
                
                // Sprite is white on black, so any channel works as the bolt mask
                float boltMask = SAMPLE_TEXTURE2D(_LightningSpriteAtlas, sampler_LightningSpriteAtlas, atlasUV).r;
                
                // Soft glow halo around the sprite footprint - strength controlled by user
                float distFromCenter = abs(angleDiff) / angularHalfWidth;
                float glow = smoothstep(1.0, 0.15, distFromCenter) * _DistantLightningGlow * 0.4;
                
                // Combine sprite bolt and glow
                float lightningShape = saturate(boltMask + glow);
                
                // Fade based on height (brighter at top)
                lightningShape *= lerp(0.6, 1.0, heightInBolt);
                
                return _LightningColor.rgb * lightningShape * lightningIntensity * _DistantLightningBrightness * 12.0;
            }
            
            // Rainbow arc
            float3 generateRainbow(float3 worldDir, float3 lightDir)
            {
                // Rainbow appears opposite to sun, with adjustable angle offset
                // Rotate light direction by angle offset
                float cosOffset = cos(_RainbowAngleOffset);
                float sinOffset = sin(_RainbowAngleOffset);
                float3 rotatedLightDir = float3(
                    lightDir.x * cosOffset - lightDir.z * sinOffset,
                    lightDir.y,
                    lightDir.x * sinOffset + lightDir.z * cosOffset
                );
                
                float3 rainbowCenter = -rotatedLightDir;
                
                // Only show when sun is at right angle (after rain conditions)
                float sunHeight = saturate(lightDir.y * 3.0);
                float rainbowCondition = smoothstep(0.1, 0.3, lightDir.y) * smoothstep(0.6, 0.4, lightDir.y);
                
                // Distance from rainbow center
                float distToCenter = distance(worldDir, rainbowCenter);
                
                // Create rainbow arc
                float arcDist = abs(distToCenter - _RainbowRadius);
                float rainbow = smoothstep(_RainbowWidth, 0.0, arcDist);
                
                // Only show above horizon
                rainbow *= smoothstep(0.0, 0.2, worldDir.y);
                
                // Color bands (stylized, not accurate spectrum)
                float colorPos = (distToCenter - _RainbowRadius + _RainbowWidth * 0.5) / _RainbowWidth;
                colorPos = saturate(colorPos);
                
                float3 rainbowColor;
                if(colorPos < 0.16) {
                    rainbowColor = lerp(float3(1, 0, 0), float3(1, 0.5, 0), colorPos / 0.16); // Red to Orange
                } else if(colorPos < 0.33) {
                    rainbowColor = lerp(float3(1, 0.5, 0), float3(1, 1, 0), (colorPos - 0.16) / 0.17); // Orange to Yellow
                } else if(colorPos < 0.5) {
                    rainbowColor = lerp(float3(1, 1, 0), float3(0, 1, 0), (colorPos - 0.33) / 0.17); // Yellow to Green
                } else if(colorPos < 0.66) {
                    rainbowColor = lerp(float3(0, 1, 0), float3(0, 0.5, 1), (colorPos - 0.5) / 0.16); // Green to Blue
                } else if(colorPos < 0.83) {
                    rainbowColor = lerp(float3(0, 0.5, 1), float3(0.5, 0, 1), (colorPos - 0.66) / 0.17); // Blue to Indigo
                } else {
                    rainbowColor = lerp(float3(0.5, 0, 1), float3(0.8, 0, 0.8), (colorPos - 0.83) / 0.17); // Indigo to Violet
                }
                
                return rainbowColor * rainbow * rainbowCondition * _RainbowIntensity;
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                
                // Compute view direction in world space for stars
                // This is camera-rotation dependent but camera-position independent
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDir = worldPos - _WorldSpaceCameraPos;
                
                return OUT;
            }

            half4 frag (Varyings IN) : SV_TARGET
            {
                // Use view direction for stars - camera position independent
                float3 skyDir = normalize(IN.viewDir);
                
                // For lighting and atmosphere effects
                float3 worldDir = normalize(IN.positionWS - _WorldSpaceCameraPos);

                // Get main light direction
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;

                float horizonFactor = saturate((worldDir.y - _OffsetHorizon) / _HorizonIntensity);
                float horizon = lerp(1, abs(worldDir.y * _HorizonIntensity), 1 - _OffsetHorizon);

                // Sky UV - use object space direction for stable star positions
                // Object space is camera-independent for skybox shaders
                float2 skyUV = float2(
                    atan2(skyDir.z, skyDir.x) * 0.15915494309, // 1/(2*PI)
                    asin(clamp(skyDir.y, -1.0, 1.0)) * 0.31830988618 + 0.5 // 1/PI
                );

                // Cloud Layer 1
                float2 flatSkyUV = IN.positionWS.xz / (abs(IN.positionWS.y) + 1.0);
                float baseNoise = SAMPLE_TEXTURE2D(_BaseNoise, sampler_BaseNoise, (flatSkyUV - (_Time.x * _DetailSpeed)) * _BaseNoiseScale).x;
                float noise1 = SAMPLE_TEXTURE2D(_Distort, sampler_Distort, ((flatSkyUV + baseNoise) - (_Time.x * _Speed)) * _DistortScale).x;
                float noise2 = SAMPLE_TEXTURE2D(_SecNoise, sampler_SecNoise, ((flatSkyUV + (noise1 * _Distortion)) - (_Time.x * (_Speed * 0.5))) * _SecNoiseScale).x;
                float finalNoise = saturate(noise1 * noise2) * 3 * saturate(worldDir.y);
                
                // Apply coverage to cloud cutoff
                float adjustedCutoff = _CloudCutoff * (1.0 - _CloudCoverage * 0.5);

                #if FUZZY
                float clouds = saturate(smoothstep(adjustedCutoff * baseNoise, adjustedCutoff * baseNoise + _Fuzziness, finalNoise));
                float cloudsunder = saturate(smoothstep(adjustedCutoff * baseNoise, adjustedCutoff * baseNoise + _FuzzinessUnder + _Fuzziness, noise2) * clouds);
                #else
                float clouds = saturate(smoothstep(adjustedCutoff, adjustedCutoff + _Fuzziness, finalNoise));
                float cloudsunder = saturate(smoothstep(adjustedCutoff, adjustedCutoff + _Fuzziness + _FuzzinessUnder, noise2) * clouds);
                #endif

                float dayAmount = saturate(lightDir.y);

                float3 cloudsColored = lerp(_CloudColorDayEdge.rgb, lerp(_CloudColorDayUnder.rgb, _CloudColorDayMain.rgb, cloudsunder), clouds) * clouds;
                float3 cloudsColoredNight = lerp(_CloudColorNightEdge.rgb, lerp(_CloudColorNightUnder.rgb, _CloudColorNightMain.rgb, cloudsunder), clouds) * clouds;
                cloudsColoredNight *= horizon;
                
                // Apply tints for independent cloud coloring
                cloudsColored *= _CloudDayTint.rgb;
                cloudsColoredNight *= _CloudNightTint.rgb;
                
                cloudsColored = lerp(cloudsColoredNight, cloudsColored, dayAmount);
                // cloud brightness transitions between sunrise and noon values
                float sunHeight = saturate(lightDir.y);
                float cloudBright = lerp(_BrightnessSunrise, _Brightness, sunHeight);
                cloudsColored += (cloudBright * cloudsColored * horizon);
                
                // Cloud Layer 2 (Parallax)
                float clouds2 = 0;
                float3 cloudsColored2 = float3(0, 0, 0);
                #ifdef CLOUD_LAYER_2
                {
                    float baseNoise2 = SAMPLE_TEXTURE2D(_BaseNoise, sampler_BaseNoise, (flatSkyUV - (_Time.x * _DetailSpeed2)) * _BaseNoiseScale2).x;
                    float noise1_2 = SAMPLE_TEXTURE2D(_Distort, sampler_Distort, ((flatSkyUV + baseNoise2) - (_Time.x * _Speed2)) * _DistortScale2).x;
                    float noise2_2 = SAMPLE_TEXTURE2D(_SecNoise, sampler_SecNoise, ((flatSkyUV + (noise1_2 * _Distortion2)) - (_Time.x * (_Speed2 * 0.5))) * _SecNoiseScale2).x;
                    float finalNoise2 = saturate(noise1_2 * noise2_2) * 3 * saturate(worldDir.y);
                    
                    float adjustedCutoff2 = _CloudCutoff2 * (1.0 - _CloudCoverage2 * 0.5);
                    
                    #if FUZZY
                    clouds2 = saturate(smoothstep(adjustedCutoff2 * baseNoise2, adjustedCutoff2 * baseNoise2 + _Fuzziness2, finalNoise2));
                    float cloudsunder2 = saturate(smoothstep(adjustedCutoff2 * baseNoise2, adjustedCutoff2 * baseNoise2 + _FuzzinessUnder2 + _Fuzziness2, noise2_2) * clouds2);
                    #else
                    clouds2 = saturate(smoothstep(adjustedCutoff2, adjustedCutoff2 + _Fuzziness2, finalNoise2));
                    float cloudsunder2 = saturate(smoothstep(adjustedCutoff2, adjustedCutoff2 + _Fuzziness2 + _FuzzinessUnder2, noise2_2) * clouds2);
                    #endif
                    
                    float3 cloudsColored2Day = lerp(_CloudColorDayEdge.rgb, lerp(_CloudColorDayUnder.rgb, _CloudColorDayMain.rgb, cloudsunder2), clouds2) * clouds2;
                    float3 cloudsColored2Night = lerp(_CloudColorNightEdge.rgb, lerp(_CloudColorNightUnder.rgb, _CloudColorNightMain.rgb, cloudsunder2), clouds2) * clouds2;
                    cloudsColored2Night *= horizon;
                    
                    cloudsColored2Day *= _CloudDayTint.rgb;
                    cloudsColored2Night *= _CloudNightTint.rgb;
                    
                    cloudsColored2 = lerp(cloudsColored2Night, cloudsColored2Day, dayAmount);
                    cloudsColored2 += (cloudBright * cloudsColored2 * horizon * 0.7);
                }
                #endif
                
                // Cloud Shadows - Layer 2 casts shadow on Layer 1
                float cloudShadow = clouds2 * _CloudShadowIntensity;
                cloudsColored *= (1.0 - cloudShadow * 0.7); // Darken layer 1 where layer 2 overlaps
                
                // Combine cloud layers - layer 2 is in front of layer 1 for parallax
                float cloudsCombined = saturate(clouds + clouds2);
                float3 cloudsColoredCombined = cloudsColored * (1 - clouds2) + cloudsColored2;

                float cloudsNegative = (1 - cloudsCombined) * horizon;

                // Sun
                float sun = distance(worldDir.xyz, lightDir);
                float sunDisc = 1 - (sun / _SunRadius);
                sunDisc = saturate(sunDisc * 50);

                // Moon
                float moon = distance(worldDir.xyz, -lightDir);
                float crescentMoon = distance(float3(worldDir.x + _MoonOffset, worldDir.yz), -lightDir);
                float crescentMoonDisc = 1 - (crescentMoon / _MoonRadius);
                crescentMoonDisc = saturate(crescentMoonDisc * 50);
                float moonDisc = 1 - (moon / _MoonRadius);
                moonDisc = saturate(moonDisc * 50);
                moonDisc = saturate(moonDisc - crescentMoonDisc);
                
                // Add painterly moon texture (darker patches)
                float moonTexture = 1.0;
                if(moonDisc > 0.01)
                {
                    // Use world direction relative to moon for stable texture
                    float3 moonDir = normalize(-lightDir);
                    float3 toPixel = normalize(worldDir);
                    
                    // Create UV coordinates on moon surface
                    float3 moonUp = float3(0, 1, 0);
                    float3 moonRight = normalize(cross(moonUp, moonDir));
                    moonUp = cross(moonDir, moonRight);
                    
                    float2 moonUV = float2(
                        dot(toPixel, moonRight),
                        dot(toPixel, moonUp)
                    ) * _MoonTextureScale;
                    
                    // Apply rotation to texture
                    float cosRot = cos(_MoonTextureRotation);
                    float sinRot = sin(_MoonTextureRotation);
                    float2 rotatedUV = float2(
                        moonUV.x * cosRot - moonUV.y * sinRot,
                        moonUV.x * sinRot + moonUV.y * cosRot
                    );
                    
                    // Apply seed offset to change pattern
                    float2 seedOffset = float2(_MoonTextureSeed * 7.13, _MoonTextureSeed * 3.79);
                    
                    // Multi-scale noise for organic darker patches
                    float patch1 = noise(rotatedUV * 1.0 + seedOffset);
                    float patch2 = noise(rotatedUV * 2.3 + seedOffset + 5.7);
                    float patch3 = noise(rotatedUV * 4.7 + seedOffset + 12.3);
                    
                    // Combine patches with different weights for painterly look
                    float patches = patch1 * 0.6 + patch2 * 0.3 + patch3 * 0.1;
                    
                    // Create darker regions with adjustable contrast
                    float contrastCenter = 0.5;
                    float contrastRange = 0.2 / _MoonTextureContrast;
                    float darkPatches = smoothstep(contrastCenter - contrastRange, contrastCenter + contrastRange, patches);
                    
                    // Apply texture - darken some areas
                    moonTexture = lerp(1.0 - _MoonTextureIntensity, 1.0, darkPatches);
                }
                
                moonDisc *= moonTexture;

                // Moon Halo (Ice Crystal Ring)
                float moonHalo = 0;
                if(_HaloIntensity > 0.001)
                {
                    float moonDist = distance(worldDir.xyz, -lightDir);
                    
                    // Normalize the distance similar to how moon disc is calculated
                    float normalizedDist = moonDist / _MoonRadius;
                    
                    // Add noise to distort halo shape (applied before ring calculation)
                    float2 haloNoiseUV = worldDir.xz * _HaloNoiseScale + _Time.y * _HaloNoiseSpeed * 0.1;
                    float haloNoise1 = noise(haloNoiseUV);
                    float haloNoise2 = noise(haloNoiseUV * 2.3 + 1.5);
                    float haloNoisePattern = (haloNoise1 * 0.6 + haloNoise2 * 0.4);
                    
                    // Distort the distance using noise
                    normalizedDist += (haloNoisePattern - 0.5) * _HaloNoiseAmount * 2.0;
                    
                    // Create ring between inner and outer radius with smooth edges
                    float innerFadeWidth = 0.3;
                    float innerFade = smoothstep(_HaloInnerRadius - innerFadeWidth, _HaloInnerRadius + innerFadeWidth, normalizedDist);
                    float outerFade = 1.0 - smoothstep(_HaloOuterRadius - _HaloSoftness, _HaloOuterRadius + _HaloSoftness, normalizedDist);
                    float haloRing = innerFade * outerFade;
                    
                    moonHalo = haloRing * _HaloIntensity * saturate(-lightDir.y * 2);
                }
                
                // Celestial Glow - Soft gradient around sun and moon
                float sunGlowDist = distance(worldDir.xyz, lightDir);
                float sunGlow = smoothstep(_CelestialGlowSize * _SunRadius, 0.0, sunGlowDist);
                sunGlow = pow(sunGlow, 2.0) * _CelestialGlowIntensity * saturate(lightDir.y);
                
                float moonGlowDist = distance(worldDir.xyz, -lightDir);
                float moonGlow = smoothstep(_CelestialGlowSize * _MoonRadius, 0.0, moonGlowDist);
                moonGlow = pow(moonGlow, 2.0) * _CelestialGlowIntensity * saturate(-lightDir.y);

                float3 sunAndMoon = (sunDisc * _SunColor.rgb) + (moonDisc * _MoonColor.rgb);
                float3 celestialGlow = (sunGlow * _SunGlowColor.rgb) + (moonGlow * _MoonGlowColor.rgb);
                float3 halo = moonHalo * _HaloColor.rgb;
                
                // Lightning effect on clouds - localized lighting
                float3 distantLightningBolts = float3(0, 0, 0);
                #ifdef LIGHTNING
                float lightningIntensity;
                float3 lightningPosition;
                generateLightning(_Time.y, lightningIntensity, lightningPosition);
                
                if(lightningIntensity > 0.001)
                {
                    // Calculate distance from current sky position to lightning
                    float distToLightning = distance(worldDir, lightningPosition);
                    
                    // Localization: 0 = global flash, 1 = very localized around bolt
                    float lightningFalloff = lerp(1.0, smoothstep(0.8, 0.0, distToLightning), _LightningLocalization);
                    
                    // Apply to clouds only, with falloff, using custom color
                    cloudsColoredCombined += lightningIntensity * _LightningColor.rgb * cloudsCombined * lightningFalloff;
                    
                    // Generate distant visible lightning bolts
                    #ifdef DISTANT_LIGHTNING
                    distantLightningBolts = generateDistantLightning(worldDir, _Time.y, lightningPosition, lightningIntensity);
                    #endif
                }
                #endif
                
                sunAndMoon *= cloudsNegative;
                celestialGlow *= cloudsNegative;
                halo *= cloudsNegative;

                // Procedural Stars
                float3 stars = generateProceduralStars(skyDir, _Time.y);
                #ifdef STARS_ALWAYS_VISIBLE
                // Stars always visible
                #else
                stars *= saturate(-lightDir.y);
                #endif
                stars += (baseNoise * _StarsSkyColor.rgb * 0.1);
                stars *= cloudsNegative;
                
                // Shooting Stars
                #ifdef SHOOTING_STARS
                float3 shootingStars = generateShootingStars(skyDir, _Time.y);
                #ifdef STARS_ALWAYS_VISIBLE
                // Shooting stars always visible
                #else
                shootingStars *= saturate(-lightDir.y);
                #endif
                shootingStars *= cloudsNegative;
                stars += shootingStars;
                #endif
                
                // Milky Way
                float3 milkyWay = float3(0, 0, 0);
                #ifdef MILKY_WAY
                milkyWay = generateMilkyWay(worldDir);
                milkyWay *= saturate(-lightDir.y); // Only at night
                milkyWay *= cloudsNegative;
                stars += milkyWay;
                #endif
                
                // Aurora (Northern Lights)
                float3 aurora = float3(0, 0, 0);
                #ifdef AURORA
                aurora = generateAurora(worldDir, _Time.y);
                aurora *= saturate(-lightDir.y * 2.0); // Only at night
                aurora *= cloudsNegative;
                #endif

                // Sky gradients
                float3 gradientDay = lerp(_DayBottomColor.rgb, _DayTopColor.rgb, saturate(horizon));
                float3 gradientNight = lerp(_NightBottomColor.rgb, _NightTopColor.rgb, saturate(horizon));
                float3 skyGradients = lerp(gradientNight, gradientDay, dayAmount) * cloudsNegative;
                
                // Atmospheric Haze near horizon
                float hazeFactor = smoothstep(_HazeHeight, 0.0, abs(worldDir.y));
                float3 hazeColor = lerp(_HazeNightColor.rgb, _HazeDayColor.rgb, dayAmount);
                float3 atmosphericHaze = hazeColor * hazeFactor * _HazeIntensity * cloudsNegative;

                // Sunset/rise
                float sunset = saturate((1 - horizon) * saturate(lightDir.y * 5));
                // interpolate between start and end colors based on how high above the horizon the sun is
                float tColor = saturate(lightDir.y * 2.0);
                float3 sunsetColor = lerp(_SunsetColorStart.rgb, _SunsetColorEnd.rgb, tColor);
                // zenith slider smoothly fades out sunrise colors as sun rises
                float zenithFactor = 1.0 - saturate(lightDir.y * _ZenithFade);
                float3 sunsetColoured = sunset * sunsetColor * zenithFactor;
                
                // Rainbow
                float3 rainbow = float3(0, 0, 0);
                #ifdef RAINBOW
                rainbow = generateRainbow(worldDir, lightDir);
                rainbow *= cloudsNegative;
                #endif

                // Horizon glow
                float3 horizonGlow = saturate((1 - horizon * 5) * saturate(lightDir.y * 10)) * _HorizonColorDay.rgb;
                float3 horizonGlowNight = saturate((1 - horizon * 5) * saturate(-lightDir.y * 10)) * _HorizonColorNight.rgb;
                horizonGlow += horizonGlowNight;

                float3 combined = skyGradients + atmosphericHaze + sunAndMoon + celestialGlow + halo + sunsetColoured + stars + aurora + rainbow + cloudsColoredCombined + horizonGlow + distantLightningBolts;
                
                return float4(combined, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
