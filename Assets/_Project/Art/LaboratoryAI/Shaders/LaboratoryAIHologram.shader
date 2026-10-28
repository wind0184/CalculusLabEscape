Shader "CalculusLab/AI/Hologram"
{
    Properties
    {
        [HDR] _Color ("Colour and opacity", Color) = (0.01,0.4,1,0.15)
        _UseVertexAlpha ("Use soft vertex fade", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; fixed4 colour:COLOR; };
            struct v2f { float4 position:SV_POSITION; fixed4 colour:COLOR; };
            half4 _Color; float _UseVertexAlpha;
            v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex); o.colour=v.colour; return o; }
            half4 frag(v2f i):SV_Target { return half4(_Color.rgb,_Color.a*lerp(1,i.colour.a,_UseVertexAlpha)); }
            ENDCG
        }
    }
}
