Shader "Custom/AdventureShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BumpMap("Normal map (RGB)", 2D) = "bump" {}
        _SpecGloss("Smoothness (RGB)", 2D) = "black" {}
        _Falloff ("Fur Falloff", 2D) = "white" {}
        _SmoothnessFrm("Smoothness From", Range(0,1)) = 0
        _Smoothness ("Smoothness Range", Range(0,1)) = 1.0
        [HDR]_RimColor("Rim Color", Color) = (1,1,1,1)
        _NormalPower("NormalPower", Range(0,4.0)) = 1.0
        _RimMul("Rim Multiplication", Float) = 1
        _RimOFrom("Rim Range From Min", Float) = 0
        _RimOTo("Rim Range From Max", Float) = 1
        _RimTFrom("Rim Range To Min", Float) = 0
        _RimTTo("Rim Range To Max", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows nolightmap nolppv

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _SpecGloss;
        sampler2D _Falloff;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            float3 viewDir;
            float4 color : COLOR;
            float3 worldRefl;
        };

        fixed4 _Color;
        fixed4 _RimColor;
        half _SmoothnessFrm;
        half _Smoothness;
        half _NormalPower;
        half _RimMul;
        float _RimOFrom;
        float _RimOTo;
        float _RimTFrom;
        float _RimTTo;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        float4 invLerp(float4 from, float4 to, float4 value) {
            return (value - from) / (to - from);
        }

        float4 remap(float4 origFrom, float4 origTo, float4 targetFrom, float4 targetTo, float4 value) {
            float4 rel = invLerp(origFrom, origTo, value);
            return lerp(targetFrom, targetTo, rel);
        }


        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * IN.color;
            fixed4 spc = tex2D(_SpecGloss, IN.uv_MainTex);
            fixed4 fur = tex2D(_Falloff, IN.uv_MainTex);
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = 0;
            o.Smoothness = spc.a* _Smoothness;//max(remap(_SmoothnessFrm, _Smoothness, 0, 1, spc.a),0);
            //o.Albedo = max(remap(_SmoothnessFrm, _Smoothness, 0, 1, spc.a), 0);
            o.Alpha = 1;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            /*float4 envSample = UNITY_SAMPLE_TEXCUBE_LOD(
                unity_SpecCube0, IN.worldRefl, roughness * UNITY_SPECCUBE_LOD_STEPS
            );*/
            float3 shl = ShadeSH9(float4(o.Normal, 1));
            o.Emission = ((_RimColor*shl) * max(remap(_RimOFrom, _RimOTo, _RimTFrom, _RimTTo, rim),0)) * _RimMul * fur;
            o.Normal = UnpackNormalWithScale(tex2D(_BumpMap, IN.uv_BumpMap), _NormalPower);
        }

        ENDCG

            
    }
    FallBack "Diffuse"
}
