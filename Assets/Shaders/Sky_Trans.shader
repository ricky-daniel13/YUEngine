Shader "Unlit/Sky_Trans"
{
    Properties{
        _MainTex("Base (RGB)", 2D) = "white" {}
        _AlphaTex("Alpha", 2D) = "white" {}
        _EmmPower("EmmFactor", Range(0,100)) = 0.0
        _zStart("ZStart", Float) = 10000.0
        _zEnd("ZEnd", Float) = 10100.0
        _ScrollXSpeed("XSpeed", Range(0,10)) = 2
        // Blend mode values
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Blend From", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Blend To", Float) = 0
            // Will set "ENABLE_FANCY" shader keyword when set.
        [Toggle(USE_VCOL)] _VCol("Use Vertex Color", Float) = 0
        [Toggle(USE_MAlpha)] _MAlpha("Alpha From MainText", Float) = 0
    }

    SubShader{
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Offset [_zStart],[_zEnd]
        ZWrite Off
        Blend [_SrcBlend] [_DstBlend]

        Pass {
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile _ USE_VCOL
                #pragma multi_compile _ USE_MAlpha

                #include "UnityCG.cginc"

                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    #ifdef USE_VCOL
                        float4 color : COLOR;
                    #endif
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    float2 texcoord : TEXCOORD0;
                    #ifdef USE_VCOL
                        float4 color : COLOR;
                    #endif
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                sampler2D _AlphaTex;
                float4 _MainTex_ST;
                float _EmmPower;
                float _ScrollXSpeed;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    #ifdef USE_VCOL
                        o.color = v.color;
                    #endif
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float4 finCol = 1.0;

                    fixed xScrollValue = _ScrollXSpeed * _Time;
                    i.texcoord+=fixed2(xScrollValue, 0);

                    #ifdef USE_VCOL
                        finCol.rgb = (tex2D(_MainTex, i.texcoord).rgb*i.color) * 0.5;
                    #else
                        finCol.rgb = (tex2D(_MainTex, i.texcoord).rgb) * 0.5;
                    #endif
                    finCol.rgb = saturate(finCol.rgb);
                    #ifdef USE_MAlpha
                        finCol.a = tex2D(_MainTex, i.texcoord).a;
                    #else
                        finCol.a = tex2D(_AlphaTex, i.texcoord).r;
                    #endif

                    finCol.rgb *= _EmmPower;
                    return finCol;
                }
            ENDCG
        }
    }

}