// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/EyeShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _PbrTex("PBR (RGB)", 2D) = "white" {}
        _EyeSpot("Eye spot(RGB)", 2D) = "white" {}
        _ChrEye1("Eye val 1", Vector) = (0.03,-0.05,0.01,0.01)
        _ChrEye2("Eye val 2", Vector) = (0.02,0.09,0.12,0.07)
        _ChrEye3("Eye val 3", Vector) = (0.1,0,0,0.1)
        _EyeSmooth ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows nolightmap nolppv vertex:vert

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _PbrTex;
        sampler2D _EyeSpot;

        struct Input
        {
            float2 uv_EyeSpot;
            float3 viewDir;
            float3 worldRefl;
            float3 eyeDir : TEXCOORD3;
        };

        half _EyeSmooth;
        fixed4 _Color;
        float4 _ChrEye1;
        float4 _ChrEye2;
        float4 _ChrEye3;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input o){
            UNITY_INITIALIZE_OUTPUT(Input, o);
            //TANGENT_SPACE_ROTATION;
            //o.eyeDir = mul(unity_ObjectToWorld, float4(0, 0, 1, 0));
            //o.eyeDir = mul(rotation, float4(0, 0, 1, 0));
            //o.eyeDir = mul(mul(float4(output.eyeNormal.xyz, 0), g_MtxWorld), g_MtxView).xyz;
            o.eyeDir = mul(UNITY_MATRIX_V, mul(unity_ObjectToWorld, float4(0, 0, 1, 0))).xyz;
            //o.eyeDir = UnityObjectToClipPos(float4(0, 0, 1, 0)).xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_EyeSpot) * _Color;
            //o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = 0;
            o.Smoothness = _EyeSmooth;
            o.Alpha = 1;

            float3 eyeNormal = normalize(IN.eyeDir.xyz);
            float2 directionOffset = float2(dot(eyeNormal, float3(-1, 0, 0)), dot(eyeNormal, float3(0, 1, 0)));
            float2 diffuseAlphaOffset = -(_ChrEye1.zw * directionOffset);
            float2 reflectionOffset = _ChrEye3.xy + _ChrEye3.zw * directionOffset;
            float2 reflectionAlphaOffset = _ChrEye1.xy - float2(directionOffset.x < 0 ? _ChrEye2.x : _ChrEye2.y, directionOffset.y < 0 ? _ChrEye2.z : _ChrEye2.w) * directionOffset;


            float diffuseAlpha = tex2D(_MainTex, IN.uv_EyeSpot + diffuseAlphaOffset).a;
            float3 reflection = tex2D(_EyeSpot, IN.uv_EyeSpot + reflectionOffset).rgb;
            float3 reflectionAlpha = tex2D(_EyeSpot, IN.uv_EyeSpot + reflectionAlphaOffset).a;

            o.Albedo = lerp((c.rgb + reflection * (tex2D(_PbrTex, IN.uv_EyeSpot).w)) * diffuseAlpha, 1, reflectionAlpha);
            o.Emission = lerp(0, 1, reflectionAlpha);
            //o.Albedo = lerp(0, 1, float4(directionOffset, 0, 1));
            //material.ao = lerp(material.ao, 1, reflectionAlpha);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
