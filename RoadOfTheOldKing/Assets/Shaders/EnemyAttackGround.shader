Shader "RoadOfTheOldKing/EnemyAttackGround"
{
    // The ground under an attack, on the 16-PPU grid in flat tones. The rim is exactly what the attack reaches.
    // Through the windup a front spreads out from the enemy, so the spread is the clock: it meets the rim as the blow
    // lands. A heavy attack also cracks: on impact the disc flashes, a wave runs out to the rim and the cracks glow,
    // then cool and fade.
    // A draw adds streaks racing in across the floor, in the Recall's cyan, beyond the rim as well as inside it.
    // An arc under a full circle draws as a wedge: its rim warms when the facing locks, and on impact a band crosses
    // from one side to the other, the swing itself, leaving scuffs that cool the same way.
    // A charge draws as a lane with arrows down its length: the band follows the charger along it, leaving ruts.
    // Ghost Steps replaces both for ordinary enemies: a dotted line under the after-images (drawn by EnemyAttackGround) and a
    // crescent on the rim of a swing that thickens through the windup.
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
        [HideInInspector] _Heavy ("Thick rim, notches and cracks", Float) = 0
        [HideInInspector] _Lane ("Lane length (0 off)", Float) = 0
        [HideInInspector] _Direction ("Lane direction", Vector) = (1, 0, 0, 0)
        [HideInInspector] _Sweep ("How far along the strike is", Float) = 0
        [HideInInspector] _Ghost ("Ghost Steps", Float) = 0
        _GhostPale ("Ghost while aiming", Color) = (0.839, 0.894, 0.957, 1)
        _GhostHot ("Ghost once committed", Color) = (0.98, 0.588, 0.431, 1)
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
                half4 _Warn, _Hot, _Flash, _Crack, _Flow, _GhostPale, _GhostHot;
                float4 _Center, _Direction;
                float _Radius, _Windup, _Impact, _Draw, _Arc, _Facing, _Locked, _Heavy, _Lane, _Sweep, _Ghost;
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

            half3 RimColor(bool landed) { return landed ? _Hot.rgb : _Locked > 0 ? lerp(_Warn.rgb, _Hot.rgb, 0.6) : _Warn.rgb; }

            // One pixel while it can still turn and two once committed: the lock is the cue to dodge.
            float RimWidth(bool landed) { return landed || _Locked > 0 || _Heavy > 0 ? 2 : 1; }
            half RimAlpha(bool landed, float cool) { return landed ? cool : _Locked > 0 || _Heavy > 0 ? 0.95 : 0.8; }

            half4 LaneFrag(float2 d)
            {
                float along = dot(d, _Direction.xy) * 16;
                float signedSide = dot(d, float2(-_Direction.y, _Direction.x)) * 16, side = abs(signedSide);
                float length_ = _Lane * 16, halfWidth = _Radius * 16;
                clip(min(along, min(length_ - along, halfWidth - side)));

                bool landed = _Impact > 0;
                float cool = 1 - floor(saturate(_Impact) * 4) / 4;
                float front = _Windup * length_;
                half4 color = half4(_Warn.rgb, 0);
                color = Over(color, _Warn.rgb, landed ? 0.2 * cool : along < front ? 0.22 : 0.08);
                if (!landed && along < front && along > front - 3) color = Over(color, _Warn.rgb, 0.65);

                // Arrows down the lane, so it reads as a direction and not just a strip.
                bool arrow = frac((along + side * 0.7) / 14) < 0.15 && along > 5 && along < length_ - 4;
                if (arrow && !landed) color = Over(color, _Warn.rgb, along < front ? 0.5 : 0.22);

                float rim = RimWidth(landed);
                if (side > halfWidth - rim || along < rim || along > length_ - rim) color = Over(color, RimColor(landed), RimAlpha(landed, cool));

                if (landed)
                {
                    float band = _Sweep * length_;
                    if (_Sweep < 1 && abs(along - band) < 7) color = Over(color, _Flash.rgb, 0.95);
                    // Two ruts torn up behind the charge.
                    bool rut = abs(side - halfWidth * 0.5) < 1.1 && along < band && Noise(float2(along * 0.25, signedSide > 0 ? 3 : 8)) > 0.35;
                    if (rut) color = Over(color, lerp(_Crack.rgb, _Hot.rgb, cool), max(cool, 0.25));
                }
                clip(color.a - 0.01);
                return color;
            }

            // The path under the after-images: a dotted line that runs out with the windup and goes as the charger passes.
            half4 GhostLane(float2 d)
            {
                float along = dot(d, _Direction.xy) * 16, side = abs(dot(d, float2(-_Direction.y, _Direction.x))) * 16;
                bool landed = _Impact > 0;
                float length_ = _Lane * 16, front = landed ? length_ : _Windup * length_;
                bool on = side <= 1 && along >= 0 && along < front && frac(along / 6) < 0.34 && !(landed && along < _Sweep * length_);
                clip(on ? 1 : -1);
                float cool = 1 - floor(saturate(_Impact) * 4) / 4;
                return half4(_Locked > 0 || landed ? _GhostHot.rgb : _GhostPale.rgb, 0.5 * (landed ? cool : 1));
            }

            half4 GhostWedge(float pixels, float rim, float fromFacing)
            {
                float t = abs(fromFacing) / _Arc;
                clip(1 - t);
                clip(rim - pixels);
                float thickness = (rim * 0.06 + rim * 0.28 * _Windup) * sqrt(max(0, 1 - t * t));
                clip(pixels - (rim - thickness));
                if (_Impact > 0)
                {
                    // The swing passes across the crescent, which then cools away.
                    clip(lerp(-_Arc, _Arc, saturate(_Impact * 5)) - fromFacing);
                    return half4(_Flash.rgb, 0.95 * (1 - floor(saturate(_Impact) * 4) / 4));
                }
                return _Locked > 0 ? half4(_GhostHot.rgb, 0.72) : half4(_GhostPale.rgb, 0.45);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 d = floor(input.world * 16) / 16 + 1.0 / 32 - _Center.xy;
                if (_Lane > 0) return _Ghost > 0 ? GhostLane(d) : LaneFrag(d);
                float pixels = length(d) * 16, rim = _Radius * 16, r = pixels / rim;
                float turn = atan2(d.y, d.x) / 6.28318 + 0.5;
                if (_Ghost > 0) return GhostWedge(pixels, rim, frac(turn - _Facing + 0.5) - 0.5);

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
                bool crack = _Heavy > 0 && !wedge && line_ < 1 && r > 0.16 && r < length_;

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

                // The rim, solid and exact; a heavy one has short notches turned inward like the Watcher's sun wheel.
                float rimWidth = RimWidth(landed);
                bool ring = pixels > rim - rimWidth || (wedge && (_Arc - abs(fromFacing)) * 6.28318 * pixels < rimWidth);
                bool notch = _Heavy > 0 && pixels > rim - 5 && abs(frac(turn * 24) - 0.5) * (6.28318 / 24) * pixels < 1;
                half3 rimColor = landed || wedge ? RimColor(landed) : _Warn.rgb;
                if (ring || notch) color = Over(color, rimColor, RimAlpha(landed, cool));

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
