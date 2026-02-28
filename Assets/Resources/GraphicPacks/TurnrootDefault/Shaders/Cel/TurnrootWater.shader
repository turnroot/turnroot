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
            float  _DistortionStrength;

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
                // Derive a perturbed surface normal from the noise gradient using
                // screen-space derivatives. ddx/ddy of the noise value give the
                // rate of change in XZ, which is exactly the slope of the "wave"
                // implied by the noise field — no extra texture taps required.
                //
                // The murkiness and distortion fields are then used to attenuate
                // the specular, so turbid / rough patches look dull and the
                // distorted silhouette zone at the waterline has wavy highlights.
                // ─────────────────────────────────────────────────────────
                float3 baseNormalWS = normalize(input.normalWS);

                // Combined noise value used to drive the wave slope
                float nCombined = n1 * 0.7 + n2 * 0.3;

                // Screen-space gradient of the noise — gives XZ slope of the wave
                float3 dPdx  = ddx(input.positionWS);
                float3 dPdy  = ddy(input.positionWS);
                float  dNdx  = ddx(nCombined);
                float  dNdy  = ddy(nCombined);

                // Reconstruct a perturbed normal: push the surface along its tangent
                // plane by the noise gradient, scaled by _SpecularNormalStrength.
                float3 waveNormalWS = normalize(
                    baseNormalWS
                    - dNdx * normalize(dPdx) * _SpecularNormalStrength
                    - dNdy * normalize(dPdy) * _SpecularNormalStrength
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
                float murkinessSuppress  = 1.0 - murkiness * _MurkinessStrength * _SpecularMurkinessSuppress;
                // Distortion noise is already in n1 (same texture, same UVs as distortion)
                float distortionRoughen = 1.0 - saturate((n1 * 2.0 - 1.0) * _DistortionStrength * 40.0);
                float specularAttenuation = saturate(murkinessSuppress * distortionRoughen);

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
                // ─────────────────────────────────────────────────────────
                float distNoise = n1 * 2.0 - 1.0; // remap 0..1 → -1..1
                float2 distortedScreenUV = screenUV + distNoise * _DistortionStrength;
                float  rawSceneDepthD    = SampleSceneDepth(distortedScreenUV);
                float  sceneEyeDepthD    = LinearEyeDepth(rawSceneDepthD, _ZBufferParams);
                float  depthDiffDistorted = max(0.0, sceneEyeDepthD - fragEyeDepth);

                // ─────────────────────────────────────────────────────────
                // 10. INTERSECTION RIPPLES
                // ─────────────────────────────────────────────────────────
                float dxD    = ddx(depthDiffDistorted);
                float dyD    = ddy(depthDiffDistorted);
                float gradLen = length(float2(dxD, dyD));

                float pixelDist  = depthDiffDistorted / max(gradLen, 0.0001);
                float wsPerPixel = length(ddx(input.positionWS));
                float horizDist  = pixelDist * wsPerPixel;

                // Warp the effective distance from the edge using both noise layers.
                // This displaces where each ring sits in world space — perfect circles
                // become irregular, organic shapes. The two layers at different scales
                // give large sweeping bends (n1) and finer local kinks (n2).
                float nDistort = (n1 - 0.5) * 0.8 + (n2 - 0.5) * 0.2; // range ~-0.5..0.5
                horizDist = max(0.0, horizDist + nDistort * _RippleDistortion * _RippleDistance * 0.5);

                float rippleMask = 1.0 - saturate(horizDist / max(_RippleDistance, 0.001));
                rippleMask *= step(0.001, depthDiffDistorted);

                float edgeFade = saturate(rippleMask / max(_RippleEdgeFade, 0.01));

                float ringPhase = (horizDist / max(_RippleDistance, 0.001)) * _RippleCount
                                  - _Time.y * _RippleSpeed;
                float ring     = frac(ringPhase);
                float ringLine = 1.0 - abs(ring - 0.5) * 2.0;
                ringLine = saturate((ringLine - 0.5) * _RippleSharpness * 0.1 + 0.5);
                ringLine = saturate(ringLine * 2.0 - 0.75);

                float n2b = SampleNoise(noiseUV2);
                float noiseSubtract = n1 * _RippleNoiseStrength + n2b * _RippleNoiseStrength2;
                ringLine = saturate(ringLine - noiseSubtract);

                float rippleAlpha = ringLine * rippleMask * edgeFade * _RippleOpacity;

                // Shadow mask — lerp between full ripple (shadowAtten=1) and no ripple
                // (shadowAtten=0) based on _RippleShadowMask. At 0 the slider has no
                // effect; at 1 ripples vanish completely in shadowed areas.
                float rippleShadowFactor = lerp(1.0, shadowAtten, _RippleShadowMask);
                rippleAlpha *= rippleShadowFactor;

                waterCol.rgb = lerp(waterCol.rgb, _RippleColor.rgb, rippleAlpha * _RippleColor.a);
                waterCol.a   = max(waterCol.a, rippleAlpha * _RippleColor.a);

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
