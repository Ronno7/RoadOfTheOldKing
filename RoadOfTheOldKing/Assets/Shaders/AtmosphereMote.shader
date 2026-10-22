Shader "RoadOfTheOldKing/AtmosphereMote"
{
    // Ambient particles (pollen, dust, seeds). Snapped: a hard square on the 1/16 world grid, like the bonfire
    // smoke. Soft: a round glow that may sit between pixels. Glow blends from covering (0) to adding light (1).
    Properties
    {
        _PixelSnap ("Snap to the 1/16 world grid (square)", Float) = 1
        _Glow ("Covering (0) to added light (1)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            float _PixelSnap, _Glow;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                if (_PixelSnap > 0.5) world.xy = floor(world.xy * 16 + 0.5) / 16;
                output.positionCS = TransformWorldToHClip(world);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float a = input.color.a;
                if (_PixelSnap < 0.5) a *= pow(saturate(1 - length(input.uv * 2 - 1)), 1.5);
                return half4(input.color.rgb * a, a * (1 - _Glow));
            }
            ENDHLSL
        }
    }
}
