Shader "RoadOfTheOldKing/SluiceDrain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _Waterline ("World waterline", Float) = 1000
        _BasinDepth ("Basin bed height data", 2D) = "white" {}
        _BasinBounds ("World basin rectangle", Vector) = (0,0,1,1)
        _UseBasinDepth ("Recede by bed elevation", Float) = 0
        _WaterLevel ("Basin water level", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex DrainVert
            #pragma fragment DrainFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            struct drain_v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 world : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float _Waterline;
            sampler2D _BasinDepth;
            float4 _BasinBounds;
            float _UseBasinDepth, _WaterLevel;
            drain_v2f DrainVert(appdata_t IN)
            {
                v2f sprite = SpriteVert(IN);
                drain_v2f OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = sprite.vertex;
                OUT.color = sprite.color;
                OUT.texcoord = sprite.texcoord;
                OUT.world = mul(unity_ObjectToWorld, IN.vertex).xy;
                return OUT;
            }
            fixed4 DrainFrag(drain_v2f IN) : SV_Target
            {
                float2 pixel = floor(IN.world * 16) / 16;
                float edge = _Waterline + floor((sin(pixel.x * 1.7) + sin(pixel.x * .63)) * 1.5) / 16;
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;
                float2 basinUV=(pixel+.03125-_BasinBounds.xy)/max(_BasinBounds.zw,.001);
                float inBasin=step(0,basinUV.x)*step(0,basinUV.y)*step(basinUV.x,1)*step(basinUV.y,1);
                fixed4 bed=tex2D(_BasinDepth,basinUV);
                if(_UseBasinDepth>.5 && inBasin>.5 && bed.a>.5){
                    float depth=_WaterLevel-bed.r;
                    clip(depth);
                    // Native broken wet margins travel around every cove instead
                    // of cutting the whole region with a screen-direction wipe.
                    float mark=fmod(floor(pixel.x*4)+floor(pixel.y*2)*7,11);
                    if(depth<.007 && mark<3)c.rgb=lerp(c.rgb,fixed3(.48,.63,.58),.4);
                    else if(depth<.025)c.rgb*=.94;
                }else clip(edge-pixel.y);
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
