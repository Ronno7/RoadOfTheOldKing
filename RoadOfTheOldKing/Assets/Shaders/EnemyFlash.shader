Shader "RoadOfTheOldKing/EnemyFlash"
{
    // Character sprite (player and enemies) with a hit flash. Lit by URP 2D lights like the world around it (22 Oct);
    // the flash is applied after lighting so it stays a clear, full-strength cue in any light.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FlashAmount ("Flash", Range(0,1)) = 0
        _FlashColor ("Flash Color", Color) = (1,0.94,0.8,1)
        _HoodieColor ("Hoodie Color (alpha 0 = as drawn)", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile_instancing
            #include "Light2D.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FlashColor;
                half4 _HoodieColor;
                half _FlashAmount;
            CBUFFER_END

            // The hoodie's mid blue (#32628E): its brightest channel, which the other blues are shaded against.
            static const half HoodieMid = 0.557h;

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half2 lightingUV : TEXCOORD1; half4 color : COLOR; };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.lightingUV = LightingUV(o.positionCS);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                // The hero's chosen hoodie colour (set on his body only): saturated blues take it, keeping their
                // shade; the near-black outline, skin, hair and boots stay as drawn. HoodieColor.Recolor matches it.
                if (_HoodieColor.a > 0.5h)
                {
                    half top = max(c.r, max(c.g, c.b));
                    half low = min(c.r, min(c.g, c.b));
                    if (c.b >= top && top > 0.2h && top - low > 0.3h * top)
                        c.rgb = saturate(_HoodieColor.rgb * (top / HoodieMid));
                }
                c.rgb = ApplyLight2D(c.rgb, input.lightingUV);
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _FlashAmount);
                c.rgb *= c.a;
                return c;
            }
            ENDHLSL
        }
    }
}
