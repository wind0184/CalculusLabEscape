Shader "Calculus/Valley Morning Sky" {
 Properties { _Top("Zenith",Color)=(.25,.53,.68,1) _Horizon("Horizon",Color)=(.83,.88,.78,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Top,_Horizon;
 struct v2f{float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
 v2f vert(float4 p:POSITION){v2f o;o.pos=UnityObjectToClipPos(p);o.dir=p.xyz;return o;}
 fixed4 frag(v2f i):SV_Target{float3 d=normalize(i.dir);float t=pow(saturate(d.y),.55);float3 col=lerp(_Horizon.rgb,_Top.rgb,t);float sun=pow(saturate(dot(d,normalize(float3(-.45,.63,.52)))),420);col+=sun*float3(.95,.71,.35);return fixed4(col,1);}
 ENDCG }
 } Fallback Off
}
