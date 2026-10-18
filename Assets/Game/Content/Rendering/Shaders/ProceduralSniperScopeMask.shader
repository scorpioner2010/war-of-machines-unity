Shader "Game/UI/Procedural Sniper Scope Mask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ApertureRadius ("Aperture Radius (Screen Height)", Range(0.01, 0.5)) = 0.373
        _ApertureCenter ("Aperture Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _EdgeSoftnessPixels ("Extra Edge Softness (Pixels)", Range(0, 4)) = 0

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "ProceduralScopeMask"

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct Attributes
            {
                float4 positionOS : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 positionOS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ApertureCenter;
            float _ApertureRadius;
            float _EdgeSoftnessPixels;
            float4 _ClipRect;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionOS = input.positionOS;
                output.positionCS = UnityObjectToClipPos(input.positionOS);
                output.texcoord = input.texcoord;
                output.color = input.color;
                return output;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float2 aperturePoint = input.texcoord - _ApertureCenter.xy;
                aperturePoint.x *= _ScreenParams.x / _ScreenParams.y;

                float distanceToApertureCenter = length(aperturePoint);
                float pixelAntialiasing = fwidth(distanceToApertureCenter) * 0.5;
                float requestedSoftness = _EdgeSoftnessPixels / _ScreenParams.y;
                float edgeWidth = max(pixelAntialiasing, requestedSoftness);
                float outsideAperture = smoothstep(
                    _ApertureRadius - edgeWidth,
                    _ApertureRadius + edgeWidth,
                    distanceToApertureCenter);

                fixed4 color = input.color;
                color.a *= outsideAperture;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.positionOS.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
