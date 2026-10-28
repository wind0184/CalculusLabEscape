Shader "Calculus/Observatory Sky" {
Properties { _Tint("Tint",Color)=(0.015,0.035,0.075,1) }
SubShader { Tags {"Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox"} Cull Off ZWrite Off
Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct v2f {float4 pos:SV_POSITION;float3 dir:TEXCOORD0;};
float4 _Tint;
v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.dir=v.vertex.xyz;return o;}
float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
fixed4 frag(v2f i):SV_Target{
 float3 d=normalize(i.dir);float band=pow(saturate(1-abs(d.y*.7+d.x*.3-.2)),14);
 float3 col=_Tint.rgb+band*float3(.011,.025,.045);
 float3 p=d*240;float3 cell=floor(p);float h=hash(cell);
 float star=pow(saturate(1-length(frac(p)-.5)*2),14)*step(.976,h);
 float3 q=d*490;float fine=pow(saturate(1-length(frac(q)-.5)*2),18)*step(.988,hash(floor(q)));
 return float4(col+star*float3(.6,.78,1)*3+fine*2,1);
}
ENDCG } } }
