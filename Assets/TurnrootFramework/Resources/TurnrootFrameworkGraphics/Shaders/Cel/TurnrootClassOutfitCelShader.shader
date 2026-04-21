Shader "Turnroot/Class Outfit Cel Shader"
{
    Properties
    {
        [Header(Outlines)]
        [Toggle(_USE_OUTLINES_ON)] _use_outlines("Use Outlines", Float) = 0
        _ASEOutlineWidth("Outline Width", Range(0, .005)) = 0.002
        _ASEOutlineColor("Outline Color", Color) = (0.0, 0.0, 0, 1)
        [HideInInspector] _ASEOutalpha("_ASEOutalpha", Range(-1, 0)) = 0

        [Header(Main)]
        _Base("Base", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _Cutoff("Mask Alpha Clip Cutoff", Float) = 0.5
        [HideInInspector] _texcoord("", 2D) = "white" {}

        // Tint colors provided by Visuals.cs
        _Accent_Color_1("Accent Color 1", Color) = (1, 1, 1, 1)
        _Accent_Color_2("Accent Color 2", Color) = (1, 1, 1, 1)
        _Accent_Color_3("Accent Color 3", Color) = (1, 1, 1, 1)
        _Skin_Color("Skin Color", Color) = (1, 0.87, 0.77, 1)

        _Cel_Shader_Offset("Cel Shader Offset", Range(0, 1)) = 0.64

        _light("Light", Color) = (1, 1, 1, 0)
        _dark("Dark", Color) = (0, 0, 0, 0)

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

        [Header(Additional Maps)]
        _MSE("MSE (Metal/Smooth/Emission) Map", 2D) = "white" {}
        _Tint_Mask("Tint Mask", 2D) = "black" {}

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
            float  _ASEOutlineWidth;
            float4 _ASEOutlineColor;
            float  _ASEOutalpha;
            float  _MatcapIntensity;
            float  _MatcapObjectSpace;
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
            #pragma shader_feature_local _USE_MATCAT_ON
            #pragma shader_feature_local _USE_FRENSEL_ON
            #pragma shader_feature_local _USE_MAIN_EMISSIVE_ON
            #pragma shader_feature_local _USE_MATCAP_EMISSIVE_ON
            #pragma shader_feature_local _USE_MATCAP_REFLECTION_ON
            #pragma shader_feature_local _USE_MATCAP_ANIMATION_ON
            #pragma shader_feature_local _USE_LIGHT_TEX_ON
            #pragma shader_feature_local _USE_DARK_TEX_ON
            #pragma shader_feature_local _USE_SHADOW_NOISE_ON
            #pragma shader_feature_local _USE_HIGHLIGHT_MASK_ON

            // FIX: _MAIN_LIGHT_SHADOWS_SCREEN required for Unity 6 URP screen-space shadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // FIX: URP 14+ (Unity 6) quality-tier soft shadow keywords
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            // FIX: DBuffer decal support — without these, URP projector decals are invisible
            #pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ DECAL_NORMAL_BLEND_LOW DECAL_NORMAL_BLEND_MEDIUM DECAL_NORMAL_BLEND_HIGH
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Base_ST;
            float4 _NormalMap_ST;
            float4 _special_buff_switch_ST;
            float4 _Main_Emissive_Tex_ST;
            float4 _Matcap_Emissive_Tex_ST;
            float4 _LightTex_ST;
            float4 _DarkTex_ST;
            float4 _Shadow_Noise_Tex_ST;
            float4 _Highlight_Mask_Tex_ST;
            float4 _Tint_Mask_ST;
            float4 _MSE_ST;
            float  _Cel_Shader_Offset;
            float4 _light;
            float4 _dark;
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
            float  _special_buff_switch_edge_hardness;
            float  _special_buff_dissolve;
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
            float4 _Accent_Color_1;
            float4 _Accent_Color_2;
            float4 _Accent_Color_3;
            float4 _Skin_Color;
            CBUFFER_END

            TEXTURE2D(_Base);              SAMPLER(sampler_Base);
            TEXTURE2D(_NormalMap);         SAMPLER(sampler_NormalMap);
            TEXTURE2D(_matcap);            SAMPLER(sampler_matcap);
            TEXTURE2D(_LightTex);          SAMPLER(sampler_LightTex);
            TEXTURE2D(_DarkTex);           SAMPLER(sampler_DarkTex);
            TEXTURE2D(_Shadow_Noise_Tex);  SAMPLER(sampler_Shadow_Noise_Tex);
            TEXTURE2D(_Highlight_Mask_Tex);SAMPLER(sampler_Highlight_Mask_Tex);
            TEXTURE2D(_special_buff_switch);SAMPLER(sampler_special_buff_switch);
            TEXTURE2D(_Main_Emissive_Tex); SAMPLER(sampler_Main_Emissive_Tex);
            TEXTURE2D(_Matcap_Emissive_Tex);SAMPLER(sampler_Matcap_Emissive_Tex);
            TEXTURE2D(_MSE);               SAMPLER(sampler_MSE);
            TEXTURE2D(_Tint_Mask);         SAMPLER(sampler_Tint_Mask);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float4 color      : COLOR;  // vertex color: selects skin/accent regions
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
                float4 color       : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS    = normalInput.normalWS;
                output.tangentWS   = float4(normalInput.tangentWS, input.tangentOS.w);
                output.bitangentWS = normalInput.bitangentWS;

                output.uv    = input.uv;
                output.color = input.color;

                output.shadowCoord = GetShadowCoord(vertexInput);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // ── Alpha clip ──────────────────────────────────────────────
                float2 uv_Base = TRANSFORM_TEX(input.uv, _Base);
                float4 baseTex = SAMPLE_TEXTURE2D(_Base, sampler_Base, uv_Base);
                clip(baseTex.a - _Cutoff);

                // ── DBuffer Decals ──────────────────────────────────────────
                // Applied after alpha clip but before tinting/lighting so decal
                // albedo participates in the full cel pipeline. No-op when no
                // DBuffer keyword is active (zero cost on the non-decal path).
                #if defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
                    ApplyDecalToBaseColor(input.positionCS, baseTex.rgb);
                #endif

                // ── Main Emissive ───────────────────────────────────────────
                #if defined(_USE_MAIN_EMISSIVE_ON)
                {
                    float2 uv_e = TRANSFORM_TEX(input.uv, _Main_Emissive_Tex);
                    float  eVal = SAMPLE_TEXTURE2D(_Main_Emissive_Tex, sampler_Main_Emissive_Tex, uv_e).r;
                    baseTex    += eVal * (_Main_Emissve_color * _Main_Emissve_power);
                }
                #endif

                // ── Tint Mask + Vertex Color ────────────────────────────────
                // Tint mask R/G/B selects Accent 1/2/3 replacement.
                // Vertex color near-white (all channels >= 0.99) signals "skin" area
                // and substitutes _Skin_Color for all three accent channels.
                float4 tintMask = SAMPLE_TEXTURE2D(_Tint_Mask, sampler_Tint_Mask,
                                                   TRANSFORM_TEX(input.uv, _Tint_Mask));
                float3 vcol    = input.color.rgb;
                float  useSkin = step(0.99, min(vcol.r, min(vcol.g, vcol.b)));

                float3 tintColor1 = lerp(_Accent_Color_1.rgb, _Skin_Color.rgb, useSkin);
                float3 tintColor2 = lerp(_Accent_Color_2.rgb, _Skin_Color.rgb, useSkin);
                float3 tintColor3 = lerp(_Accent_Color_3.rgb, _Skin_Color.rgb, useSkin);

                float3 tinted  = baseTex.rgb;
                tinted = lerp(tinted, tintColor1, tintMask.r);
                tinted = lerp(tinted, tintColor2, tintMask.g);
                tinted = lerp(tinted, tintColor3, tintMask.b);
                baseTex.rgb = tinted;

                float4 albedo = baseTex;

                // ── Normal ──────────────────────────────────────────────────
                float2 uv_NormalMap  = TRANSFORM_TEX(input.uv, _NormalMap);
                // FIX: use UnpackNormal instead of manual * 2 - 1 (handles DXT5nm / BC5 correctly)
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

                        float sBase = 1.0 - smoothstep(_Shadow_Offset,    _Shadow_Offset    + max(_Shadow_Smoothness,    0.0001), hl_noised);
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

                        if (distAtten > 0.001)
                        {
                            float ndl = dot(normalWS, addLight.direction);

                            if (ndl > 0.0)
                            {
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
                float  gray         = dot(highlightRgb, float3(0.299, 0.587, 0.114));
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
                #if defined(_ADDITIONAL_LIGHTS)
                {
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
                    float addGray = dot(additionalHl, float3(0.299, 0.587, 0.114));
                    additionalHl = saturate(addGray + (additionalHl - addGray) * (1.0 + _Highlight_Saturation));

                    // Add additional highlights on top (additive blend)
                    litColor.rgb = saturate(litColor.rgb + additionalHl);

                    // Apply shadows from additional lights
                    float addShadow = saturate(additionalShadowDark * additionalMask * _Shadow_Strength);
                    if (_Shadow_Replace > 0.5)
                        litColor.rgb = lerp(litColor.rgb, darkCol.rgb, addShadow);
                    else
                        litColor.rgb = lerp(litColor.rgb, litColor.rgb * darkCol.rgb, addShadow);
                }
                #endif

                // ── Matcap ───────────────────────────────────────────────────
                float4 basePart = litColor;
                #if defined(_USE_MATCAT_ON)
                {
                    float2 matcapUV;

                    if (_MatcapObjectSpace > 0.5)
                    {
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
                        float    angle = _Time.y * _matcap_animation_speed;
                        float    ca    = cos(angle);
                        float    sa    = sin(angle);
                        float2x2 rot   = float2x2(ca, -sa, sa, ca);
                        matcapUV = mul(rot, matcapUV - 0.5) + 0.5;
                    }
                    #endif

                    float4 matcapCol = SAMPLE_TEXTURE2D(_matcap, sampler_matcap, matcapUV);

                    #if defined(_USE_MATCAP_EMISSIVE_ON)
                    {
                        float2 uv_me   = TRANSFORM_TEX(input.uv, _Matcap_Emissive_Tex);
                        float  emisVal = SAMPLE_TEXTURE2D(_Matcap_Emissive_Tex, sampler_Matcap_Emissive_Tex, uv_me).r;
                        matcapCol     += emisVal * (_Matcap_Emissve_color * _Matcap_Emissve_power);
                    }
                    #endif

                    matcapCol *= _MatcapIntensity;

                    float2 uv_mask    = TRANSFORM_TEX(input.uv, _special_buff_switch);
                    float  specialVal = SAMPLE_TEXTURE2D(_special_buff_switch, sampler_special_buff_switch, uv_mask).r;
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

                // Apply night tint (driven by SceneSkyboxSetter)
                color.rgb = lerp(color.rgb, _NightTintColor.rgb, _NightTintIntensity);
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
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Base_ST;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_Base); SAMPLER(sampler_Base);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;  // FIX: was missing — needed for correct shadow bias
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
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
                // FIX: actual vertex normal instead of hardcoded (0,0,1)
                float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));

                // Clamp to avoid shadow pancaking on thin geometry
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
                float2 uv    = TRANSFORM_TEX(input.uv, _Base);
                float4 color = SAMPLE_TEXTURE2D(_Base, sampler_Base, uv);
                clip(color.a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────
        // DEPTH NORMALS PASS  (required for screen-space shadows, SSAO,
        // and decal normal blending in URP)
        // ─────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            ColorMask RGBA

            HLSLPROGRAM
            #pragma vertex   depthNormalsVert
            #pragma fragment depthNormalsFrag
            #pragma multi_compile _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile _ DECAL_NORMAL_BLEND_LOW DECAL_NORMAL_BLEND_MEDIUM DECAL_NORMAL_BLEND_HIGH
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Base_ST;
            float4 _NormalMap_ST;
            float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_Base);      SAMPLER(sampler_Base);
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

            float4 depthNormalsFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 base = SAMPLE_TEXTURE2D(_Base, sampler_Base, TRANSFORM_TEX(input.uv, _Base));
                clip(base.a - _Cutoff);

                float3 tangentNormal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,
                                                    TRANSFORM_TEX(input.uv, _NormalMap)));
                float3x3 tbn     = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS  = normalize(mul(tangentNormal, tbn));

                // DepthNormals is a prepass; the DBuffer (decal normal) data isn't initialized yet.
                // Applying decal normals here would read uninitialized data and can corrupt the output.
                // (See TurnrootGenericCel.shader for reference behavior.)
                return float4(normalWS * 0.5 + 0.5, 1.0);
            }
            ENDHLSL
        }
    }

    CustomEditor "Turnroot.TurnrootClassOutfitShader"
}
