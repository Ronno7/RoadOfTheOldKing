Shader "RoadOfTheOldKing/InteriorDarkness"
{
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct output { float4 vertex : SV_POSITION; float2 world : TEXCOORD0; float inside : TEXCOORD1; };
            float4 _Origin, _Daylight;
            float _Feather, _Lantern;
            output vert(input v)
            {
                output o; o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xy; o.inside = v.uv.x; return o;
            }
            fixed4 frag(output i) : SV_Target
            {
                float2 edge = abs(i.world - _Daylight.xy) - _Daylight.zw;
                float daylight = 1 - smoothstep(0, _Feather, max(edge.x, edge.y));
                float lantern = _Lantern * i.inside * (1 - smoothstep(_Origin.z, _Origin.w, distance(i.world, _Origin.xy)));
                return fixed4(0, 0, 0, 1 - max(daylight, lantern));
            }
            ENDCG
        }
    }
}
