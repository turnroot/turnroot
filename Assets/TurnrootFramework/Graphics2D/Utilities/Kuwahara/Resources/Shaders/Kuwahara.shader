Shader "Hidden/Kuwahara" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ ANIMATE_SIZE
            #pragma multi_compile_local _ ANIMATE_ORIGIN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                int    _KernelSize;
                int    _MinKernelSize;
                float  _SizeAnimationSpeed;
                float  _NoiseFrequency;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                return OUT;
            }

            // Scalar luminance (BT.601)
            float Luminance601(float3 c) {
                return dot(c, float3(0.299, 0.587, 0.114));
            }

            // Integer hash (Hugo Elias)
            float KuwHash(uint n) {
                n = (n << 13U) ^ n;
                n = n * (n * n * 15731U + 0x789221U) + 0x1376312589U;
                return float(n & 0x7fffffffU) / float(0x7fffffff);
            }

            // Sample one quadrant; returns (avgColor.rgb, variance) in float4.
            // Uses tex2Dlod (mip 0) to avoid implicit gradients inside loops.
            float4 SampleQuadrant(float2 uv, int x1, int x2, int y1, int y2, float rcpN) {
                float  lumSum  = 0.0;
                float  lumSum2 = 0.0;
                float3 colSum  = 0.0;

                UNITY_LOOP
                for (int x = x1; x <= x2; ++x) {
                    UNITY_LOOP
                    for (int y = y1; y <= y2; ++y) {
                        float3 s = SAMPLE_TEXTURE2D_LOD(
                            _MainTex, sampler_MainTex,
                            uv + float2(x, y) * _MainTex_TexelSize.xy, 0).rgb;
                        s = saturate(s);
                        float l = Luminance601(s);
                        lumSum  += l;
                        lumSum2 += l * l;
                        colSum  += s;
                    }
                }

                float mean = lumSum * rcpN;
                float var  = abs(lumSum2 * rcpN - mean * mean);
                return float4(colSum * rcpN, var);
            }

            // Pick the lowest-variance quadrant (or average tied ones).
            float4 ResolveQuadrants(float4 q1, float4 q2, float4 q3, float4 q4) {
                float  minStd = min(min(q1.a, q2.a), min(q3.a, q4.a));
                float4 mask   = float4(
                    q1.a == minStd,
                    q2.a == minStd,
                    q3.a == minStd,
                    q4.a == minStd);
                float  cnt    = dot(mask, 1.0);

                float3 col = cnt > 1.0
                    ? (q1.rgb * mask.x + q2.rgb * mask.y +
                       q3.rgb * mask.z + q4.rgb * mask.w) / cnt
                    : (q1.rgb * mask.x + q2.rgb * mask.y +
                       q3.rgb * mask.z + q4.rgb * mask.w);

                return saturate(float4(col, 1.0));
            }

            float4 EvalKernel(float2 uv, int radius) {
                float windowSize   = 2.0 * radius + 1.0;
                int   quadrantSize = (int)ceil(windowSize * 0.5);
                float rcpN         = 1.0 / (quadrantSize * quadrantSize);

                float4 q1 = SampleQuadrant(uv, -radius, 0,  -radius, 0,  rcpN);
                float4 q2 = SampleQuadrant(uv,  0, radius,  -radius, 0,  rcpN);
                float4 q3 = SampleQuadrant(uv,  0, radius,   0, radius,  rcpN);
                float4 q4 = SampleQuadrant(uv, -radius, 0,   0, radius,  rcpN);

                return ResolveQuadrants(q1, q2, q3, q4);
            }

            float4 frag(Varyings IN) : SV_Target {
            #if defined(ANIMATE_SIZE)
                // Build a per-pixel seed from integer pixel coords to avoid
                // the original bug (seed was immediately overwritten).
                int2   px   = (int2)(IN.uv * _MainTex_TexelSize.zw);
                uint   seed = (uint)(px.x + _MainTex_TexelSize.z * px.y);
                float  t01  = sin(_Time.y * _SizeAnimationSpeed +
                                  KuwHash(seed) * _NoiseFrequency) * 0.5 + 0.5;

                float kernelRange  = t01 * _KernelSize + _MinKernelSize;
                int   minRadius    = (int)floor(kernelRange);
                int   maxRadius    = (int)ceil(kernelRange);
                float blend        = frac(kernelRange);

                float4 r1 = EvalKernel(IN.uv, minRadius);
                float4 r2 = EvalKernel(IN.uv, maxRadius);
                return lerp(r1, r2, blend);
            #else
                return EvalKernel(IN.uv, _KernelSize);
            #endif
            }

            ENDHLSL
        }
    }
}
