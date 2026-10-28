Shader "Calculus/Valley Flowing Water" {
 Properties { _Color("Water",Color)=(.08,.47,.49,1) _Deep("Deep water",Color)=(.025,.24,.29,1) _Foam("Ripples",Color)=(.66,.89,.80,1) _Fall("Waterfall",Range(0,1))=0 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 fixed4 _Color,_Deep,_Foam;half _Fall;
 struct Input {float3 worldPos;};
 void surf(Input i,inout SurfaceOutputStandard o){float2 p=i.worldPos.xz;float wave=sin(p.x*.7+_Time.y*.8)*sin(p.y*2.3-_Time.y*.9);float ripple=pow(saturate(sin(p.x*2.7+p.y*4-_Time.y*2.6)),26)*.23;float falls=pow(saturate(sin(i.worldPos.y*8+_Time.y*4+p.y*2)),3);float3 col=lerp(_Deep.rgb,_Color.rgb,.55+wave*.18);col=lerp(col,_Foam.rgb,ripple);col=lerp(col,lerp(_Color.rgb,_Foam.rgb,.62+falls*.24),_Fall);o.Albedo=col;o.Metallic=.08;o.Smoothness=.72;o.Emission=_Fall*_Foam.rgb*.08;o.Alpha=1;}
 ENDCG
 } Fallback "Standard"
}
