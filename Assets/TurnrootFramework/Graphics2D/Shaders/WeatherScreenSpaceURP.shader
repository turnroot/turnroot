// Turnroot/Weather/ScreenSpaceURP
// Procedural screen-space weather overlay for URP.
// Supports rain and snow (snow can be used as ash via preset color/behavior).

Shader "Turnroot/Weather/ScreenSpaceURP"
{
    Properties
    {
        [Header(Global)]
        _GlobalOpacity ("Global Opacity", Range(0,1)) = 1
        _Brightness ("Brightness", Range(0,3)) = 1
        _Contrast ("Contrast", Range(0,3)) = 1

        [Header(Camera Alignment)]
        _WorldForwardXZ ("Reference Forward XZ (x,z)", Vector) = (1,0,0,0)
        _GlobalWindAngle ("Global Wind Angle (deg)", Range(-180,180)) = 0

        [Header(Parallax)]
        _ParallaxEnabled ("Enable Parallax", Float) = 1
        _ParallaxAmount ("Parallax Amount", Range(0,12)) = 0.35
        _ParallaxYawAmount ("Parallax Yaw Influence", Range(0,2)) = 1
        _ParallaxPitchAmount ("Parallax Pitch Influence", Range(0,2)) = 1
        _ParallaxRain ("Parallax Rain", Range(0,4)) = 1.2
        _ParallaxSnow ("Parallax Snow", Range(0,4)) = 0.65

        [Header(Layer Parallax)]
        _LayerBackParallax ("Back Layer Parallax", Range(0,6)) = 0.35
        _LayerMidParallax ("Mid Layer Parallax", Range(0,6)) = 1
        _LayerForeParallax ("Fore Layer Parallax", Range(0,6)) = 2

        [Header(Layer Density)]
        _LayerBackDensity ("Back Layer Density", Range(0.1,4)) = 1.6
        _LayerMidDensity ("Mid Layer Density", Range(0.1,4)) = 1
        _LayerForeDensity ("Fore Layer Density", Range(0.1,4)) = 0.65

        [Header(Layer Size)]
        _LayerBackSize ("Back Layer Size", Range(0.1,4)) = 0.7
        _LayerMidSize ("Mid Layer Size", Range(0.1,4)) = 1
        _LayerForeSize ("Fore Layer Size", Range(0.1,4)) = 1.45

        [Header(Rain)]
        _RainEnabled ("Enable Rain", Float) = 1
        _RainIntensity ("Rain Intensity", Range(0,2)) = 0.8
        _RainOpacity ("Rain Opacity", Range(0,1)) = 0.55
        _RainColor1 ("Rain Color 1", Color) = (0.78,0.85,1,1)
        _RainColor2 ("Rain Color 2", Color) = (0.62,0.72,0.92,1)
        _RainColor1Chance ("Rain Color 1 Chance", Range(0,1)) = 1
        _RainDensity ("Rain Density", Range(10,900)) = 280
        _RainSpeed ("Rain Speed", Range(0,20)) = 9
        _RainWidth ("Rain Width", Range(0.0002,0.25)) = 0.008
        _RainLength ("Rain Length", Range(0.02,0.99)) = 0.62
        _RainWidthRandomness ("Rain Width Randomness", Range(0,1)) = 0.35
        _RainLengthRandomness ("Rain Length Randomness", Range(0,1)) = 0.4
        _RainStreakTiling ("Rain Streak Tiling", Range(0.05,2.0)) = 0.35
        _RainFlatBody ("Rain Flat Body", Range(0,1)) = 1
        _RainFallAngle ("Rain Fall Angle (deg)", Range(-80,80)) = 18
        _RainCameraYawInfluence ("Rain Camera Yaw Influence", Range(-1,1)) = 0.45
        _RainAngleClampMin ("Rain Angle Clamp Min", Range(-89,89)) = -70
        _RainAngleClampMax ("Rain Angle Clamp Max", Range(-89,89)) = 70
        _RainJitter ("Rain Horizontal Jitter", Range(0,1)) = 0.35
        _RainSpawn ("Rain Spawn Chance", Range(0,1)) = 0.78
        _RainSoftness ("Rain Edge Softness", Range(0.0005,0.25)) = 0.018

        [Header(Snow)]
        _SnowEnabled ("Enable Snow", Float) = 0
        _SnowIntensity ("Snow Intensity", Range(0,2)) = 0.7
        _SnowOpacity ("Snow Opacity", Range(0,1)) = 0.8
        _SnowColor1 ("Snow Color 1", Color) = (1,1,1,1)
        _SnowColor2 ("Snow Color 2", Color) = (0.88,0.94,1,1)
        _SnowColor1Chance ("Snow Color 1 Chance", Range(0,1)) = 1
        _SnowDensity ("Snow Density", Range(2,250)) = 50
        _SnowSpeed ("Snow Speed", Range(0,10)) = 1.3
        _SnowSize ("Snow Size", Range(0.001,0.25)) = 0.05
        _SnowSizeRandomness ("Snow Size Randomness", Range(0,1)) = 0.65
        _SnowDriftAmount ("Snow Drift Amount", Range(0,1)) = 0.45
        _SnowDriftSpeed ("Snow Drift Speed", Range(0,8)) = 1.4
        _SnowFallAngle ("Snow Fall Angle (deg)", Range(-80,80)) = 5
        _SnowCameraYawInfluence ("Snow Camera Yaw Influence", Range(-1,1)) = 0.2
        _SnowSpawn ("Snow Spawn Chance", Range(0,1)) = 0.86
        _SnowDotEdgeSoftness ("Snow Dot Edge Softness", Range(0.001,1)) = 0.08

        [Header(Depth Fade)]
        _VerticalFadeTop ("Top Fade", Range(0,1)) = 0
        _VerticalFadeBottom ("Bottom Fade", Range(0,1)) = 0
        _HorizontalFadeLeft ("Left Fade", Range(0,1)) = 0
        _HorizontalFadeRight ("Right Fade", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ScreenSpaceWeather"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float _GlobalOpacity;
                float _Brightness;
                float _Contrast;

                float4 _WorldForwardXZ;
                float _GlobalWindAngle;

                float _ParallaxEnabled;
                float _ParallaxAmount;
                float _ParallaxYawAmount;
                float _ParallaxPitchAmount;
                float _ParallaxRain;
                float _ParallaxSnow;

                float _LayerBackParallax;
                float _LayerMidParallax;
                float _LayerForeParallax;

                float _LayerBackDensity;
                float _LayerMidDensity;
                float _LayerForeDensity;

                float _LayerBackSize;
                float _LayerMidSize;
                float _LayerForeSize;

                float _RainEnabled;
                float _RainIntensity;
                float _RainOpacity;
                float4 _RainColor1;
                float4 _RainColor2;
                float _RainColor1Chance;
                float _RainDensity;
                float _RainSpeed;
                float _RainWidth;
                float _RainLength;
                float _RainWidthRandomness;
                float _RainLengthRandomness;
                float _RainStreakTiling;
                float _RainFlatBody;
                float _RainFallAngle;
                float _RainCameraYawInfluence;
                float _RainAngleClampMin;
                float _RainAngleClampMax;
                float _RainJitter;
                float _RainSpawn;
                float _RainSoftness;

                float _SnowEnabled;
                float _SnowIntensity;
                float _SnowOpacity;
                float4 _SnowColor1;
                float4 _SnowColor2;
                float _SnowColor1Chance;
                float _SnowDensity;
                float _SnowSpeed;
                float _SnowSize;
                float _SnowSizeRandomness;
                float _SnowDriftAmount;
                float _SnowDriftSpeed;
                float _SnowFallAngle;
                float _SnowCameraYawInfluence;
                float _SnowSpawn;
                float _SnowDotEdgeSoftness;

                float _VerticalFadeTop;
                float _VerticalFadeBottom;
                float _HorizontalFadeLeft;
                float _HorizontalFadeRight;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 Hash22(float2 p)
            {
                float n = Hash21(p);
                return float2(n, Hash11(n * 97.13));
            }

            float2 GetCameraForwardXZ()
            {
                float3 camForwardWS = normalize(mul((float3x3)UNITY_MATRIX_I_V, float3(0, 0, -1)));
                float2 xz = camForwardWS.xz;
                float len = max(length(xz), 1e-5);
                return xz / len;
            }

            float2 GetParallaxOffset(float layerScale)
            {
                if (_ParallaxEnabled < 0.5 || _ParallaxAmount <= 1e-5)
                {
                    return float2(0, 0);
                }

                float3 camForwardWS = normalize(mul((float3x3)UNITY_MATRIX_I_V, float3(0, 0, -1)));
                float2 camXZ = normalize(max(abs(camForwardWS.xz), 1e-5) * sign(camForwardWS.xz + 1e-5));
                float camYaw = atan2(camXZ.y, camXZ.x);

                float2 refXZ = _WorldForwardXZ.xy;
                if (length(refXZ) < 1e-5)
                {
                    refXZ = float2(1.0, 0.0);
                }
                else
                {
                    refXZ = normalize(refXZ);
                }

                float refYaw = atan2(refXZ.y, refXZ.x);
                float relYaw = camYaw - refYaw;
                float relPitch = camForwardWS.y;

                float2 offset = float2(
                    relYaw * 0.5 * _ParallaxYawAmount,
                    relPitch * 0.9 * _ParallaxPitchAmount
                );
                return offset * _ParallaxAmount * layerScale;
            }

            float2 GetFallDirection(float baseAngleDeg, float cameraYawInfluence)
            {
                float2 camXZ = GetCameraForwardXZ();
                float camYaw = atan2(camXZ.y, camXZ.x);

                float2 refXZ = _WorldForwardXZ.xy;
                if (length(refXZ) < 1e-5)
                {
                    refXZ = float2(1.0, 0.0);
                }
                else
                {
                    refXZ = normalize(refXZ);
                }
                float refYaw = atan2(refXZ.y, refXZ.x);
                float relativeYaw = camYaw - refYaw;

                float angle = radians(baseAngleDeg + _GlobalWindAngle) + relativeYaw * cameraYawInfluence;

                float2 dir = float2(sin(angle), -cos(angle));
                return normalize(dir);
            }

            float2 GetRainFallDirection()
            {
                float2 camXZ = GetCameraForwardXZ();
                float camYaw = atan2(camXZ.y, camXZ.x);

                float2 refXZ = _WorldForwardXZ.xy;
                if (length(refXZ) < 1e-5)
                {
                    refXZ = float2(1.0, 0.0);
                }
                else
                {
                    refXZ = normalize(refXZ);
                }

                float refYaw = atan2(refXZ.y, refXZ.x);
                float relativeYaw = camYaw - refYaw;
                float angleDeg = _RainFallAngle + _GlobalWindAngle + degrees(relativeYaw * _RainCameraYawInfluence);

                float minAngle = min(_RainAngleClampMin, _RainAngleClampMax);
                float maxAngle = max(_RainAngleClampMin, _RainAngleClampMax);
                angleDeg = clamp(angleDeg, minAngle, maxAngle);

                float angle = radians(angleDeg);
                float2 dir = float2(sin(angle), -cos(angle));
                return normalize(dir);
            }

            float EdgeMask(float2 uv)
            {
                float anyFade = _VerticalFadeTop + _VerticalFadeBottom + _HorizontalFadeLeft + _HorizontalFadeRight;
                if (anyFade < 0.0001)
                {
                    return 1.0;
                }

                float top = 1.0 - smoothstep(1.0 - _VerticalFadeTop, 1.0, uv.y);
                float bottom = smoothstep(0.0, _VerticalFadeBottom, uv.y);
                float left = smoothstep(0.0, _HorizontalFadeLeft, uv.x);
                float right = 1.0 - smoothstep(1.0 - _HorizontalFadeRight, 1.0, uv.x);
                return saturate(top * bottom * left * right);
            }

            float2 SampleRainLayer(
                float2 uv,
                float density,
                float baseGrid,
                float densityMax,
                float speed,
                float width,
                float lengthNorm,
                float widthRandomness,
                float lengthRandomness,
                float streakTiling,
                float flatBody,
                float2 fallDir,
                float jitter,
                float spawn,
                float color1Chance,
                float softness,
                float seed
            )
            {
                float2 perp = float2(-fallDir.y, fallDir.x);
                float density01 = saturate(density / max(densityMax, 1.0));
                float effectiveSpawn = saturate(spawn * (0.1 + density01 * 1.8));

                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 suv = float2((uv.x - 0.5) * aspect + 0.5, uv.y);

                float2 rot = float2(dot(suv, perp), dot(suv, fallDir));
                float2 p = float2(
                    rot.x * max(baseGrid, 1.0),
                    rot.y * max(baseGrid, 1.0) * max(streakTiling, 0.01)
                );
                p.y -= _Time.y * speed;

                float2 cell = floor(p);
                float2 f = frac(p) - 0.5;

                float2 h = Hash22(cell + seed);
                float spawnMask = step(1.0 - effectiveSpawn, h.x);
                float widthRand = lerp(1.0 - widthRandomness, 1.0 + widthRandomness, h.y);
                float lenRand = lerp(1.0 - lengthRandomness, 1.0 + lengthRandomness, Hash11(h.x * 31.7));

                float xOffset = (h.y - 0.5) * jitter;
                float xDist = abs(f.x + xOffset);

                float localWidth = width * widthRand;
                float lineSoftness = max(softness, 1e-4);
                float localLength = saturate(lengthNorm * lenRand);

                float yHead = frac(p.y + h.x * 17.31);
                float ySoft =
                    (1.0 - smoothstep(localLength, localLength + softness, yHead))
                    * smoothstep(0.0, softness * 1.5, yHead);
                float flatEdgeSoftness = max(softness * 0.5, 1e-4);
                float yFlat = 1.0 - smoothstep(localLength, localLength + flatEdgeSoftness, yHead);
                float yMask = lerp(ySoft, yFlat, saturate(flatBody));

                float lineMask = 1.0 - smoothstep(localWidth, localWidth + lineSoftness, xDist);
                float particleMask = saturate(lineMask * yMask * spawnMask);

                float colorRnd = Hash11(h.x * 53.17 + seed * 0.37);
                float type1 = particleMask * step(colorRnd, saturate(color1Chance));
                float type2 = particleMask - type1;
                return float2(type1, type2);
            }

            float2 SampleFlakeLayer(
                float2 uv,
                float density,
                float baseGrid,
                float densityMax,
                float fallSpeed,
                float size,
                float sizeRandomness,
                float2 fallDir,
                float driftAmount,
                float driftSpeed,
                float spawn,
                float color1Chance,
                float edgeSoftness,
                float seed
            )
            {
                float2 perp = float2(-fallDir.y, fallDir.x);
                float density01 = saturate(density / max(densityMax, 1.0));
                float effectiveSpawn = saturate(spawn * (0.1 + density01 * 1.8));
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 suv = float2((uv.x - 0.5) * aspect + 0.5, uv.y);

                float2 rot = float2(dot(suv, perp), dot(suv, fallDir));
                float2 p = rot * max(baseGrid, 1.0);

                p.y -= _Time.y * fallSpeed;
                p.x += sin(_Time.y * driftSpeed + p.y * 0.12 + seed) * driftAmount;

                float2 id = floor(p);
                float2 f = frac(p) - 0.5;

                float2 rnd = Hash22(id + seed);
                float spawnMask = step(1.0 - effectiveSpawn, rnd.x);

                float localSize = size * lerp(1.0 - sizeRandomness, 1.0 + sizeRandomness, rnd.y);
                localSize = min(localSize, 0.49);

                float2 center = (rnd - 0.5) * 0.65;

                float2 d = f - center;
                float dist = length(d);
                float edge = max(edgeSoftness, 0.001);
                float disk = 1.0 - smoothstep(localSize, localSize * (1.0 + edge), dist);
                float particleMask = saturate(disk * spawnMask);

                float colorRnd = Hash11(rnd.x * 71.91 + seed * 0.19);
                float type1 = particleMask * step(colorRnd, saturate(color1Chance));
                float type2 = particleMask - type1;
                return float2(type1, type2);
            }

            float3 ApplyColorGrade(float3 c)
            {
                c = (c - 0.5) * _Contrast + 0.5;
                c *= _Brightness;
                return saturate(c);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float edgeMask = EdgeMask(uv);

                float2 uvRain = uv + GetParallaxOffset(_ParallaxRain);
                float2 uvSnow = uv + GetParallaxOffset(_ParallaxSnow);

                float rain1 = 0;
                float rain2 = 0;
                float snow1 = 0;
                float snow2 = 0;

                if (_RainEnabled > 0.5)
                {
                    float2 dir = GetRainFallDirection();

                    float2 rainBack = SampleRainLayer(
                        uv + GetParallaxOffset(_ParallaxRain * _LayerBackParallax),
                        _RainDensity * _LayerBackDensity,
                        240.0,
                        900.0,
                        _RainSpeed * 0.9,
                        _RainWidth * _LayerBackSize,
                        _RainLength * _LayerBackSize,
                        _RainWidthRandomness,
                        _RainLengthRandomness,
                        _RainStreakTiling,
                        _RainFlatBody,
                        dir,
                        _RainJitter,
                        _RainSpawn,
                        _RainColor1Chance,
                        _RainSoftness,
                        11.0
                    );
                    float2 rainMid = SampleRainLayer(
                        uvRain,
                        _RainDensity * _LayerMidDensity,
                        240.0,
                        900.0,
                        _RainSpeed,
                        _RainWidth * _LayerMidSize,
                        _RainLength * _LayerMidSize,
                        _RainWidthRandomness,
                        _RainLengthRandomness,
                        _RainStreakTiling,
                        _RainFlatBody,
                        dir,
                        _RainJitter,
                        _RainSpawn,
                        _RainColor1Chance,
                        _RainSoftness,
                        29.0
                    );
                    float2 rainFore = SampleRainLayer(
                        uv + GetParallaxOffset(_ParallaxRain * _LayerForeParallax),
                        _RainDensity * _LayerForeDensity,
                        240.0,
                        900.0,
                        _RainSpeed * 1.15,
                        _RainWidth * _LayerForeSize,
                        _RainLength * _LayerForeSize,
                        _RainWidthRandomness,
                        _RainLengthRandomness,
                        _RainStreakTiling,
                        _RainFlatBody,
                        dir,
                        _RainJitter * 1.1,
                        saturate(_RainSpawn * 0.92),
                        _RainColor1Chance,
                        _RainSoftness,
                        47.0
                    );

                    rain1 = (rainBack.x * 0.6 + rainMid.x + rainFore.x * 0.8) * _RainIntensity;
                    rain2 = (rainBack.y * 0.6 + rainMid.y + rainFore.y * 0.8) * _RainIntensity;
                }

                if (_SnowEnabled > 0.5)
                {
                    float2 dir = GetFallDirection(_SnowFallAngle, _SnowCameraYawInfluence);

                    float2 snowBack = SampleFlakeLayer(
                        uv + GetParallaxOffset(_ParallaxSnow * _LayerBackParallax),
                        _SnowDensity * _LayerBackDensity,
                        110.0,
                        250.0,
                        _SnowSpeed * 0.9,
                        _SnowSize * _LayerBackSize,
                        _SnowSizeRandomness,
                        dir,
                        _SnowDriftAmount,
                        _SnowDriftSpeed,
                        _SnowSpawn,
                        _SnowColor1Chance,
                        _SnowDotEdgeSoftness,
                        131.0
                    );
                    float2 snowMid = SampleFlakeLayer(
                        uvSnow,
                        _SnowDensity * _LayerMidDensity,
                        110.0,
                        250.0,
                        _SnowSpeed,
                        _SnowSize * _LayerMidSize,
                        _SnowSizeRandomness,
                        dir,
                        _SnowDriftAmount,
                        _SnowDriftSpeed,
                        _SnowSpawn,
                        _SnowColor1Chance,
                        _SnowDotEdgeSoftness,
                        149.0
                    );
                    float2 snowFore = SampleFlakeLayer(
                        uv + GetParallaxOffset(_ParallaxSnow * _LayerForeParallax),
                        _SnowDensity * _LayerForeDensity,
                        110.0,
                        250.0,
                        _SnowSpeed * 1.08,
                        _SnowSize * _LayerForeSize,
                        _SnowSizeRandomness,
                        dir,
                        _SnowDriftAmount * 1.1,
                        _SnowDriftSpeed * 1.2,
                        saturate(_SnowSpawn * 0.95),
                        _SnowColor1Chance,
                        _SnowDotEdgeSoftness,
                        167.0
                    );

                    snow1 = (snowBack.x * 0.65 + snowMid.x + snowFore.x * 0.85) * _SnowIntensity;
                    snow2 = (snowBack.y * 0.65 + snowMid.y + snowFore.y * 0.85) * _SnowIntensity;
                }

                float rainA1 = saturate(rain1 * _RainOpacity);
                float rainA2 = saturate(rain2 * _RainOpacity);
                float snowA1 = saturate(snow1 * _SnowOpacity);
                float snowA2 = saturate(snow2 * _SnowOpacity);

                float alpha = saturate(rainA1 + rainA2 + snowA1 + snowA2) * _GlobalOpacity * edgeMask;
                float3 rgb =
                    _RainColor1.rgb * rainA1
                    + _RainColor2.rgb * rainA2
                    + _SnowColor1.rgb * snowA1
                    + _SnowColor2.rgb * snowA2;

                if (alpha > 1e-5)
                {
                    rgb = rgb / max(alpha, 1e-5);
                }

                rgb = ApplyColorGrade(rgb);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
