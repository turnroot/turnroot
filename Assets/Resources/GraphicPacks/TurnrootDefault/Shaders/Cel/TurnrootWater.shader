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
        // Distorts the screen-space depth sample using the noise texture, giving
        // underwater edges a wobbly look. Uses the same noise texture as ripples.
        _DistortionStrength("Distortion Strength", Range(0, 0.05)) = 0.008

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

            float4 _RippleColor;
            float  _RippleDistance;
            float  _RippleCount;
            float  _RippleSpeed;
            float  _RippleOpacity;
            float  _RippleSharpness;
            float4 _RippleNoiseTex_ST;
            float  _RippleNoiseScale;
            float  _RippleNoiseStrength;
            float  _RippleNoiseScale2;
            float  _RippleNoiseStrength2;
            float  _RippleEdgeFade;
            float  _RippleNoiseSpeed;
            float  _DistortionStrength;

            float  _ShadowStrength;
            float4 _ShadowColor;
            CBUFFER_END

            TEXTURE2D(_MurkinessTex);  SAMPLER(sampler_MurkinessTex);
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
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
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

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ─────────────────────────────────────────────────────────
                // 1. DEPTH DIFFERENCE
                // Scene depth = how far the opaque pixel behind the water is
                // Fragment eye depth = how far the water surface is
                // depthDiff = underwater depth at this pixel (0 at surface intersection)
                // ─────────────────────────────────────────────────────────
                float2 screenUV       = input.screenPos.xy / input.screenPos.w;
                float  rawSceneDepth  = SampleSceneDepth(screenUV);
                float  sceneEyeDepth  = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float  fragEyeDepth   = input.screenPos.w;
                // Clamp to 0 — avoids negative values at skybox / far plane
                float  depthDiff      = max(0.0, sceneEyeDepth - fragEyeDepth);

                // ─────────────────────────────────────────────────────────
                // 2. WATER COLOR & ALPHA (depth gradient)
                // ─────────────────────────────────────────────────────────
                float  depthT    = saturate(depthDiff / max(_DepthDistance, 0.001));
                float4 waterCol  = lerp(_ShallowColor, _DeepColor, depthT);

                // ─────────────────────────────────────────────────────────
                // 3. MURKINESS
                // Adds darker/more-opaque patches via a scrolling noise texture
                // ─────────────────────────────────────────────────────────
                float2 murk_uv = input.positionWS.xz * _MurkinessScale * 0.1;
                #if defined(_MURKINESS_ANIM_ON)
                    murk_uv += float2(_MurkinessSpeedX, _MurkinessSpeedY) * _Time.y;
                #endif
                float murkiness = SAMPLE_TEXTURE2D(_MurkinessTex, sampler_MurkinessTex, murk_uv).r;
                // Murkiness raises opacity without changing hue — dark patches feel like turbid water
                waterCol.a = saturate(waterCol.a + murkiness * _MurkinessStrength * depthT);

                // ─────────────────────────────────────────────────────────
                // 4. SHADOWS
                // Darken the water surface under shadowing geometry
                // ─────────────────────────────────────────────────────────
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, unity_ProbesOcclusion);
                #else
                    Light mainLight = GetMainLight();
                #endif

                float shadowAtten = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                waterCol.rgb = lerp(_ShadowColor.rgb * waterCol.rgb, waterCol.rgb, shadowAtten);

                // ─────────────────────────────────────────────────────────
                // 5. FRESNEL
                // Glancing angles (viewed from the side) push toward FresnelColor
                // and raise opacity — simulates reflective water edges
                // ─────────────────────────────────────────────────────────
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 normalWS  = normalize(input.normalWS);
                float  nDotV     = saturate(dot(normalWS, viewDirWS));
                float  fresnel   = pow(1.0 - nDotV, _FresnelPower) * _FresnelStrength;
                waterCol.rgb     = lerp(waterCol.rgb, _FresnelColor.rgb, fresnel);
                waterCol.a       = saturate(waterCol.a + fresnel * _FresnelColor.a);

                // ─────────────────────────────────────────────────────────
                // 6. INTERSECTION RIPPLES
                //
                // depthDiff near 0 means we are right at a mesh surface.
                // We use this distance to generate animated concentric rings,
                // then distort them with two layers of noise at different scales
                // to produce the irregular, hand-drawn look from the references.
                // ─────────────────────────────────────────────────────────

                // World-XZ UVs for noise (scale independent of mesh UV)
                float2 wXZ  = input.positionWS.xz;
                float2 wXZ2 = wXZ * 1.37 + float2(3.1, 7.4); // offset for second layer

                // Animated noise UVs — scroll over time so the ripple breakup varies
                float2 noiseUV1 = wXZ  * _RippleNoiseScale  * 0.1 + _Time.y * _RippleNoiseSpeed;
                float2 noiseUV2 = wXZ2 * _RippleNoiseScale2 * 0.1 - _Time.y * _RippleNoiseSpeed * 0.7;

                // Distortion: use noise to offset the screen UV before sampling scene depth.
                // This makes the silhouette of underwater objects wobble at the waterline.
                float distNoise = SampleNoise(noiseUV1) * 2.0 - 1.0; // remap 0..1 → -1..1
                float2 distortedScreenUV = screenUV + distNoise * _DistortionStrength;
                float  rawSceneDepthD    = SampleSceneDepth(distortedScreenUV);

                // ── TRUE WORLD-SPACE DISTANCE TO INTERSECTION EDGE ──────────────────
                // The intersection (where an underwater mesh meets the water plane) is
                // where depthDiff = 0. We want the world-space XZ distance from this
                // fragment to that zero-crossing — the same N world units from a rock
                // as from a gently sloped shoreline.
                //
                // Key insight: ddx/ddy give us the screen-space rate-of-change of
                // depthDiff per pixel. Dividing depthDiff by that gradient magnitude
                // gives the pixel distance to the zero-crossing. Multiplying by
                // world-units-per-pixel converts to world space. Crucially, a steep
                // cliff has a huge gradient (fast depth change) and a tiny actual XZ
                // extent — both cancel correctly. A gentle shore has a tiny gradient
                // over a large XZ extent — also cancels correctly.
                float sceneEyeDepthD     = LinearEyeDepth(rawSceneDepthD, _ZBufferParams);
                float depthDiffDistorted = max(0.0, sceneEyeDepthD - fragEyeDepth);

                // Screen-space gradient of depthDiff (change per pixel)
                float dxD = ddx(depthDiffDistorted);
                float dyD = ddy(depthDiffDistorted);
                float gradLen = length(float2(dxD, dyD));

                // Pixel distance to the zero-crossing (the edge), guarded against /0
                float pixelDist = depthDiffDistorted / max(gradLen, 0.0001);

                // World units per screen pixel at this surface location
                float wsPerPixel = length(ddx(input.positionWS));

                // Final world-space estimate of XZ distance from intersection edge
                float horizDist = pixelDist * wsPerPixel;

                // Mask: ripples only within _RippleDistance world units of an edge.
                // step() zeros out fragments where nothing is below (sky/far plane).
                float rippleMask = 1.0 - saturate(horizDist / max(_RippleDistance, 0.001));
                rippleMask *= step(0.001, depthDiffDistorted);

                // Smooth the outer edge of the ripple zone so bands don't cut hard
                float edgeFade = saturate(rippleMask / max(_RippleEdgeFade, 0.01));

                // Animated concentric rings spaced by world-space distance from edge
                float ringPhase = (horizDist / max(_RippleDistance, 0.001)) * _RippleCount
                                  - _Time.y * _RippleSpeed;
                float ring = frac(ringPhase);  // 0→1 sawtooth, one full cycle = one ring

                // Convert sawtooth to a thin line: triangle peak at 0.5, then threshold
                float ringLine = 1.0 - abs(ring - 0.5) * 2.0;         // triangle 0..1..0
                ringLine = saturate((ringLine - 0.5) * _RippleSharpness * 0.1 + 0.5);
                ringLine = saturate(ringLine * 2.0 - 0.75);

                // Noise subtraction: sample two animated noise layers and subtract grayscale
                // from the ring brightness — punches moving holes/gaps into the rings
                float n1 = SampleNoise(noiseUV1);
                float n2 = SampleNoise(noiseUV2);
                float noiseSubtract = n1 * _RippleNoiseStrength + n2 * _RippleNoiseStrength2;
                ringLine = saturate(ringLine - noiseSubtract);

                // Combine: mask × edge fade × opacity
                float rippleAlpha = ringLine * rippleMask * edgeFade * _RippleOpacity;

                // Composite ripple over water color (additive-style so bright color shows on any bg)
                waterCol.rgb = lerp(waterCol.rgb, _RippleColor.rgb, rippleAlpha * _RippleColor.a);
                waterCol.a   = max(waterCol.a,   rippleAlpha * _RippleColor.a);

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
            // Minimal: water plane never alpha-clips so no texture needed here
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
        // DEPTH NORMALS (required for screen-space shadow path)
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite Off   // transparent — don't write depth normals for the water itself
            ColorMask 0  // no output; presence of pass satisfies the renderer feature check
            HLSLPROGRAM
            #pragma vertex   dnVert
            #pragma fragment dnFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _DepthDistance;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionCS : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings dnVert(Attributes i) {
                Varyings o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz); return o;
            }
            half4 dnFrag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
