Shader "Takoyaki/TakoyakiCinematic"
{
    Properties
    {
        [Header(Base Textures)]
        _MainTex ("Raw Batter (Albedo)", 2D) = "white" {}
        _CookedTex ("Cooked Texture", 2D) = "white" {}
        _BurntTex ("Burnt Texture", 2D) = "black" {}
        _NoiseTex ("Noise Map (Displacement & Variation)", 2D) = "gray" {}

        [Header(Cooking Status)]
        _CookLevel ("Cook Level (0-2)", Range(0, 2)) = 0.0
        _BatterAmount ("Batter Amount (0-1)", Range(0, 1)) = 1.0

        [Header(Cinematic Features)]
        _DisplacementStrength ("Puff Strength", Range(0, 0.2)) = 0.05
        _OilFresnel ("Oil Fresnel Power", Range(0, 10)) = 5.0
        _OilRoughness ("Oil Roughness", Range(0, 1)) = 0.2
        _SSSIntensity ("SSS Intensity", Range(0, 1)) = 0.5
        _SSSColor ("SSS Color", Color) = (1, 0.8, 0.6, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        // Physically based Standard lighting model, with vertex displacement function
        #pragma surface surf Standard vertex:vert fullforwardshadows addshadow

        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _CookedTex;
        sampler2D _BurntTex;
        sampler2D _NoiseTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
            float3 viewDir;
            float3 worldNormal;
        };

        half _CookLevel;
        half _BatterAmount;
        half _DisplacementStrength;
        half _OilFresnel;
        half _OilRoughness;
        half _SSSIntensity;
        fixed4 _SSSColor;

        // Vertex Displacement: Puff up the takoyaki as it cooks
        void vert (inout appdata_full v) 
        {
            // Read noise at vertex position (using texcoord for mapping)
            float noise = tex2Dlod(_NoiseTex, float4(v.texcoord.xy, 0, 0)).r;
            
            // Displacement logic:
            // Only displace if cooking has started (> 0.2)
            // Displacement increases with cook level up to a point, then maybe shrinks if burnt?
            float cookFactor = smoothstep(0.2, 1.0, _CookLevel);
            
            // Expand along normal
            // "Puff" unevenly based on noise
            float displacement = noise * _DisplacementStrength * cookFactor;
            
            v.vertex.xyz += v.normal * displacement;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // --- Texture Blending Logic ---
            float noise = tex2D (_NoiseTex, IN.uv_NoiseTex).r;
            float localCook = _CookLevel * (0.8 + noise * 0.4); // Uneven cooking

            fixed4 c_raw = tex2D (_MainTex, IN.uv_MainTex);
            fixed4 c_cooked = tex2D (_CookedTex, IN.uv_MainTex);
            fixed4 c_burnt = tex2D (_BurntTex, IN.uv_MainTex);

            // Raw -> Cooked
            float blend1 = smoothstep(0.2, 0.8, localCook);
            fixed4 albedo = lerp(c_raw, c_cooked, blend1);

            // Cooked -> Burnt
            float blend2 = smoothstep(1.0, 1.5, localCook);
            albedo = lerp(albedo, c_burnt, blend2);

            o.Albedo = albedo.rgb;

            // --- Oil / Fresnel Logic ---
            // Oiliness is high when raw, decreases slightly when cooked, then dry when burnt?
            // Actually, delicious takoyaki is oiled. 
            // Let's say Oil is visible on Cooked parts primarily.
            
            // Standard Smoothness
            float baseSmoothness = 0.3; // Raw batter is wet but matte?
            float cookedSmoothness = 1.0 - _OilRoughness; // Shiny
            
            o.Smoothness = lerp(baseSmoothness, cookedSmoothness, blend1);
            if (localCook > 1.2) o.Smoothness *= 0.5; // Burnt is dry

            // Fresnel effect for extra shiny edges (Oil Glaze)
            // Standard shader handles Fresnel via Smoothness/Metallic, but we can boost Emission for "Glaze" look
            float fresnel = dot(IN.worldNormal, IN.viewDir);
            fresnel = saturate(1.0 - fresnel);
            fresnel = pow(fresnel, _OilFresnel);
            
            // --- SSS (Subsurface Scattering) Fake ---
            // Raw batter transmits light. Cooked is opaque.
            // We simulate SSS by adding a fake glow relative to thickness/noise when Raw
            float sssMask = 1.0 - blend1; // Only for raw/semi-cooked
            fixed3 sssEffect = _SSSColor.rgb * _SSSIntensity * sssMask * (0.5 + noise * 0.5);
            
            // Combine Fresnel Glaze and SSS into Emission
            // Oil reflection adds to light, SSS adds inner glow
            fixed3 oilGlaze = fixed3(1,1,1) * fresnel * 0.5 * blend1; // Only glaze when cooked
            
            o.Emission = sssEffect + oilGlaze;
            
            o.Metallic = 0.0;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
