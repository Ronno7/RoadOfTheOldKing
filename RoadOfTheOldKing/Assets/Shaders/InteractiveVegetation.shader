Shader "RoadOfTheOldKing/InteractiveVegetation"
{
    Properties
    {
        _MainTex ("Plant atlas", 2D) = "white" {}
        [HideInInspector] _PlantDepthSide ("Depth side", Float) = -1
        [HideInInspector] _CutDebris ("Cut debris", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _PlantClock;
                float4 _PlantTouch[8]; // world feet xy, intensity, radius
                float4 _PlantDepthWindow; // nearby roots: min xy, max x, player's feet y
                float _PlantDepthSide; // -1 ground draw, +1 foreground draw
                float _CutDebris;
            CBUFFER_END
            struct Attributes
            {
                float3 vertex:POSITION;
                float2 uv:TEXCOORD0;
                float4 plant:TEXCOORD1; // bottom pivot xy, phase, flexibility
                float2 motion:TEXCOORD2; // rest angle radians, wind variation
                half4 color:COLOR;
            };
            struct Varyings
            {
                float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float depthSide:TEXCOORD1;
                float mote:TEXCOORD2; half4 color:COLOR;
            };
            Varyings Vert(Attributes v)
            {
                Varyings o=(Varyings)0;
                if(_CutDebris>.5)
                {
                    o.vertex=TransformObjectToHClip(v.vertex);o.uv=v.uv;o.color=v.color;o.mote=v.plant.z;
                    o.depthSide=(v.plant.y<_PlantDepthWindow.w?1:-1)*_PlantDepthSide;
                    return o;
                }
                // Continuous angular motion rotates intact pixel artwork. The two leaves
                // share the traveling breeze but carry their own rest angle and timing.
                float t=_PlantClock;
                float wind=(sin(t*1.55-v.plant.x*.42-v.plant.y*.31+v.plant.z*.35)*.139626
                           +sin(t*2.7+v.plant.z)*.034907)*v.motion.y;
                float push=0, weight=0;
                [unroll] for(int i=0;i<8;i++)
                {
                    float2 delta=v.plant.xy-_PlantTouch[i].xy;
                    float distance=length(delta);
                    float reach=saturate(1-distance/max(.01,_PlantTouch[i].w));
                    float strength=reach*reach*_PlantTouch[i].z;
                    // Positive rotation leans left. This smooth signed direction avoids
                    // snapping when the player crosses a root or a trail sample expires.
                    float away=-delta.x/sqrt(dot(delta,delta)+.0225);
                    push+=away*strength;
                    weight+=strength;
                }
                float contact=clamp(push*2/max(1,weight),-1,1)*.785398;
                float angle=clamp(v.motion.x+(wind+contact)*v.plant.w,-1.22173,1.22173);
                float sine,cosine;
                sincos(angle,sine,cosine);
                float2 local=v.vertex.xy-v.plant.xy;
                float2 rotated=float2(local.x*cosine-local.y*sine,local.x*sine+local.y*cosine);
                v.vertex.xy=v.plant.xy+rotated;
                o.vertex=TransformObjectToHClip(v.vertex);
                o.uv=v.uv;
                // Classify the whole leaf by its stationary root, never by its waving tip.
                bool inFront=v.plant.x>=_PlantDepthWindow.x && v.plant.x<=_PlantDepthWindow.z
                    && v.plant.y>=_PlantDepthWindow.y && v.plant.y<_PlantDepthWindow.w;
                o.depthSide=(inFront?1:-1)*_PlantDepthSide;
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                clip(i.depthSide);
                if(_CutDebris>.5)
                {
                    half4 debris=i.color;
                    if(i.mote>.5)
                    {
                        float2 p=i.uv*2-1;
                        debris.a*=1-smoothstep(.25,1,dot(p,p));
                    }
                    else debris*=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                    clip(debris.a-.005);
                    return debris;
                }
                half4 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                clip(color.a-.5);
                return half4(color.rgb,1);
            }
            ENDHLSL
        }
    }
}
