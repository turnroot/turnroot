// Turnroot/UI/MapQuadrantBlend  (Universal Render Pipeline)
// UI shader that composites four quadrant textures (TL, TR, BL, BR) of a map
// and blends the edges with animated smoky FBM noise so the boundaries between
// explored/unexplored regions look organic rather than razor-sharp.
//
// Usage:
//   - Create a Material using this shader.
//   - Assign it to MapQuadrantBlendImage.BaseMaterial in the inspector.
//   - Each quadrant texture should cover the FULL UV space of the RawImage.
//
// Boundary animation:
//   Large-scale FBM warps the overall boundary shape.
//   Small-scale FBM adds fine smoky tendrils along the same edges.
//   Both layers animate independently via _NoiseSpeed.

Shader "Turnroot/UI/MapQuadrantBlend"
{
    Properties
    {
        // ── Quadrant textures ──────────────────────────────────────────
        _TopLeft     ("Top Left",     2D) = "white" {}
        _TopRight    ("Top Right",    2D) = "white" {}
        _BottomLeft  ("Bottom Left",  2D) = "white" {}
        _BottomRight ("Bottom Right", 2D) = "white" {}

        // ── Noise / edge parameters ────────────────────────────────────
        [Header(Large Scale Noise)]
        _LargeNoiseScale    ("Scale",     Float)         = 4.0
        _LargeNoiseStrength ("Amplitude", Range(0, 0.2)) = 0.055

        [Header(Small Scale Noise)]
        _SmallNoiseScale    ("Scale",     Float)          = 22.0
        _SmallNoiseStrength ("Amplitude", Range(0, 0.06)) = 0.018

        [Header(Animation)]
        _NoiseSpeed ("Noise Speed", Float) = 0.07

        [Header(Blending)]
        _EdgeSmoothness ("Edge Blend Width", Range(0.001, 0.1)) = 0.022

        // ── Required Unity UI properties ──────────────────────────────
        _StencilComp      ("Stencil Comparison", Float) = 8
        _Stencil          ("Stencil ID",         Float) = 0
        _StencilOp        ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask        ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "MapQuadrantBlend"
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.0

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Textures (outside CBUFFER — required by URP/SRP) ─────────

            TEXTURE2D(_TopLeft);     SAMPLER(sampler_TopLeft);
            TEXTURE2D(_TopRight);    SAMPLER(sampler_TopRight);
            TEXTURE2D(_BottomLeft);  SAMPLER(sampler_BottomLeft);
            TEXTURE2D(_BottomRight); SAMPLER(sampler_BottomRight);

            // ── Per-material constants (CBUFFER for SRP Batcher compat) ──

            CBUFFER_START(UnityPerMaterial)
                float  _LargeNoiseScale;
                float  _LargeNoiseStrength;
                float  _SmallNoiseScale;
                float  _SmallNoiseStrength;
                float  _NoiseSpeed;
                float  _EdgeSmoothness;
                float4 _ClipRect;
            CBUFFER_END

            // ── Structs ──────────────────────────────────────────────────

            struct Attributes
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 posCS         : SV_POSITION;
                half4  color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Vertex shader ─────────────────────────────────────────────

            Varyings vert(Attributes v)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.posCS         = TransformObjectToHClip(v.vertex.xyz);
                OUT.texcoord      = v.texcoord;
                OUT.color         = v.color;
                return OUT;
            }

            // ── Noise helpers ─────────────────────────────────────────────
            //
            // Value noise: smoothly interpolated random values on a unit grid.
            // Two seed offsets keep the horizontal and vertical boundary
            // animations visually independent from each other.

            float _hash(float2 p, float seed)
            {
                p += seed;
                p  = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float _vnoise(float2 p, float seed)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep fade

                float a = _hash(i,                     seed);
                float b = _hash(i + float2(1.0, 0.0),  seed);
                float c = _hash(i + float2(0.0, 1.0),  seed);
                float d = _hash(i + float2(1.0, 1.0),  seed);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 4-octave fBm for rich organic detail.
            float _fbm(float2 p, float seed)
            {
                float  value     = 0.0;
                float  amplitude = 0.5;
                float2 shift     = float2(100.0, 100.0);

                UNITY_UNROLL
                for (int i = 0; i < 4; i++)
                {
                    value     += amplitude * _vnoise(p, seed);
                    p          = p * 2.1 + shift;
                    amplitude *= 0.5;
                }
                return value;
            }

            // Signed displacement [-1, 1] from two noise layers.
            float _noisedisplace(float2 p, float seed)
            {
                float large = (_fbm(p * _LargeNoiseScale, seed         ) - 0.5) * 2.0;
                float small = (_fbm(p * _SmallNoiseScale, seed + 73.51 ) - 0.5) * 2.0;
                return large * _LargeNoiseStrength + small * _SmallNoiseStrength;
            }

            // ── Clip rect (reimplements UnityGet2DClipping without UnityCG) ─

            float InClipRect(float2 pos, float4 rect)
            {
                float2 inside = step(rect.xy, pos) * step(pos, rect.zw);
                return inside.x * inside.y;
            }

            // ── Fragment shader ───────────────────────────────────────────

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float  t  = _Time.y * _NoiseSpeed;

                // ── Horizontal boundary (left / right half split at u=0.5) ──
                float hDisplace   = _noisedisplace(float2(uv.y, t), 0.0);
                float hBoundary   = 0.5 + hDisplace;
                float rightWeight = smoothstep(
                    hBoundary - _EdgeSmoothness,
                    hBoundary + _EdgeSmoothness,
                    uv.x
                );

                // ── Vertical boundary (bottom / top half split at v=0.5) ───
                float vDisplace = _noisedisplace(float2(uv.x, t + 47.3), 13.7);
                float vBoundary = 0.5 + vDisplace;
                float topWeight = smoothstep(
                    vBoundary - _EdgeSmoothness,
                    vBoundary + _EdgeSmoothness,
                    uv.y
                );

                // ── Sample all four quadrant textures ───────────────────
                half4 colBL = SAMPLE_TEXTURE2D(_BottomLeft,  sampler_BottomLeft,  uv);
                half4 colBR = SAMPLE_TEXTURE2D(_BottomRight, sampler_BottomRight, uv);
                half4 colTL = SAMPLE_TEXTURE2D(_TopLeft,     sampler_TopLeft,     uv);
                half4 colTR = SAMPLE_TEXTURE2D(_TopRight,    sampler_TopRight,    uv);

                // ── Bilinear blend: horizontal then vertical ─────────────
                half4 bottom = lerp(colBL, colBR, rightWeight);
                half4 top    = lerp(colTL, colTR, rightWeight);
                half4 color  = lerp(bottom, top, topWeight);

                color *= IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= InClipRect(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDHLSL
        }
    }

    FallBack "UI/Default"
}
