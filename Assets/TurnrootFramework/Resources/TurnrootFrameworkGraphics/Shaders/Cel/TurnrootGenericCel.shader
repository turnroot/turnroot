Shader "Turnroot/Generic Cel Shader"
{
    Properties
    {
        [Header(Outlines)]
        [Toggle(_USE_OUTLINES_ON)] _use_outlines("Use Outlines", Float) = 0
        _ASEOutlineWidth("Outline Width", Range(0, .03)) = 0.002
        _ASEOutlineColor("Outline Color", Color) = (0.0, 0.0, 0, 1)
        [HideInInspector] _ASEOutalpha("_ASEOutalpha", Range(-1, 0)) = 0

        [Header(Main)]
        _MainTex("Main Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _Cutoff("Mask Alpha Clip Cutoff", Float) = 0.5
        [HideInInspector] _texcoord("", 2D) = "white" {}

        _Cel_Shader_Offset("Cel Shader Offset", Range(0, 1)) = 0.64

        _light("Light", Color) = (1, 1, 1, 0)
        _dark("Dark", Color) = (0, 0, 0, 0)
        _BaseTint("Base Tint", Color) = (1, 1, 1, 0)
        [Header(Night Tint)]
        _NightTintColor("Night Tint Color", Color) = (0.1, 0.13, 0.25, 1)
        [Range(0,1)]
        _NightTintIntensity("Night Tint Intensity", Float) = 0.0
        [Toggle(_USE_LIGHT_TEX_ON)] _use_light_tex("Use Light Texture", Float) = 0
        _LightTex("Light Texture", 2D) = "white" {}
        [Toggle(_USE_DARK_TEX_ON)] _use_dark_tex("Use Dark Texture", Float) = 0
        _DarkTex("Dark Texture", 2D) = "black" {}

        [Header(Main Emissive)]
        [Toggle(_USE_MAIN_EMISSIVE_ON)] _use_main_emissive("Use Main Emissive", Float) = 0
        _Main_Emissive_Tex("Main Emissive Texture", 2D) = "white" {}
        _Main_Emissve_color("Main Emissive Color", Color) = (1, 1, 1, 0)
        _Main_Emissve_power("Main Emissive Power", Range(-1, 5)) = 1

        [Header(Matcap)]
        // NOTE: keyword is _USE_MATCAT_ON (no second P) — matches Toggle and #if defined usage
        [Toggle(_USE_MATCAT_ON)] _use_matcat("Use Mat Cap", Float) = 0
        _matcap("Matcap Texture", 2D) = "white" {}
        _special_buff_switch("Matcap Switch Mask", 2D) = "white" {}
        _special_buff_switch_edge_hardness("Matcap Switch Edge Hardness", Range(0, 22)) = 1
        _special_buff_dissolve("Matcap Switch Dissolve", Range(0, 1)) = 1

        [Header(Matcap Controls)]
        _MatcapIntensity("Matcap Intensity", Range(0, 1)) = 1
        _MatcapObjectSpace("Matcap Object-Space", Float) = 0
        [Toggle(_USE_MATCAP_REFLECTION_ON)] _use_matcap_reflection("Matcap Reflection Mode", Float) = 1

        [Header(Matcap Emissive)]
        [Toggle(_USE_MATCAP_EMISSIVE_ON)] _use_matcap_emissive("Use Matcap Emissive", Float) = 0
        _Matcap_Emissive_Tex("Matcap Emissive Texture", 2D) = "white" {}
        _Matcap_Emissve_color("Matcap Emissive Color", Color) = (0, 0, 0, 0)
        _Matcap_Emissve_power("Matcap Emissive Power", Range(-1, 3)) = 0

        [Header(Matcap Animation)]
        [Toggle(_USE_MATCAP_ANIMATION_ON)] _use_matcap_animation("Animate Matcap Texture", Float) = 0
        _matcap_animation_speed("Matcap Animation Speed", Range(0, 10)) = 1

        [Header(Fresnel)]
        [Toggle(_USE_FRENSEL_ON)] _use_frensel("Use Fresnel", Float) = 0
        _frensel_range("Fresnel Range", Range(-1, 1)) = .6
        _frensel_hard("Fresnel Hardness", Range(0, 1)) = .8
        _frensel_power("Fresnel Power", Range(0, 3)) = 1
        [HDR] _frensel_color("Fresnel Color", Color) = (0, 0, 0, 0)

        [Header(Shading)]
        _Shadow_Strength("Shadow Strength", Range(0, 1)) = 1
        _Shadow_Replace("Shadow Replace", Float) = 0
        _Shadow_Roughness("Shadow Roughness", Range(0, 1)) = 0
        _Shadow_Offset("Shadow Offset", Range(0, 1)) = 0.64
        _Shadow_Smoothness("Shadow Smoothness", Range(0, 1)) = 0
        [Toggle(_USE_SHADOW_NOISE_ON)] _use_shadow_noise("Use Shadow Noise", Float) = 0
        _Shadow_Noise_Tex("Shadow Noise Texture", 2D) = "white" {}
        _Shadow_Noise_Amount("Shadow Noise Amount", Range(0, 1)) = 0

        _Highlight_Amount("Highlight Amount", Range(0, 3)) = 1
        _Highlight_Replace("Highlight Replace", Float) = 0
        _Highlight_Offset("Highlight Offset", Range(-1, 1)) = 0.64
        _Highlight_Smoothness("Highlight Smoothness", Range(0, 1)) = 0
        _Highlight_Roughness("Highlight Roughness", Range(0, 1)) = 0
        [Toggle(_USE_HIGHLIGHT_MASK_ON)] _use_highlight_mask("Use Highlight Mask", Float) = 0
        _Highlight_Mask_Tex("Highlight Mask Texture", 2D) = "white" {}
        _Highlight_Mask_Amount("Highlight Mask Amount", Range(0, 1)) = 1
        _Highlight_Saturation("Highlight Saturation", Range(0, 2)) = 0
        _Show_Masks("Show Masks (RGB S/M/H)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest+0"
            "RenderPipeline" = "UniversalPipeline"
        }

        // ─────────────────────────────────────────────────────────────
        // OUTLINE PASS
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   outlineVert
            #pragma fragment outlineFrag
            #pragma shader_feature_local _USE_OUTLINES_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _ASEOutlineWidth;
            float4 _ASEOutlineColor;
            float _ASEOutalpha;
            float _MatcapIntensity;
            float _MatcapObjectSpace;
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

            #ifdef _USE_OUTLINES_ON
            Varyings outlineVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                input.positionOS.xyz += input.normalOS * _ASEOutlineWidth;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 outlineFrag(Varyings input) : SV_Target
            {
                clip(_ASEOutalpha);
                return half4(_ASEOutlineColor.rgb, 1);
            }
            #else
            // Collapse to zero-area triangles — no rasterisation, no fragment cost
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

            // Feature toggles
            #pragma shader_feature_local _USE_MATCAT_ON           // FIX: was _USE_MATCAP_ON — must match Toggle keyword & #if defined
            #pragma shader_feature_local _USE_FRENSEL_ON
            #pragma shader_feature_local _USE_MAIN_EMISSIVE_ON
            #pragma shader_feature_local _USE_MATCAP_EMISSIVE_ON
            #pragma shader_feature_local _USE_MATCAP_REFLECTION_ON
            #pragma shader_feature_local _USE_MATCAP_ANIMATION_ON
            #pragma shader_feature_local _USE_LIGHT_TEX_ON
            #pragma shader_feature_local _USE_DARK_TEX_ON
            #pragma shader_feature_local _USE_SHADOW_NOISE_ON
            #pragma shader_feature_local _USE_HIGHLIGHT_MASK_ON

            // FIX: Added _MAIN_LIGHT_SHADOWS_SCREEN — required for Unity 6 URP screen-space shadows.
            // Without this variant, objects never receive shadows cast by other objects.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // FIX: URP 14+ (Unity 6) replaced _SHADOWS_SOFT with quality-tier keywords.
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // FIX: DBuffer decal keywords — without these the shader never reads decal data and
            // URP projector decals are invisible on this surface.
            #pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            // Required for decal normal blending modes
            #pragma multi_compile _ DECAL_NORMAL_BLEND_LOW DECAL_NORMAL_BLEND_MEDIUM DECAL_NORMAL_BLEND_HIGH
            // Required when Decal Renderer Feature has "Use Rendering Layers" enabled
            #pragma multi_compile _ _DECAL_LAYERS
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            // Required for Rendering Debugger material override support
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // Provides ApplyDecalToBaseColor / ApplyDecalToSurfaceData for DBuffer path
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _NormalMap_ST;
            float4 _special_buff_switch_ST;
            float4 _Main_Emissive_Tex_ST;
            float4 _Matcap_Emissive_Tex_ST;
            float4 _LightTex_ST;
            float4 _DarkTex_ST;
            float4 _Shadow_Noise_Tex_ST;
            float4 _Highlight_Mask_Tex_ST;
            float  _Cel_Shader_Offset;
            float4 _light;
            float4 _dark;
            float4 _BaseTint;
            float4 _NightTintColor;
            float  _NightTintIntensity;
            float  _frensel_range;
            float  _frensel_hard;
            float  _frensel_power;
            float4 _frensel_color;
            float4 _Main_Emissve_color;
            float  _Main_Emissve_power;
            float4 _Matcap_Emissve_color;
            float  _Matcap_Emissve_power;
            float  _Cutoff;
            float  _MatcapObjectSpace;
            float  _MatcapIntensity;
            float  _matcap_animation_speed;
            float  _use_light_tex;
            float  _use_dark_tex;
            float  _use_highlight_mask;
            float  _Highlight_Mask_Amount;
            float  _Shadow_Strength;
            float  _Shadow_Replace;
            float  _Shadow_Roughness;
            float  _Shadow_Offset;
            float  _Shadow_Smoothness;
            float  _use_shadow_noise;
            float  _Shadow_Noise_Amount;
            float  _Highlight_Amount;
            float  _Highlight_Replace;
            float  _Highlight_Offset;
            float  _Highlight_Smoothness;
            float  _Highlight_Roughness;
            float  _Highlight_Saturation;
            float  _Show_Masks;
            float  _special_buff_switch_edge_hardness;
            float  _special_buff_dissolve;
            CBUFFER_END

            TEXTURE2D(_MainTex);             SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);           SAMPLER(sampler_NormalMap);
            TEXTURE2D(_matcap);              SAMPLER(sampler_matcap);
            TEXTURE2D(_LightTex);            SAMPLER(sampler_LightTex);
            TEXTURE2D(_DarkTex);             SAMPLER(sampler_DarkTex);
            TEXTURE2D(_Shadow_Noise_Tex);    SAMPLER(sampler_Shadow_Noise_Tex);
            TEXTURE2D(_Highlight_Mask_Tex);  SAMPLER(sampler_Highlight_Mask_Tex);
            TEXTURE2D(_special_buff_switch); SAMPLER(sampler_special_buff_switch);
            TEXTURE2D(_Main_Emissive_Tex);   SAMPLER(sampler_Main_Emissive_Tex);
            TEXTURE2D(_Matcap_Emissive_Tex); SAMPLER(sampler_Matcap_Emissive_Tex);

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

                // FIX: GetShadowCoord handles both cascade and screen-space shadow paths
                // correctly once all multi_compile variants are declared above.
                output.shadowCoord = GetShadowCoord(vertexInput);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ── Alpha clip ──────────────────────────────────────────────
                float2 uv_MainTex = TRANSFORM_TEX(input.uv, _MainTex);
                float4 baseTex    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv_MainTex);
                // apply base tint towards target colour using alpha as strength
                // treat white tint with full alpha as 'no tint' (legacy materials)
                float blend = _BaseTint.a;
                #ifdef UNITY_PRECISION_HIGHP
                if (blend > 0 && all(_BaseTint.rgb >= float3(0.999,0.999,0.999))) blend = 0;
                #else
                if (blend > 0 && all(_BaseTint.rgb >= float3(0.99,0.99,0.99))) blend = 0;
                #endif
                baseTex.rgb = lerp(baseTex.rgb, _BaseTint.rgb, blend);

                // Night tint (driven by SceneSkyboxSetter)
                baseTex.rgb = lerp(baseTex.rgb, _NightTintColor.rgb, _NightTintIntensity);

                clip(baseTex.a - _Cutoff);

                // ── DBuffer Decals ──────────────────────────────────────────
                // Store the original albedo before decal, then apply decal to a copy.
                // We re-composite POST-lighting so the decal shows uniformly in both
                // lit and shadowed areas — if we only apply pre-lighting the cel
                // highlight pass washes the decal out and it only appears in shadow.
                float3 preDecalRgb = baseTex.rgb;
                #if defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
                    ApplyDecalToBaseColor(input.positionCS, baseTex.rgb);
                #endif
                // How strongly a decal modified this pixel (0 = no decal, 1 = full coverage)
                float decalMask = saturate(length(baseTex.rgb - preDecalRgb) * 4.0);

                // ── Main Emissive ───────────────────────────────────────────
                #if defined(_USE_MAIN_EMISSIVE_ON)
                {
                    float2 uv_e   = TRANSFORM_TEX(input.uv, _Main_Emissive_Tex);
                    float  eVal   = SAMPLE_TEXTURE2D(_Main_Emissive_Tex, sampler_Main_Emissive_Tex, uv_e).r;
                    baseTex      += eVal * (_Main_Emissve_color * _Main_Emissve_power);
                }
                #endif

                float4 albedo = baseTex;

                // ── Normal ──────────────────────────────────────────────────
                float2 uv_NormalMap  = TRANSFORM_TEX(input.uv, _NormalMap);
                float3 tangentNormal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv_NormalMap));
                float3x3 tbn         = float3x3(input.tangentWS.xyz, input.bitangentWS, input.normalWS);
                float3 normalWS      = normalize(mul(tangentNormal, tbn));

                // ── View dir ────────────────────────────────────────────────
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // ── Light / Dark texture tints ───────────────────────────────
                float4 lightCol = _light;
                #if defined(_USE_LIGHT_TEX_ON)
                {
                    float2 uv_lt = TRANSFORM_TEX(input.uv, _LightTex);
                    lightCol = SAMPLE_TEXTURE2D(_LightTex, sampler_LightTex, uv_lt) * _light;
                }
                #endif

                float4 darkCol = _dark;
                #if defined(_USE_DARK_TEX_ON)
                {
                    float2 uv_dt = TRANSFORM_TEX(input.uv, _DarkTex);
                    darkCol = SAMPLE_TEXTURE2D(_DarkTex, sampler_DarkTex, uv_dt) * _dark;
                }
                #endif

                // ── Shadow noise ─────────────────────────────────────────────
                float noiseN = 0.0;
                #if defined(_USE_SHADOW_NOISE_ON)
                {
                    float2 uv_noise = TRANSFORM_TEX(input.uv, _Shadow_Noise_Tex);
                    noiseN = SAMPLE_TEXTURE2D(_Shadow_Noise_Tex, sampler_Shadow_Noise_Tex, uv_noise).r - 0.5;
                }
                #endif

                // ── Lighting accumulation ────────────────────────────────────
                // Main light controls shadow/mid/highlight cel bands
                float shadowMask    = 0.0;
                float highlightMask = 0.0;
                
                // Additional lights accumulate colored highlights separately
                float3 additionalHighlights = float3(0.0, 0.0, 0.0);
                // Additional lights also accumulate shadow darkening (lit-but-occluded areas)
                float  additionalShadowDark = 0.0;

                // Main light — controls base cel shading (shadow/mid/highlight)
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, unity_ProbesOcclusion);
                #else
                    Light mainLight = GetMainLight();
                #endif

                {
                    float luma = dot(mainLight.color, float3(0.2126, 0.7152, 0.0722));
                    if (luma > 0.0001)
                    {
                        float ndl       = dot(normalWS, mainLight.direction);
                        float atten     = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                        float hl        = (ndl * atten + 1.0) * 0.5;
                        float hl_noised = hl + noiseN * _Shadow_Noise_Amount;

                        float shadowExp    = lerp(0.6, 3.0, 1.0 - _Shadow_Roughness);
                        float highlightExp = lerp(0.6, 3.0, 1.0 - _Highlight_Roughness);

                        float sBase = 1.0 - smoothstep(_Shadow_Offset, _Shadow_Offset + max(_Shadow_Smoothness, 0.0001), hl_noised);
                        float hBase = smoothstep(_Highlight_Offset, _Highlight_Offset + max(_Highlight_Smoothness, 0.0001), hl);

                        shadowMask    = pow(saturate(sBase), shadowExp);
                        highlightMask = pow(saturate(hBase), highlightExp);
                    }
                }

                // Additional lights — add colored highlights and cast shadows
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    int addLightCount = GetAdditionalLightsCount();
                    for (int li = 0; li < addLightCount; ++li)
                    {
                        Light addLight = GetAdditionalLight(li, input.positionWS, unity_ProbesOcclusion);
                        float distAtten = addLight.distanceAttenuation;

                        // Skip lights with no reach at this pixel
                        if (distAtten > 0.001)
                        {
                            float ndl = dot(normalWS, addLight.direction);

                            if (ndl > 0.0)
                            {
                                // Use ndl * distAtten as the raw influence.
                                // NOTE: do NOT threshold against _Highlight_Offset here —
                                // that value is tuned for the main light's half-Lambert
                                // (ndl*atten+1)*0.5 remapping. Additional lights use raw
                                // ndl, so the offset would reject almost every point light.
                                // Instead start the smoothstep at 0 so any facing pixel
                                // contributes, with _Highlight_Smoothness controlling
                                // edge softness exactly as the user expects.
                                    // Cel shading: use ndl alone for the highlight shape.
                                // Using ndl*distAtten here causes 1/r² falloff inside the
                                // threshold — many orders of magnitude per unit of distance.
                                // The distAtten > 0.001 gate above already handles range cutoff.
                                float highlightExp = lerp(0.6, 3.0, 1.0 - _Highlight_Roughness);
                                float addHlBase = smoothstep(0.0, max(_Highlight_Smoothness, 0.001), ndl);
                                float addHlMask = pow(saturate(addHlBase), highlightExp);

                                // Highlight: light color scaled by shadow attenuation
                                additionalHighlights += addLight.color * addHlMask * addLight.shadowAttenuation;

                                // Shadow: areas that would be lit but are occluded
                                additionalShadowDark += addHlMask * (1.0 - addLight.shadowAttenuation);
                            }
                        }
                    }
                }
                #endif

                // ── Debug mask visualisation ─────────────────────────────────
                if (_Show_Masks > 0.5)
                {
                    float midMask = saturate(1.0 - max(shadowMask, highlightMask));
                    return float4(shadowMask, midMask, highlightMask, 1.0);
                }

                // ── Apply shadow ─────────────────────────────────────────────
                float4 litColor = albedo;
                float  sm       = saturate(shadowMask * _Shadow_Strength);
                if (_Shadow_Replace > 0.5)
                    litColor = lerp(albedo, darkCol, sm);
                else
                    litColor = lerp(albedo, albedo * darkCol, sm);

                // ── Apply main light highlight ───────────────────────────────
                float hm = saturate((highlightMask - shadowMask) * _Highlight_Amount);

                #if defined(_USE_HIGHLIGHT_MASK_ON)
                {
                    float2 uv_hm = TRANSFORM_TEX(input.uv, _Highlight_Mask_Tex);
                    float  hmask = SAMPLE_TEXTURE2D(_Highlight_Mask_Tex, sampler_Highlight_Mask_Tex, uv_hm).r;
                    hm = max(0.0, hm - hmask * _Highlight_Mask_Amount);
                }
                #endif

                float3 highlightRgb = albedo.rgb * lightCol.rgb;
                float  gray         = dot(highlightRgb, float3(0.299, 0.58701, 0.114));
                highlightRgb = saturate(gray + (highlightRgb - gray) * (1.0 + _Highlight_Saturation));
                float4 highlightCol = float4(highlightRgb, albedo.a);

                if (_Highlight_Replace > 0.5)
                {
                    litColor = lerp(litColor, highlightCol, hm);
                }
                else
                {
                    float4 scaledHl = highlightCol * hm;
                    litColor = 1.0 - (1.0 - litColor) * (1.0 - scaledHl);
                }

                // ── Apply additional light highlights ────────────────────────
                // Additional lights add colored highlights on top of main lighting
                #if defined(_ADDITIONAL_LIGHTS)
                {
                    // Apply highlight mask texture to additional lights too
                    float additionalMask = 1.0;
                    #if defined(_USE_HIGHLIGHT_MASK_ON)
                    {
                        float2 uv_hm = TRANSFORM_TEX(input.uv, _Highlight_Mask_Tex);
                        float  hmask = SAMPLE_TEXTURE2D(_Highlight_Mask_Tex, sampler_Highlight_Mask_Tex, uv_hm).r;
                        additionalMask = saturate(1.0 - hmask * _Highlight_Mask_Amount);
                    }
                    #endif
                    
                    // Apply saturation control to additional highlights
                    float3 additionalHl = additionalHighlights * _Highlight_Amount * albedo.rgb * additionalMask;
                    float addGray = dot(additionalHl, float3(0.299, 0.58701, 0.114));
                    additionalHl = saturate(addGray + (additionalHl - addGray) * (1.0 + _Highlight_Saturation));
                    
                    // Add additional highlights on top (additive blend)
                    litColor.rgb = saturate(litColor.rgb + additionalHl);

                    // Apply shadows from additional lights (areas lit but occluded by a shadow caster)
                    float addShadow = saturate(additionalShadowDark * additionalMask * _Shadow_Strength);
                    if (_Shadow_Replace > 0.5)
                        litColor.rgb = lerp(litColor.rgb, darkCol.rgb, addShadow);
                    else
                        litColor.rgb = lerp(litColor.rgb, litColor.rgb * darkCol.rgb, addShadow);
                }
                #endif

                // ── Matcap ───────────────────────────────────────────────────
                // FIX: pragma now declares _USE_MATCAT_ON (matching this #if and the Toggle)
                float4 basePart = litColor;
                #if defined(_USE_MATCAT_ON)
                {
                    float2 matcapUV;

                    if (_MatcapObjectSpace > 0.5)
                    {
                        // Object-space: lock highlight to object orientation
                        float3 nOS = normalize(TransformWorldToObjectDir(normalWS));
                        matcapUV   = nOS.xy * 0.5 + 0.5;
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
                        float angle  = _Time.y * _matcap_animation_speed;
                        float ca     = cos(angle);
                        float sa     = sin(angle);
                        float2x2 rot = float2x2(ca, -sa, sa, ca);
                        matcapUV     = mul(rot, matcapUV - 0.5) + 0.5;
                    }
                    #endif

                    float4 matcapCol = SAMPLE_TEXTURE2D(_matcap, sampler_matcap, matcapUV);

                    #if defined(_USE_MATCAP_EMISSIVE_ON)
                    {
                        float2 uv_me     = TRANSFORM_TEX(input.uv, _Matcap_Emissive_Tex);
                        float  emisVal   = SAMPLE_TEXTURE2D(_Matcap_Emissive_Tex, sampler_Matcap_Emissive_Tex, uv_me).r;
                        matcapCol       += emisVal * (_Matcap_Emissve_color * _Matcap_Emissve_power);
                    }
                    #endif

                    matcapCol *= _MatcapIntensity;

                    float2 uv_mask     = TRANSFORM_TEX(input.uv, _special_buff_switch);
                    float  specialVal  = SAMPLE_TEXTURE2D(_special_buff_switch, sampler_special_buff_switch, uv_mask).r;
                    float  specialLerp = lerp(_special_buff_switch_edge_hardness, -1.0, _special_buff_dissolve);
                    float  specialMask = saturate((specialVal * _special_buff_switch_edge_hardness) - specialLerp);

                    basePart = lerp(basePart, basePart + matcapCol, specialMask);
                }
                #endif

                // ── Fresnel ──────────────────────────────────────────────────
                float4 color = basePart;
                #if defined(_USE_FRENSEL_ON)
                {
                    float  nv         = dot(normalWS, viewDirWS);
                    float  rim        = 1.0 - nv;
                    float  rimSmooth  = smoothstep(_frensel_range, _frensel_range + _frensel_hard, rim);
                    float4 fresnelCol = (_frensel_color * _frensel_power) * saturate(rimSmooth);
                    color += fresnelCol;
                }
                #endif

                // ── DBuffer Decals (post-lighting composite) ─────────────────────
                // Overlay the decal-modified albedo on top of the fully-lit result.
                // decalMask is 0 where no decal was projected, so this is free on
                // non-decal pixels. baseTex.rgb already contains the decal-blended color.
                #if defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
                    color.rgb = lerp(color.rgb, baseTex.rgb, decalMask);
                #endif

                color.rgb = saturate(color.rgb);
                return color;
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
            // Avoid z-fighting on self-shadowing
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            // Required so URP passes _LightPosition when rendering point/spot shadow maps.
            // Without this the vertex shader always uses _LightDirection (directional) and
            // point/spot lights never produce correct shadow maps.
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // FIX: _LightDirection / _LightPositionAndBias are provided by URP — we just need them declared.
            // In URP 14+ these live in Shadows.hlsl already, so no manual declaration needed.

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;   // FIX: was missing — needed for shadow bias
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // FIX: _LightDirection declared in Shadows.hlsl in URP 14+; guard against redeclaration.
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
                // FIX: use actual vertex normal, not hardcoded (0,0,1)
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));

                // Clamp depth to avoid shadow pancaking artifacts on very thin geometry
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
                float2 uv    = TRANSFORM_TEX(input.uv, _MainTex);
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                clip(color.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }


        // ─────────────────────────────────────────────────────────────
        // DEPTH ONLY PASS
        // Populates _CameraDepthTexture before the decal projector runs.
        // Without this pass, the decal projector reconstructs world
        // positions from stale depth data and projects incorrectly at
        // any notable distance from the camera.
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
            float4 _MainTex_ST;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

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
                float2 uv = TRANSFORM_TEX(input.uv, _MainTex);
                clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // DEPTH NORMALS PASS
        // Populates _CameraNormalsTexture (normals) and, when URP binds
        // a second render target, _CameraRenderingLayersTexture (layers).
        // The decal projector reads both: normals for blending direction,
        // layers to decide which pixels it is allowed to paint.
        // Without the rendering-layers MRT write, the layer-mask check
        // always fails and no decal is applied regardless of what the
        // MeshRenderer's Rendering Layer Mask is set to.
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
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ DECAL_NORMAL_BLEND_LOW DECAL_NORMAL_BLEND_MEDIUM DECAL_NORMAL_BLEND_HIGH

            // Injects _WRITE_RENDERING_LAYERS when URP needs the second MRT
            // for _CameraRenderingLayersTexture (read by decal projectors to
            // decide which pixels they are allowed to paint).
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _NormalMap_ST;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

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

            // When _WRITE_RENDERING_LAYERS is active URP binds a second render
            // target (_CameraRenderingLayersTexture). We must declare a matching
            // multi-target output struct so the layer value actually gets written.
            // Without this the decal projector's layer-mask check always fails and
            // no decal is ever applied, regardless of the Rendering Layer Mask set
            // on the MeshRenderer.
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

            // Return type switches between multi-target struct and plain half4
            // depending on whether URP has bound the rendering-layers texture.
            #if defined(_WRITE_RENDERING_LAYERS)
            FragmentOutput depthNormalsFrag(Varyings input)
            #else
            half4 depthNormalsFrag(Varyings input) : SV_Target
            #endif
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv   = TRANSFORM_TEX(input.uv, _MainTex);
                float4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                clip(base.a - _Cutoff);

                float3 tangentNormal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, TRANSFORM_TEX(input.uv, _NormalMap)));
                float3x3 tbn         = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS      = normalize(mul(tangentNormal, tbn));

                // CORRECT encoding: URP's DecalProjectorPass reads _CameraNormalsTexture
                // with a plain "rgb * 2 - 1" unpack, so we must write "normalWS * 0.5 + 0.5".
                // PackNormalOctRectEncode produces a 2-channel value that gets completely
                // misread by that unpack — it was the cause of the angle-dependent failure.
                //
                // ApplyDecalToNormal is intentionally NOT called here. DepthNormals is a
                // prepass — the DBuffer has not been written yet at this point, so calling
                // it reads uninitialized memory and corrupts the normal we just computed.
                // Decal normal blending happens in ForwardLit where the DBuffer is valid.
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

    CustomEditor "Turnroot.TurnrootDefaultCharacterShader"
}
