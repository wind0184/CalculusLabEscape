Shader "Calculus/Observatory Gate" {
Properties { _Progress("Calibrated",Range(0,1))=0 }
SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};float _Progress;
v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
fixed4 frag(v2f i):SV_Target{
 float2 p=(i.uv-.5)*2;float r=length(p);clip(1-r);float a=atan2(p.y,p.x);
 float wave=sin(r*25-_Time.y*1.3+sin(a*4+_Time.y*.25)*.3)*.5+.5;
 float rim=pow(r,9);float grid=pow(abs(sin(p.x*13)*sin(p.y*13)),28)*.07;
 float3 col=lerp(float3(.015,.065,.09),float3(.035,.28,.36),_Progress)*(.5+wave*.35)+rim*float3(.09,.55,.65)+grid;
 return float4(col,.86);
}
ENDCG } } }
