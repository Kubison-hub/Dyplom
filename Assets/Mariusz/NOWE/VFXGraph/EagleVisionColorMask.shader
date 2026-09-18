Shader "Hidden/Mariusz/Eagle Vision Color Mask"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "EagleVisionColorMask"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _EagleVisionScanCenter;
            float _EagleVisionScanRadius;
            float _EagleVisionColorMaskEnabled;
            float _EagleVisionColorMaskStrength;
            float _EagleVisionInsideSaturation;
            float _EagleVisionInsideBrightness;
            float _EagleVisionInsideContrast;
            float4 _EagleVisionInsideTint;
            float _EagleVisionOutsideSaturation;
            float _EagleVisionOutsideBrightness;
            float _EagleVisionOutsideContrast;
            float4 _EagleVisionOutsideTint;
            float _EagleVisionBoundarySoftness;
            float4 _EagleVisionBoundaryTint;

            static const float EagleVisionMaximumHeight = 2.95;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float rawDepth = SampleSceneDepth(uv);
                float3 worldPosition = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float horizontalDistance = distance(worldPosition.xz, _EagleVisionScanCenter.xz);
                float insideHeight = step(
                    worldPosition.y - _EagleVisionScanCenter.y,
                    EagleVisionMaximumHeight);
                float softness = max(0.001, _EagleVisionBoundarySoftness);
                float insideCylinder = 1.0 - smoothstep(
                    max(0.0, _EagleVisionScanRadius - softness),
                    _EagleVisionScanRadius + softness,
                    horizontalDistance);

#if UNITY_REVERSED_Z
                float hasGeometry = step(0.000001, rawDepth);
#else
                float hasGeometry = step(rawDepth, 0.999999);
#endif
                insideCylinder *= insideHeight * hasGeometry * saturate(_EagleVisionColorMaskEnabled);

                half luminance = dot(source.rgb, half3(0.2126, 0.7152, 0.0722));
                half insideSaturationFactor = 1.0h + (half)(_EagleVisionInsideSaturation * 0.01);
                half outsideSaturationFactor = 1.0h + (half)(_EagleVisionOutsideSaturation * 0.01);
                half3 insideColor = lerp(luminance.xxx, source.rgb, insideSaturationFactor);
                half3 outsideColor = lerp(luminance.xxx, source.rgb, outsideSaturationFactor);

                half insideContrastFactor = 1.0h + (half)(_EagleVisionInsideContrast * 0.01);
                half outsideContrastFactor = 1.0h + (half)(_EagleVisionOutsideContrast * 0.01);
                insideColor = (insideColor - 0.5h) * insideContrastFactor + 0.5h;
                outsideColor = (outsideColor - 0.5h) * outsideContrastFactor + 0.5h;
                insideColor = (insideColor + (half)_EagleVisionInsideBrightness) * _EagleVisionInsideTint.rgb;
                outsideColor = (outsideColor + (half)_EagleVisionOutsideBrightness) * _EagleVisionOutsideTint.rgb;

                half3 maskedColor = lerp(outsideColor, insideColor, insideCylinder);
                float effectStrength = saturate(_EagleVisionColorMaskStrength);
                half3 result = lerp(source.rgb, maskedColor, effectStrength);

                float boundaryDistance = abs(horizontalDistance - _EagleVisionScanRadius);
                float boundary = 1.0 - smoothstep(0.0, softness * 1.5, boundaryDistance);
                boundary *= insideHeight * hasGeometry * saturate(_EagleVisionColorMaskEnabled);
                result += _EagleVisionBoundaryTint.rgb * _EagleVisionBoundaryTint.a * boundary * effectStrength;

                return half4(result, source.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
