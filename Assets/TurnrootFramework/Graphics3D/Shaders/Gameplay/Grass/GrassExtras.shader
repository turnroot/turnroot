Shader "Turnroot/GrassExtras"
{
    Properties
    {
        _MainTex      ("Texture", 2D) = "white" {}
        _Tint         ("Tint Color", Color) = (1,1,1,1)
        _AlphaCutoff  ("Alpha Cutoff", Float) = 0.25
        _MaxDistance  ("Max Distance", Float) = 50
        _FadeStartDistance ("Fade Start", Float) = 35
        [HideInInspector]
        _CameraPosition ("Camera Position", Vector) = (0,0,0,0)

        // wind parameters (optional, copied from grass shader)
        _WindDirection  ("Wind Direction XZ", Vector) = (1,0,0,0)
        _WindSpeed      ("Wind Speed", Float) = 1.2
        _WindStrength   ("Wind Strength", Float) = 0.0
        _WindTurbulence ("Wind Turbulence", Float) = 0.0

        [Header(Tinting)]
        _CelTintColor       ("Cel Tint Color", Color) = (1,1,1,1)
        [Range(0,1)]
        _CelTintIntensity   ("Cel Tint Intensity", Float) = 0.0
        _NightTintColor     ("Night Tint Color", Color) = (0.1,0.13,0.25,1)
        [Range(0,1)]
        _NightTintIntensity ("Night Tint Intensity", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct BladeData { float3 position; float3 normal; float height; float width; float phase; float facingAngle; };
            StructuredBuffer<BladeData> _VisibleBlades;

            CBUFFER_START(UnityPerMaterial)
            float4 _Tint;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _AlphaCutoff;
            float _MaxDistance;
            float _FadeStartDistance;
            float4 _CameraPosition;
            float4 _WindDirection;
            float _WindSpeed;
            float _WindStrength;
            float _WindTurbulence;
            float4 _CelTintColor;
            float  _CelTintIntensity;
            float4 _NightTintColor;
            float  _NightTintIntensity;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float distFade    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // build right/forward axes from normal + facingAngle
            void BladeAxes(float3 surfaceNormal, float facingAngle,
                           out float3 right, out float3 fwd)
            {
                float3 worldUp = abs(surfaceNormal.y) < 0.98 ? float3(0,1,0) : float3(1,0,0);
                float3 baseRight = normalize(cross(surfaceNormal, worldUp));
                float ca = cos(facingAngle), sa = sin(facingAngle);
                right = ca * baseRight + sa * cross(surfaceNormal, baseRight);
                fwd   = cross(right, surfaceNormal);
            }

            Varyings vert(float3 position : POSITION, float2 uv : TEXCOORD0, uint instanceID : SV_InstanceID)
            {
                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                BladeData blade = _VisibleBlades[instanceID];

                float3 surfNorm = normalize(blade.normal);
                float3 right, fwd;
                BladeAxes(surfNorm, blade.facingAngle, right, fwd);

                // apply wind sway (uses full height)
                float swayAmt = 0.0;
                if (_WindStrength > 0.0001)
                {
                    float speedVar = _WindSpeed * (1.0 + (blade.phase/(2*PI)-0.5) * _WindTurbulence);
                    swayAmt = sin(_Time.y * speedVar + blade.phase) * _WindStrength * blade.height;
                }
                float3 windDir = normalize(float3(_WindDirection.x,0,_WindDirection.z));

                // position.x = horizontal offset, position.y = vertical fraction (0..1)
                float side = position.x;
                float t    = position.y;

                float3 worldPos = blade.position
                    + surfNorm * (t * blade.height)
                    + right   * (side * blade.width)
                    + windDir * (swayAmt * t);

                o.positionCS = TransformWorldToHClip(worldPos);
                o.uv = uv;

                float dist = distance(blade.position, _CameraPosition.xyz);
                o.distFade = 1.0 - saturate((dist - _FadeStartDistance)/max(_MaxDistance - _FadeStartDistance,0.001));
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float alpha = i.distFade;
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Tint;
                col.rgb = lerp(col.rgb, _CelTintColor.rgb, _CelTintIntensity);
                col.rgb = lerp(col.rgb, _NightTintColor.rgb, _NightTintIntensity);
                clip(alpha * col.a - _AlphaCutoff);
                return half4(col.rgb, alpha * col.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct BladeData { float3 position; float3 normal; float height; float width; float phase; float facingAngle; };
            StructuredBuffer<BladeData> _VisibleBlades;
            CBUFFER_START(UnityPerMaterial)
            float _AlphaCutoff;
            float _MaxDistance;
            float _FadeStartDistance;
            float4 _CameraPosition;
            float4 _WindDirection;
            float _WindSpeed;
            float _WindStrength;
            float _WindTurbulence;
            CBUFFER_END

            void BladeAxes(float3 surfaceNormal, float facingAngle,
                           out float3 right, out float3 fwd)
            {
                float3 worldUp = abs(surfaceNormal.y) < 0.98 ? float3(0,1,0) : float3(1,0,0);
                float3 baseRight = normalize(cross(surfaceNormal, worldUp));
                float ca = cos(facingAngle), sa = sin(facingAngle);
                right = ca * baseRight + sa * cross(surfaceNormal, baseRight);
                fwd   = cross(right, surfaceNormal);
            }

            struct Varyings { float4 pos : SV_POSITION; float distFade : TEXCOORD0; };

            Varyings shadowVert(float3 position : POSITION, uint instanceID : SV_InstanceID)
            {
                Varyings o;
                BladeData blade = _VisibleBlades[instanceID];
                float3 surfNorm = normalize(blade.normal);
                float3 right, fwd;
                BladeAxes(surfNorm, blade.facingAngle, right, fwd);
                float swayAmt = 0.0;
                if (_WindStrength > 0.0001)
                {
                    float speedVar = _WindSpeed * (1.0 + (blade.phase/(2*PI)-0.5) * _WindTurbulence);
                    swayAmt = sin(_Time.y * speedVar + blade.phase) * _WindStrength * blade.height;
                }
                float3 windDir = normalize(float3(_WindDirection.x,0,_WindDirection.z));
                float3 worldPos = blade.position
                    + right * (position.x * blade.width)
                    + fwd  * (position.z * blade.width)
                    + windDir * (swayAmt * position.y);
                o.pos = TransformWorldToHClip(worldPos);
                float dist = distance(blade.position, _CameraPosition.xyz);
                o.distFade = 1.0 - saturate((dist - _FadeStartDistance)/max(_MaxDistance - _FadeStartDistance,0.001));
                return o;
            }

            half4 shadowFrag(Varyings i) : SV_Target
            {
                float alpha = i.distFade;
                clip(alpha - _AlphaCutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
