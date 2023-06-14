Shader "Custom/SonicShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _SpecGloss("Smoothness (RGB)", 2D) = "white" {}
        _BumpMap("Bump Map", 2D) = "normal" {}
        _Metalness("Metallic", Float) = 0
        _BumpPower("Normal Power", Float) = 0
        _SmoothOFrom("Rim Range From Min", Float) = 0
        _SmoothOTo("Rim Range From Max", Float) = 1
        _SmoothTFrom("Rim Range To Min", Float) = 0
        _SmoothTTo("Rim Range To Max", Float) = 1
        [Toggle(_PREVIEWSMOOTH)] _PREVIEWSMOOTH("Preview Smoothness", Float) = 0.0
        [Toggle(_HasBumpMap)] _HasBumpMap("Use normal map", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows nolightmap nolppv 
        #pragma shader_feature _PREVIEWSMOOTH
        #pragma shader_feature _HasBumpMap

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _SpecGloss;
#if defined(_HasBumpMap)
        sampler2D _BumpMap;
#endif

        struct Input
        {
            float2 uv_MainTex;
        };

        fixed4 _Color;
        half _Metalness;
        half _BumpPower;
        float _SmoothOFrom;
        float _SmoothOTo;
        float _SmoothTFrom;
        float _SmoothTTo;

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
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            fixed4 spc = tex2D(_SpecGloss, IN.uv_MainTex);
            o.Metallic = _Metalness;
            o.Occlusion = 1;
            o.Smoothness = max(remap(_SmoothOFrom, _SmoothOTo, _SmoothTFrom, _SmoothTTo, spc.r), 0);
            #if defined(_PREVIEWSMOOTH)
            o.Albedo = (max(remap(_SmoothOFrom, _SmoothOTo, _SmoothTFrom, _SmoothTTo, spc.r), 0)).rrr;
            o.Emission = (max(remap(_SmoothOFrom, _SmoothOTo, _SmoothTFrom, _SmoothTTo, spc.r), 0)).rrr;
            #else
            o.Albedo = c.rgb;
            #endif
            o.Alpha = 1;

            #if defined(_HasBumpMap)
            o.Normal = UnpackNormalWithScale(tex2D(_BumpMap, IN.uv_MainTex), _BumpPower);
            #endif
        }
        ENDCG
    }
    FallBack "Diffuse"
}
