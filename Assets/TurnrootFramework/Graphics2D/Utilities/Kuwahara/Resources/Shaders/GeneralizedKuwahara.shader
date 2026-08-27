Shader "Hidden/GeneralizedKuwahara" {
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
                int    _N;          // always 8
                float  _Hardness;
                float  _Q;
                float  _ZeroCrossing;
                float  _Zeta;
            CBUFFER_END

            Varyings vert(Attributes IN) {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv         = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target {
                // Precompute kernel constants once per pixel
                int   kernelRadius = _KernelSize / 2;
                float rcpRadius    = 1.0 / max(kernelRadius, 1);

                float zeta        = _Zeta;
                float zeroCross   = _ZeroCrossing;
                float sinZC       = sin(zeroCross);
                float eta         = (zeta + cos(zeroCross)) / (sinZC * sinZC);

                // Rotated sub-sector: 45-degree rotation factor baked in
                static const float ROT45 = 0.70710678118; // sqrt(2)/2

                float4 m[8];
                float3 s[8];

                UNITY_UNROLL
                for (int k = 0; k < 8; ++k) {
                    m[k] = 0.0;
                    s[k] = 0.0;
                }

                UNITY_LOOP
                for (int y = -kernelRadius; y <= kernelRadius; ++y) {
                    UNITY_LOOP
                    for (int x = -kernelRadius; x <= kernelRadius; ++x) {
                        float2 v = float2(x, y) * rcpRadius;

                        float3 c = SAMPLE_TEXTURE2D_LOD(
                            _MainTex, sampler_MainTex,
                            IN.uv + float2(x, y) * _MainTex_TexelSize.xy, 0).rgb;
                        c = saturate(c);

                        // --- Axis-aligned sector weights (sectors 0,2,4,6) ---
                        float vxx = zeta - eta * v.x * v.x;
                        float vyy = zeta - eta * v.y * v.y;

                        float z0 = max(0,  v.y + vxx); float w0 = z0 * z0;
                        float z2 = max(0, -v.x + vyy); float w2 = z2 * z2;
                        float z4 = max(0, -v.y + vxx); float w4 = z4 * z4;
                        float z6 = max(0,  v.x + vyy); float w6 = z6 * z6;

                        // --- Rotated sector weights (sectors 1,3,5,7) ---
                        float2 vr  = ROT45 * float2(v.x - v.y, v.x + v.y);
                        float vxxr = zeta - eta * vr.x * vr.x;
                        float vyyr = zeta - eta * vr.y * vr.y;

                        float z1 = max(0,  vr.y + vxxr); float w1 = z1 * z1;
                        float z3 = max(0, -vr.x + vyyr); float w3 = z3 * z3;
                        float z5 = max(0, -vr.y + vxxr); float w5 = z5 * z5;
                        float z7 = max(0,  vr.x + vyyr); float w7 = z7 * z7;

                        float sum = w0 + w1 + w2 + w3 + w4 + w5 + w6 + w7;

                        // Gaussian uses rotated v (last computed v in original);
                        // match original: exp(-3.125 * dot(vr,vr))
                        float g = exp(-3.125 * dot(vr, vr)) / max(sum, 1e-6);

                        float gw0 = w0 * g; m[0] += float4(c * gw0, gw0); s[0] += c * c * gw0;
                        float gw1 = w1 * g; m[1] += float4(c * gw1, gw1); s[1] += c * c * gw1;
                        float gw2 = w2 * g; m[2] += float4(c * gw2, gw2); s[2] += c * c * gw2;
                        float gw3 = w3 * g; m[3] += float4(c * gw3, gw3); s[3] += c * c * gw3;
                        float gw4 = w4 * g; m[4] += float4(c * gw4, gw4); s[4] += c * c * gw4;
                        float gw5 = w5 * g; m[5] += float4(c * gw5, gw5); s[5] += c * c * gw5;
                        float gw6 = w6 * g; m[6] += float4(c * gw6, gw6); s[6] += c * c * gw6;
                        float gw7 = w7 * g; m[7] += float4(c * gw7, gw7); s[7] += c * c * gw7;
                    }
                }

                float4 output = 0.0;
                float hardness1000 = _Hardness * 1000.0;
                float halfQ        = 0.5 * _Q;

                UNITY_UNROLL
                for (int k = 0; k < 8; ++k) {
                    float rcpW    = 1.0 / max(m[k].w, 1e-6);
                    float3 mean   = m[k].rgb * rcpW;
                    float3 var3   = abs(s[k] * rcpW - mean * mean);
                    float  sigma2 = var3.r + var3.g + var3.b;

                    // pow(x, halfQ) where halfQ == 0.5*_Q: use sqrt only when Q≈1
                    // General case: exp(halfQ * log(x)) is fine; the compiler may
                    // already do this, but writing pow explicitly is cleaner.
                    float wk = 1.0 / (1.0 + pow(hardness1000 * sigma2, halfQ));

                    output += float4(mean * wk, wk);
                }

                return saturate(output / output.w);
            }

            ENDHLSL
        }
    }
}
