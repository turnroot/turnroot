Shader "Turnroot/Water"
{
    Properties
    {
        [Header(Water Color and Depth)]
        _ShallowColor("Shallow Color", Color) = (0.35, 0.65, 0.80, 0.4)
        _DeepColor("Deep Color", Color) = (0.05, 0.18, 0.35, 0.92)
        _DepthDistance("Depth Distance", Float) = 4.0
        // At exactly 0 depth (no geometry below) use shallow. At DepthDistance+, use deep.

        [Header(Fresnel)]
        // As the camera angle becomes more glancing, fresnel pushes toward FresnelColor
        _FresnelColor("Fresnel Color", Color) = (0.75, 0.88, 1.0, 1.0)
        _FresnelPower("Fresnel Power", Range(0.5, 12)) = 4.0
        _FresnelStrength("Fresnel Strength", Range(0, 1)) = 0.6

        [Header(Murkiness)]
        _MurkinessTex("Murkiness Texture", 2D) = "white" {}
        _MurkinessStrength("Murkiness Strength", Range(0, 1)) = 0.25
        _MurkinessScale("Murkiness UV Scale", Float) = 1.0
        [Toggle(_MURKINESS_ANIM_ON)] _murkiness_anim("Animate Murkiness", Float) = 1
        _MurkinessSpeedX("Murkiness Speed X", Float) = 0.04
        _MurkinessSpeedY("Murkiness Speed Y", Float) = 0.02

        [Header(Surface Reflections)]
        // Light specular highlights off the water surface.
        // The wave normal is derived from the same noise as ripples/distortion,
        // so highlights break up naturally. Murkiness suppresses them.
        _SpecularColor("Specular Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _SpecularStrength("Specular Strength", Range(0, 2)) = 0.8
        _SpecularPower("Specular Power", Range(1, 512)) = 128
        // How much the ripple/distortion noise bends the surface normal for specular
        _SpecularNormalStrength("Wave Normal Strength", Range(0, 1)) = 0.3
        // Murkiness suppresses specular — 0 = murk has no effect, 1 = fully suppressed
        _SpecularMurkinessSuppress("Murkiness Suppression", Range(0, 1)) = 0.7

        [Header(Intersection Ripples)]
        _RippleColor("Ripple Color", Color) = (1, 1, 1, 1)
        // How far from a mesh surface (in world units) ripples appear
        _RippleDistance("Ripple Distance", Float) = 1.2
        // How many concentric ring bands appear within that distance
        _RippleCount("Ripple Ring Count", Float) = 5.0
        // How fast rings animate outward (0 = static)
        _RippleSpeed("Ripple Speed", Float) = 0.8
        // Overall opacity of the ripple rings
        _RippleOpacity("Ripple Opacity", Range(0, 1)) = 0.85
        // How strongly shadows suppress ripples. 0 = ripples unaffected by shadow,
        // 1 = ripples fully absent in shadowed areas. Values in between give a
        // soft falloff so the transition from lit to shadowed water is gradual.
        _RippleShadowMask("Ripple Shadow Mask", Range(0, 1)) = 0.75
        // Width of each ring line: 0 = hair thin, 1 = full block
        _RippleSharpness("Ripple Sharpness", Range(1, 40)) = 12.0
        // Noise texture distorts ring shape for stylized look
        _RippleNoiseTex("Ripple Noise Texture", 2D) = "white" {}
        _RippleNoiseScale("Ripple Noise Scale", Float) = 0.6
        _RippleNoiseStrength("Ripple Noise Strength", Range(0, 2)) = 0.45
        // Second noise layer at different scale for more complexity
        _RippleNoiseScale2("Ripple Noise Scale 2", Float) = 1.4
        _RippleNoiseStrength2("Ripple Noise Strength 2", Range(0, 1)) = 0.2
        // Fade ripples out at the very edge of their range (softens the outer boundary)
        _RippleEdgeFade("Ripple Edge Fade", Range(0, 1)) = 0.25
        // How fast the noise texture scrolls (adds variation to ripple breaks over time)
        _RippleNoiseSpeed("Ripple Noise Speed", Float) = 0.05
        // Warps the world-space distance used to place ring bands, making concentric
        // rings irregular and organic. 0 = perfect circles, higher = wobbly/broken.
        _RippleDistortion("Ripple Distortion", Range(0, 2)) = 0.4
        // Adds a per-pixel phase offset to the ring animation based on world position.
        // This prevents all edge pixels from peaking simultaneously, which causes a
        // visible "pulse" ~once per second when the edge is a flat surface. At 0 the
        // pulse is fully present; increasing this breaks it up progressively.
        _RipplePhaseJitter("Ripple Phase Jitter", Range(0, 1)) = 0.5
        // Distorts the screen-space depth sample using the noise texture, giving
        // underwater edges a wobbly look. Uses the same noise texture as ripples.
        _DistortionStrength("Distortion Strength", Range(0, 0.05)) = 0.008

        [Header(Constant Intersection Foam)]
        [Toggle(_CONSTANT_FOAM_ON)] _constant_foam("Enable Constant Foam", Float) = 0
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamThickness("Foam Thickness", Float) = 0.3
        _FoamSharpness("Foam Sharpness", Range(1, 40)) = 15.0
        _FoamDistortion("Foam Distortion", Range(0, 2)) = 0.3
        _FoamNoiseStrength("Foam Noise Strength", Range(0, 2)) = 0.8

        [Header(Height Variation)]
        [Toggle(_HEIGHT_VARIATION_ON)] _height_variation("Enable Height Variation", Float) = 0
        _WaveHeight("Wave Height", Range(0, 2)) = 0.2
        _WaveDirectionX("Wave Direction X", Float) = 1.0
        _WaveDirectionZ("Wave Direction Z", Float) = 0.5
        _WaveFrequency("Wave Frequency", Range(0.1, 5)) = 1.0
        _WaveSpeed("Wave Speed", Float) = 0.5
        _WaveSmoothness("Wave Smoothness", Range(0.1, 5)) = 1.0
        _WaveNoiseScale("Wave Noise Scale", Float) = 0.5
        _WaveNoiseStrength("Wave Noise Strength", Range(0, 1)) = 0.3

        [Header(Shadows)]
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 0.25
        _ShadowColor("Shadow Color", Color) = (0.0, 0.1, 0.2, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+10"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        // ─────────────────────────────────────────────────────────────
        // MAIN FORWARD PASS
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma shader_feature_local _MURKINESS_ANIM_ON
            #pragma shader_feature_local _CONSTANT_FOAM_ON
            #pragma shader_feature_local _HEIGHT_VARIATION_ON

            // Shadow keywords — same set as the fixed character shaders
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // Provides SampleSceneDepth() — the depth buffer of opaque objects rendered before us
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _ShallowColor;
            float4 _DeepColor;
            float  _DepthDistance;

            float4 _FresnelColor;
            float  _FresnelPower;
            float  _FresnelStrength;

            float4 _MurkinessTex_ST;
            float  _MurkinessStrength;
            float  _MurkinessScale;
            float  _MurkinessSpeedX;
            float  _MurkinessSpeedY;

            float4 _SpecularColor;
            float  _SpecularStrength;
            float  _SpecularPower;
            float  _SpecularNormalStrength;
            float  _SpecularMurkinessSuppress;

            float4 _RippleColor;
            float  _RippleDistance;
            float  _RippleCount;
            float  _RippleSpeed;
            float  _RippleOpacity;
            float  _RippleShadowMask;
            float  _RippleSharpness;
            float4 _RippleNoiseTex_ST;
            float  _RippleNoiseScale;
            float  _RippleNoiseStrength;
            float  _RippleNoiseScale2;
            float  _RippleNoiseStrength2;
            float  _RippleEdgeFade;
            float  _RippleNoiseSpeed;
            float  _RippleDistortion;
            float  _RipplePhaseJitter;
            float  _DistortionStrength;

            float4 _FoamColor;
            float  _FoamThickness;
            float  _FoamSharpness;
            float  _FoamDistortion;
            float  _FoamNoiseStrength;

            float  _WaveHeight;
            float  _WaveDirectionX;
            float  _WaveDirectionZ;
            float  _WaveFrequency;
            float  _WaveSpeed;
            float  _WaveSmoothness;
            float  _WaveNoiseScale;
            float  _WaveNoiseStrength;

            float  _ShadowStrength;
            float4 _ShadowColor;
            CBUFFER_END

            TEXTURE2D(_MurkinessTex);   SAMPLER(sampler_MurkinessTex);
            TEXTURE2D(_RippleNoiseTex); SAMPLER(sampler_RippleNoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                // screenPos.xy = screen UV (before /w), screenPos.w = eye depth of fragment
                float4 screenPos   : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = input.positionOS.xyz;

                // ─────────────────────────────────────────────────────────
                // HEIGHT VARIATION
                // Two crossing sine waves at different frequencies create
                // natural-looking interference. Noise modulates amplitude
                // per-vertex (calmer/rougher patches) rather than replacing
                // the wave signal, so the slider blends between uniform
                // and organic waves instead of wave↔noise.
                // ─────────────────────────────────────────────────────────
                #if defined(_HEIGHT_VARIATION_ON)
                {
                    float3 positionWS = TransformObjectToWorld(positionOS);
                    float2 wavePos    = positionWS.xz;
                    float  waveTime   = _Time.y * _WaveSpeed;

                    float2 waveDir1 = normalize(float2(_WaveDirectionX, _WaveDirectionZ));
                    // Secondary wave rotated ~60 degrees for crossing interference
                    float2 waveDir2 = normalize(float2(
                        _WaveDirectionZ * 0.866 - _WaveDirectionX * 0.5,
                        _WaveDirectionX * 0.866 + _WaveDirectionZ * 0.5
                    ));

                    float phase1 = dot(wavePos, waveDir1) * _WaveFrequency + waveTime;
                    float wave1  = saturate(sin(phase1) * 0.5 + 0.5);
                    wave1        = pow(wave1, _WaveSmoothness);

                    // Secondary at 0.6x frequency so periods don't perfectly repeat
                    float phase2 = dot(wavePos, waveDir2) * _WaveFrequency * 0.6 + waveTime * 1.1;
                    float wave2  = saturate(sin(phase2) * 0.5 + 0.5);
                    wave2        = pow(wave2, _WaveSmoothness);

                    float waveBase = wave1 * 0.7 + wave2 * 0.3;

                    // Noise modulates amplitude — creates calmer and rougher patches
                    // without overriding the wave pattern entirely.
                    float2 noiseUV = wavePos * _WaveNoiseScale * 0.1;
                    float  waveNoise = SAMPLE_TEXTURE2D_LOD(_RippleNoiseTex, sampler_RippleNoiseTex,
                                                            TRANSFORM_TEX(noiseUV, _RippleNoiseTex), 0).r;

                    float amplitudeScale = lerp(1.0, waveNoise, _WaveNoiseStrength);
                    float heightOffset   = (waveBase * 2.0 - 1.0) * _WaveHeight * amplitudeScale;

                    positionOS += input.normalOS * heightOffset;
                }
                #endif

                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                output.positionCS  = vertexInput.positionCS;
                output.positionWS  = vertexInput.positionWS;
                output.uv          = input.uv;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;

                output.shadowCoord = GetShadowCoord(vertexInput);
                // ComputeScreenPos keeps clip-space W intact; .w = eye depth after interpolation
                output.screenPos   = ComputeScreenPos(vertexInput.positionCS);

                return output;
            }

            // ── Utility: single-octave value noise via tiled texture sample ──────────
            float SampleNoise(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_RippleNoiseTex, sampler_RippleNoiseTex,
                                        TRANSFORM_TEX(uv, _RippleNoiseTex)).r;
            }

            // ── Blinn-Phong specular for a single light ──────────────────────────────
            // Returns specular intensity (scalar) — caller multiplies by light color.
            float BlinnPhongSpecular(float3 normalWS, float3 lightDir, float3 viewDir, float power)
            {
                float3 halfDir = normalize(lightDir + viewDir);
                float  nDotH   = saturate(dot(normalWS, halfDir));
                return pow(nDotH, max(power, 1.0));
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ─────────────────────────────────────────────────────────
                // 1. DEPTH DIFFERENCE
                // ─────────────────────────────────────────────────────────
                float2 screenUV       = input.screenPos.xy / input.screenPos.w;
                float  rawSceneDepth  = SampleSceneDepth(screenUV);
                float  sceneEyeDepth  = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float  fragEyeDepth   = input.screenPos.w;
                float  depthDiff      = max(0.0, sceneEyeDepth - fragEyeDepth);

                // ─────────────────────────────────────────────────────────
                // 2. WATER COLOR & ALPHA (depth gradient)
                // ─────────────────────────────────────────────────────────
                float  depthT    = saturate(depthDiff / max(_DepthDistance, 0.001));
                float4 waterCol  = lerp(_ShallowColor, _DeepColor, depthT);

                // Fade out ripples/foam in deeper water (when viewing through water or underwater)
                // Use 1-depthT so effects are full strength at edges (depthT=0) and fade in deep water
                float depthFadeOut = 1.0 - saturate(depthT * 2.0); // fade starts at 50% depth

                // ─────────────────────────────────────────────────────────
                // 3. MURKINESS
                // ─────────────────────────────────────────────────────────
                float2 murk_uv = input.positionWS.xz * _MurkinessScale * 0.1;
                #if defined(_MURKINESS_ANIM_ON)
                    murk_uv += float2(_MurkinessSpeedX, _MurkinessSpeedY) * _Time.y;
                #endif
                float murkiness = SAMPLE_TEXTURE2D(_MurkinessTex, sampler_MurkinessTex, murk_uv).r;
                waterCol.a = saturate(waterCol.a + murkiness * _MurkinessStrength * depthT);

                // ─────────────────────────────────────────────────────────
                // 4. NOISE UVs (shared by ripples, distortion, specular normal)
                // ─────────────────────────────────────────────────────────
                float2 wXZ  = input.positionWS.xz;
                float2 wXZ2 = wXZ * 1.37 + float2(3.1, 7.4);

                float2 noiseUV1 = wXZ  * _RippleNoiseScale  * 0.1 + _Time.y * _RippleNoiseSpeed;
                float2 noiseUV2 = wXZ2 * _RippleNoiseScale2 * 0.1 - _Time.y * _RippleNoiseSpeed * 0.7;

                float n1 = SampleNoise(noiseUV1);
                float n2 = SampleNoise(noiseUV2);

                // ─────────────────────────────────────────────────────────
                // 5. WAVE NORMAL (for specular)
                // Compute a surface normal by sampling the noise texture at
                // small world-space offsets in X and Z (central differences).
                // This avoids ddx/ddy, which is constant per 2×2 pixel quad
                // and produces the blocky "square pixel" faceting artifacts.
                // ─────────────────────────────────────────────────────────
                float3 baseNormalWS = normalize(input.normalWS);

                // Sample noise at ±eps offsets in world XZ to estimate gradient
                float eps = 0.08;
                float2 nUV1_px = (wXZ + float2( eps, 0))   * _RippleNoiseScale  * 0.1 + _Time.y * _RippleNoiseSpeed;
                float2 nUV1_nx = (wXZ + float2(-eps, 0))   * _RippleNoiseScale  * 0.1 + _Time.y * _RippleNoiseSpeed;
                float2 nUV1_pz = (wXZ + float2(0,    eps)) * _RippleNoiseScale  * 0.1 + _Time.y * _RippleNoiseSpeed;
                float2 nUV1_nz = (wXZ + float2(0,   -eps)) * _RippleNoiseScale  * 0.1 + _Time.y * _RippleNoiseSpeed;

                float gradX = (SampleNoise(nUV1_px) - SampleNoise(nUV1_nx)) / (2.0 * eps);
                float gradZ = (SampleNoise(nUV1_pz) - SampleNoise(nUV1_nz)) / (2.0 * eps);

                // Build perturbed normal: tilt baseNormal by (gradX, 0, gradZ) slope
                float3 waveNormalWS = normalize(
                    baseNormalWS + float3(-gradX, 0, -gradZ) * _SpecularNormalStrength
                );

                // ─────────────────────────────────────────────────────────
                // 6. SHADOWS
                // ─────────────────────────────────────────────────────────
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, unity_ProbesOcclusion);
                #else
                    Light mainLight = GetMainLight();
                #endif

                float shadowAtten = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                waterCol.rgb = lerp(_ShadowColor.rgb * waterCol.rgb, waterCol.rgb, shadowAtten);

                // ─────────────────────────────────────────────────────────
                // 7. FRESNEL
                // ─────────────────────────────────────────────────────────
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float  nDotV     = saturate(dot(baseNormalWS, viewDirWS));
                float  fresnel   = pow(1.0 - nDotV, _FresnelPower) * _FresnelStrength;
                waterCol.rgb     = lerp(waterCol.rgb, _FresnelColor.rgb, fresnel);
                waterCol.a       = saturate(waterCol.a + fresnel * _FresnelColor.a);

                // ─────────────────────────────────────────────────────────
                // 8. SURFACE REFLECTIONS (specular)
                //
                // Blinn-Phong specular from main light + additional lights.
                // The wave normal (section 5) breaks up the highlight so it
                // shimmers and shifts with the noise animation.
                //
                // Two attenuation factors:
                //   • murkiness  — turbid water absorbs specular at murky patches
                //   • distortion — the wobble zone near edges is rougher; reuse
                //                  the distortion noise (n1 remap) to soften highlights
                //                  in the same areas where the depth edge wobbles.
                // ─────────────────────────────────────────────────────────
                float murkinessSuppress   = 1.0 - murkiness * _MurkinessStrength * _SpecularMurkinessSuppress;
                // Wave noise roughens specular independently of screen distortion strength.
                // n1 is already in 0..1; remap to a 0..1 roughness amount, scale by
                // _SpecularNormalStrength so the same slider that controls specular normal
                // intensity also controls how much the noise breaks up the highlight.
                float noiseRoughen        = saturate(abs(n1 * 2.0 - 1.0) * _SpecularNormalStrength * 2.0);
                float specularAttenuation = saturate(murkinessSuppress * (1.0 - noiseRoughen * 0.5));

                float3 specularAccum = 0.0;

                // Main light specular
                {
                    float spec = BlinnPhongSpecular(waveNormalWS, mainLight.direction, viewDirWS, _SpecularPower);
                    // Shadow attenuation also dims specular — shadowed water doesn't sparkle
                    specularAccum += spec * mainLight.color * mainLight.shadowAttenuation;
                }

                // Additional lights specular
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    int addLightCount = GetAdditionalLightsCount();
                    for (int li = 0; li < addLightCount; ++li)
                    {
                        Light addLight = GetAdditionalLight(li, input.positionWS, unity_ProbesOcclusion);
                        float spec = BlinnPhongSpecular(waveNormalWS, addLight.direction, viewDirWS, _SpecularPower);
                        specularAccum += spec * addLight.color
                                       * addLight.shadowAttenuation
                                       * addLight.distanceAttenuation;
                    }
                }
                #endif

                float3 specularContrib = specularAccum * _SpecularColor.rgb * _SpecularStrength * specularAttenuation;

                // Additive specular — bright highlights lift both color and alpha
                waterCol.rgb = saturate(waterCol.rgb + specularContrib);
                // Specular increases perceived opacity (sparkling water looks more solid)
                float specLuma = dot(specularContrib, float3(0.2126, 0.7152, 0.0722));
                waterCol.a = saturate(waterCol.a + specLuma * _SpecularColor.a);

                // ─────────────────────────────────────────────────────────
                // 9. DISTORTION (depth sample)
                // Two independent noise channels give true 2D screen-space
                // wobble. Fallback to undistorted depth when distorted sample
                // lands in front of the water plane (rock face, skybox, etc.)
                // to prevent sharp jumps at edges.
                // ─────────────────────────────────────────────────────────
                float distNoiseX = n1 * 2.0 - 1.0;
                float distNoiseY = n2 * 2.0 - 1.0;
                float2 distortedScreenUV = screenUV + float2(distNoiseX, distNoiseY) * _DistortionStrength;

                float rawSceneDepthD  = SampleSceneDepth(distortedScreenUV);
                float sceneEyeDepthD  = LinearEyeDepth(rawSceneDepthD, _ZBufferParams);

                // Fallback: if distorted sample is in front of water, use undistorted
                bool  useUndistorted  = sceneEyeDepthD <= fragEyeDepth;
                float2 safeScreenUV   = useUndistorted ? screenUV        : distortedScreenUV;
                float  safeRawDepth   = useUndistorted ? rawSceneDepth   : rawSceneDepthD;

                float  sceneEyeDepthSafe  = useUndistorted ? sceneEyeDepth : sceneEyeDepthD;
                float  depthDiffDistorted = max(0.0, sceneEyeDepthSafe - fragEyeDepth);

                // Reconstruct world-space XZ of the scene geometry under the water.
                // Horizontal distance from this to the water fragment is the correct
                // shore distance at any camera angle — eye-space depth alone is only
                // valid from above; at grazing / first-person angles it diverges badly.
                float3 sceneWorldPos = ComputeWorldSpacePosition(safeScreenUV, safeRawDepth, UNITY_MATRIX_I_VP);

                // ─────────────────────────────────────────────────────────
                // 10. INTERSECTION RIPPLES
                // Use the true XZ planar distance from the water fragment to
                // the scene geometry reconstructed from the depth buffer.
                // This is angle-independent — works from top-down, first-person,
                // and all angles in between. Eye-space depth diverges badly at
                // grazing angles and is only kept for the depth color gradient.
                // ─────────────────────────────────────────────────────────
                float horizDist = length(sceneWorldPos.xz - input.positionWS.xz);

                // ── Domain warp ────────────────────────────────────────────────────
                // Sample two noise values at a coarse world scale to warp the XZ
                // position before all subsequent ring/breakup lookups. This bends
                // the coordinate space itself rather than only offsetting horizDist,
                // so rings deform in a flowing, water-current way rather than
                // expanding and contracting radially.
                float2 warp1UV = wXZ * _RippleNoiseScale * 0.04 + _Time.y * _RippleNoiseSpeed * 0.25;
                float wx = SampleNoise(warp1UV)                  * 2.0 - 1.0;
                float wz = SampleNoise(warp1UV + float2(5.2, 1.3)) * 2.0 - 1.0;
                float2 warpedXZ = wXZ + float2(wx, wz) * _RippleDistortion;

                // Re-sample noise at warped position — all breakup uses this,
                // so strokes follow the deformed coordinate flow.
                float2 wNoiseUV1 = warpedXZ * _RippleNoiseScale  * 0.1 + _Time.y * _RippleNoiseSpeed;
                float2 wNoiseUV2 = warpedXZ * _RippleNoiseScale2 * 0.1 - _Time.y * _RippleNoiseSpeed * 0.7;
                float wn1 = SampleNoise(wNoiseUV1);
                float wn2 = SampleNoise(wNoiseUV2);

                // Warp horizDist for organic ring placement (wobbly, not perfect circles)
                float nDistort = (wn1 - 0.5) * 0.8 + (wn2 - 0.5) * 0.2;
                horizDist = max(0.0, horizDist + nDistort * _RippleDistortion * _RippleDistance * 0.25);

                float rippleMask = 1.0 - saturate(horizDist / max(_RippleDistance, 0.001));
                rippleMask *= step(0.001, depthDiffDistorted);

                float edgeFade = saturate(rippleMask / max(_RippleEdgeFade, 0.01));

                // ── Phase jitter via smooth noise (no grid artifacts) ───────────────
                // The old floor-hash produced phase discontinuities on a 0.125 WU grid
                // visible as a repeating seam on flat shores. Smooth noise interpolates
                // continuously so phase varies organically across the shoreline.
                float phaseJitter = SampleNoise(wXZ * 0.3 + float2(4.1, 8.7)) * _RipplePhaseJitter;

                // ── Ring profile: asymmetric brushstroke shape ─────────────────────
                // ring=0 is the leading (outer) edge, ring→1 is the trailing edge.
                // Sharp outer edge (water recedes quickly) + soft inner fade
                // (water arrives gradually) mimics the feel of a real brushstroke.
                float ringPhase = (horizDist / max(_RippleDistance, 0.001)) * _RippleCount
                                  - _Time.y * _RippleSpeed + phaseJitter;
                float ring = frac(ringPhase);

                float sharpness  = _RippleSharpness * 0.04;
                float outerEdge  = smoothstep(0.5 + sharpness, 0.5,              ring);
                float innerEdge  = smoothstep(0.0,             0.18 + sharpness, ring);
                float ringLine   = outerEdge * innerEdge;

                // ── Brush-stroke breakup (threshold-based, not subtractive) ────────
                // Two noise layers sampled at oblong UV scales so noise cells are
                // elongated rather than round. This produces long clean gaps and long
                // clean segments — the defining character of a brushstroke — instead
                // of uniformly stippling or thinning the ring.
                float2 strokeUV_a = float2(warpedXZ.x * 1.0, warpedXZ.y * 2.8)
                                  * _RippleNoiseScale * 0.08
                                  + _Time.y * _RippleNoiseSpeed * 0.3;
                float2 strokeUV_b = float2(warpedXZ.x * 2.5, warpedXZ.y * 0.7)
                                  * _RippleNoiseScale * 0.09
                                  + _Time.y * _RippleNoiseSpeed * 0.15 + float2(7.3, 2.1);

                float strokeA = SampleNoise(strokeUV_a);
                float strokeB = SampleNoise(strokeUV_b);
                // Blend: coarse A gives large stroke shapes, fine B adds sub-stroke texture
                float strokeMask = strokeA * 0.65 + strokeB * 0.35;

                // Threshold cut: _RippleNoiseStrength=0 → full ring, higher → more/longer breaks
                float strokeThresh = 1.0 - saturate(_RippleNoiseStrength);
                float brushBreak   = smoothstep(strokeThresh - 0.08, strokeThresh + 0.08, strokeMask);

                // Fine secondary texture within surviving stroke segments
                float fineBreak = saturate(1.0 - wn2 * _RippleNoiseStrength2);

                ringLine *= brushBreak * fineBreak;

                float rippleAlpha = ringLine * rippleMask * edgeFade * _RippleOpacity;

                // Shadow mask — lerp between full ripple (shadowAtten=1) and no ripple
                // (shadowAtten=0) based on _RippleShadowMask. At 0 the slider has no
                // effect; at 1 ripples vanish completely in shadowed areas.
                float rippleShadowFactor = lerp(1.0, shadowAtten, _RippleShadowMask);
                rippleAlpha *= rippleShadowFactor;

                // Fade out ripples in deeper water
                rippleAlpha *= depthFadeOut;

                waterCol.rgb = lerp(waterCol.rgb, _RippleColor.rgb, rippleAlpha * _RippleColor.a);
                waterCol.a   = max(waterCol.a, rippleAlpha * _RippleColor.a);

                // ─────────────────────────────────────────────────────────
                // 11. CONSTANT INTERSECTION FOAM
                // Persistent foam band at the waterline. Uses the same
                // domain-warped coordinate (warpedXZ) as the ripples so
                // foam and ripples share the same organic edge deformation.
                // Uses threshold-based noise breakup (matching ripple style)
                // rather than the old subtractive approach.
                // ─────────────────────────────────────────────────────────
                #if defined(_CONSTANT_FOAM_ON)
                {
                    // Start fresh from the raw depth difference so foam is
                    // not double-distorted on top of the already-warped horizDist.
                    // Apply the same warp as ripples via warpedXZ for consistency.
                    float2 foamWarp1UV = wXZ * _RippleNoiseScale * 0.04;
                    float fwx = SampleNoise(foamWarp1UV)                    * 2.0 - 1.0;
                    float fwz = SampleNoise(foamWarp1UV + float2(5.2, 1.3)) * 2.0 - 1.0;
                    // Share warpedXZ from ripples (already computed), scale distortion
                    // independently by _FoamDistortion vs _RippleDistortion.
                    float2 foamWarpedXZ = wXZ + float2(fwx, fwz) * _FoamDistortion;

                    float2 foamWNoiseUV = foamWarpedXZ * _RippleNoiseScale * 0.1;
                    float fn1 = SampleNoise(foamWNoiseUV);
                    float fn2 = SampleNoise(foamWNoiseUV * 1.7 + float2(3.1, 7.4));
                    float foamNDistort = (fn1 - 0.5) * 0.8 + (fn2 - 0.5) * 0.2;

                    float foamRawDist = depthDiffDistorted;
                    foamRawDist = max(0.0, foamRawDist + foamNDistort * _FoamDistortion * _FoamThickness * 0.3);

                    float foamMask = 1.0 - saturate(foamRawDist / max(_FoamThickness, 0.001));
                    foamMask *= step(0.001, depthDiffDistorted);

                    float foamEdgeFade = saturate(foamMask / max(_RippleEdgeFade, 0.01));

                    // Threshold-based foam breakup using oblong noise cells.
                    // Same principle as the brushstroke ripples — clean clumps
                    // and clean gaps rather than uniformly thinned foam.
                    float2 foamStrokeUV_a = float2(foamWarpedXZ.x * 1.0, foamWarpedXZ.y * 2.5)
                                          * _RippleNoiseScale * 0.1 + float2(1.1, 4.4);
                    float2 foamStrokeUV_b = float2(foamWarpedXZ.x * 2.3, foamWarpedXZ.y * 0.8)
                                          * _RippleNoiseScale * 0.12 + float2(6.2, 9.7);

                    float foamNA = SampleNoise(foamStrokeUV_a);
                    float foamNB = SampleNoise(foamStrokeUV_b);
                    float foamStrokeMask = foamNA * 0.6 + foamNB * 0.4;

                    // _FoamNoiseStrength drives the threshold: 0 = solid band, 1 = broken clumps
                    float foamThresh   = 1.0 - saturate(_FoamNoiseStrength * 0.85);
                    float foamBreak    = smoothstep(foamThresh - 0.1, foamThresh + 0.1, foamStrokeMask);

                    // Sharp inner/outer edges using _FoamSharpness
                    float foamSharp   = _FoamSharpness * 0.04;
                    float foamInner   = smoothstep(0.0,           0.15 + foamSharp, foamMask);
                    float foamOuter   = smoothstep(foamSharp * 2.0, 0.0,            1.0 - foamMask);
                    float foamPattern = foamInner * foamOuter * foamBreak * foamEdgeFade;

                    float foamShadowFactor = lerp(1.0, shadowAtten, _RippleShadowMask);
                    foamPattern *= foamShadowFactor * depthFadeOut;

                    waterCol.rgb = lerp(waterCol.rgb, _FoamColor.rgb, foamPattern * _FoamColor.a);
                    waterCol.a   = max(waterCol.a, foamPattern * _FoamColor.a);
                }
                #endif

                // Final clamp
                waterCol = saturate(waterCol);
                return waterCol;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // SHADOW CASTER (optional — transparent water usually doesn't
        // cast hard shadows, but included so the toggle works if needed)
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _DepthDistance; // dummy — keeps CBUFFER non-empty
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #if !defined(SHADOWS_SHADOWMASK)
            float3 _LightDirection;
            float3 _LightPosition;
            #endif

            Varyings shadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));

                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return output;
            }

            half4 shadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // DEPTH NORMALS
        // Water is transparent so we don't write to the depth/normals
        // buffer (ZWrite Off, ColorMask 0). However we still need to
        // write the rendering layer to SV_Target1 when URP binds
        // _CameraRenderingLayersTexture — otherwise the decal projector's
        // layer-mask check fails for any pixel covered by the water mesh
        // even though decals aren't projected onto transparent surfaces.
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   dnVert
            #pragma fragment dnFrag
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            // Injects _WRITE_RENDERING_LAYERS when URP needs the second MRT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _DepthDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #if defined(_WRITE_RENDERING_LAYERS)
            struct FragmentOutput
            {
                half4 color           : SV_Target0;
                float renderingLayers : SV_Target1;
            };
            #endif

            Varyings dnVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            #if defined(_WRITE_RENDERING_LAYERS)
            FragmentOutput dnFrag(Varyings input)
            #else
            half4 dnFrag(Varyings input) : SV_Target
            #endif
            {
                #if defined(_WRITE_RENDERING_LAYERS)
                    FragmentOutput output;
                    output.color          = 0;
                    output.renderingLayers = float(GetMeshRenderingLayer());
                    return output;
                #else
                    return 0;
                #endif
            }
            ENDHLSL
        }
    }
}
