Shader "RoadOfTheOldKing/InteriorDarkness"
{
    // Dark-interior cover (22 Oct rework): drawn over the whole interior after the world, it reads the URP 2D light at
    // each pixel and turns opaque black only where no light reaches. The lantern and daylight are real Light2Ds with
    // wall shadows, so this matches their soft falloff and shadow edges exactly, and still hides unlit content
    // (world UI, effects) in the dark, which lighting alone would not.
    Properties
    {
        _DarkBelow ("Fully dark below light level", Float) = 0.03
        _ClearAbove ("Fully clear above light level", Float) = 0.3
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha, Zero One
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Light2D.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _DarkBelow;
                half _ClearAbove;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; half2 lightingUV : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.lightingUV = LightingUV(o.positionCS);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Light reaching a white surface here (HDR emulation included), so 1 is full daylight.
                half3 light = ApplyLight2D(half3(1, 1, 1), input.lightingUV);
                half level = max(light.r, max(light.g, light.b));
                return half4(0, 0, 0, 1 - smoothstep(_DarkBelow, _ClearAbove, level));
            }
            ENDHLSL
        }
    }
}
