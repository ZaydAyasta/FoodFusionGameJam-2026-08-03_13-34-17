Shader "FoodFusion/Fake Projected Light"
{
    Properties
    {
        _Color ("Color", Color) = (1,0.7,0.35,0.2)
        _Softness ("Softness", Range(0.01,1)) = 0.65
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 1
        [HideInInspector] _BlendMode ("Blend Mode", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Softness;
                half _BlendMode;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float distanceFromCenter = length(input.uv * 2.0 - 1.0);
                half mask = 1.0 - smoothstep(max(0.0, 1.0 - _Softness), 1.0, distanceFromCenter);
                half strength = saturate(_Color.a * mask);

                // Multiply needs white outside the circle; screen-style soft additive
                // needs its RGB pre-scaled so the transparent corners remain untouched.
                if (_BlendMode > 2.5h && _BlendMode < 3.5h)
                    return half4(lerp(1.0h, _Color.rgb, strength), 1.0h);
                if ((_BlendMode > 0.5h && _BlendMode < 1.5h) || _BlendMode > 3.5h)
                    return half4(_Color.rgb * strength, strength);

                return half4(_Color.rgb, strength);
            }
            ENDHLSL
        }
    }
}
