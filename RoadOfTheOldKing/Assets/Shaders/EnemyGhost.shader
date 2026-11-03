Shader "RoadOfTheOldKing/EnemyGhost"
{
    // A sprite as a flat tinted after-image: the tint and strength come from the SpriteRenderer's colour, and the sprite's
    // own light and dark keep its shape readable. Unlit, on the 16-PPU grid, with hard edges.
    Properties
    {
        [HideInInspector] _MainTex ("Sprite", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha, Zero One
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                clip(sprite.a - 0.5);
                half light = dot(sprite.rgb, half3(0.3, 0.59, 0.11));
                return half4(saturate(input.color.rgb * (0.55 + 0.75 * light)), input.color.a);
            }
            ENDHLSL
        }
    }
}
