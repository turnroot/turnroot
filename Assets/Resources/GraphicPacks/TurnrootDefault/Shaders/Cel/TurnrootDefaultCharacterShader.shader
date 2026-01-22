Shader "Turnroot/Character Cel Shader"
{
    Properties
    {
        [Header(Outlines)]
        // Outline Properties
        [Toggle(_USE_OUTLINES_ON)] _use_outlines("Use Outlines", Float ) = 0
        _ASEOutlineWidth( "Outline Width", Range(0,.005) ) = 0.002
        _ASEOutlineColor( "Outline Color", Color ) = (0.0,0.0,0,1)
        [HideInInspector]_ASEOutalpha( "_ASEOutalpha", Range(-1,0) ) = 0

        [Header(Main)]
        // General Properties
        _MainTex("Main Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}

        _Cutoff( "Mask Alpha Clip Cutoff", Float ) = 0.5
        [HideInInspector] _texcoord( "", 2D ) = "white" {}

        // Shadow Properties
        _Cel_Shader_Offset("Cel Shader Offset", Range( 0 , 1)) = 0.64

        // Lighting Color Properties
        _light("Light", Color ) = (1,1,1,0)
        _dark("Dark", Color ) = (0,0,0,0)
        [Toggle(_USE_LIGHT_TEX_ON)] _use_light_tex("Use Light Texture", Float) = 0
        _LightTex("Light Texture", 2D) = "white" {}
        [Toggle(_USE_DARK_TEX_ON)] _use_dark_tex("Use Dark Texture", Float) = 0
        _DarkTex("Dark Texture", 2D) = "black" {}



        [Header(Main Emissive)]
        [Toggle(_USE_MAIN_EMISSIVE_ON)] _use_main_emissive("Use Main Emissive", Float ) = 0
        _Main_Emissive_Tex("Main Emissive Texture", 2D) = "white" {}
        _Main_Emissve_color("Main Emissive Color", Color ) = (1,1,1,0)
        _Main_Emissve_power("Main Emissive Power", Range( -1 , 5)) = 1


        // Matcap Properties
        [Toggle(_USE_MATCAT_ON)] _use_matcat("Use Mat Cap", Float ) = 0
        _matcap("Matcap Texture", 2D) = "white" {}
        _special_buff_switch("Matcap Switch Mask", 2D) = "white" {}
        _special_buff_switch_edge_hardness("Matcap Switch Edge Hardness", Range( 0 , 22)) = 1
        _special_buff_dissolve("Matcap Switch Dissolve", Range( 0 , 1)) = 1
        [Header(Matcap Controls)]
        _MatcapIntensity("Matcap Intensity", Range(0,1)) = 1      // blend amount
        _MatcapObjectSpace("Matcap Object-Space", Float) = 0       // 0 = view-space (current), 1 = object-space
        [Toggle(_USE_MATCAP_REFLECTION_ON)] _use_matcap_reflection("Matcap Reflection Mode", Float ) = 1

        [Header(Matcap Emissive)]
        [Toggle(_USE_MATCAP_EMISSIVE_ON)] _use_matcap_emissive("Use Matcap Emissive", Float ) = 0
        _Matcap_Emissive_Tex("Matcap Emissive Texture", 2D) = "white" {}
        _Matcap_Emissve_color("Matcap Emissive Color", Color ) = (0,0,0,0)
        _Matcap_Emissve_power("Matcap Emissive Power", Range( -1 , 3)) = 0

        [Header(Matcap Animation)]
        [Toggle(_USE_MATCAP_ANIMATION_ON)] _use_matcap_animation("Animate Matcap Texture", Float ) = 0
        _matcap_animation_speed("Matcap Animation Speed", Range(0,10)) = 1

        // Fresnel Properties
        [Toggle(_USE_FRENSEL_ON)] _use_frensel("Use Fresnel", Float ) = 0
        _frensel_range("Fresnel Range", Range( -1 , 1)) = .6
        _frensel_hard("Fresnel Hardness", Range( 0 , 1)) = .8
        _frensel_power("Fresnel Power", Range( 0 , 3)) = 1
        [HDR]_frensel_color("Fresnel Color", Color ) = (0,0,0,0)

        // Shading refinements
        _Shadow_Strength("Shadow Strength", Range(0,1)) = 1
        _Shadow_Replace("Shadow Replace", Float) = 0
        _Shadow_Roughness("Shadow Roughness", Range(0,1)) = 0
        _Shadow_Offset("Shadow Offset", Range(0,1)) = 0.64
        _Shadow_Smoothness("Shadow Smoothness", Range(0,1)) = 0
        [Toggle(_USE_SHADOW_NOISE_ON)] _use_shadow_noise("Use Shadow Noise", Float) = 0
        _Shadow_Noise_Tex("Shadow Noise Texture", 2D) = "white" {}
        _Shadow_Noise_Amount("Shadow Noise Amount", Range(0,1)) = 0

        _Highlight_Amount("Highlight Amount", Range(0,3)) = 1
        _Highlight_Replace("Highlight Replace", Float) = 0
        _Highlight_Offset("Highlight Offset", Range(-1,1)) = 0.64
        _Highlight_Smoothness("Highlight Smoothness", Range(0,1)) = 0
        _Highlight_Roughness("Highlight Roughness", Range(0,1)) = 0
        [Toggle(_USE_HIGHLIGHT_MASK_ON)] _use_highlight_mask("Use Highlight Mask", Float) = 0
        _Highlight_Mask_Tex("Highlight Mask Texture", 2D) = "white" {}
        _Highlight_Mask_Amount("Highlight Mask Amount", Range(0,1)) = 1
        _Highlight_Saturation("Highlight Saturation", Range(0,2)) = 0
        _Show_Masks("Show Masks (RGB S/M/H)", Float) = 0
        [Toggle(_USE_LIGHT_TEX_ON)] _use_light_tex("Use Light Texture", Float) = 0
        _LightTex("Light Texture", 2D) = "white" {}
        [Toggle(_USE_DARK_TEX_ON)] _use_dark_tex("Use Dark Texture", Float) = 0
        _DarkTex("Dark Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags{"RenderType" = "Opaque" "Queue" = "AlphaTest+0" "RenderPipeline" = "UniversalPipeline"}

        // Outline Pass
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex outlineVert
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
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            #ifdef _USE_OUTLINES_ON
            Varyings outlineVert (Attributes input)
            {
                Varyings output;
                input.positionOS.xyz += input.normalOS * _ASEOutlineWidth;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 outlineFrag (Varyings input) : SV_Target
            {
                clip(_ASEOutalpha);
                return half4(_ASEOutlineColor.rgb, 1);
            }
            #else
            // Degenerate case: collapse to a point to avoid rasterization and fragment compute
            Varyings outlineVert (Attributes input)
            {
                Varyings output;
                output.positionCS = float4(0, 0, 0, 1); // All vertices same point -> zero-area triangles
                return output;
            }

            half4 outlineFrag (Varyings input) : SV_Target
            {
                discard; // Safety, though no frags should run
                return 0;
            }
            #endif
            ENDHLSL
        }

        // Main Forward Pass
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _USE_MATCAP_ON
            #pragma shader_feature_local _USE_FRENSEL_ON
            #pragma shader_feature_local _USE_MAIN_EMISSIVE_ON
            #pragma shader_feature_local _USE_MATCAP_EMISSIVE_ON
            #pragma shader_feature_local _USE_MATCAP_REFLECTION_ON
            #pragma shader_feature_local _USE_MATCAP_ANIMATION_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
            float _Cel_Shader_Offset;
            float4 _light;
            float4 _dark;
            float _frensel_range;
            float _frensel_hard;
            float _frensel_power;
            float4 _frensel_color;
            float4 _Main_Emissve_color;
            float _Main_Emissve_power;
            float4 _Matcap_Emissve_color;
            float _Matcap_Emissve_power;
            float _Cutoff;
            float _MatcapObjectSpace;
            float _MatcapIntensity;
            float _matcap_animation_speed;
            float _use_light_tex;
            float _use_dark_tex;
            float _use_highlight_mask;
            float _Highlight_Mask_Amount;
            float _Shadow_Strength;
            float _Shadow_Replace;
            float _Shadow_Roughness;
            float _Shadow_Offset;
            float _Shadow_Smoothness;
            float _use_shadow_noise;
            float _Shadow_Noise_Amount;
            float _Highlight_Amount;
            float _Highlight_Replace;
            float _Highlight_Offset;
            float _Highlight_Smoothness;
            float _Highlight_Roughness;
            float _Highlight_Saturation;
            float _Show_Masks;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_matcap); SAMPLER(sampler_matcap);
            TEXTURE2D(_LightTex); SAMPLER(sampler_LightTex);
            TEXTURE2D(_DarkTex); SAMPLER(sampler_DarkTex);
            TEXTURE2D(_Shadow_Noise_Tex); SAMPLER(sampler_Shadow_Noise_Tex);
            TEXTURE2D(_Highlight_Mask_Tex); SAMPLER(sampler_Highlight_Mask_Tex);
            TEXTURE2D(_special_buff_switch); SAMPLER(sampler_special_buff_switch);
            TEXTURE2D(_Main_Emissive_Tex); SAMPLER(sampler_Main_Emissive_Tex);
            TEXTURE2D(_Matcap_Emissive_Tex); SAMPLER(sampler_Matcap_Emissive_Tex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.bitangentWS = normalInput.bitangentWS;

                output.uv = input.uv;

                output.shadowCoord = GetShadowCoord(vertexInput);

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Alpha clip from main texture alpha (use _MainTex alpha)
                float2 uv_MainTex = TRANSFORM_TEX(input.uv, _MainTex);
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv_MainTex);
                float alpha = baseTex.a;
                clip(alpha - _Cutoff);

                // Albedo (already sampled above into baseTex)

                // Main Emissive
                #if defined(_USE_MAIN_EMISSIVE_ON)
                float2 uv_Main_Emissive = TRANSFORM_TEX(input.uv, _Main_Emissive_Tex);
                float mainEmisVal = SAMPLE_TEXTURE2D(_Main_Emissive_Tex, sampler_Main_Emissive_Tex, uv_Main_Emissive).r;
                float4 mainEmisCol = mainEmisVal * (_Main_Emissve_color * _Main_Emissve_power);
                baseTex += mainEmisCol;
                #endif

                float4 albedo = baseTex;


                // Normal
                float2 uv_NormalMap = TRANSFORM_TEX(input.uv, _NormalMap);
                float3 tangentNormal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv_NormalMap).xyz * 2.0 - 1.0;
                float3x3 tbn = float3x3(input.tangentWS.xyz, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(tangentNormal, tbn));

                // View dir
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                // Lighting accumulation with Shadow Strength, Blend Mode and Highlights
                // Sample light/dark textures if enabled
                float4 lightCol = _light;
                if (_use_light_tex > 0.5)
                {
                    float2 uv_light = TRANSFORM_TEX(input.uv, _LightTex);
                    float4 lt = SAMPLE_TEXTURE2D(_LightTex, sampler_LightTex, uv_light);
                    lightCol = lt * _light;
                }
                float4 darkCol = _dark;
                if (_use_dark_tex > 0.5)
                {
                    float2 uv_dark = TRANSFORM_TEX(input.uv, _DarkTex);
                    float4 dt = SAMPLE_TEXTURE2D(_DarkTex, sampler_DarkTex, uv_dark);
                    darkCol = dt * _dark;
                }

                // Lighting accumulation - compute shadow & highlight masks (max across lights)
                float noiseN = 0;
                if (_use_shadow_noise > 0.5)
                {
                    float2 uv_noise = TRANSFORM_TEX(input.uv, _Shadow_Noise_Tex);
                    noiseN = SAMPLE_TEXTURE2D(_Shadow_Noise_Tex, sampler_Shadow_Noise_Tex, uv_noise).r - 0.5;
                }

                float shadowMask = 0;
                float highlightMask = 0;

                // Main light
                Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, 1);
                if (mainLight.color.r + mainLight.color.g + mainLight.color.b > 0.0001)
                {
                    float ndl = dot(normalWS, mainLight.direction);
                    float atten = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                    float hl = (ndl * atten + 1.0) * 0.5;
                    float hl_noised = hl + noiseN * _Shadow_Noise_Amount;

                    // Edge mask (smoothness controls edge width)
                    // Shadow should be on the low-hl side (dark areas), so invert the smoothstep
                    float shadowBase = 1.0 - smoothstep(_Shadow_Offset, _Shadow_Offset + _Shadow_Smoothness, hl_noised);
                    float highlightBase = smoothstep(_Highlight_Offset, _Highlight_Offset + _Highlight_Smoothness, hl);

                    // Roughness controls shine (spec-like falloff). lower roughness => shinier (narrow peak)
                    float shadowExp = lerp(0.6, 3.0, 1.0 - _Shadow_Roughness);
                    float highlightExp = lerp(0.6, 3.0, 1.0 - _Highlight_Roughness);

                    float sShadow = pow(shadowBase, shadowExp);
                    float sHighlight = pow(highlightBase, highlightExp);

                    shadowMask = max(shadowMask, sShadow);
                    highlightMask = max(highlightMask, sHighlight);
                }

                // Additional lights
                #if defined(_ADDITIONAL_LIGHTS)
                int addLightCount = GetAdditionalLightsCount();
                for (int li = 0; li < addLightCount; ++li)
                {
                    Light addLight = GetAdditionalLight(li, input.positionWS, 1);
                    if (addLight.color.r + addLight.color.g + addLight.color.b > 0.0001)
                    {
                        float ndl = dot(normalWS, addLight.direction);
                        float atten = addLight.shadowAttenuation * addLight.distanceAttenuation;
                        float hl = (ndl * atten + 1.0) * 0.5;
                        float hl_noised = hl + noiseN * _Shadow_Noise_Amount;

                        // Edge mask (smoothness controls edge width)
                        float shadowBase = 1.0 - smoothstep(_Shadow_Offset, _Shadow_Offset + _Shadow_Smoothness, hl_noised);
                        float highlightBase = smoothstep(_Highlight_Offset, _Highlight_Offset + _Highlight_Smoothness, hl);

                        float shadowExp = lerp(0.6, 3.0, 1.0 - _Shadow_Roughness);
                        float highlightExp = lerp(0.6, 3.0, 1.0 - _Highlight_Roughness);

                        float sShadow = pow(shadowBase, shadowExp);
                        float sHighlight = pow(highlightBase, highlightExp);

                        shadowMask = max(shadowMask, sShadow);
                        highlightMask = max(highlightMask, sHighlight);
                    }
                }
                #endif

                // Debug mask visualization: Shadow (R), Middle (G), Highlight (B)
                if (_Show_Masks > 0.5)
                {
                    float midMask = saturate(1.0 - max(shadowMask, highlightMask));
                    float3 viz = float3(shadowMask, midMask, highlightMask);
                    return float4(viz, 1.0);
                }

                // Apply shadow to base (only where mask>0)
                float4 litColor = albedo;
                float sm = saturate(shadowMask * _Shadow_Strength);
                if (_Shadow_Replace > 0.5)
                    litColor = lerp(albedo, darkCol, sm);
                else
                    litColor = lerp(albedo, albedo * darkCol, sm);

                // Apply highlight: ensure shadow -> middle -> highlight ordering
                // Highlight only where highlight mask is stronger than shadow mask
                float hm = saturate((highlightMask - shadowMask) * _Highlight_Amount);

                // Highlight mask texture subtracts from highlight amount
                if (_use_highlight_mask > 0.5)
                {
                    float2 uv_hm = TRANSFORM_TEX(input.uv, _Highlight_Mask_Tex);
                    float hmask = SAMPLE_TEXTURE2D(_Highlight_Mask_Tex, sampler_Highlight_Mask_Tex, uv_hm).r;
                    hm = max(0.0, hm - hmask * _Highlight_Mask_Amount);
                }

                // Highlight color and saturation control
                float3 highlightRgb = albedo.rgb * lightCol.rgb;
                float gray = dot(highlightRgb, float3(0.299, 0.587, 0.114));
                highlightRgb = gray + (highlightRgb - gray) * (1.0 + _Highlight_Saturation);
                highlightRgb = saturate(highlightRgb);
                float4 highlightCol = float4(highlightRgb, albedo.a);

                if (_Highlight_Replace > 0.5)
                {
                    litColor = lerp(litColor, highlightCol, hm);
                }
                else
                {
                    // Screen-like brightening: result = 1 - (1 - base) * (1 - highlight*hm)
                    float4 scaledHl = highlightCol * hm;
                    litColor = 1.0 - (1.0 - litColor) * (1.0 - scaledHl);
                }

                // ──────────────────────────────────────────
                // Matcap
                float4 basePart = litColor;
                #if defined(_USE_MATCAT_ON)

                    // ─── UV generation (object-space vs view-space / reflection) ───
                    float2 matcapUV;
                    if (_MatcapObjectSpace > 0.5)               // checkbox → locks highlight
                    {
                        float3 nObj  = normalize(input.normalWS);
                        matcapUV     = nObj.xy * 0.5 + 0.5;
                    }
                    else
                    {
                        #ifdef _USE_MATCAP_REFLECTION_ON
                            float3 reflectDir  = reflect(-viewDirWS, normalWS);
                            float3 viewReflect = mul((float3x3)UNITY_MATRIX_V, reflectDir);
                            float  m           = 2.828427 * sqrt(viewReflect.z + 1.0);
                            matcapUV = viewReflect.xy / m + 0.5;
                        #else
                            float3 normalView = mul((float3x3)UNITY_MATRIX_V, normalWS);
                            matcapUV = normalView.xy * 0.5 + 0.5;
                        #endif
                    }

                    // ─── Animation (rotate UV if enabled) ───
                    #if defined(_USE_MATCAP_ANIMATION_ON)
                        float angle = _Time.y * _matcap_animation_speed;
                        float ca = cos(angle);
                        float sa = sin(angle);
                        float2x2 rot = float2x2(ca, -sa, sa, ca);
                        matcapUV = mul(rot, (matcapUV - 0.5)) + 0.5;
                    #endif

                    // ─── Sample & optional emissive ───
                    float4 matcapCol = SAMPLE_TEXTURE2D(_matcap, sampler_matcap, matcapUV);

                    #if defined(_USE_MATCAP_EMISSIVE_ON)
                        float2 uv_Matcap_Emissive = TRANSFORM_TEX(input.uv, _Matcap_Emissive_Tex);
                        float  matcapEmisVal      = SAMPLE_TEXTURE2D(_Matcap_Emissive_Tex,
                                                                    sampler_Matcap_Emissive_Tex,
                                                                    uv_Matcap_Emissive).r;
                        float4 matcapEmisCol      = matcapEmisVal *
                                                    (_Matcap_Emissve_color * _Matcap_Emissve_power);
                        matcapCol += matcapEmisCol;
                    #endif

                    // ─── Scale by intensity slider ───
                    matcapCol *= _MatcapIntensity;

                    // ─── Mask & additive blend ───
                    float2 uv_special_buff_switch = TRANSFORM_TEX(input.uv, _special_buff_switch);
                    float  specialVal   = SAMPLE_TEXTURE2D(_special_buff_switch,
                                                        sampler_special_buff_switch,
                                                        uv_special_buff_switch).r;
                    float  specialHard  = _special_buff_switch_edge_hardness;
                    float  specialLerp  = lerp(specialHard, -1.0, _special_buff_dissolve);
                    float  specialMask  = saturate((specialVal * specialHard) - specialLerp);

                    // Additive tint instead of full replace
                    basePart = lerp(basePart, basePart + matcapCol, specialMask);
                #endif

                // ──────────────────────────────────────────
                // Fresnel
                float4 color = basePart;
                #if defined(_USE_FRENSEL_ON)
                    float nv         = dot(normalWS, viewDirWS);
                    float rim        = 1.0 - nv;
                    float rimSmooth  = smoothstep(_frensel_range, _frensel_range + _frensel_hard, rim);
                    float4 fresnelCol = (_frensel_color * _frensel_power) * saturate(rimSmooth);
                    color += fresnelCol;
                #endif


                color.rgb = saturate(color.rgb);

                return color;

            }
            ENDHLSL
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float _Cutoff;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 _LightDirection; // For normal bias

            Varyings shadowVert (Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(float3(0,0,1)); // Simple, since no normal needed for clip
                float4 clipPos = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                output.positionCS = clipPos;
                output.uv = input.uv;
                return output;
            }

            half4 shadowFrag (Varyings input) : SV_Target
            {
                float2 uv_MainTex = TRANSFORM_TEX(input.uv, _MainTex);
                float4 baseTexShadow = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv_MainTex);
                float alpha = baseTexShadow.a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
    CustomEditor "Turnroot.TurnrootDefaultCharacterShader"
}