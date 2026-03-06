Shader "UI/TwinkleMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _MaskOffset ("Mask Offset", Vector) = (0,0,0,0)
        _MaskScale ("Mask Scale", Vector) = (1,1,0,0)
        _MaskRotation ("Mask Rotation", Range(0, 360)) = 0
        
        _Intensity ("Effect Intensity", Range(0, 2)) = 1
        
        [Space(10)]
        [Header(Lighten Add Mode)]
        _LightenThreshold ("Lighten Threshold", Range(0, 1)) = 0.5
        _LightenIntensity ("Lighten Intensity", Range(0, 2)) = 1
        
        [Space(10)]
        [Header(Darken Subtract Mode)]
        _DarkenThreshold ("Darken Threshold", Range(0, 1)) = 0.5
        _DarkenIntensity ("Darken Intensity", Range(0, 2)) = 1
        
        [Space(10)]
        [Header(Blend Modes)]
        [KeywordEnum(Add, Lighten, Subtract, Darken, Both)] _BlendMode ("Blend Mode", Float) = 0
        
        [Space(10)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _BLENDMODE_ADD _BLENDMODE_LIGHTEN _BLENDMODE_SUBTRACT _BLENDMODE_DARKEN _BLENDMODE_BOTH

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float2 maskUV : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _MaskTex_ST;
            float2 _MaskOffset;
            float2 _MaskScale;
            float _MaskRotation;
            float _Intensity;
            float _LightenThreshold;
            float _LightenIntensity;
            float _DarkenThreshold;
            float _DarkenIntensity;

            // Rotation matrix helper
            float2 RotateUV(float2 uv, float rotation)
            {
                float angle = rotation * 0.0174533; // Convert to radians
                float2 center = float2(0.5, 0.5);
                uv -= center;
                float s = sin(angle);
                float c = cos(angle);
                float2x2 rotMatrix = float2x2(c, -s, s, c);
                uv = mul(rotMatrix, uv);
                uv += center;
                return uv;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                
                // Calculate mask UV with transformations
                float2 maskUV = v.texcoord;
                maskUV = (maskUV - 0.5) * _MaskScale + 0.5;
                maskUV = RotateUV(maskUV, _MaskRotation);
                maskUV += _MaskOffset;
                OUT.maskUV = TRANSFORM_TEX(maskUV, _MaskTex);
                
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Sample base texture
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                
                // Sample mask texture
                half4 mask = tex2D(_MaskTex, IN.maskUV);
                half maskValue = (mask.r + mask.g + mask.b) / 3.0; // Use average of RGB as mask value
                
                // Apply blend modes
                #if defined(_BLENDMODE_ADD) || defined(_BLENDMODE_BOTH)
                    // Add/Lighten mode
                    if (maskValue > _LightenThreshold)
                    {
                        float lightenAmount = (maskValue - _LightenThreshold) / (1.0 - _LightenThreshold);
                        lightenAmount *= _LightenIntensity * _Intensity;
                        
                        #ifdef _BLENDMODE_ADD
                            color.rgb += lightenAmount;
                        #else
                            color.rgb = max(color.rgb, color.rgb + lightenAmount);
                        #endif
                    }
                #endif
                
                #if defined(_BLENDMODE_SUBTRACT) || defined(_BLENDMODE_DARKEN) || defined(_BLENDMODE_BOTH)
                    // Subtract/Darken mode
                    if (maskValue < _DarkenThreshold)
                    {
                        float darkenAmount = (_DarkenThreshold - maskValue) / _DarkenThreshold;
                        darkenAmount *= _DarkenIntensity * _Intensity;
                        
                        #if defined(_BLENDMODE_SUBTRACT)
                            color.rgb -= darkenAmount;
                        #elif defined(_BLENDMODE_DARKEN)
                            color.rgb = min(color.rgb, color.rgb - darkenAmount);
                        #elif defined(_BLENDMODE_BOTH)
                            // In Both mode, use subtract for darken
                            color.rgb -= darkenAmount;
                        #endif
                    }
                #endif
                
                #ifdef _BLENDMODE_LIGHTEN
                    // Pure Lighten mode
                    if (maskValue > _LightenThreshold)
                    {
                        float lightenAmount = (maskValue - _LightenThreshold) / (1.0 - _LightenThreshold);
                        lightenAmount *= _LightenIntensity * _Intensity;
                        color.rgb = max(color.rgb, color.rgb + lightenAmount);
                    }
                #endif

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
