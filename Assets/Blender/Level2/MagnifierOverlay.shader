Shader "Custom/URP/MagnifierOverlay"
{
    Properties
    {
        _Radius("Radius", Float) = 0.12
        _Zoom("Zoom", Float) = 2.0
        _Center("Center", Vector) = (0.5, 0.5, 0, 0)
        _EdgeSoftness("Edge Softness", Float) = 0.01
        _MainTex("MainTex", 2D) = "white" {}
        _HiddenCluesTex("Hidden Clues Texture", 2D) = "black" {}
        _HiddenCluesStrength("Hidden Clues Strength", Float) = 1
        _DesaturateScene("Desaturate Scene", Range(0,1)) = 1
        _SceneTex("Scene Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "MagnifierPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SceneTex);
            SAMPLER(sampler_SceneTex);

            TEXTURE2D(_HiddenCluesTex);
            SAMPLER(sampler_HiddenCluesTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Center;
                float _Radius;
                float _Zoom;
                float _EdgeSoftness;
                float _HiddenCluesStrength;
                float _DesaturateScene;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                float2 delta = uv - _Center.xy;
                delta.x *= _ScreenParams.x / _ScreenParams.y;
                float dist = length(delta);

                if (dist > _Radius)
                    return half4(0, 0, 0, 0);

                float2 zoomUV = _Center.xy + (uv - _Center.xy) / _Zoom;
                zoomUV = saturate(zoomUV);

                half4 sceneCol = SAMPLE_TEXTURE2D(_SceneTex, sampler_SceneTex, zoomUV);
                half4 hiddenCol = SAMPLE_TEXTURE2D(_HiddenCluesTex, sampler_HiddenCluesTex, zoomUV);

                half luminance = dot(sceneCol.rgb, half3(0.299, 0.587, 0.114));
                half3 sceneBW = half3(luminance, luminance, luminance);

                half3 sceneFinal = lerp(sceneCol.rgb, sceneBW, _DesaturateScene);

                half3 finalRgb = sceneFinal + hiddenCol.rgb * hiddenCol.a * _HiddenCluesStrength;
                finalRgb = saturate(finalRgb);

                float alpha = 1.0 - smoothstep(_Radius - _EdgeSoftness, _Radius, dist);

                return half4(finalRgb, alpha);

                
            }

            ENDHLSL
        }
    }
}