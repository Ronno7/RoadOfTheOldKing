#ifndef ROAD_OF_THE_OLD_KING_LIGHT2D
#define ROAD_OF_THE_OLD_KING_LIGHT2D

// URP 2D lighting for custom sprite-like shaders (22 Oct): the same result as Sprite-Lit-Default without mask or normal
// maps, so grass and characters light exactly like the tiles around them. Use from a pass tagged
// "LightMode"="Universal2D" after #include_with_pragmas ".../2D/Include/ShapeLightShared.hlsl".
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightVariables.hlsl"

// Screen-space coordinate into the light textures; compute in the vertex stage from the clip-space position.
half2 LightingUV(float4 positionCS)
{
    return half2(ComputeScreenPos(positionCS / positionCS.w).xy);
}

#define ROTOK_SHAPE_LIGHT(index) \
    { half4 light = SAMPLE_TEXTURE2D(_ShapeLightTexture##index, sampler_ShapeLightTexture##index, lightingUV); \
      modulate += light * _ShapeLightBlendFactors##index.x; additive += light * _ShapeLightBlendFactors##index.y; }

half3 ApplyLight2D(half3 albedo, half2 lightingUV)
{
#if !USE_SHAPE_LIGHT_TYPE_0 && !USE_SHAPE_LIGHT_TYPE_1 && !USE_SHAPE_LIGHT_TYPE_2 && !USE_SHAPE_LIGHT_TYPE_3
    return albedo; // No lights on this layer: unlit, as Sprite-Lit-Default.
#else
    half4 modulate = 0, additive = 0;
    #if USE_SHAPE_LIGHT_TYPE_0
    ROTOK_SHAPE_LIGHT(0)
    #endif
    #if USE_SHAPE_LIGHT_TYPE_1
    ROTOK_SHAPE_LIGHT(1)
    #endif
    #if USE_SHAPE_LIGHT_TYPE_2
    ROTOK_SHAPE_LIGHT(2)
    #endif
    #if USE_SHAPE_LIGHT_TYPE_3
    ROTOK_SHAPE_LIGHT(3)
    #endif
    return max(0, _HDREmulationScale * (albedo * modulate.rgb + additive.rgb));
#endif
}

#endif
