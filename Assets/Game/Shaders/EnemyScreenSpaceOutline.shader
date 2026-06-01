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
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_WOM_HoverOutlineMask);

            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _OutlineWidthPixels;
                float4 _OutlineTexelSize;
                float4 _OutlineMaskUvScale;
            CBUFFER_END

            half SampleMask(float2 uv)
            {
                float2 halfTexel = _OutlineTexelSize.xy * 0.5;
                float2 maxUv = max(halfTexel, _OutlineMaskUvScale.xy - halfTexel);
                float2 safeUv = clamp(uv, halfTexel, maxUv);
                return SAMPLE_TEXTURE2D_X_LOD(_WOM_HoverOutlineMask, sampler_PointClamp, safeUv, 0).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord.xy;

                half centerMask = SampleMask(uv);
                half expandedMask = centerMask;
                float radius = clamp(_OutlineWidthPixels, 1.0, 8.0);
                float2 axisOffset = _OutlineTexelSize.xy * radius;
                float2 diagonalOffset = axisOffset * 0.70710678;

                // Fixed taps keep the fullscreen composite cheap while preserving a round outline.
                expandedMask = max(expandedMask, SampleMask(uv + float2(axisOffset.x, 0.0)));
                expandedMask = max(expandedMask, SampleMask(uv - float2(axisOffset.x, 0.0)));
                expandedMask = max(expandedMask, SampleMask(uv + float2(0.0, axisOffset.y)));
                expandedMask = max(expandedMask, SampleMask(uv - float2(0.0, axisOffset.y)));
                expandedMask = max(expandedMask, SampleMask(uv + diagonalOffset));
                expandedMask = max(expandedMask, SampleMask(uv - diagonalOffset));
                expandedMask = max(expandedMask, SampleMask(uv + float2(diagonalOffset.x, -diagonalOffset.y)));
                expandedMask = max(expandedMask, SampleMask(uv + float2(-diagonalOffset.x, diagonalOffset.y)));

                half outlineMask = saturate(expandedMask - centerMask);
                half alpha = outlineMask * _OutlineColor.a;
                return half4(_OutlineColor.rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
