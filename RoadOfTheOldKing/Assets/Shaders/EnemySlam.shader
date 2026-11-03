Shader "RoadOfTheOldKing/EnemySlam"
{
    // The ground under a heavy attack, on the 16-PPU grid in flat tones. The rim is exactly what the attack reaches.
    // Through the windup cracks spread out from the centre, so the spread is the clock: it meets the rim as the slam
    // lands. On impact the disc flashes, a wave runs out to the rim and the cracks glow, then cool and fade.
    // A draw adds streaks racing in across the floor, in the Recall's cyan, beyond the rim as well as inside it.
    // An arc under a full circle draws as a wedge: its rim warms when the facing locks, and on impact a band crosses
    // from one side to the other, the swing itself, leaving scuffs that cool the same way.
    Properties
    {
        _Flow ("Draw streaks", Color) = (0.2980, 0.7961, 0.8667, 1)
        [HideInInspector] _Draw ("Draw streak reach (0 off)", Float) = 0
        _Warn ("Warning", Color) = (1, 0.75, 0.2, 1)
        _Hot ("Impact", Color) = (1, 0.36, 0.12, 1)
        _Flash ("Flash", Color) = (1, 0.93, 0.7, 1)
        _Crack ("Cold crack", Color) = (0.16, 0.1, 0.07, 1)
        [HideInInspector] _Center ("Centre", Vector) = (0, 0, 0, 0)
        [HideInInspector] _Radius ("Radius", Float) = 1
        [HideInInspector] _Windup ("Windup progress", Float) = 0
        [HideInInspector] _Impact ("Impact progress (0 before it lands)", Float) = 0
        [HideInInspector] _Arc ("Half the arc in turns (0.5 is a full circle)", Float) = 0.5
        [HideInInspector] _Facing ("Facing in turns", Float) = 0
        [HideInInspector] _Locked ("Facing locked", Float) = 0
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

            CBUFFER_START(UnityPerMaterial)
                half4 _Warn, _Hot, _Flash, _Crack, _Flow;
                float4 _Center;
                float _Radius, _Windup, _Impact, _Draw, _Arc, _Facing, _Locked;
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

            float Hash(float p) { return frac(sin(p * 91.17) * 43758.5453); }

            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3 - 2 * f);
                float a = Hash(i.x + i.y * 57), b = Hash(i.x + 1 + i.y * 57);
                float c = Hash(i.x + (i.y + 1) * 57), d = Hash(i.x + 1 + (i.y + 1) * 57);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half4 Over(half4 under, half3 color, half alpha) { return half4(lerp(under.rgb, color, alpha), max(under.a, alpha)); }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 d = floor(input.world * 16) / 16 + 1.0 / 32 - _Center.xy;
                float pixels = length(d) * 16, rim = _Radius * 16, r = pixels / rim;
                float turn = atan2(d.y, d.x) / 6.28318 + 0.5;

                // Short streaks on spokes, each spoke with its own phase, all running inward. Thinner toward the far end.
                half4 streak = half4(_Flow.rgb, 0);
                if (_Draw > 0 && pixels < _Draw * 16)
                {
                    const float Spokes = 44;
                    float spoke = floor(turn * Spokes);
                    float along = frac((pixels / 16 + _Time.y * 7 + Hash(spoke) * 9) / 3.5);
                    bool onSpoke = abs(frac(turn * Spokes) - 0.5) * (6.28318 / Spokes) * pixels < 0.8;
                    if (onSpoke && along < 0.3 && Hash(spoke + 11) > 0.3)
                        streak.a = pixels < _Draw * 8 ? 0.6 : 0.35;
                }
                bool wedge = _Arc < 0.5;
                float fromFacing = frac(turn - _Facing + 0.5) - 0.5;
                if (pixels > rim || (wedge && abs(fromFacing) > _Arc))
                {
                    clip(streak.a - 0.01);
                    return streak;
                }

                // Jagged cracks, one to a slice of the circle, each stopping short of the rim at its own length.
                const float Slices = 15;
                float slice = floor(turn * Slices);
                float wander = (Noise(float2(slice * 7.3, pixels * 0.11)) - 0.5) * 0.8;
                float line_ = abs(frac(turn * Slices) - 0.5 - wander) * (6.28318 / Slices) * pixels;
                float length_ = 0.62 + Hash(slice + 3) * 0.34;
                bool crack = !wedge && line_ < 1 && r > 0.16 && r < length_;

                bool landed = _Impact > 0;
                // Flat steps: it cools and fades in four tones, never a smooth fade.
                float cool = 1 - floor(saturate(_Impact) * 4) / 4;
                half4 color = streak.a > 0 ? streak : half4(_Warn.rgb, 0);

                // The danger zone reads the whole time, a little stronger behind the spreading front.
                float spread = landed ? 1 : _Windup;
                color = Over(color, _Warn.rgb, landed ? 0.2 * cool : r < spread ? 0.22 : 0.08);
                if (!landed && pixels < spread * rim && pixels > spread * rim - 3) color = Over(color, _Warn.rgb, 0.65);

                if (crack && r < spread)
                {
                    if (landed) color = Over(color, lerp(_Crack.rgb, _Hot.rgb, cool), max(cool, 0.25));
                    else color = Over(color, _Crack.rgb, 0.7);
                }

                // The rim, solid and exact, with short notches turned inward like the Watcher's sun wheel.
                bool ring = pixels > rim - 2 || (wedge && (_Arc - abs(fromFacing)) * 6.28318 * pixels < 2);
                bool notch = pixels > rim - 5 && abs(frac(turn * 24) - 0.5) * (6.28318 / 24) * pixels < 1;
                half3 rimColor = landed ? _Hot.rgb : wedge && _Locked > 0 ? lerp(_Warn.rgb, _Hot.rgb, 0.6) : _Warn.rgb;
                if (ring || notch) color = Over(color, rimColor, landed ? cool : 0.95);

                if (landed && wedge)
                {
                    // The swing crosses the wedge over the first fifth, and the scuffs show where it has been.
                    float across = lerp(-_Arc, _Arc, saturate(_Impact * 5));
                    if (_Impact < 0.2 && abs(fromFacing - across) * 6.28318 * pixels < 7) color = Over(color, _Flash.rgb, 0.95);
                    bool scuff = frac(pixels / 9) < 0.2 && Noise(float2(turn * 60, pixels * 0.1)) > 0.5 && r > 0.3 && fromFacing < across;
                    if (scuff) color = Over(color, lerp(_Crack.rgb, _Hot.rgb, cool), max(cool, 0.25));
                }
                else if (landed)
                {
                    // A flash in the first instant, and a wave running out to the rim over the first fifth.
                    float wave = saturate(_Impact * 5);
                    if (_Impact < 0.2 && r < wave && r > wave - 0.22) color = Over(color, _Flash.rgb, 0.95);
                    if (_Impact < 0.08) color = Over(color, _Flash.rgb, 0.7);
                }
                clip(color.a - 0.01);
                return color;
            }
            ENDHLSL
        }
    }
}
