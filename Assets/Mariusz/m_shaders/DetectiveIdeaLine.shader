Shader "Mariusz/Detective Idea Line"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.35, 0.85, 0.7, 1)
        _CoreWidth ("Core Width", Range(0.01, 1)) = 0.28
        _GlowSoftness ("Glow Softness", Range(0.1, 5)) = 1.8
        _Opacity ("Opacity", Range(0, 1)) = 0.5
        _EmissionIntensity ("Emission Intensity", Range(0, 8)) = 1.2
        [Toggle] _VerticalFlow ("Vertical Flow", Float) = 1
        _FlowSpeed ("Flow Speed", Range(-5, 5)) = 0.25
        _FlowStrength ("Flow Strength", Range(0, 1)) = 0.08
        [HDR] _PulseColor ("Pulse Color", Color) = (0.7, 1.6, 1.1, 1)
        _PulseSpeed ("Pulse Speed", Range(0.05, 3)) = 0.65
        _PulseWidth ("Pulse Width", Range(0.02, 0.5)) = 0.18
        _PulseIntensity ("Pulse Intensity", Range(0, 8)) = 3
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
            Name "DetectiveIdeaLine"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _GlowColor;
                float _CoreWidth;
                float _GlowSoftness;
                float _Opacity;
                float _EmissionIntensity;
                float _VerticalFlow;
                float _FlowSpeed;
                float _FlowStrength;
                half4 _PulseColor;
                float _PulseSpeed;
                float _PulseWidth;
                float _PulseIntensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float across = abs(input.uv.y - 0.5) * 2.0;
                float core = 1.0 - smoothstep(_CoreWidth * 0.5, _CoreWidth, across);
                float glow = pow(saturate(1.0 - across), _GlowSoftness);

                float flowPosition = lerp(input.uv.x, input.uv.y, saturate(_VerticalFlow));
                // Integer spatial periods keep the animated pattern continuous at UV seams.
                float phase = flowPosition * 6.2831853;
                float time = _Time.y * _FlowSpeed;
                float flow = 0.5 + 0.5 * sin(phase * 7.0 - time);
                flow *= 0.75 + 0.25 * sin(phase * 13.0 + time * 0.7);
                float flowGain = 1.0 + _FlowStrength * flow;

                float pulseCenter = frac(_Time.y * _PulseSpeed);
                float pulse = saturate(1.0 - abs(input.uv.x - pulseCenter) / _PulseWidth);

                half3 tint = input.color.rgb;
                half3 lightColor = (_CoreColor.rgb * core + _GlowColor.rgb * glow * 0.5) * tint;
                lightColor = lightColor * _EmissionIntensity * flowGain
                    + _PulseColor.rgb * tint * pulse * _PulseIntensity * max(core, glow * 0.7);
                half alpha = saturate(max(core, glow * 0.6) * _Opacity * input.color.a
                    + pulse * max(core, glow * 0.7) * input.color.a);
                return half4(lightColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
