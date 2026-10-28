Shader "CalculusLabEscape/UI/ScreenTransition"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Cover colour",Color)=(.035,.08,.105,1)
        _EdgeColor("Edge colour",Color)=(.48,.85,.9,.45)
        _Progress("Coverage",Range(0,1))=0
        _Effect("Circle / Fade / Diagonal",Float)=0
        _Aspect("Screen aspect",Float)=1.77778
        _Feather("Edge softness",Range(.001,.03))=.006
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
    }
    SubShader
    {
        Tags {"Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            fixed4 _Color,_EdgeColor;
            float _Progress,_Effect,_Aspect,_Feather;
            v2f vert(appdata input){v2f output;output.vertex=UnityObjectToClipPos(input.vertex);output.color=input.color;output.uv=input.uv;return output;}
            fixed4 frag(v2f input):SV_Target
            {
                if(_Progress<=.00001)return 0;
                if(_Progress>=.99999)return _Color*input.color;
                float mask,edge=0;
                if(_Effect<.5)
                {
                    float2 position=(input.uv-.5)*float2(_Aspect,1);
                    float distance=length(position);
                    float radius=(sqrt(_Aspect*_Aspect+1)*.5+_Feather)*(1-_Progress);
                    mask=smoothstep(radius-_Feather,radius+_Feather,distance);
                    edge=1-smoothstep(0,_Feather*1.5,abs(distance-radius));
                }
                else if(_Effect<1.5)mask=_Progress;
                else
                {
                    float diagonal=(input.uv.x+input.uv.y*.24)/1.24;
                    mask=smoothstep(1-_Progress-_Feather,1-_Progress+_Feather,diagonal);
                    edge=1-smoothstep(0,_Feather*1.5,abs(diagonal-(1-_Progress)));
                }
                fixed4 result=_Color;result.rgb=lerp(result.rgb,_EdgeColor.rgb,edge*_EdgeColor.a);
                result.a*=mask;return result*input.color;
            }
            ENDCG
        }
    }
}
