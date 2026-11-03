Shader "RoadOfTheOldKing/BossFog"
{
    // A boss room's fog, drawn over the finished scene: a bright wall of mist on the room's edge, thinning into a dim
    // veil over everything beyond. Inside the room it draws nothing. World anchored, on the 16-PPU grid in flat tones.
    Properties
    {
        _Mist ("Wall mist", Color) = (0.8, 0.82, 0.74, 1)
        _Veil ("Far veil", Color) = (0.14, 0.18, 0.18, 1)
        _WallAlpha ("Wall opacity", Range(0, 1)) = 0.75
        _VeilAlpha ("Veil opacity", Range(0, 1)) = 0.50
        _Steps ("Tone steps", Float) = 5
        [HideInInspector] _Area ("Room (xMin, yMin, xMax, yMax)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _Rise ("Rise (0 gone, 1 up)", Float) = 0
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
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Mist, _Veil;
                half _WallAlpha, _VeilAlpha;
                float _Steps, _Rise;
                float4 _Area;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world.xy;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), u.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), u.x), u.y);
            }

            float Fbm(float2 p) { return Noise(p) * 0.65 + Noise(p * 2.03 + 17.1) * 0.35; }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 w = floor(input.world * 16) / 16 + 1.0 / 32;
                float t = _Time.y;

                // Depth past the room's edge, roughened by the drift so the wall is no ruled line.
                float2 past = max(_Area.xy - w, w - _Area.zw);
                float depth = max(past.x, past.y) + (Fbm(w * 0.7 + float2(t * 0.14, -t * 0.22)) - 0.5) * 1.3;

                // It spreads out from the edge as it rises and draws back to it as it clears, breaking up at the front.
                // At full reach it passes the widest view: zoomed all the way out and free looking, on an ultrawide.
                clip(min(_Rise * _Rise * 42, 36) - depth - Noise(w * 1.5) * 2);
                clip(depth);

                // Bright at the wall, with large slow wisps through the veil; turned and warped to hide the noise lattice.
                float wall = 1 - saturate(depth / 2.5);
                float2 q = w * 0.3 + float2(-t * 0.07, t * 0.05);
                float wisps = Fbm(float2(q.x + q.y * 0.6, q.y - q.x * 0.6) + Fbm(q * 1.7) * 1.5);
                float tone = saturate(wall + smoothstep(0.45, 0.8, wisps) * 0.35);
                tone = floor(tone * _Steps + 0.5) / _Steps;
                return half4(lerp(_Veil.rgb, _Mist.rgb, tone), lerp(_VeilAlpha, _WallAlpha, tone));
            }
            ENDHLSL
        }
    }
}
