Shader "Hidden/Game/HoverOutlineMask"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "Mask"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite Off
            ZTest Always
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #define MASK_DEPTH_BIAS 0.0001

            struct Attributes
            {
                float4 positionOS : POSITION;
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

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = SampleSceneDepth(screenUV);
                float objectDepth = input.positionCS.z;

#if UNITY_REVERSED_Z
                if (objectDepth < sceneDepth - MASK_DEPTH_BIAS)
                {
                    discard;
                }
#else
                if (objectDepth > sceneDepth + MASK_DEPTH_BIAS)
                {
                    discard;
                }
#endif

                return half4(1, 0, 0, 1);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
