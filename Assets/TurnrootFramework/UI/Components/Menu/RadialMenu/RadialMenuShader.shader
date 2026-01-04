Shader "UI/RadialSegment"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _InnerRadius ("Inner Radius", Range(0, 1)) = 0.3
        _StartAngle ("Start Angle", Range(0, 360)) = 0
        _EndAngle ("End Angle", Range(0, 360)) = 120
        _GapSize ("Gap Size", Range(0, 0.1)) = 0.01
        _VisualScale ("Visual Scale", Range(0.1, 30.0)) = 1.0
        
        [Toggle] _IsCenter ("Is Center Circle", Float) = 0
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        
        _ColorMask ("Color Mask", Float) = 15
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _InnerRadius;
            float _StartAngle;
            float _EndAngle;
            float _GapSize;
            float _IsCenter;
            float _VisualScale;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }
            
            float angleDiff(float a, float b)
            {
                float diff = fmod(b - a + 180.0, 360.0) - 180.0;
                return diff < -180.0 ? diff + 360.0 : diff;
            }
            
            fixed4 frag(v2f IN) : SV_Target
            {
                // Convert UV to centered coordinates (-0.5 to 0.5)
                float2 uv = IN.texcoord - 0.5;
                
                // Calculate distance from center
                float dist = (length(uv) * 2.0);
                
                // Calculate angle (0-360, with 0 at top)
                float angle = atan2(uv.x, uv.y) * 57.2958; // Convert to degrees
                angle = fmod(angle + 360.0, 360.0);
                
                fixed4 color = IN.color;
                
                if (_IsCenter > 0.5)
                {
                    // Center circle mode - stays inside inner radius
                    if (dist > _InnerRadius * 0.9) // Slightly smaller to create gap
                    {
                        discard;
                    }
                }
                else
                {
                    // Segment mode
                    // Add gap between center and segments
                    float minDist = _InnerRadius + _GapSize;
                    
                    // Check if outside outer radius or inside inner radius (with gap)
                    if (dist > 1.0 || dist < minDist)
                    {
                        discard;
                    }
                    
                    // Normalize angles
                    float startAngle = fmod(_StartAngle + 360.0, 360.0);
                    float endAngle = fmod(_EndAngle + 360.0, 360.0);
                    
                    // Check if angle is within segment range
                    float angleInSegment = 0.0;
                    
                    if (startAngle < endAngle)
                    {
                        angleInSegment = (angle >= startAngle && angle <= endAngle) ? 1.0 : 0.0;
                    }
                    else
                    {
                        // Wraps around 0
                        angleInSegment = (angle >= startAngle || angle <= endAngle) ? 1.0 : 0.0;
                    }
                    
                    if (angleInSegment < 0.5)
                    {
                        discard;
                    }
                    
                    // Apply radial gap with consistent pixel width
                    // Convert gap to angle based on distance from center
                    float gapAngleAtRadius = (_GapSize * 360.0) / (dist * 3.14159);
                    
                    float distFromStart = abs(angleDiff(angle, startAngle));
                    float distFromEnd = abs(angleDiff(angle, endAngle));
                    
                    if (distFromStart < gapAngleAtRadius || distFromEnd < gapAngleAtRadius)
                    {
                        discard;
                    }
                }
                
                // Apply clipping
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                // Premultiply alpha
                color.rgb *= color.a;
                
                return color;
            }
            ENDCG
        }
    }
}