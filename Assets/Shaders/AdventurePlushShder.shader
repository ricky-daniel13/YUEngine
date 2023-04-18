Shader "Custom/AdventurePlushShader"
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
        #include  "UnityPBSLighting.cginc"
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Minnaert fullforwardshadows nolightmap nolppv

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

        // Main Physically Based BRDF
        // Derived from Disney work and based on Torrance-Sparrow micro-facet model
        //
        //   BRDF = kD / pi + kS * (D * V * F) / 4
        //   I = BRDF * NdotL
        //
        // * NDF (depending on UNITY_BRDF_GGX):
        //  a) Normalized BlinnPhong
        //  b) GGX
        // * Smith for Visiblity term
        // * Schlick approximation for Fresnel
        half4 BRDFSilk_Unity_PBS(half3 diffColor, half3 specColor, half oneMinusReflectivity, half smoothness,
            float3 normal, float3 viewDir,
            UnityLight light, UnityIndirect gi)
        {
            float perceptualRoughness = SmoothnessToPerceptualRoughness(smoothness);
            float3 halfDir = Unity_SafeNormalize(float3(light.dir) + viewDir);

            half nv = abs(dot(normal, viewDir));    // This abs allow to limit artifact

            float nl = saturate(dot(normal, light.dir));
            float nh = saturate(dot(normal, halfDir));

            half lv = saturate(dot(light.dir, viewDir));
            half lh = saturate(dot(light.dir, halfDir));

            // Diffuse term
            //half diffuseTerm = DisneyDiffuse(nv, nl, lh, perceptualRoughness) * nl;
            half diffuseTerm = saturate(nl * pow(nl * nv, perceptualRoughness));

            // Specular term
            // HACK: theoretically we should divide diffuseTerm by Pi and not multiply specularTerm!
            // BUT 1) that will make shader look significantly darker than Legacy ones
            // and 2) on engine side "Non-important" lights have to be divided by Pi too in cases when they are injected into ambient SH
            float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
#if UNITY_BRDF_GGX
            // GGX with roughtness to 0 would mean no specular at all, using max(roughness, 0.002) here to match HDrenderloop roughtness remapping.
            roughness = max(roughness, 0.002);
            float V = SmithJointGGXVisibilityTerm(nl, nv, roughness);
            float D = GGXTerm(nh, roughness);
#else
            // Legacy
            half V = SmithBeckmannVisibilityTerm(nl, nv, roughness);
            half D = NDFBlinnPhongNormalizedTerm(nh, PerceptualRoughnessToSpecPower(perceptualRoughness));
#endif

            float specularTerm = V * D * UNITY_PI; // Torrance-Sparrow model, Fresnel is applied later

#   ifdef UNITY_COLORSPACE_GAMMA
            specularTerm = sqrt(max(1e-4h, specularTerm));
#   endif

            // specularTerm * nl can be NaN on Metal in some cases, use max() to make sure it's a sane value
            specularTerm = max(0, specularTerm * nl);
#if defined(_SPECULARHIGHLIGHTS_OFF)
            specularTerm = 0.0;
#endif

            // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
            half surfaceReduction;
#   ifdef UNITY_COLORSPACE_GAMMA
            surfaceReduction = 1.0 - 0.28 * roughness * perceptualRoughness;      // 1-0.28*x^3 as approximation for (1/(x^4+1))^(1/2.2) on the domain [0;1]
#   else
            surfaceReduction = 1.0 / (roughness * roughness + 1.0);           // fade \in [0.5;1]
#   endif

    // To provide true Lambert lighting, we need to be able to kill specular completely.
            specularTerm *= any(specColor) ? 1.0 : 0.0;

            half grazingTerm = saturate(smoothness + (1 - oneMinusReflectivity));
            half3 color = diffColor * (gi.diffuse + light.color * diffuseTerm)
                + specularTerm * light.color * FresnelTerm(specColor, lh)
                + surfaceReduction * gi.specular * FresnelLerp(specColor, grazingTerm, nv);

            return half4(color, 1);
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

        inline half4 LightingMinnaert(SurfaceOutputStandard s, half3 viewDir, UnityGI gi) {
            s.Normal = normalize(s.Normal);

            half oneMinusReflectivity;
            half3 specColor;
            s.Albedo = DiffuseAndSpecularFromMetallic(s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

            // shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
            // this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
            half outputAlpha;
            s.Albedo = PreMultiplyAlpha(s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

            half4 c = BRDFSilk_Unity_PBS(s.Albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Normal, viewDir, gi.light, gi.indirect);
            c.a = outputAlpha;
            return c;
        }
        void LightingMinnaert_GI(SurfaceOutputStandard s, UnityGIInput data, inout UnityGI gi) {
            LightingStandard_GI(s, data, gi);
        }


        ENDCG

            
    }
    FallBack "Diffuse"
}
