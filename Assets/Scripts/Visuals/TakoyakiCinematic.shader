Shader "Takoyaki/TakoyakiCinematic"
{
    Properties
    {
        [Header(Base Textures)]
        _MainTex ("Raw Batter (Albedo)", 2D) = "white" {}
        _CookedTex ("Cooked (Albedo)", 2D) = "white" {}
        _BurntTex ("Burnt (Albedo)", 2D) = "black" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}

        [Header(Cooking State)]
        _CookLevel ("Cook Level (0-2)", Range(0, 2)) = 0.0
        
        [Header(Subsurface Scattering)]
        _SSSColor ("SSS Color", Color) = (1, 0.8, 0.6, 1)
        _SSSIntensity ("SSS Intensity", Range(0, 1)) = 0.5
        _SSSDistortion ("SSS Distortion", Range(0, 1)) = 0.2

        [Header(Oil & Glaze)]
        _OilColor ("Oil Color", Color) = (1, 1, 1, 1)
        _OilRoughness ("Oil Roughness", Range(0, 1)) = 0.1
        _OilFresnel ("Oil Fresnel Power", Range(0.1, 10)) = 5.0

        [Header(Displacement)]
        _NoiseTex ("Displacement Noise", 2D) = "gray" {}
        _DisplacementStrength ("Displacement Strength", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert

        // Use Shader Model 3.0 for better lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _CookedTex;
        sampler2D _BurntTex;
        sampler2D _BumpMap;
        sampler2D _NoiseTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
            float3 viewDir;
            float3 worldNormal;
        };

        half _CookLevel;
        half _OilRoughness;
        half _OilFresnel;
        fixed4 _OilColor;
        fixed4 _SSSColor;
        half _SSSIntensity;
        half _SSSDistortion;
        half _DisplacementStrength;

        void vert (inout appdata_full v) {
            // Vertex Displacement logic (Bubbling effect)
            // Bubbling happens mostly in cooked state (0.5 - 1.2)
            float bubbleFactor = smoothstep(0.4, 0.8, _CookLevel) * (1.0 - smoothstep(1.2, 1.5, _CookLevel));
            
            float noise = tex2Dlod(_NoiseTex, float4(v.texcoord.xy, 0, 0)).r;
            v.vertex.xyz += v.normal * noise * _DisplacementStrength * bubbleFactor;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. Multifaceted Blending (Raw -> Cooked -> Burnt)
            fixed4 c_raw = tex2D (_MainTex, IN.uv_MainTex);
            fixed4 c_cooked = tex2D (_CookedTex, IN.uv_MainTex);
            fixed4 c_burnt = tex2D (_BurntTex, IN.uv_MainTex);
            
            // Noise for cook variation
            float cookNoise = tex2D(_NoiseTex, IN.uv_NoiseTex).r;
            float localCook = _CookLevel * (0.9 + cookNoise * 0.2);

            float blend1 = smoothstep(0.3, 0.8, localCook);
            float blend2 = smoothstep(1.0, 1.6, localCook);
            
            fixed4 baseAlbedo = lerp(c_raw, c_cooked, blend1);
            baseAlbedo = lerp(baseAlbedo, c_burnt, blend2);
            
            o.Albedo = baseAlbedo.rgb;
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_MainTex));

            // 2. Oil / Specular
            // Raw is wet (smooth), Cooked is crispy (rougher), Burnt is dry (rough)
            // But oil layer stays on top.
            float roughness = lerp(0.2, 0.8, blend1); // Becomes crisper
            roughness = lerp(roughness, 1.0, blend2); // Becomes dry
            
            // Oil Fresnel
            float fresnel = pow(1.0 - dot(IN.worldNormal, IN.viewDir), _OilFresnel);
            // Oiliness adds gloss back
            float oilFactor = 0.3; // Constant oil base
            
            o.Smoothness = (1.0 - roughness) + fresnel * oilFactor;
            o.Metallic = 0.0;

            // 3. Subsurface Scattering (Fake Emission)
            // Only for Raw/Semi-cooked batter
            // Light transmits through thin edges
            float sssFactor = 1.0 - blend1; // Disappears when cooked
            
            // Simple inverted normal approximation (Wrap lighting) - simplified here as Emission
            // Real SSS requires custom lighting model, but Emission + Fresnel works for "Glow"
            float backlight = pow(1.0 - dot(IN.viewDir, -IN.worldNormal), 2.0); // Fake transluency
            o.Emission = _SSSColor * _SSSIntensity * sssFactor * backlight; 
        }
        ENDCG
    }
    FallBack "Diffuse"
}
