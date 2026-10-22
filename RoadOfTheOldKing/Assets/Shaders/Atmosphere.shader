Shader "RoadOfTheOldKing/Atmosphere"
{
    // World-anchored atmosphere over the finished scene: drifting cloud shade, sunlit breaks, light shafts and mist
    // that a mask can confine (e.g. to water). The far-edge haze was removed at the user's request (22 Oct). Drawn on one camera-fitted quad
    // above the world and below world UI. Pixel snap and tone steps keep the result on the 16-PPU grid.
    Properties
    {
        [Header(Cloud shade)]
        _ShadeColor ("Shade colour (a = strength)", Color) = (0.12, 0.16, 0.2, 0.18)
        _CloudScale ("Cloud size (world units)", Float) = 18
        _CloudCover ("Cover (0 clear, 1 overcast)", Range(0, 1)) = 0.45
        _CloudSoft ("Cloud edge softness", Range(0.01, 0.5)) = 0.12
        _Wind ("Wind (world units per second, xy)", Vector) = (0.6, 0.2, 0, 0)
        [ToggleUI] _CloudPuffs ("Puffy clouds (off = soft banks)", Float) = 0
        [Header(Sun breaks)]
        _SunColor ("Sunlit gaps (added, a = strength)", Color) = (1, 0.85, 0.55, 0)
        [Header(Light shafts)]
        _ShaftColor ("Shaft colour (added, a = strength)", Color) = (1, 0.8, 0.5, 0)
        _ShaftAngle ("Shaft direction (degrees)", Float) = -40
        _ShaftScale ("Shaft spacing (world units)", Float) = 6
        _ShaftSpeed ("Shaft drift", Float) = 0.05
        [Header(Mist)]
        _MistColor ("Mist (a = strength)", Color) = (0.85, 0.9, 0.92, 0)
        _MistScale ("Mist size (world units)", Float) = 6
        _MistMask ("Mist mask (alpha)", 2D) = "white" {}
        _MistBounds ("Mist mask world rect (x, y, w, h; w = 0 everywhere)", Vector) = (0, 0, 0, 0)
        [Header(Pixel grid)]
        _PixelSnap ("Snap to the 1/16 world grid", Float) = 0
        _Steps ("Tone steps (0 = smooth)", Float) = 0
        [HideInInspector] _Intensity ("Runtime strength (eased down in combat)", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off ZTest Always
        Blend One OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; };

            half4 _ShadeColor, _SunColor, _ShaftColor, _MistColor;
            float4 _Wind, _MistBounds;
            float _CloudScale, _CloudCover, _CloudSoft, _CloudPuffs, _ShaftAngle, _ShaftScale, _ShaftSpeed;
            float _MistScale, _PixelSnap, _Steps, _Intensity;
            TEXTURE2D(_MistMask); SAMPLER(sampler_MistMask);

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

            // Four octaves give clouds scalloped, billowing edges instead of round blobs.
            float Fbm4(float2 p)
            {
                float v = 0, a = 0.5;
                [unroll] for (int k = 0; k < 4; k++) { v += a * Noise(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return v / 0.9375;
            }

            // Cloud density in [0,1]. Soft banks: broad drifting noise. Puffs: separate clouds, stretched a little
            // along the wind, domain-warped, with lumpy edges that slowly reshape as they travel; the result is
            // spread so that cover is roughly the fraction of ground under cloud.
            float CloudField(float2 w, float t)
            {
                float2 q = (w - _Wind.xy * t) / max(_CloudScale, 0.01);
                float c;
                [branch] if (_CloudPuffs < 0.5) c = Fbm(q);
                else
                {
                    float2 along = normalize(_Wind.xy + float2(1e-4, 0)), side = float2(-along.y, along.x);
                    q = float2(dot(q, along) * 0.75, dot(q, side));
                    float2 warp = float2(Fbm(q * 0.6 + 3.1 + t * 0.01), Fbm(q * 0.6 + 7.7 - t * 0.01)) - 0.5;
                    float body = Fbm4(q + warp * 1.2);
                    float lumps = Fbm4(q * 3.1 + 11.3 + float2(t * 0.02, 0));
                    c = saturate((body + (lumps - 0.5) * 0.45 - 0.5) * 2.6 + 0.5);
                }
                return c;
            }

            // With steps, tones fall into a few flat bands like painted pixel shading.
            float Tone(float v) { return _Steps > 0.5 ? floor(v * _Steps + 0.5) / _Steps : v; }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 w = input.world;
                if (_PixelSnap > 0.5) w = floor(w * 16) / 16 + 1.0 / 32;
                float t = _Time.y;

                // Clouds: 1 under cloud. Sunlit gaps are what the clouds leave open.
                float c = CloudField(w, t);
                float edge = 1 - _CloudCover;
                float cloud = Tone(smoothstep(edge - _CloudSoft, edge + _CloudSoft, c));
                float shade = cloud * _ShadeColor.a;
                float3 light = (1 - cloud) * _SunColor.rgb * _SunColor.a;

                // Shafts: bands across the light direction, broken along their length, weaker under cloud.
                float a = radians(_ShaftAngle);
                float2 dir = float2(cos(a), sin(a)), across = float2(-dir.y, dir.x);
                float u = dot(w, across) / max(_ShaftScale, 0.01) + t * _ShaftSpeed;
                float v = dot(w, dir) / max(_ShaftScale * 4, 0.01);
                float band = smoothstep(0.58, 0.78, Noise(float2(u, 0.5))) * (0.4 + 0.6 * Noise(float2(u * 0.5 + 3.1, v - t * 0.03)));
                light += Tone(band) * _ShaftColor.rgb * _ShaftColor.a * (1 - cloud * 0.75);

                // Mist, optionally confined by a world-space mask. Five taps 1.5 units apart feather the mask's
                // cell edges so mist thins over the banks instead of stopping on a tile line.
                float mask = 1;
                if (_MistBounds.z > 0)
                {
                    float2 uv = (w - _MistBounds.xy) / _MistBounds.zw;
                    float2 spread = 1.5 / _MistBounds.zw;
                    mask = 0;
                    float2 taps[5] = { float2(0, 0), float2(spread.x, 0), float2(-spread.x, 0), float2(0, spread.y), float2(0, -spread.y) };
                    [unroll] for (int k = 0; k < 5; k++)
                    {
                        float2 tap = uv + taps[k];
                        mask += all(tap >= 0) && all(tap <= 1) ? SAMPLE_TEXTURE2D(_MistMask, sampler_MistMask, tap).a : 0;
                    }
                    mask = smoothstep(0, 1, mask / 5);
                }
                float mist = Tone(smoothstep(0.4, 0.8, Fbm((w - _Wind.xy * t * 0.35) / max(_MistScale, 0.01) + 3.7)) * mask) * _MistColor.a;

                // Premultiplied: shade and mist cover the scene; sun and shafts add light.
                float alpha = saturate(shade + mist);
                float3 rgb = _ShadeColor.rgb * shade + _MistColor.rgb * mist + light;
                return half4(rgb, alpha) * _Intensity;
            }
            ENDHLSL
        }
    }
}
