Shader "Turnroot/Performative Generic Cel Shader"
{
    Properties
    {
        [Header(Outline)]
        [Toggle(_USE_OUTLINES_ON)] _UseOutlines("Use Outlines", Float) = 0
        _OutlineWidth("Outline Width", Range(0, 0.03)) = 0.002
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)

        [Header(Surface)]
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color Tint", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Clip Threshold", Range(0, 1)) = 0.5
        [HideInInspector] _texcoord("", 2D) = "white" {}

        [Header(Shadow Band)]
        _ShadowColor("Shadow Color", Color) = (0.35, 0.38, 0.5, 1)
        [Toggle] _ShadowColorReplace("Replace (off = multiply)", Float) = 0
        _ShadowStrength("Shadow Strength", Range(0, 1)) = 1
        _ShadowOffset("Shadow Offset", Range(-1, 1)) = 0.5
        _ShadowSoftness("Shadow Edge Softness", Range(0.001, 1)) = 0.05

        [Header(Highlight Band)]
        _HighlightColor("Highlight / Light Color", Color) = (1, 1, 1, 1)
        [Toggle] _HighlightColorReplace("Replace (off = screen blend)", Float) = 0
        _HighlightStrength("Highlight Strength", Range(0, 3)) = 1
        _HighlightOffset("Highlight Offset", Range(0, 1)) = 0.55
        _HighlightSoftness("Highlight Edge Softness", Range(0.001, 1)) = 0.05
        _HighlightSaturation("Highlight Saturation", Range(0, 2)) = 1
        [Toggle(_USE_HIGHLIGHT_MASK_ON)] _UseHighlightMask("Use Highlight Mask", Float) = 0
        _HighlightMaskTex("Highlight Mask (R)", 2D) = "black" {}
        _HighlightMaskAmount("Highlight Mask Amount", Range(0, 1)) = 1

        [Header(Cel Noise)]
        [Toggle(_USE_SHADOW_NOISE_ON)] _UseShadowNoise("Break Up Band Edges With Noise", Float) = 0
        _ShadowNoiseTex("Noise Texture (R)", 2D) = "gray" {}
        _ShadowNoiseAmount("Noise Amount", Range(0, 1)) = 0

        [Header(Night Tint)]
        _NightTintColor("Night Tint Color", Color) = (0.1, 0.13, 0.25, 1)
        _NightTintIntensity("Night Tint Intensity", Range(0, 1)) = 0

        [Header(Emission)]
        [Toggle(_USE_EMISSION_ON)] _UseEmission("Use Emission", Float) = 0
        _EmissionMap("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)

        [Header(Debug)]
        [Toggle] _ShowCelMasks("Show Cel Masks (RGB: shadow/mid/highlight)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        // ─────────────────────────────────────────────────────────────
        // OUTLINE PASS
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   outlineVert
            #pragma fragment outlineFrag
            #pragma shader_feature_local _USE_OUTLINES_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float  _OutlineWidth;
            float4 _OutlineColor;
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

            #if defined(_USE_OUTLINES_ON)
            Varyings outlineVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionOS = input.positionOS.xyz + input.normalOS * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 outlineFrag(Varyings input) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1);
            }
            #else
            // Collapsed to a zero-area triangle — no rasterisation, no fragment cost.
            Varyings outlineVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = float4(0, 0, 0, 1);
                return output;
            }

            half4 outlineFrag(Varyings input) : SV_Target
            {
                discard;
                return 0;
            }
            #endif
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // MAIN FORWARD PASS
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // ── Feature toggles ─────────────────────────────────────
            #pragma shader_feature_local _USE_EMISSION_ON
            #pragma shader_feature_local _USE_SHADOW_NOISE_ON
            #pragma shader_feature_local _USE_HIGHLIGHT_MASK_ON

            // ── URP lighting variants ───────────────────────────────
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            // Decals (DBuffer)
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ _DECAL_LAYERS

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _EmissionMap_ST;
            float4 _HighlightMaskTex_ST;
            float4 _ShadowNoiseTex_ST;

            float4 _BaseColor;
            float  _Cutoff;

            float4 _ShadowColor;
            float  _ShadowColorReplace;
            float  _ShadowStrength;
            float  _ShadowOffset;
            float  _ShadowSoftness;

            float4 _HighlightColor;
            float  _HighlightColorReplace;
            float  _HighlightStrength;
            float  _HighlightOffset;
            float  _HighlightSoftness;
            float  _HighlightSaturation;
            float  _HighlightMaskAmount;

            float  _ShadowNoiseAmount;

            float4 _NightTintColor;
            float  _NightTintIntensity;

            float4 _EmissionColor;

            float  _ShowCelMasks;
            CBUFFER_END

            TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);      SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_HighlightMaskTex); SAMPLER(sampler_HighlightMaskTex);
            TEXTURE2D(_ShadowNoiseTex);   SAMPLER(sampler_ShadowNoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float  fogFactor   : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.uv         = input.uv;
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogFactor   = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ── Base color / alpha clip ─────────────────────────────
                float2 uv_Base = TRANSFORM_TEX(input.uv, _BaseMap);
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv_Base);
                baseTex.rgb *= _BaseColor.rgb;

                // Night tint, driven externally (e.g. by a time-of-day controller).
                baseTex.rgb = lerp(baseTex.rgb, _NightTintColor.rgb, _NightTintIntensity);

                clip(baseTex.a - _Cutoff);

                float3 albedo  = baseTex.rgb;
                float3 normalWS = normalize(input.normalWS);

                // The Forward+ light loop (LIGHT_LOOP_BEGIN) requires an InputData
                // struct with exactly this name/scope to resolve per-tile lights.
                InputData inputData = (InputData)0;
                inputData.positionWS              = input.positionWS;
                inputData.normalWS                = normalWS;
                inputData.viewDirectionWS         = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                uint meshRenderingLayers = GetMeshRenderingLayer();

                // ── Cel noise sample (shared by both band edges) ─────────
                float noise = 0.5;
                #if defined(_USE_SHADOW_NOISE_ON)
                {
                    float2 uv_Noise = TRANSFORM_TEX(input.uv, _ShadowNoiseTex);
                    noise = SAMPLE_TEXTURE2D(_ShadowNoiseTex, sampler_ShadowNoiseTex, uv_Noise).r;
                }
                #endif
                float noiseOffset = (noise - 0.5) * _ShadowNoiseAmount;

                // ── Main light — shapes the base shadow/mid/highlight bands ──
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    Light mainLight = GetMainLight(input.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                float shadowMask    = 0.0;
                float highlightMask = 0.0;

                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
                {
                    float ndl  = dot(normalWS, mainLight.direction);
                    float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                    float lit      = ndl * atten;
                    float litNoised = lit + noiseOffset;

                    float shadowEdge    = max(_ShadowSoftness, 0.0001);
                    float highlightEdge = max(_HighlightSoftness, 0.0001);

                    shadowMask    = 1.0 - smoothstep(_ShadowOffset, _ShadowOffset + shadowEdge, litNoised);
                    highlightMask = smoothstep(_HighlightOffset, _HighlightOffset + highlightEdge, litNoised);
                }

                // ── Debug mask visualisation ─────────────────────────────
                if (_ShowCelMasks > 0.5)
                {
                    float midMask = saturate(1.0 - max(shadowMask, highlightMask));
                    return half4(shadowMask, midMask, highlightMask, 1.0);
                }

                // ── Apply shadow band ─────────────────────────────────────
                float sm = saturate(shadowMask * _ShadowStrength);
                float3 litColor = (_ShadowColorReplace > 0.5)
                    ? lerp(albedo, _ShadowColor.rgb, sm)
                    : lerp(albedo, albedo * _ShadowColor.rgb, sm);

                // ── Apply highlight band (main light) ────────────────────
                float hm = saturate((highlightMask - shadowMask) * _HighlightStrength);

                #if defined(_USE_HIGHLIGHT_MASK_ON)
                {
                    float2 uv_HM = TRANSFORM_TEX(input.uv, _HighlightMaskTex);
                    float  hmask = SAMPLE_TEXTURE2D(_HighlightMaskTex, sampler_HighlightMaskTex, uv_HM).r;
                    hm = max(0.0, hm - hmask * _HighlightMaskAmount);
                }
                #endif

                float3 highlightRgb = albedo * mainLight.color * _HighlightColor.rgb;
                float  gray = dot(highlightRgb, float3(0.2126, 0.7152, 0.0722));
                highlightRgb = saturate(gray + (highlightRgb - gray) * _HighlightSaturation);

                litColor = (_HighlightColorReplace > 0.5)
                    ? lerp(litColor, highlightRgb, hm)
                    : 1.0 - (1.0 - litColor) * (1.0 - saturate(highlightRgb * hm));

                // ── Additional lights ─────────────────────────────────────
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    float3 additional   = 0;
                    float highlightEdge = max(_HighlightSoftness, 0.0001);

                    #if USE_CLUSTER_LIGHT_LOOP
                    UNITY_LOOP
                    for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        Light dirLight = GetAdditionalLight(dirIndex, inputData.positionWS, half4(1, 1, 1, 1));
                        if (IsMatchingLightLayer(dirLight.layerMask, meshRenderingLayers))
                        {
                            float ndl       = dot(normalWS, dirLight.direction);
                            float angleMask = smoothstep(-highlightEdge, highlightEdge, ndl);
                            additional += dirLight.color * angleMask * dirLight.shadowAttenuation;
                        }
                    }
                    #endif

                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
                        if (IsMatchingLightLayer(addLight.layerMask, meshRenderingLayers))
                        {
                            float ndl       = dot(normalWS, addLight.direction);
                            float angleMask = smoothstep(-highlightEdge, highlightEdge, ndl);
                            additional += addLight.color * angleMask * addLight.distanceAttenuation * addLight.shadowAttenuation;
                        }
                    LIGHT_LOOP_END

                    float3 additionalRgb = albedo * additional * _HighlightColor.rgb * _HighlightStrength;

                    #if defined(_USE_HIGHLIGHT_MASK_ON)
                    {
                        float2 uv_HM = TRANSFORM_TEX(input.uv, _HighlightMaskTex);
                        float  hmask = SAMPLE_TEXTURE2D(_HighlightMaskTex, sampler_HighlightMaskTex, uv_HM).r;
                        additionalRgb *= saturate(1.0 - hmask * _HighlightMaskAmount);
                    }
                    #endif

                    float addGray = dot(additionalRgb, float3(0.2126, 0.7152, 0.0722));
                    additionalRgb = saturate(addGray + (additionalRgb - addGray) * _HighlightSaturation);

                    litColor = 1.0 - (1.0 - litColor) * (1.0 - saturate(additionalRgb));
                }
                #endif // _ADDITIONAL_LIGHTS

                // ── Emission ───────────────────────────────────────────────
                float3 color = litColor;
                #if defined(_USE_EMISSION_ON)
                {
                    float2 uv_Em = TRANSFORM_TEX(input.uv, _EmissionMap);
                    float3 emis  = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv_Em).rgb;
                    color += emis * _EmissionColor.rgb;
                }
                #endif

                // ── Decals (DBuffer) ─────────────────────────────────────
                #if defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
                    ApplyDecalToBaseColor(input.positionCS, color);
                #endif

                color = MixFog(color, input.fogFactor);
                color = saturate(color);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // SHADOW CASTER PASS
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings shadowVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
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

                output.uv = input.uv;
                return output;
            }

            half4 shadowFrag(Varyings input) : SV_Target
            {
                float2 uv    = TRANSFORM_TEX(input.uv, _BaseMap);
                float4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                clip(color.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // DEPTH ONLY PASS
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex   depthOnlyVert
            #pragma fragment depthOnlyFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings depthOnlyVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv         = input.uv;
                return output;
            }

            half depthOnlyFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = TRANSFORM_TEX(input.uv, _BaseMap);
                clip(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // DEPTH NORMALS PASS
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex   depthNormalsVert
            #pragma fragment depthNormalsFrag

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #if defined(_WRITE_RENDERING_LAYERS)
            struct FragmentOutput
            {
                half4 normalWS        : SV_Target0;
                float renderingLayers : SV_Target1;
            };
            #endif

            Varyings depthNormalsVert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv         = input.uv;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            #if defined(_WRITE_RENDERING_LAYERS)
            FragmentOutput depthNormalsFrag(Varyings input)
            #else
            half4 depthNormalsFrag(Varyings input) : SV_Target
            #endif
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv   = TRANSFORM_TEX(input.uv, _BaseMap);
                float4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                clip(base.a - _Cutoff);

                float3 normalWS = normalize(input.normalWS);
                half4 normalOut = half4(normalWS * 0.5 + 0.5, 0.0);

                #if defined(_WRITE_RENDERING_LAYERS)
                    FragmentOutput output;
                    output.normalWS        = normalOut;
                    output.renderingLayers = float(GetMeshRenderingLayer());
                    return output;
                #else
                    return normalOut;
                #endif
            }
            ENDHLSL
        }
    }
}
