// Grass.shader
// URP grass shader for DrawMeshInstancedIndirect.
// Blade geometry is built procedurally in the vertex shader from the template mesh
// (5 verts encoding shape as xy = (side, t)) and per-instance data from _VisibleBlades.
//
// Stylization guide:
//   Ghibli:        soft BaseColor/TipColor, ColorVariance ~0.2, AOStrength ~0.6,
//                  Translucency ~0.4, low WindStrength, BladeCurve ~0.2
//   Breath of Wild: saturated yellow-green Base, lighter Tip, ColorVariance ~0.1,
//                  higher WindStrength, WindTurbulence ~0.5, TipTaper ~0.05
//   Dense meadow:  high density in GrassRenderer, TipTaper ~0.15, BladeCurve ~0.3

Shader "Turnroot/Grass"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor          ("Base Color",        Color)          = (0.08, 0.28, 0.04, 1)
        _TipColor           ("Tip Color",         Color)          = (0.38, 0.66, 0.08, 1)
        [Range(0,1)]
        _ColorVariance      ("Color Variance",    Float)          = 0.15
        [Range(0,1)]
        _HueVariance        ("Hue Variation",     Float)          = 0.1
        _ShadowColor        ("Shadow Color",      Color)          = (0,0,0,1)
        
        [Header(Styling)]
        [Range(0,1)]
        _UnderlyingMix      ("Mix with Underlying Color", Float) = 0.0
        _GroundTex          ("Ground Albedo Texture", 2D) = "white" {}
        _GroundTex_ST       ("Ground UV Tiling/Offset", Vector) = (1,1,0,0)

        [Header(Tinting)]
        _CelTintColor       ("Cel Tint Color", Color) = (1,1,1,1)
        [Range(0,1)]
        _CelTintIntensity   ("Cel Tint Intensity", Float) = 0.0
        _NightTintColor     ("Night Tint Color", Color) = (0.1,0.13,0.25,1)
        [Range(0,1)]
        _NightTintIntensity ("Night Tint Intensity", Float) = 0.0

        [Header(Wind)]
        _WindDirection      ("Wind Direction XZ", Vector)         = (1, 0, 0, 0)
        _WindSpeed          ("Wind Speed",        Float)          = 1.2
        [Range(0,1)]
        _WindStrength       ("Wind Strength",     Float)          = 0.28
        // Per-blade speed randomisation — higher = choppier, more organic movement
        [Range(0,1)]
        _WindTurbulence     ("Wind Turbulence",   Float)          = 0.35

        [Header(Lighting)]
        [Range(0,1)]
        _AOStrength         ("AO Strength",       Float)          = 0.65
        [Range(0,1)]
        _Translucency       ("Translucency",      Float)          = 0.3
        [Range(0,1)]
        _ShadowStrength     ("Shadow Strength",   Float)          = 0.75

        [Header(LOD and Alpha)]
        [Range(0,1)]
        _AlphaCutoff        ("Alpha Cutoff",      Float)          = 0.25
        // Distance values are also set from GrassRenderer.cs each frame
        _MaxDistance        ("Max Distance",      Float)          = 50
        _FadeStartDistance  ("Fade Start",        Float)          = 35
        // Set each frame by GrassRenderer.cs — do not edit manually
        [HideInInspector]
        _CameraPosition     ("Camera Position",   Vector)         = (0,0,0,0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "TransparentCutout"
            "Queue"          = "AlphaTest"
        }

        // ─────────────────────────────────────────────────────────────────────
        // FORWARD LIT PASS
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off   // double-sided — we want to see blades from below/behind
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ DEBUG_DISPLAY

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // mark this material transparent so URP skips screen-space AO
            #define _SURFACE_TYPE_TRANSPARENT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            // ── Blade data (mirrors GrassRenderer.cs BladeData and GrassCompute.compute) ──
            struct BladeData
            {
                float3 position;
                float3 normal;
                float  height;
                float  width;
                float  phase;
                float  facingAngle;
            };

            StructuredBuffer<BladeData> _VisibleBlades;

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _TipColor;
            float  _ColorVariance;
            float  _HueVariance;
            float4 _ShadowColor;
            float  _UnderlyingMix;
            TEXTURE2D(_GroundTex);
            SAMPLER(sampler_GroundTex);
            float4 _GroundTex_ST;
            float4 _CelTintColor;
            float  _CelTintIntensity;
            float4 _NightTintColor;
            float  _NightTintIntensity;
            float  _TipTaper;
            float  _BladeCurve;
            float4 _WindDirection;
            float  _WindSpeed;
            float  _WindStrength;
            float  _WindTurbulence;
            // legacy lighting params (ignored)
            float  _AOStrength;
            float  _Translucency;
            float  _ShadowStrength;
            float  _AlphaCutoff;
            float  _MaxDistance;
            float  _FadeStartDistance;
            float4 _CameraPosition;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 shapeUV     : TEXCOORD0;  // (side+0.5, t) from mesh vertex
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                float  colorJitter : TEXCOORD4;  // per-blade random, baked from phase
                float  distFade    : TEXCOORD5;  // 0..1 LOD alpha
                float4 screenPos   : TEXCOORD6;  // for sampling underlying scene color
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // utility: convert RGB to HSV and back (hue variations only)
            float3 RGBToHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(abs(q.z + (q.w - q.y)/(6.0*d+e)), d/(q.x+e), q.x);
            }

            float3 HSVToRGB(float3 c)
            {
                float4 K = float4(1.0, 2.0/3.0, 1.0/3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            // ── Build per-blade orientation basis ────────────────────────────────────
            // Returns right and forward vectors in the blade's tangent plane.
            void BladeAxes(float3 surfaceNormal, float facingAngle,
                           out float3 right, out float3 fwd)
            {
                // Stable base right: avoid degenerate cross when normal == worldUp
                float3 worldUp = abs(surfaceNormal.y) < 0.98 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 baseRight = normalize(cross(surfaceNormal, worldUp));

                // Rotate baseRight around surfaceNormal by facingAngle
                float ca = cos(facingAngle), sa = sin(facingAngle);
                right = ca * baseRight + sa * cross(surfaceNormal, baseRight);
                fwd   = cross(right, surfaceNormal); // not directly used but available
            }

            Varyings vert(float4 vertex : POSITION, float2 uv : TEXCOORD0,
                          uint instanceID : SV_InstanceID)
            {
                Varyings o = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                BladeData blade = _VisibleBlades[instanceID];

                // vertex.x = side (-0.5..0.5), vertex.y = t (0..1)
                float side = vertex.x;
                float t    = vertex.y;
                o.shapeUV  = float2(side + 0.5, t);

                // ── Build orientation ──────────────────────────────────────────
                float3 surfNorm = normalize(blade.normal);
                float3 bladeRight, bladeFwd;
                BladeAxes(surfNorm, blade.facingAngle, bladeRight, bladeFwd);

                // ── Width taper: narrows toward tip ────────────────────────────
                float taperT   = pow(abs(t), 0.7);       // slight exponent = more natural shape
                float halfW    = blade.width * 0.5 * lerp(1.0, _TipTaper, taperT);

                // ── Sway ───────────────────────────────────────────────────────
                // Quadratic falloff (t²) keeps base planted, maximises tip movement.
                float swayT    = t * t;
                float speedVar = _WindSpeed * (1.0 + (blade.phase / (2.0 * PI) - 0.5) * _WindTurbulence);
                float swayAmt  = sin(_Time.y * speedVar + blade.phase) * _WindStrength * blade.height * swayT;
                float3 windDir = normalize(float3(_WindDirection.x, 0.0, _WindDirection.z));

                // ── Natural curve (gentle forward lean independent of wind) ────
                float curveAmt = _BladeCurve * t * t * blade.height * 0.35;

                // ── Assemble world position ────────────────────────────────────
                float3 worldPos = blade.position
                    + bladeRight   * (side * blade.width * lerp(1.0, _TipTaper, taperT))
                    + surfNorm     * (t * blade.height)
                    + windDir      * (swayAmt + curveAmt);

                // ── Shadow coord ───────────────────────────────────────────────
                VertexPositionInputs vpi;
                vpi.positionWS = worldPos;
                vpi.positionVS = TransformWorldToView(worldPos);
                vpi.positionCS = TransformWorldToHClip(worldPos);
                vpi.positionNDC = float4(0,0,0,0); // not needed

                o.positionCS  = vpi.positionCS;
                o.positionWS  = worldPos;

                // ── Normal: blend surface normal toward world-up for smoother lighting ─
                // This gives the consistent, almost-flat-lit look of stylized cel grass.
                float3 geomNormal = normalize(cross(bladeRight, surfNorm));
                o.normalWS    = normalize(lerp(geomNormal, surfNorm, 0.6));

                o.shadowCoord = GetShadowCoord(vpi);

                // capture screen position for sampling
                o.screenPos   = ComputeScreenPos(o.positionCS);

                // ── Per-blade variation baked from phase ───────────────────────
                o.colorJitter = frac(blade.phase * 0.15915); // 0..1 unique per blade

                // ── Distance LOD fade ──────────────────────────────────────────
                float dist = distance(blade.position, _CameraPosition.xyz);
                o.distFade  = 1.0 - saturate((dist - _FadeStartDistance)
                                           / max(_MaxDistance - _FadeStartDistance, 0.001));

                return o;
            }

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float t    = input.shapeUV.y;   // 0 = base, 1 = tip
                float side = input.shapeUV.x;   // 0..1 left→right (used for potential future texturing)

                // ── LOD fade / distance alpha ──────────────────────────────────
                float alpha = input.distFade;
                // Additionally fade the very tip for a softer silhouette
                alpha *= lerp(1.0, 0.4, t * t);
                clip(alpha - _AlphaCutoff);

                // ── Base color + tip gradient + per-blade variance ─────────────
                float3 grassCol = lerp(_BaseColor.rgb, _TipColor.rgb, saturate(t * 1.2));
                // Subtle lightness jitter per blade for visual density
                float  jitter   = (input.colorJitter - 0.5) * _ColorVariance;
                grassCol        = saturate(grassCol + jitter);

                // mix with ground texture if requested
                if (_UnderlyingMix > 0.0001)
                {
                    float2 worldUV = input.positionWS.xz;
                    float2 uv = worldUV * _GroundTex_ST.xy + _GroundTex_ST.zw;
                    float3 underCol = SAMPLE_TEXTURE2D(_GroundTex, sampler_GroundTex, uv).rgb;
                    grassCol = lerp(grassCol, underCol, _UnderlyingMix);
                }

                // apply cel/night tinting
                grassCol = lerp(grassCol, _CelTintColor.rgb, _CelTintIntensity);
                grassCol = lerp(grassCol, _NightTintColor.rgb, _NightTintIntensity);

                // ── Decals (still allowed)
                #if defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
                    ApplyDecalToBaseColor(input.positionCS, grassCol);
                #endif

                // ── Hue variation (non-lightness)
                float3 hsv;
                hsv = RGBToHSV(grassCol);
                hsv.x += (input.colorJitter - 0.5) * _HueVariance;
                hsv.x = frac(hsv.x);
                grassCol = HSVToRGB(hsv);

                // ── Shadow color blending
                float shadowAtten = 1.0;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    Light mainLight = GetMainLight(input.shadowCoord, input.positionWS, unity_ProbesOcclusion);
                    shadowAtten = lerp(1.0, mainLight.shadowAttenuation, _ShadowStrength);
                #endif
                // multiply the grass color by shadow tint rather than replacing it
                float3 tint = lerp(float3(1,1,1), _ShadowColor.rgb, 1.0 - shadowAtten);
                float3 finalColor = grassCol * tint;
                return half4(saturate(finalColor), alpha);
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────────────
        // SHADOW CASTER
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex   shadowVert
            #pragma fragment shadowFrag

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct BladeData
            {
                float3 position;
                float3 normal;
                float  height;
                float  width;
                float  phase;
                float  facingAngle;
            };

            StructuredBuffer<BladeData> _VisibleBlades;

            CBUFFER_START(UnityPerMaterial)
            float4 _WindDirection;
            float  _WindSpeed;
            float  _WindStrength;
            float  _WindTurbulence;
            float  _TipTaper;
            float  _BladeCurve;
            float  _AlphaCutoff;
            float4 _CameraPosition;
            float  _MaxDistance;
            float  _FadeStartDistance;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  distFade   : TEXCOORD0;
                float  t          : TEXCOORD1;
            };

            Varyings shadowVert(float4 vertex : POSITION, uint instanceID : SV_InstanceID)
            {
                Varyings o;
                BladeData blade = _VisibleBlades[instanceID];

                float side = vertex.x;
                float t    = vertex.y;

                float3 surfNorm  = normalize(blade.normal);
                float3 worldUp2  = abs(surfNorm.y) < 0.98 ? float3(0,1,0) : float3(1,0,0);
                float3 baseRight = normalize(cross(surfNorm, worldUp2));
                float  ca        = cos(blade.facingAngle), sa = sin(blade.facingAngle);
                float3 bladeRight = ca * baseRight + sa * cross(surfNorm, baseRight);

                float swayT   = t * t;
                float speedV  = _WindSpeed * (1.0 + (blade.phase / (PI * 2.0) - 0.5) * _WindTurbulence);
                float swayAmt = sin(_Time.y * speedV + blade.phase) * _WindStrength * blade.height * swayT;
                float3 windD  = normalize(float3(_WindDirection.x, 0, _WindDirection.z));
                float  curve  = _BladeCurve * t * t * blade.height * 0.35;

                float3 worldPos = blade.position
                    + bladeRight * (side * blade.width * lerp(1.0, _TipTaper, t * t))
                    + surfNorm   * (t * blade.height)
                    + windD      * (swayAmt + curve);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDir = normalize(_LightPosition - worldPos);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                o.positionCS = TransformWorldToHClip(ApplyShadowBias(worldPos, surfNorm, lightDir));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                float dist = distance(blade.position, _CameraPosition.xyz);
                o.distFade = 1.0 - saturate((dist - _FadeStartDistance)
                                          / max(_MaxDistance - _FadeStartDistance, 0.001));
                o.t = t;
                return o;
            }

            half4 shadowFrag(Varyings i) : SV_Target
            {
                float alpha = i.distFade * lerp(1.0, 0.4, i.t * i.t);
                clip(alpha - _AlphaCutoff);
                return 0;
            }
            ENDHLSL
        }

        // ─────────────────────────────────────────────────────────────────────
        // DEPTH NORMALS
        // Writes normals to SV_Target0 and rendering layer mask to SV_Target1
        // when _WRITE_RENDERING_LAYERS is active, so URP decal projectors can
        // correctly layer-filter grass pixels.
        // ─────────────────────────────────────────────────────────────────────
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex   dnVert
            #pragma fragment dnFrag

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct BladeData
            {
                float3 position;
                float3 normal;
                float  height;
                float  width;
                float  phase;
                float  facingAngle;
            };

            StructuredBuffer<BladeData> _VisibleBlades;

            CBUFFER_START(UnityPerMaterial)
            float4 _WindDirection;
            float  _WindSpeed;
            float  _WindStrength;
            float  _WindTurbulence;
            float  _TipTaper;
            float  _BladeCurve;
            float  _AlphaCutoff;
            float4 _CameraPosition;
            float  _MaxDistance;
            float  _FadeStartDistance;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float  distFade   : TEXCOORD1;
                float  t          : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #if defined(_WRITE_RENDERING_LAYERS)
            struct FragmentOutput
            {
                half4 normalOut       : SV_Target0;
                float renderingLayers : SV_Target1;
            };
            #endif

            Varyings dnVert(float4 vertex : POSITION, uint instanceID : SV_InstanceID)
            {
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                BladeData blade = _VisibleBlades[instanceID];

                float side = vertex.x;
                float t    = vertex.y;

                float3 surfNorm  = normalize(blade.normal);
                float3 worldUp3  = abs(surfNorm.y) < 0.98 ? float3(0,1,0) : float3(1,0,0);
                float3 baseRight = normalize(cross(surfNorm, worldUp3));
                float  ca        = cos(blade.facingAngle), sa = sin(blade.facingAngle);
                float3 bladeRight = ca * baseRight + sa * cross(surfNorm, baseRight);

                float swayT   = t * t;
                float speedV  = _WindSpeed * (1.0 + (blade.phase / (PI * 2.0) - 0.5) * _WindTurbulence);
                float swayAmt = sin(_Time.y * speedV + blade.phase) * _WindStrength * blade.height * swayT;
                float3 windD  = normalize(float3(_WindDirection.x, 0, _WindDirection.z));
                float  curve  = _BladeCurve * t * t * blade.height * 0.35;

                float3 worldPos = blade.position
                    + bladeRight * (side * blade.width * lerp(1.0, _TipTaper, t * t))
                    + surfNorm   * (t * blade.height)
                    + windD      * (swayAmt + curve);

                o.positionCS = TransformWorldToHClip(worldPos);
                o.normalWS   = NormalizeNormalPerPixel(surfNorm);

                float dist = distance(blade.position, _CameraPosition.xyz);
                o.distFade = 1.0 - saturate((dist - _FadeStartDistance)
                                          / max(_MaxDistance - _FadeStartDistance, 0.001));
                o.t = t;
                return o;
            }

            #if defined(_WRITE_RENDERING_LAYERS)
            FragmentOutput dnFrag(Varyings input)
            #else
            half4 dnFrag(Varyings input) : SV_Target
            #endif
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float alpha = input.distFade * lerp(1.0, 0.4, input.t * input.t);
                clip(alpha - _AlphaCutoff);

                half4 normalOut = half4(input.normalWS * 0.5 + 0.5, 0.0);

                #if defined(_WRITE_RENDERING_LAYERS)
                    FragmentOutput o;
                    o.normalOut       = normalOut;
                    o.renderingLayers = float(GetMeshRenderingLayer());
                    return o;
                #else
                    return normalOut;
                #endif
            }
            ENDHLSL
        }
    }
}
