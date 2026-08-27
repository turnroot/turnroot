Shader "Hidden/AnisotropicKuwahara" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType" = "Opaque" }
        Cull Off ZWrite Off ZTest Always

        HLSLINCLUDE

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

        TEXTURE2D(_TFM);
        SAMPLER(sampler_TFM);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            int    _KernelSize;
            int    _N;
            float  _Hardness;
            float  _Q;
            float  _Alpha;
            float  _ZeroCrossing;
            float  _Zeta;
        CBUFFER_END

        Varyings vert(Attributes IN) {
            Varyings OUT;
            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
            OUT.uv         = IN.uv;
            return OUT;
        }

        // Precomputed Gaussian weights for radius-5 blur (sigma=2).
        // gaussian(2, x) for x in [-5..5], normalised so sum == 1.
        // Computed offline: unnorm = exp(-x^2/8), sum = sum of all 11 values.
        static const int   BLUR_RADIUS    = 5;
        static const float GAUSS_SIGMA    = 2.0;
        // Raw (unnormalised) weights; we'll normalise at the bottom.
        // exp(-x*x / (2*sigma*sigma)) with sigma=2 → exp(-x*x/8)
        static const float GAUSS_W[11] = {
            0.00329603, // x=-5
            0.01330373, // x=-4
            0.04393693, // x=-3
            0.11893044, // x=-2
            0.26359714, // x=-1
            0.47856736, // x= 0
            0.26359714, // x= 1
            0.11893044, // x= 2
            0.04393693, // x= 3
            0.01330373, // x= 4
            0.00329603  // x= 5
        };
        // Sum of the above = 1.35469590 → rcp baked in for the normalised fetch below
        static const float GAUSS_RCP_SUM = 0.73817706;

        ENDHLSL

        // -----------------------------------------------------------------------
        // Pass 0 – Structure Tensor
        //   Computes per-pixel Sobel gradients and outputs (Sxx, Syy, Sxy, 1).
        //   Corners are shared between Sx and Sy, so we cache all 9 samples.
        // -----------------------------------------------------------------------
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_structuretensor

            float4 frag_structuretensor(Varyings IN) : SV_Target {
                float2 d = _MainTex_TexelSize.xy;

                // Fetch the 3x3 neighbourhood once; corners are reused by both kernels.
                float3 s00 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv + float2(-d.x, -d.y), 0).rgb;
                float3 s10 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv + float2( 0.0, -d.y), 0).rgb;
                float3 s20 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv + float2( d.x, -d.y), 0).rgb;
                float3 s01 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv + float2(-d.x,  0.0), 0).rgb;
                // centre not needed by Sobel
                float3 s21 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv + float2( d.x,  0.0), 0).rgb;
                float3 s02 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv + float2(-d.x,  d.y), 0).rgb;
                float3 s12 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv + float2( 0.0,  d.y), 0).rgb;
                float3 s22 = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, IN.uv + float2( d.x,  d.y), 0).rgb;

                // Sobel (same as original, /4 baked into coefficients)
                float3 Sx = (s20 - s00 + 2.0 * (s21 - s01) + s22 - s02) * 0.25;
                float3 Sy = (s00 - s02 + 2.0 * (s10 - s12) + s20 - s22) * 0.25;

                return float4(dot(Sx, Sx), dot(Sy, Sy), dot(Sx, Sy), 1.0);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------
        // Pass 1 – Horizontal Gaussian blur of the structure tensor
        // -----------------------------------------------------------------------
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blurh

            float4 frag_blurh(Varyings IN) : SV_Target {
                float4 col = 0.0;

                UNITY_UNROLL
                for (int x = -BLUR_RADIUS; x <= BLUR_RADIUS; ++x) {
                    float4 c = SAMPLE_TEXTURE2D_LOD(
                        _MainTex, sampler_MainTex,
                        IN.uv + float2(x, 0) * _MainTex_TexelSize.xy, 0);
                    col += c * GAUSS_W[x + BLUR_RADIUS];
                }

                // Multiply by reciprocal of the weight sum (precomputed constant)
                return col * GAUSS_RCP_SUM;
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------
        // Pass 2 – Vertical Gaussian blur + eigenvector extraction
        //   Outputs (t.x, t.y, phi, A) where t is the tangent direction,
        //   phi is the flow angle, and A is the anisotropy magnitude.
        // -----------------------------------------------------------------------
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_blurv

            float4 frag_blurv(Varyings IN) : SV_Target {
                float4 col = 0.0;

                UNITY_UNROLL
                for (int y = -BLUR_RADIUS; y <= BLUR_RADIUS; ++y) {
                    float4 c = SAMPLE_TEXTURE2D_LOD(
                        _MainTex, sampler_MainTex,
                        IN.uv + float2(0, y) * _MainTex_TexelSize.xy, 0);
                    col += c * GAUSS_W[y + BLUR_RADIUS];
                }

                float3 g = (col * GAUSS_RCP_SUM).rgb;

                // Eigenvalues of the 2x2 structure tensor [g.x g.z; g.z g.y].
                // discriminant = sqrt((g.x-g.y)^2 + 4*g.z^2) — computed once.
                float diff   = g.x - g.y;
                float disc   = sqrt(diff * diff + 4.0 * g.z * g.z);
                float lambda1 = 0.5 * (g.x + g.y + disc);
                float lambda2 = 0.5 * (g.x + g.y - disc);

                float2 v   = float2(lambda1 - g.x, -g.z);
                float2 t   = length(v) > 1e-6 ? normalize(v) : float2(0.0, 1.0);
                float  phi = -atan2(t.y, t.x);

                float sumL = lambda1 + lambda2;
                float A    = sumL > 1e-6 ? (lambda1 - lambda2) / sumL : 0.0;

                return float4(t, phi, A);
            }
            ENDHLSL
        }

        // -----------------------------------------------------------------------
        // Pass 3 – Anisotropic Kuwahara filter
        //   Uses the TFM (tensor field map) from Pass 2 to orient the kernel.
        // -----------------------------------------------------------------------
        Pass {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_kuwahara

            static const float ROT45 = 0.70710678118; // sqrt(2)/2

            float4 frag_kuwahara(Varyings IN) : SV_Target {
                float alpha = _Alpha;
                float4 tfm  = SAMPLE_TEXTURE2D_LOD(_TFM, sampler_TFM, IN.uv, 0);

                int   kernelRadius = _KernelSize / 2;
                float fr           = (float)kernelRadius;

                float aniso    = tfm.w;
                float a        = fr * clamp((alpha + aniso) / alpha, 0.1, 2.0);
                float b        = fr * clamp(alpha / (alpha + aniso), 0.1, 2.0);

                float cos_phi  = cos(tfm.z);
                float sin_phi  = sin(tfm.z);

                // SR = S * R, where R rotates and S scales into the unit ellipse.
                // Precompute the 2x2 matrix SR directly — avoids two separate
                // matrix multiplications in the inner loop.
                float rcpA = 0.5 / a;
                float rcpB = 0.5 / b;
                // SR = [[rcpA*cos_phi, -rcpA*sin_phi],
                //        [rcpB*sin_phi,  rcpB*cos_phi]]
                float sr00 =  rcpA * cos_phi;
                float sr01 = -rcpA * sin_phi;
                float sr10 =  rcpB * sin_phi;
                float sr11 =  rcpB * cos_phi;

                // Tight bounding box for the rotated ellipse
                int max_x = (int)sqrt(a * a * cos_phi * cos_phi + b * b * sin_phi * sin_phi);
                int max_y = (int)sqrt(a * a * sin_phi * sin_phi + b * b * cos_phi * cos_phi);

                float zeta      = _Zeta;
                float zeroCross = _ZeroCrossing;
                float sinZC     = sin(zeroCross);
                float eta       = (zeta + cos(zeroCross)) / (sinZC * sinZC);

                float4 m[8];
                float3 s[8];

                UNITY_UNROLL
                for (int k = 0; k < 8; ++k) {
                    m[k] = 0.0;
                    s[k] = 0.0;
                }

                UNITY_LOOP
                for (int y = -max_y; y <= max_y; ++y) {
                    UNITY_LOOP
                    for (int x = -max_x; x <= max_x; ++x) {
                        // Map (x,y) into ellipse-normalised space via SR
                        float2 v = float2(sr00 * x + sr01 * y,
                                          sr10 * x + sr11 * y);

                        // Discard samples outside the unit ellipse
                        if (dot(v, v) > 0.25)
                            continue;

                        float3 c = SAMPLE_TEXTURE2D_LOD(
                            _MainTex, sampler_MainTex,
                            IN.uv + float2(x, y) * _MainTex_TexelSize.xy, 0).rgb;
                        c = saturate(c);

                        // Axis-aligned sector weights
                        float vxx = zeta - eta * v.x * v.x;
                        float vyy = zeta - eta * v.y * v.y;

                        float z0 = max(0,  v.y + vxx); float w0 = z0 * z0;
                        float z2 = max(0, -v.x + vyy); float w2 = z2 * z2;
                        float z4 = max(0, -v.y + vxx); float w4 = z4 * z4;
                        float z6 = max(0,  v.x + vyy); float w6 = z6 * z6;

                        // Rotated sector weights
                        float2 vr   = ROT45 * float2(v.x - v.y, v.x + v.y);
                        float vxxr  = zeta - eta * vr.x * vr.x;
                        float vyyr  = zeta - eta * vr.y * vr.y;

                        float z1 = max(0,  vr.y + vxxr); float w1 = z1 * z1;
                        float z3 = max(0, -vr.x + vyyr); float w3 = z3 * z3;
                        float z5 = max(0, -vr.y + vxxr); float w5 = z5 * z5;
                        float z7 = max(0,  vr.x + vyyr); float w7 = z7 * z7;

                        float wsum = w0+w1+w2+w3+w4+w5+w6+w7;
                        float g    = exp(-3.125 * dot(vr, vr)) / max(wsum, 1e-6);

                        float gw0 = w0*g; m[0]+=float4(c*gw0,gw0); s[0]+=c*c*gw0;
                        float gw1 = w1*g; m[1]+=float4(c*gw1,gw1); s[1]+=c*c*gw1;
                        float gw2 = w2*g; m[2]+=float4(c*gw2,gw2); s[2]+=c*c*gw2;
                        float gw3 = w3*g; m[3]+=float4(c*gw3,gw3); s[3]+=c*c*gw3;
                        float gw4 = w4*g; m[4]+=float4(c*gw4,gw4); s[4]+=c*c*gw4;
                        float gw5 = w5*g; m[5]+=float4(c*gw5,gw5); s[5]+=c*c*gw5;
                        float gw6 = w6*g; m[6]+=float4(c*gw6,gw6); s[6]+=c*c*gw6;
                        float gw7 = w7*g; m[7]+=float4(c*gw7,gw7); s[7]+=c*c*gw7;
                    }
                }

                float4 output       = 0.0;
                float  hardness1000 = _Hardness * 1000.0;
                float  halfQ        = 0.5 * _Q;

                UNITY_UNROLL
                for (int k = 0; k < 8; ++k) {
                    float  rcpW  = 1.0 / max(m[k].w, 1e-6);
                    float3 mean  = m[k].rgb * rcpW;
                    float3 var3  = abs(s[k] * rcpW - mean * mean);
                    float  sigma2 = var3.r + var3.g + var3.b;

                    float wk = 1.0 / (1.0 + pow(hardness1000 * sigma2, halfQ));
                    output  += float4(mean * wk, wk);
                }

                return saturate(output / output.w);
            }
            ENDHLSL
        }
    }
}
