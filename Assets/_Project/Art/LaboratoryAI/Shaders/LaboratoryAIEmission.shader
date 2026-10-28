Shader "CalculusLab/AI/Emission"
{
    Properties { [HDR] _Color ("Emission colour", Color) = (0.01,0.8,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 position:SV_POSITION; };
            half4 _Color;
            v2f vert(appdata v) { v2f o; o.position=UnityObjectToClipPos(v.vertex); return o; }
            half4 frag(v2f i):SV_Target { return half4(_Color.rgb,1); }
            ENDCG
        }
    }
}
