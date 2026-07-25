Shader "Turnroot/Generic Cel Shader"
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
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1
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

        [Header(Matcap)]
        [Toggle(_USE_MATCAP_ON)] _UseMatcap("Use Matcap", Float) = 0
        _MatcapTex("Matcap Texture", 2D) = "white" {}
        _MatcapIntensity("Matcap Intensity", Range(0, 2)) = 1
        [Toggle] _MatcapObjectSpace("Matcap Object-Space (off = view-space)", Float) = 0
        [Toggle(_USE_MATCAP_REFLECTION_ON)] _UseMatcapReflection("Use Reflection Vector (off = normal)", Float) = 1
        [Toggle(_USE_MATCAP_ANIMATION_ON)] _UseMatcapAnimation("Animate Matcap", Float) = 0
        _MatcapAnimationSpeed("Matcap Animation Speed", Range(0, 10)) = 1
        _MatcapMaskTex("Matcap Mask (R)", 2D) = "white" {}
        _MatcapMaskHardness("Matcap Mask Edge Hardness", Range(0, 22)) = 1
        _MatcapMaskDissolve("Matcap Mask Dissolve", Range(0, 1)) = 1
        [Toggle(_USE_MATCAP_EMISSIVE_ON)] _UseMatcapEmissive("Use Matcap Emissive", Float) = 0
        _MatcapEmissiveTex("Matcap Emissive (R)", 2D) = "white" {}
        [HDR] _MatcapEmissiveColor("Matcap Emissive Color", Color) = (0, 0, 0, 1)
        _MatcapEmissivePower("Matcap Emissive Power", Range(0, 3)) = 0

        [Header(Fresnel)]
        [Toggle(_USE_FRESNEL_ON)] _UseFresnel("Use Fresnel", Float) = 0
        [HDR] _FresnelColor("Fresnel Color", Color) = (0, 0, 0, 1)
        _FresnelRange("Fresnel Range", Range(-1, 1)) = 0.6
        _FresnelHardness("Fresnel Hardness", Range(0.001, 1)) = 0.8
        _FresnelPower("Fresnel Power", Range(0, 3)) = 1

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
            #pragma shader_feature_local _USE_MATCAP_ON
            #pragma shader_feature_local _USE_MATCAP_REFLECTION_ON
            #pragma shader_feature_local _USE_MATCAP_ANIMATION_ON
            #pragma shader_feature_local _USE_MATCAP_EMISSIVE_ON
            #pragma shader_feature_local _USE_FRESNEL_ON
            #pragma shader_feature_local _USE_EMISSION_ON
            #pragma shader_feature_local _USE_SHADOW_NOISE_ON
            #pragma shader_feature_local _USE_HIGHLIGHT_MASK_ON

            // ── URP lighting variants ───────────────────────────────
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // Forward+ additional-light loop. _FORWARD_PLUS is deprecated as of
            // Unity 6.1 (URP 17) in favour of _CLUSTER_LIGHT_LOOP — using the old
            // keyword here is exactly what produced the "unbounded circle that
            // ignores walls" symptom, since URP's Forward+ path resolves lights
            // per-screen-tile and the old keyword no longer routes into that path.
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BumpMap_ST;
            float4 _EmissionMap_ST;
            float4 _HighlightMaskTex_ST;
            float4 _ShadowNoiseTex_ST;
            float4 _MatcapMaskTex_ST;
            float4 _MatcapEmissiveTex_ST;

            float4 _BaseColor;
            float  _BumpScale;
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

            float  _MatcapIntensity;
            float  _MatcapObjectSpace;
            float  _MatcapAnimationSpeed;
            float  _MatcapMaskHardness;
            float  _MatcapMaskDissolve;
            float4 _MatcapEmissiveColor;
            float  _MatcapEmissivePower;

            float4 _FresnelColor;
            float  _FresnelRange;
            float  _FresnelHardness;
            float  _FresnelPower;

            float  _ShowCelMasks;
            CBUFFER_END

            TEXTURE2D(_BaseMap);           SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);           SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMap);       SAMPLER(sampler_EmissionMap);
            TEXTURE2D(_HighlightMaskTex);  SAMPLER(sampler_HighlightMaskTex);
            TEXTURE2D(_ShadowNoiseTex);    SAMPLER(sampler_ShadowNoiseTex);
            TEXTURE2D(_MatcapTex);         SAMPLER(sampler_MatcapTex);
            TEXTURE2D(_MatcapMaskTex);     SAMPLER(sampler_MatcapMaskTex);
            TEXTURE2D(_MatcapEmissiveTex); SAMPLER(sampler_MatcapEmissiveTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                float  fogFactor   : TEXCOORD6;
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

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS    = normalInput.normalWS;
                output.tangentWS   = float4(normalInput.tangentWS, input.tangentOS.w);
                output.bitangentWS = normalInput.bitangentWS;

                output.uv = input.uv;
                output.shadowCoord = GetShadowCoord(vertexInput);
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

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

                float3 albedo = baseTex.rgb;

                // ── Normal ────────────────────────────────────────────────
                float2 uv_Bump = TRANSFORM_TEX(input.uv, _BumpMap);
                float3 tangentNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv_Bump), _BumpScale);
                float3x3 tbn = float3x3(input.tangentWS.xyz, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(tangentNormal, tbn));

                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // The Forward+ light loop (LIGHT_LOOP_BEGIN) requires an InputData
                // struct with exactly this name/scope to resolve per-tile lights.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
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
                // No baked lighting/shadowmask in this project, so we use the
                // plain realtime-shadow overload rather than the 3-arg one that
                // blends in a baked shadowmask via light-probe occlusion data.
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    Light mainLight = GetMainLight(input.shadowCoord);
                #else
                    Light mainLight = GetMainLight();
                #endif

                float shadowMask = 0.0;
                float highlightMask = 0.0;

                if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
                {
                    float ndl = dot(normalWS, mainLight.direction);
                    float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;

                    // "How lit" this pixel is, 0..1. A pixel that's either facing
                    // away from the light OR occluded by the shadow map lands at 0
                    // (shadow band) — not a mid-tone remap artefact.
                    float lit = saturate(ndl) * atten;
                    float litNoised = saturate(lit + noiseOffset);

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
                // Real point/spot (and non-main directional) lights: shadowed by
                // geometry, falls off with range, tinted by both the light's own
                // color and the material's Highlight Color.
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    float3 additional = 0;
                    float highlightEdge = max(_HighlightSoftness, 0.0001);

                    // Forward+ path: additional *directional* lights are not part
                    // of the per-tile cluster list and need this dedicated loop.
                    // NOTE: this checks the internal USE_CLUSTER_LIGHT_LOOP macro
                    // (set by RealtimeLights.hlsl from the _CLUSTER_LIGHT_LOOP
                    // keyword), matching Unity's own reference implementation —
                    // not a plain defined(_CLUSTER_LIGHT_LOOP).
                    #if USE_CLUSTER_LIGHT_LOOP
                    UNITY_LOOP
                    for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
                    {
                        Light dirLight = GetAdditionalLight(dirIndex, inputData.positionWS, half4(1, 1, 1, 1));
                        if (IsMatchingLightLayer(dirLight.layerMask, meshRenderingLayers))
                        {
                            float ndl = dot(normalWS, dirLight.direction);
                            float angleMask = smoothstep(-highlightEdge, highlightEdge, ndl);
                            additional += dirLight.color * angleMask * dirLight.shadowAttenuation;
                        }
                    }
                    #endif

                    // Point / spot lights (and, on the non-Forward+ path, all
                    // additional lights) via URP's standard tiled/flat light loop.
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
                        if (IsMatchingLightLayer(addLight.layerMask, meshRenderingLayers))
                        {
                            float ndl = dot(normalWS, addLight.direction);
                            float angleMask = smoothstep(-highlightEdge, highlightEdge, ndl);
                            // distanceAttenuation already includes URP's soft range
                            // windowing; shadowAttenuation is 0 when occluded.
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

                // ── Matcap (fakes specular) ───────────────────────────────
                float3 basePart = litColor;
                #if defined(_USE_MATCAP_ON)
                {
                    float2 matcapUV;

                    if (_MatcapObjectSpace > 0.5)
                    {
                        float3 nOS = normalize(TransformWorldToObjectDir(normalWS));
                        matcapUV = nOS.xy * 0.5 + 0.5;
                    }
                    else
                    {
                        #if defined(_USE_MATCAP_REFLECTION_ON)
                            float3 reflectDir  = reflect(-viewDirWS, normalWS);
                            float3 viewReflect = mul((float3x3)UNITY_MATRIX_V, reflectDir);
                            float  m           = 2.828427 * sqrt(max(viewReflect.z + 1.0, 0.0001));
                            matcapUV = viewReflect.xy / m + 0.5;
                        #else
                            float3 normalView = mul((float3x3)UNITY_MATRIX_V, normalWS);
                            matcapUV = normalView.xy * 0.5 + 0.5;
                        #endif
                    }

                    #if defined(_USE_MATCAP_ANIMATION_ON)
                    {
                        float angle = _Time.y * _MatcapAnimationSpeed;
                        float ca = cos(angle);
                        float sa = sin(angle);
                        float2x2 rot = float2x2(ca, -sa, sa, ca);
                        matcapUV = mul(rot, matcapUV - 0.5) + 0.5;
                    }
                    #endif

                    float3 matcapCol = SAMPLE_TEXTURE2D(_MatcapTex, sampler_MatcapTex, matcapUV).rgb;

                    #if defined(_USE_MATCAP_EMISSIVE_ON)
                    {
                        float2 uv_ME = TRANSFORM_TEX(input.uv, _MatcapEmissiveTex);
                        float  emisVal = SAMPLE_TEXTURE2D(_MatcapEmissiveTex, sampler_MatcapEmissiveTex, uv_ME).r;
                        matcapCol += emisVal * (_MatcapEmissiveColor.rgb * _MatcapEmissivePower);
                    }
                    #endif

                    matcapCol *= _MatcapIntensity;

                    float2 uv_MM = TRANSFORM_TEX(input.uv, _MatcapMaskTex);
                    float  maskVal  = SAMPLE_TEXTURE2D(_MatcapMaskTex, sampler_MatcapMaskTex, uv_MM).r;
                    float  maskLerp = lerp(_MatcapMaskHardness, -1.0, _MatcapMaskDissolve);
                    float  matcapMask = saturate((maskVal * _MatcapMaskHardness) - maskLerp);

                    basePart = lerp(basePart, basePart + matcapCol, matcapMask);
                }
                #endif

                // ── Fresnel rim ────────────────────────────────────────────
                float3 color = basePart;
                #if defined(_USE_FRESNEL_ON)
                {
                    float  nv = dot(normalWS, viewDirWS);
                    float  rim = 1.0 - nv;
                    float  rimSmooth = smoothstep(_FresnelRange, _FresnelRange + _FresnelHardness, rim);
                    color += (_FresnelColor.rgb * _FresnelPower) * saturate(rimSmooth);
                }
                #endif

                // ── Emission ───────────────────────────────────────────────
                // Added last, unaffected by cel shading — like URP/Lit.
                #if defined(_USE_EMISSION_ON)
                {
                    float2 uv_Em = TRANSFORM_TEX(input.uv, _EmissionMap);
                    float3 emis = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv_Em).rgb;
                    color += emis * _EmissionColor.rgb;
                }
                #endif

                // ── Decals (DBuffer) ─────────────────────────────────────
                // Applied to the FINAL composited color, not the pre-lit albedo.
                // ApplyDecalToBaseColor does a straight over-blend using the
                // decal's own opacity: result = color*(1-opacity) + decalColor.
                // Applied here, that means a fully opaque decal shows its exact
                // authored color regardless of shadow/highlight/matcap/fresnel —
                // by design, not "physically correct" lighting response, per
                // your call. A partially-opaque decal blends proportionally.
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
            // Required so URP passes _LightPosition when rendering point/spot
            // shadow maps; without it the vertex shader always assumes a
            // directional light and point/spot shadow maps come out wrong.
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
                float2 uv = TRANSFORM_TEX(input.uv, _BaseMap);
                float4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                clip(color.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // DEPTH ONLY PASS
        // Populates _CameraDepthTexture ahead of the decal projector, and
        // any depth-dependent post effects.
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
        // Populates _CameraNormalsTexture and, when URP needs the second
        // MRT, _CameraRenderingLayersTexture (read by decal projectors
        // using "Use Rendering Layers" to decide which pixels they may
        // paint). Decals in this shader only affect albedo, not normals,
        // so no decal-normal-blend keywords are needed here.
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

            // Injects _WRITE_RENDERING_LAYERS when URP needs the second MRT
            // for _CameraRenderingLayersTexture.
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BumpMap_ST;
            float  _BumpScale;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
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

                VertexNormalInputs ni = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS    = ni.normalWS;
                output.tangentWS   = ni.tangentWS;
                output.bitangentWS = ni.bitangentWS;
                return output;
            }

            #if defined(_WRITE_RENDERING_LAYERS)
            FragmentOutput depthNormalsFrag(Varyings input)
            #else
            half4 depthNormalsFrag(Varyings input) : SV_Target
            #endif
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = TRANSFORM_TEX(input.uv, _BaseMap);
                float4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                clip(base.a - _Cutoff);

                float2 uv_Bump = TRANSFORM_TEX(input.uv, _BumpMap);
                float3 tangentNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv_Bump), _BumpScale);
                float3x3 tbn = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(tangentNormal, tbn));

                // _CameraNormalsTexture is read back elsewhere with a plain
                // "rgb * 2 - 1" unpack, so it must be written as "* 0.5 + 0.5"
                // here (not an oct-encode, which would be misread by that unpack).
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
