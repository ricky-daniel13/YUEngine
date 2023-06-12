// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/JumballSolidShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Spheremap("Reflection", 2D) = "white" {}
        _Spheremap2("Glow", 2D) = "white" {}
        _Pow("Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                half3 normal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Pow;
            sampler2D _Spheremap;
            sampler2D _Spheremap2;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                float3 r = mul(UNITY_MATRIX_V, float4(i.normal, 0.0)).xyz;
                float2 sphUv = r.xy * 0.5 + 0.5;

                fixed4 colSph = tex2D(_Spheremap, sphUv);
                fixed4 colSph2 = tex2D(_Spheremap2, sphUv);

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col + (colSph*_Pow)+(colSph2*_Pow);
            }
            ENDCG
        }
    }
}

