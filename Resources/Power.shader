Shader "Hidden/Blit/Power"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Exponent ("Exponent", Range(0.0, 10.0)) = 2.0
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _Exponent;

            float4 frag(v2f i) : SV_Target
            {
                float4 originalColor = tex2D(_MainTex, i.uv);
                float3 poweredColor = pow(originalColor.rgb, _Exponent);

                return float4(poweredColor, originalColor.a);
            }
            ENDCG
        }
    }
}