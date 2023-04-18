Shader "Custom/SonicShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _SpecGloss("Specular and Smoothness (RGB)", 2D) = "black" {}
        _Falloff ("Fur Falloff", 2D) = "white" {}
        _Smoothness ("Smoothness Range", Range(0,1)) = 0.0
        _HairSmooth("Fur Smoothness", Range(0,1)) = 0.0
        _RimPower("Rim Power", Range(0.5,16.0)) = 3.0
        _RimSpec("Rim Spec", Range(0,2)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf StandardSpecular fullforwardshadows nolightmap nolppv

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _SpecGloss;
        sampler2D _Falloff;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float4 color : COLOR;
            float3 worldRefl;
        };

        fixed4 _Color;
        half _Smoothness;
        half _HairSmooth;
        half _RimPower;
        half _RimSpec;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandardSpecular o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color * IN.color;
            fixed4 spc = tex2D(_SpecGloss, IN.uv_MainTex);
            fixed4 fur = tex2D(_Falloff, IN.uv_MainTex);
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Specular = spc.rgb;
            o.Smoothness = spc.a*_Smoothness;
            o.Alpha = 1;
            half rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            float roughness = 1 - (_HairSmooth);
            roughness *= 1.7 - 0.7 * roughness;
            float4 envSample = UNITY_SAMPLE_TEXCUBE_LOD(
                unity_SpecCube0, IN.worldRefl, roughness * UNITY_SPECCUBE_LOD_STEPS
            );
            o.Emission = ((fur * envSample) * pow(rim, _RimPower)) * _RimSpec;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
