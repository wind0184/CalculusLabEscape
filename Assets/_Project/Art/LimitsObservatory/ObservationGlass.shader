Shader "Calculus/Observation Glass" {
Properties { _Color("Tint",Color)=(0.06,0.22,0.3,0.08) }
SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
Pass {CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float2 uv:TEXCOORD2;};float4 _Color;
v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=WorldSpaceViewDir(v.vertex);o.uv=v.texcoord.xy;return o;}
fixed4 frag(v2f i):SV_Target{float rim=pow(1-abs(dot(normalize(i.normal),normalize(i.view))),4);float sheen=smoothstep(.45,.47,i.uv.x+i.uv.y*.3)*smoothstep(.66,.64,i.uv.x+i.uv.y*.3);return float4(_Color.rgb+rim*.15+sheen*.08,_Color.a+rim*.055+sheen*.025);}
ENDCG } } }
