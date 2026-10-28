Shader "CalculusLabEscape/Simulation Sky"
{
    Properties
    {
        _TopColor ("Upper sky", Color) = (0.012,0.045,0.085,1)
        _HorizonColor ("Horizon glow", Color) = (0.07,0.21,0.27,1)
        _BottomColor ("Lower space", Color) = (0.008,0.025,0.045,1)
        _GridColor ("Simulation grid", Color) = (0.08,0.62,0.76,1)
        _GridStrength ("Grid visibility", Range(0,0.6)) = 0.23
        _GridWidth ("Grid line width", Range(0.3,2)) = 0.7
        _Longitudes ("Vertical grid divisions", Range(8,48)) = 24
        _Latitudes ("Horizontal grid divisions", Range(4,24)) = 12
        _ScanStrength ("Scan glow", Range(0,0.2)) = 0.035
        _ScanSpeed ("Scan speed", Range(0,0.1)) = 0.025
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            float4 _TopColor,_HorizonColor,_BottomColor,_GridColor;
            float _GridStrength,_GridWidth,_Longitudes,_Latitudes,_ScanStrength,_ScanSpeed;
            v2f vert(appdata input)
            {
                v2f output;output.position=UnityObjectToClipPos(input.vertex);output.direction=input.vertex.xyz;return output;
            }
            float gridLine(float coordinate)
            {
                float distance=abs(frac(coordinate+0.5)-0.5);
                return 1-smoothstep(0,max(fwidth(coordinate)*_GridWidth,0.0001),distance);
            }
            fixed4 frag(v2f input) : SV_Target
            {
                float3 direction=normalize(input.direction);
                float3 sky=lerp(_HorizonColor.rgb,_TopColor.rgb,pow(saturate(direction.y),0.55));
                sky=lerp(sky,_BottomColor.rgb,saturate(-direction.y*1.4));
                float longitude=atan2(direction.x,direction.z)/6.2831853+0.5;
                float latitude=asin(clamp(direction.y,-1,1))/3.14159265+0.5;
                float grid=max(gridLine(longitude*_Longitudes),gridLine(latitude*_Latitudes));
                float polarFade=1-smoothstep(0.78,0.98,abs(direction.y));
                sky+=_GridColor.rgb*grid*_GridStrength*polarFade;
                float sweep=frac(_Time.y*_ScanSpeed+0.35)*1.35-0.25;
                float scan=exp(-abs(direction.y-sweep)*100)*_ScanStrength;
                sky+=_GridColor.rgb*scan;
                return fixed4(sky,1);
            }
            ENDCG
        }
    }
    Fallback Off
}
