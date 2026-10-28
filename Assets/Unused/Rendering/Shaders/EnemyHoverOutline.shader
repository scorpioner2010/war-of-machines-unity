Shader "Game/EnemyHoverOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.2)) = 0.035
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 normalOS = normalize(input.normalOS);
                float3 positionOS = input.positionOS.xyz + normalOS * _OutlineWidth;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.positionCS = positionInputs.positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
