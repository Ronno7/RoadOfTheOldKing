Shader "RoadOfTheOldKing/BonfireEmber"
{
    // Fire pixels: upgrade-fire embers (#90) and the lighting burst's sparks. Each particle is one solid native pixel on
    // the 16-PPU grid (two for the burst's bigger sparks), unlit like the flame. Its vertex alpha is its age (1 to 0);
    // the colour steps down the flame's own ramp as it ages, then the pixel cuts out. No blur or fade, like the smoke.
    Properties
    {
        // 0: the gold flame (upgrade fires). 1: the ordinary orange flame.
        [Toggle] _Ramp ("Ordinary flame colours", Float) = 0
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
            half _Ramp;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                world.xy = floor(world.xy * 16 + 0.5) / 16;
                output.positionCS = TransformWorldToHClip(world);
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half age = input.color.a;
                clip(age - 0.1);
                int step = age >= 0.8 ? 0 : age >= 0.55 ? 1 : age >= 0.3 ? 2 : 3;
                // Gold: #FFEDA6, #FFD45E, #D9962A, #A8742A (BonfireFlameGold16).
                // Ordinary: #FFE4A3, #FFB035, #E56310, #B2793D (BonfireFlame16).
                half3 gold = step == 0 ? half3(1.0, 0.929, 0.651) : step == 1 ? half3(1.0, 0.831, 0.369)
                    : step == 2 ? half3(0.851, 0.588, 0.165) : half3(0.659, 0.455, 0.165);
                half3 orange = step == 0 ? half3(1.0, 0.894, 0.639) : step == 1 ? half3(1.0, 0.690, 0.208)
                    : step == 2 ? half3(0.898, 0.388, 0.063) : half3(0.698, 0.475, 0.239);
                half3 tint = _Ramp > 0.5 ? orange : gold;
                #ifndef UNITY_COLORSPACE_GAMMA
                tint = SRGBToLinear(tint);
                #endif
                return half4(tint, 1);
            }
            ENDHLSL
        }
    }
}
