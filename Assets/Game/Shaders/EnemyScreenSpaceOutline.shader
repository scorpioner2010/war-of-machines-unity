Shader "Hidden/Game/ScreenSpaceHoverOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineWidthPixels ("Outline Width Pixels", Range(1, 8)) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Composite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define MAX_OUTLINE_RADIUS 8

            TEXTURE2D_X(_WOM_HoverOutlineMask);

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidthPixels;
                float4 _OutlineTexelSize;
            CBUFFER_END

            half SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X_LOD(_WOM_HoverOutlineMask, sampler_PointClamp, uv, 0).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;
                half4 sceneColor = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);

                half centerMask = SampleMask(uv);
                half expandedMask = centerMask;
                int radius = (int)ceil(clamp(_OutlineWidthPixels, 1.0, (float)MAX_OUTLINE_RADIUS));
                float radiusSquared = (float)(radius * radius);
                float2 texel = _OutlineTexelSize.xy;

                [loop]
                for (int y = -radius; y <= radius; y++)
                {
                    [loop]
                    for (int x = -radius; x <= radius; x++)
                    {
                        float distanceSquared = (float)(x * x + y * y);
                        if (distanceSquared > radiusSquared)
                        {
                            continue;
                        }

                        float2 offset = float2((float)x, (float)y) * texel;
                        expandedMask = max(expandedMask, SampleMask(uv + offset));
                    }
                }

                half outlineMask = saturate(expandedMask - centerMask);
                half alpha = outlineMask * _OutlineColor.a;
                sceneColor.rgb = lerp(sceneColor.rgb, _OutlineColor.rgb, alpha);
                return sceneColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
