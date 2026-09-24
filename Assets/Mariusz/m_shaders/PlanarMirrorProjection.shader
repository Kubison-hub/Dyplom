Shader "Mariusz/Planar Mirror Projection"
{
    Properties
    {
        [MainColor] _Tint ("Tint", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _MirrorTexture ("Mirror Texture", 2D) = "black" {}
        _ReflectionBrightness ("Reflection Brightness", Range(0, 2)) = 0.88
        _DirtStrength ("Dirt Strength", Range(0, 1)) = 0.2
        _EdgeDirt ("Edge Dirt", Range(0, 1)) = 0.5
        _SmudgeScale ("Smudge Scale", Range(0.25, 4)) = 1
        _DirtColor ("Dirt Color", Color) = (0.2, 0.18, 0.16, 1)
        _SheenStrength ("Sheen Strength", Range(0, 1)) = 0.2
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.5
        [HDR] _SheenColor ("Sheen Color", Color) = (0.8, 0.77, 0.68, 1)
        [HideInInspector] _FlipRenderTextureY ("Flip Render Texture Y", Float) = 0
        [HideInInspector] _MirrorVerticalOffset ("Mirror Vertical Offset", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "PlanarMirror"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MirrorTexture);
            SAMPLER(sampler_MirrorTexture);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _DirtColor;
                half4 _SheenColor;
                float _ReflectionBrightness;
                float _DirtStrength;
                float _EdgeDirt;
                float _SmudgeScale;
                float _SheenStrength;
                float _FresnelPower;
                float _FlipRenderTextureY;
                float _MirrorVerticalOffset;
                float4x4 _MirrorViewProjection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 mirrorClip : TEXCOORD0;
                float2 glassUV : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 blend = frac(p);
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(
                    lerp(Hash(cell), Hash(cell + float2(1.0, 0.0)), blend.x),
                    lerp(Hash(cell + float2(0.0, 1.0)), Hash(cell + 1.0), blend.x),
                    blend.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.mirrorClip = mul(_MirrorViewProjection, float4(positionWS, 1.0));
                output.glassUV = input.uv;
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.mirrorClip.xy / max(input.mirrorClip.w, 0.0001) * 0.5 + 0.5;
                uv.y = lerp(uv.y, 1.0 - uv.y, _FlipRenderTextureY);
                uv.y += _MirrorVerticalOffset;
                half3 reflection = SAMPLE_TEXTURE2D(_MirrorTexture, sampler_MirrorTexture, saturate(uv)).rgb;

                float2 glassUV = input.glassUV;
                float2 scaledUV = glassUV * _SmudgeScale;
                float broadSmudge = ValueNoise(scaledUV * float2(7.0, 4.0));
                float fineSmudge = ValueNoise(scaledUV * float2(29.0, 13.0));
                float verticalStreak = ValueNoise(scaledUV * float2(21.0, 2.5));
                float patches = smoothstep(0.48, 0.78, broadSmudge * 0.55 + fineSmudge * 0.25 + verticalStreak * 0.2);
                float edgeDistance = min(min(glassUV.x, 1.0 - glassUV.x),
                    min(glassUV.y, 1.0 - glassUV.y));
                float edge = 1.0 - smoothstep(0.0, 0.15, edgeDistance);
                float dirt = saturate(_DirtStrength * (patches + edge * _EdgeDirt * (0.5 + broadSmudge)));

                half3 glassColor = reflection * _Tint.rgb * _ReflectionBrightness;
                glassColor = lerp(glassColor, _DirtColor.rgb, dirt * 0.55);
                float3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                float facing = abs(dot(normalize(input.normalWS), viewDirection));
                float fresnel = pow(saturate(1.0 - facing), _FresnelPower);
                float sheen = fresnel * _SheenStrength * (0.85 + 0.15 * fineSmudge);
                glassColor = lerp(glassColor, _SheenColor.rgb, saturate(sheen));
                return half4(glassColor, 1.0);
            }
            ENDHLSL
        }
    }
}
