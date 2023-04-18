Shader "Unlit/Sky_TransMaskAdd"
{
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _AlphaTex("Mask", 2D) = "white" {}
        _EmmPower("EmmFactor", Range(0,100)) = 0.0
        _ScrollXSpeed("XSpeed", Range(0,10)) = 2

        _zStart("ZStart", Float) = 10000.0
        _zEnd("ZEnd", Float) = 10100.0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend From", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend To", Float) = 0
    }

    SubShader{
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Offset[_zStart],[_zEnd]
        ZWrite Off
        Blend[_SrcBlend][_DstBlend]

        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    float2 texcoord1 : TEXCOORD2;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    float2 texcoord1 : TEXCOORD2;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                sampler2D _MaskTex;
                sampler2D _AlphaTex;
                float4 _MainTex_ST;
                float _EmmPower;
                float4 _AlphaTex_ST;
                float _ScrollXSpeed;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.texcoord1 = TRANSFORM_TEX(v.texcoord1, _AlphaTex);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float4 finCol = 1.0;

                    fixed xScrollValue = _ScrollXSpeed * _Time;
                    i.texcoord+=fixed2(xScrollValue, 0);

                    finCol.rgb = tex2D(_MainTex, i.texcoord) * 0.5;
                    finCol.rgb = saturate(finCol.rgb);
                    finCol.a = tex2D(_MaskTex, i.texcoord).r;

                    finCol.rgb += tex2D(_AlphaTex, i.texcoord1).rgb;
                    finCol.rgb *= _EmmPower;
                    //finCol.rgb *= finCol.a;

                    //col = tex2D(_AlphaTex, i.texcoord1);
                    //UNITY_OPAQUE_ALPHA(col.a);
                    return finCol;
                }
            ENDCG
        }
    }

}