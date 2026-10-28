Shader "Calculus/Observatory Planet" {
SubShader { Tags {"RenderType"="Opaque"} Pass { CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct v2f {float4 pos:SV_POSITION;float3 n:TEXCOORD0;float3 view:TEXCOORD1;};
v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.n=v.normal;o.view=ObjSpaceViewDir(v.vertex);return o;}
float h(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(h(a),h(a+float3(1,0,0)),f.x),lerp(h(a+float3(0,1,0)),h(a+float3(1,1,0)),f.x),f.y),lerp(lerp(h(a+float3(0,0,1)),h(a+float3(1,0,1)),f.x),lerp(h(a+float3(0,1,1)),h(a+1),f.x),f.y),f.z);}
fixed4 frag(v2f i):SV_Target{
 float3 n=normalize(i.n);float bands=sin(n.y*37+n.x*6+noise(n*10)*2.5)*.5+.5;
 float fine=noise(n*37)*.07;float3 col=lerp(float3(.022,.09,.16),float3(.15,.36,.43),smoothstep(.05,.95,bands)+fine);
 float clouds=pow(saturate(sin(n.y*79+n.x*13+noise(n*25)*2)*.5+.5),12);col=lerp(col,float3(.28,.43,.49),clouds*.22);
 float light=saturate(dot(n,normalize(float3(-1,1,-1))));col*=.18+light*.85;
 float rim=pow(1-saturate(dot(n,normalize(i.view))),3);col+=rim*float3(.03,.27,.46);
 return float4(col,1);
}
ENDCG } } }
