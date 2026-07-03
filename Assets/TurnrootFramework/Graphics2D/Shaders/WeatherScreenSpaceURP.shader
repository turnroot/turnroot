// Turnroot/Weather/ScreenSpaceURP
// Procedural screen-space weather overlay for URP.
// Supports rain, drizzle, snow, and ash.
//
// Intended usage:
// 1) Full-screen quad parented to camera (recommended for quick setup), OR
// 2) URP Full Screen Pass Renderer Feature using this material.
//
// This shader is transparent and overlays the scene.
// It does not require a RenderTexture unless you specifically need camera stacking/compositing workflows.

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
        _ParallaxDrizzle ("Parallax Drizzle", Range(0,4)) = 1.0
        _ParallaxSnow ("Parallax Snow", Range(0,4)) = 0.65
        _ParallaxAsh ("Parallax Ash", Range(0,4)) = 0.55

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
        _RainColor ("Rain Color", Color) = (0.78,0.85,1,1)
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
        _RainJitter ("Rain Horizontal Jitter", Range(0,1)) = 0.35
        _RainSpawn ("Rain Spawn Chance", Range(0,1)) = 0.78
        _RainSoftness ("Rain Edge Softness", Range(0.0005,0.25)) = 0.018

        [Header(Drizzle)]
        _DrizzleEnabled ("Enable Drizzle", Float) = 0
        _DrizzleIntensity ("Drizzle Intensity", Range(0,2)) = 0.6
        _DrizzleOpacity ("Drizzle Opacity", Range(0,1)) = 0.35
        _DrizzleColor ("Drizzle Color", Color) = (0.8,0.86,1,1)
        _DrizzleDensity ("Drizzle Density", Range(10,900)) = 180
        _DrizzleSpeed ("Drizzle Speed", Range(0,20)) = 4
        _DrizzleWidth ("Drizzle Width", Range(0.0002,0.2)) = 0.006
        _DrizzleLength ("Drizzle Length", Range(0.02,0.99)) = 0.4
        _DrizzleWidthRandomness ("Drizzle Width Randomness", Range(0,1)) = 0.25
        _DrizzleLengthRandomness ("Drizzle Length Randomness", Range(0,1)) = 0.3
        _DrizzleStreakTiling ("Drizzle Streak Tiling", Range(0.05,2.0)) = 0.5
        _DrizzleFlatBody ("Drizzle Flat Body", Range(0,1)) = 1
        _DrizzleFallAngle ("Drizzle Fall Angle (deg)", Range(-80,80)) = 8
        _DrizzleCameraYawInfluence ("Drizzle Camera Yaw Influence", Range(-1,1)) = 0.35
        _DrizzleJitter ("Drizzle Horizontal Jitter", Range(0,1)) = 0.4
        _DrizzleSpawn ("Drizzle Spawn Chance", Range(0,1)) = 0.62
        _DrizzleSoftness ("Drizzle Edge Softness", Range(0.0005,0.25)) = 0.03

        [Header(Snow)]
        _SnowEnabled ("Enable Snow", Float) = 0
        _SnowIntensity ("Snow Intensity", Range(0,2)) = 0.7
        _SnowOpacity ("Snow Opacity", Range(0,1)) = 0.8
        _SnowColor ("Snow Color", Color) = (1,1,1,1)
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

        [Header(Ash)]
        _AshEnabled ("Enable Ash", Float) = 0
        _AshIntensity ("Ash Intensity", Range(0,2)) = 0.8
        _AshOpacity ("Ash Opacity", Range(0,1)) = 0.7
        _AshColor ("Ash Color", Color) = (0.42,0.42,0.42,1)
        _AshDensity ("Ash Density", Range(2,250)) = 80
        _AshSpeed ("Ash Speed", Range(0,10)) = 0.9
        _AshSize ("Ash Size", Range(0.001,0.25)) = 0.04
        _AshSizeRandomness ("Ash Size Randomness", Range(0,1)) = 0.6
        _AshDriftAmount ("Ash Drift Amount", Range(0,1)) = 0.7
        _AshDriftSpeed ("Ash Drift Speed", Range(0,8)) = 2.2
        _AshFallAngle ("Ash Fall Angle (deg)", Range(-80,80)) = 12
        _AshCameraYawInfluence ("Ash Camera Yaw Influence", Range(-1,1)) = 0.3
        _AshSpawn ("Ash Spawn Chance", Range(0,1)) = 0.72
        _AshDotEdgeSoftness ("Ash Dot Edge Softness", Range(0.001,1)) = 0.1

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
                float _ParallaxDrizzle;
                float _ParallaxSnow;
                float _ParallaxAsh;

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
                float4 _RainColor;
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
                float _RainJitter;
                float _RainSpawn;
                float _RainSoftness;

                float _DrizzleEnabled;
                float _DrizzleIntensity;
                float _DrizzleOpacity;
                float4 _DrizzleColor;
                float _DrizzleDensity;
                float _DrizzleSpeed;
                float _DrizzleWidth;
                float _DrizzleLength;
                float _DrizzleWidthRandomness;
                float _DrizzleLengthRandomness;
                float _DrizzleStreakTiling;
                float _DrizzleFlatBody;
                float _DrizzleFallAngle;
                float _DrizzleCameraYawInfluence;
                float _DrizzleJitter;
                float _DrizzleSpawn;
                float _DrizzleSoftness;

                float _SnowEnabled;
                float _SnowIntensity;
                float _SnowOpacity;
                float4 _SnowColor;
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

                float _AshEnabled;
                float _AshIntensity;
                float _AshOpacity;
                float4 _AshColor;
                float _AshDensity;
                float _AshSpeed;
                float _AshSize;
                float _AshSizeRandomness;
                float _AshDriftAmount;
                float _AshDriftSpeed;
                float _AshFallAngle;
                float _AshCameraYawInfluence;
                float _AshSpawn;
                float _AshDotEdgeSoftness;

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

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i + float2(0, 0));
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float FBM(float2 p)
            {
                float value = 0;
                float amp = 0.5;
                float2 shift = float2(123.7, 271.9);

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    value += amp * Noise(p);
                    p = p * 2.03 + shift;
                    amp *= 0.5;
                }

                return value;
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

                // 0 deg means falling straight down screen.
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

            float SampleRainLayer(
                float2 uv,
                float density,
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
                float softness,
                float seed
            )
            {
                float2 perp = float2(-fallDir.y, fallDir.x);

                // Keep density visually similar on different aspect ratios.
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 suv = float2((uv.x - 0.5) * aspect + 0.5, uv.y);

                float2 rot = float2(dot(suv, perp), dot(suv, fallDir));
                float2 p = float2(
                    rot.x * max(density, 1.0),
                    rot.y * max(density, 1.0) * max(streakTiling, 0.01)
                );
                p.y += _Time.y * speed;

                float2 cell = floor(p);
                float2 f = frac(p) - 0.5;

                float2 h = Hash22(cell + seed);
                float spawnMask = step(1.0 - spawn, h.x);
                float widthRand = lerp(1.0 - widthRandomness, 1.0 + widthRandomness, h.y);
                float lenRand = lerp(1.0 - lengthRandomness, 1.0 + lengthRandomness, Hash11(h.x * 31.7));

                // Random left-right offset per cell.
                float xOffset = (h.y - 0.5) * jitter;
                float xDist = abs(f.x + xOffset);

                // Scale line thickness by density so density changes count/spacing, not apparent size.
                float densityScale = max(density, 1.0);
                float localWidth = width * widthRand * densityScale;
                float lineSoftness = max(softness * densityScale, 1e-4);
                float localLength = saturate(lengthNorm * lenRand);

                // Raindrop segment in cell space.
                float yHead = frac(p.y + h.x * 17.31);
                float ySoft =
                    (1.0 - smoothstep(localLength, localLength + softness, yHead))
                    * smoothstep(0.0, softness * 1.5, yHead);
                float yHard = step(yHead, localLength);
                float yMask = lerp(ySoft, yHard, saturate(flatBody));

                float lineMask = 1.0 - smoothstep(localWidth, localWidth + lineSoftness, xDist);
                return saturate(lineMask * yMask * spawnMask);
            }

            float SampleFlakeLayer(
                float2 uv,
                float density,
                float fallSpeed,
                float size,
                float sizeRandomness,
                float2 fallDir,
                float driftAmount,
                float driftSpeed,
                float spawn,
                float edgeSoftness,
                float seed
            )
            {
                float2 perp = float2(-fallDir.y, fallDir.x);
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 suv = float2((uv.x - 0.5) * aspect + 0.5, uv.y);

                float2 rot = float2(dot(suv, perp), dot(suv, fallDir));
                float2 p = rot * max(density, 1.0);

                // Time is applied in rotated/fall space to preserve directional movement.
                p.y += _Time.y * fallSpeed;
                p.x += sin(_Time.y * driftSpeed + p.y * 0.12 + seed) * driftAmount;

                float2 id = floor(p);
                float2 f = frac(p) - 0.5;

                float2 rnd = Hash22(id + seed);
                float spawnMask = step(1.0 - spawn, rnd.x);

                // Scale particle radius by density so density changes count/spacing, not apparent size.
                float densityScale = max(density, 1.0);
                float localSize = size * densityScale * lerp(1.0 - sizeRandomness, 1.0 + sizeRandomness, rnd.y);
                localSize = min(localSize, 0.49);

                // Center offset for natural dot spacing.
                float2 center = (rnd - 0.5) * 0.65;

                float2 d = f - center;
                float dist = length(d);
                float edge = max(edgeSoftness, 0.001);
                float disk = 1.0 - smoothstep(localSize, localSize * (1.0 + edge), dist);

                return saturate(disk * spawnMask);
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
                float2 uvDrizzle = uv + GetParallaxOffset(_ParallaxDrizzle);
                float2 uvSnow = uv + GetParallaxOffset(_ParallaxSnow);
                float2 uvAsh = uv + GetParallaxOffset(_ParallaxAsh);

                float rain = 0;
                float drizzle = 0;
                float snow = 0;
                float ash = 0;

                if (_RainEnabled > 0.5)
                {
                    float2 dir = GetFallDirection(_RainFallAngle, _RainCameraYawInfluence);
                    float rainBack = SampleRainLayer(
                        uv + GetParallaxOffset(_ParallaxRain * _LayerBackParallax),
                        _RainDensity * _LayerBackDensity,
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
                        _RainSoftness,
                        11.0
                    );
                    float rainMid = SampleRainLayer(
                        uvRain,
                        _RainDensity * _LayerMidDensity,
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
                        _RainSoftness,
                        29.0
                    );
                    float rainFore = SampleRainLayer(
                        uv + GetParallaxOffset(_ParallaxRain * _LayerForeParallax),
                        _RainDensity * _LayerForeDensity,
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
                        _RainSoftness,
                        47.0
                    );

                    rain += rainBack * 0.6 + rainMid + rainFore * 0.8;

                    rain *= _RainIntensity;
                }

                if (_DrizzleEnabled > 0.5)
                {
                    float2 dir = GetFallDirection(_DrizzleFallAngle, _DrizzleCameraYawInfluence);
                    float drizzleBack = SampleRainLayer(
                        uv + GetParallaxOffset(_ParallaxDrizzle * _LayerBackParallax),
                        _DrizzleDensity * _LayerBackDensity,
                        _DrizzleSpeed * 0.9,
                        _DrizzleWidth * _LayerBackSize,
                        _DrizzleLength * _LayerBackSize,
                        _DrizzleWidthRandomness,
                        _DrizzleLengthRandomness,
                        _DrizzleStreakTiling,
                        _DrizzleFlatBody,
                        dir,
                        _DrizzleJitter,
                        _DrizzleSpawn,
                        _DrizzleSoftness,
                        71.0
                    );
                    float drizzleMid = SampleRainLayer(
                        uvDrizzle,
                        _DrizzleDensity * _LayerMidDensity,
                        _DrizzleSpeed,
                        _DrizzleWidth * _LayerMidSize,
                        _DrizzleLength * _LayerMidSize,
                        _DrizzleWidthRandomness,
                        _DrizzleLengthRandomness,
                        _DrizzleStreakTiling,
                        _DrizzleFlatBody,
                        dir,
                        _DrizzleJitter,
                        _DrizzleSpawn,
                        _DrizzleSoftness,
                        89.0
                    );
                    float drizzleFore = SampleRainLayer(
                        uv + GetParallaxOffset(_ParallaxDrizzle * _LayerForeParallax),
                        _DrizzleDensity * _LayerForeDensity,
                        _DrizzleSpeed * 1.1,
                        _DrizzleWidth * _LayerForeSize,
                        _DrizzleLength * _LayerForeSize,
                        _DrizzleWidthRandomness,
                        _DrizzleLengthRandomness,
                        _DrizzleStreakTiling,
                        _DrizzleFlatBody,
                        dir,
                        _DrizzleJitter * 1.1,
                        _DrizzleSpawn,
                        _DrizzleSoftness,
                        107.0
                    );

                    drizzle += drizzleBack * 0.55 + drizzleMid + drizzleFore * 0.7;
                    drizzle *= _DrizzleIntensity;
                }

                if (_SnowEnabled > 0.5)
                {
                    float2 dir = GetFallDirection(_SnowFallAngle, _SnowCameraYawInfluence);

                    float snowBack = SampleFlakeLayer(
                        uv + GetParallaxOffset(_ParallaxSnow * _LayerBackParallax),
                        _SnowDensity * _LayerBackDensity,
                        _SnowSpeed * 0.9,
                        _SnowSize * _LayerBackSize,
                        _SnowSizeRandomness,
                        dir,
                        _SnowDriftAmount,
                        _SnowDriftSpeed,
                        _SnowSpawn,
                        _SnowDotEdgeSoftness,
                        131.0
                    );
                    float snowMid = SampleFlakeLayer(
                        uvSnow,
                        _SnowDensity * _LayerMidDensity,
                        _SnowSpeed,
                        _SnowSize * _LayerMidSize,
                        _SnowSizeRandomness,
                        dir,
                        _SnowDriftAmount,
                        _SnowDriftSpeed,
                        _SnowSpawn,
                        _SnowDotEdgeSoftness,
                        149.0
                    );
                    float snowFore = SampleFlakeLayer(
                        uv + GetParallaxOffset(_ParallaxSnow * _LayerForeParallax),
                        _SnowDensity * _LayerForeDensity,
                        _SnowSpeed * 1.08,
                        _SnowSize * _LayerForeSize,
                        _SnowSizeRandomness,
                        dir,
                        _SnowDriftAmount * 1.1,
                        _SnowDriftSpeed * 1.2,
                        saturate(_SnowSpawn * 0.95),
                        _SnowDotEdgeSoftness,
                        167.0
                    );

                    snow += snowBack * 0.65 + snowMid + snowFore * 0.85;

                    snow *= _SnowIntensity;
                }

                if (_AshEnabled > 0.5)
                {
                    float2 dir = GetFallDirection(_AshFallAngle, _AshCameraYawInfluence);

                    float ashBack = SampleFlakeLayer(
                        uv + GetParallaxOffset(_ParallaxAsh * _LayerBackParallax),
                        _AshDensity * _LayerBackDensity,
                        _AshSpeed * 0.9,
                        _AshSize * _LayerBackSize,
                        _AshSizeRandomness,
                        dir,
                        _AshDriftAmount,
                        _AshDriftSpeed,
                        _AshSpawn,
                        _AshDotEdgeSoftness,
                        191.0
                    );
                    float ashMid = SampleFlakeLayer(
                        uvAsh,
                        _AshDensity * _LayerMidDensity,
                        _AshSpeed,
                        _AshSize * _LayerMidSize,
                        _AshSizeRandomness,
                        dir,
                        _AshDriftAmount,
                        _AshDriftSpeed,
                        _AshSpawn,
                        _AshDotEdgeSoftness,
                        223.0
                    );
                    float ashFore = SampleFlakeLayer(
                        uv + GetParallaxOffset(_ParallaxAsh * _LayerForeParallax),
                        _AshDensity * _LayerForeDensity,
                        _AshSpeed * 1.12,
                        _AshSize * _LayerForeSize,
                        _AshSizeRandomness,
                        dir,
                        _AshDriftAmount * 1.15,
                        _AshDriftSpeed * 1.2,
                        saturate(_AshSpawn * 0.94),
                        _AshDotEdgeSoftness,
                        251.0
                    );

                    ash += ashBack * 0.7 + ashMid + ashFore * 0.8;

                    ash *= _AshIntensity;
                }

                float3 rgb = 0;
                float alpha = 0;

                float rainA = saturate(rain * _RainOpacity);
                float drizzleA = saturate(drizzle * _DrizzleOpacity);
                float snowA = saturate(snow * _SnowOpacity);
                float ashA = saturate(ash * _AshOpacity);

                rgb += _RainColor.rgb * rainA;
                rgb += _DrizzleColor.rgb * drizzleA;
                rgb += _SnowColor.rgb * snowA;
                rgb += _AshColor.rgb * ashA;

                alpha = rainA + drizzleA + snowA + ashA;
                alpha = saturate(alpha) * _GlobalOpacity * edgeMask;

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
