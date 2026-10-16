Shader "RoadOfTheOldKing/SpectralRecall"
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
        _Deep ("Recall shadow", Color) = (0.1647,0.4980,0.5843,1)
        _Cyan ("Recall cyan", Color) = (0.2980,0.7961,0.8667,1)
        _Core ("Recall core", Color) = (0.8118,0.9725,1,1)
        _Opacity ("Opacity", Range(0,1)) = 0.72
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment RecallFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            fixed4 _Deep, _Cyan, _Core;
            half _Opacity;
            fixed4 RecallFrag(v2f IN) : SV_Target
            {
                fixed4 source = SampleSpriteTexture(IN.texcoord);
                half value = dot(source.rgb, half3(.299,.587,.114));
                // Preserve the accepted alpha silhouette; only recolor its existing pixel planes.
                fixed3 ramp = value < .27 ? _Deep.rgb : value < .58 ? _Cyan.rgb : _Core.rgb;
                fixed alpha = source.a * IN.color.a * _Opacity;
                return fixed4(ramp * IN.color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
}
