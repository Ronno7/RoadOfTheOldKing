Shader "RoadOfTheOldKing/BonfireSmoke"
{
    Properties
    {
        // An upgrade fire's smoke, lit by its gold flame, sits one step lighter on the ruin ramp (#90).
        [Toggle] _Bright ("Brighter (upgrade fire)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half _Bright;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                world.xy = floor(world.xy * 16 + 0.5) / 16;
                output.positionCS = TransformWorldToHClip(world);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // Six native pixels per quad. Hard cluster edges, two colors, no blur/fade.
                float2 pixel = floor(min(input.uv, 0.9999) * 6) - 2.5;
                float radius = input.color.a >= 0.7 ? 2.8 : input.color.a >= 0.3 ? 1.8 : 0.8;
                clip(radius * radius - dot(pixel, pixel));
                bool light = pixel.y > 0.5 || pixel.x > 1.5;
                // Ordinary #929780 / #737868; upgrade #A6AA8A / #7C8370, the next step up the ruin ramp.
                half3 tint = _Bright > 0.5
                    ? (light ? half3(0.651, 0.667, 0.541) : half3(0.486, 0.514, 0.439))
                    : (light ? half3(0.573, 0.592, 0.502) : half3(0.451, 0.471, 0.408));
                #ifndef UNITY_COLORSPACE_GAMMA
                tint = SRGBToLinear(tint);
                #endif
                return half4(tint, 1);
            }
            ENDHLSL
        }
    }
}
