Shader "Custom/SkewShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _SkewDir("Skew Direction", Vector) = (0,0,1,0)
        _SkewUp("Skew UpDir", Vector) = (0,0,1,0)
        _SkewFrom("From", Vector) = (0,0,0,0)
        _Amount("Ammount", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float4 _SkewDir;
        float4 _SkewUp;
        half _Amount;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v) {
            v.vertex.xyz += _SkewDir * (dot(_SkewUp, v.vertex.xyz))* _Amount;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG


            // Pass to render object as a shadow caster
        Pass{
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Fog {Mode Off}
            ZWrite On ZTest LEqual Cull Off
            Offset 1, 1

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma fragmentoption ARB_precision_hint_fastest
            #include "UnityCG.cginc"

            float4 _SkewDir;
            float4 _SkewUp;
            half _Amount;

            struct v2f {
                V2F_SHADOW_CASTER;
            };

            v2f vert(appdata_base v) {
                v.vertex.xyz += _SkewDir * (dot(_SkewUp, v.vertex.xyz)) * _Amount;
                v2f o;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }

            float4 frag(v2f i) : COLOR {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }

            // Pass to render object as a shadow collector
        Pass{
            Name "ShadowCollector"
            Tags { "LightMode" = "ShadowCollector" }

            Fog {Mode Off}
            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma multi_compile_shadowcollector

            #define SHADOW_COLLECTOR_PASS
            #include "UnityCG.cginc"

            float4 _SkewDir;
            float4 _SkewUp;
            half _Amount;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                V2F_SHADOW_COLLECTOR;
            };

            v2f vert(appdata v) {
                v.vertex.xyz += _SkewDir * (dot(_SkewUp, v.vertex.xyz)) * _Amount;
                v2f o;
                TRANSFER_SHADOW_CASTER(o)
                return o;
            }

            fixed4 frag(v2f i) : COLOR {
                SHADOW_COLLECTOR_FRAGMENT(i)
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
