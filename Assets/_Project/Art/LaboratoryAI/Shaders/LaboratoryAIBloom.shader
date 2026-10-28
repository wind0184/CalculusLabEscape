Shader "Hidden/CalculusLab/AI/Bloom"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _BloomTex;
        float4 _MainTex_TexelSize, _BlurDirection;
        float _Threshold, _Intensity;
        half4 extract(v2f_img i):SV_Target
        {
            half3 c=tex2D(_MainTex,i.uv).rgb;
            half brightness=max(c.r,max(c.g,c.b));
            return half4(c*max(0,brightness-_Threshold)/max(brightness,.0001),1);
        }
        half4 blur(v2f_img i):SV_Target
        {
            float2 step=_MainTex_TexelSize.xy*_BlurDirection.xy;
            half3 c=tex2D(_MainTex,i.uv).rgb*.4;
            c+=(tex2D(_MainTex,i.uv+step).rgb+tex2D(_MainTex,i.uv-step).rgb)*.24;
            c+=(tex2D(_MainTex,i.uv+step*2).rgb+tex2D(_MainTex,i.uv-step*2).rgb)*.06;
            return half4(c,1);
        }
        half4 composite(v2f_img i):SV_Target { return half4(tex2D(_MainTex,i.uv).rgb+tex2D(_BloomTex,i.uv).rgb*_Intensity,1); }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment extract
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment blur
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment composite
            ENDCG
        }
    }
}
