Shader "Mariusz/Eagle Vision Scan Ring"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (0.2, 1.5, 0.75, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.02, 0.65, 0.35, 1)
        _CoreWidth ("Core Width", Range(0.01, 0.9)) = 0.16
        _GlowSoftness ("Glow Softness", Range(0.1, 8)) = 2.2
        _Opacity ("Opacity", Range(0, 1)) = 0.8
        _EmissionIntensity ("Emission Intensity", Range(0, 12)) = 2.5
        _NoiseScale ("Noise Scale", Range(1, 32)) = 10
        _NoiseSpeed ("Noise Speed", Range(-10, 10)) = 1.8
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.28
        _FlowSpeed ("Flow Speed", Range(-10, 10)) = 2.2
        _IntersectionDepth ("Intersection Depth", Range(0.001, 1)) = 0.12
        _IntersectionGlow ("Intersection Glow", Range(0, 5)) = 1.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
        }

        Pass
        {
            Name "EagleVisionScanRing"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                half4 color : COLOR;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _GlowColor;
                float _CoreWidth;
                float _GlowSoftness;
                float _Opacity;
                float _EmissionIntensity;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseStrength;
                float _FlowSpeed;
                float _IntersectionDepth;
                float _IntersectionGlow;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.screenPosition = ComputeScreenPos(positionInputs.positionCS);
                output.uv = input.uv;
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float widthDistance = abs(input.uv.y * 2.0 - 1.0);
                float core = 1.0 - smoothstep(_CoreWidth, min(1.0, _CoreWidth + 0.09), widthDistance);
                float glow = pow(saturate(1.0 - widthDistance), max(0.1, _GlowSoftness));

                float cycles = max(1.0, round(_NoiseScale));
                float phase = input.uv.x * TWO_PI * cycles + _Time.y * _FlowSpeed;
                float travellingNoise = sin(phase + sin(phase * 2.0 + _Time.y * _NoiseSpeed) * 1.35);
                travellingNoise = travellingNoise * 0.5 + 0.5;
                float noiseFactor = lerp(1.0 - _NoiseStrength, 1.0, travellingNoise);

                float2 screenUV = input.screenPosition.xy / max(input.screenPosition.w, 0.0001);
                float rawSceneDepth = SampleSceneDepth(screenUV);
                float sceneEyeDepth = LinearEyeDepth(rawSceneDepth, _ZBufferParams);
                float fragmentEyeDepth = max(input.screenPosition.w, 0.0);
                float depthDifference = abs(sceneEyeDepth - fragmentEyeDepth);
                float contact = 1.0 - smoothstep(0.0, max(0.001, _IntersectionDepth), depthDifference);

                float baseMask = saturate(core + glow * 0.55) * noiseFactor;
                float alpha = saturate(baseMask * _Opacity + contact * glow * 0.2 * _IntersectionGlow);
                half3 energyColor = lerp(_GlowColor.rgb, _CoreColor.rgb, saturate(core));
                half3 color = energyColor * input.color.rgb;
                color *= _EmissionIntensity * (1.0 + contact * _IntersectionGlow);
                color = MixFog(color, input.fogFactor);

                return half4(color, alpha * input.color.a);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
