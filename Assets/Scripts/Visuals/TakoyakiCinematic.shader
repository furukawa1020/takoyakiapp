Shader "Takoyaki/TakoyakiCinematic"
{
    Properties
    {
        [Header(Base Textures)]
        _MainTex ("Raw Batter", 2D) = "white" {}
        _CookedTex ("Cooked", 2D) = "white" {}
        _BurntTex ("Burnt", 2D) = "black" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}

        [Header(Cooking State)]
        _CookLevel ("Cook Level", Range(0, 2)) = 0.0
        _BatterAmount ("Batter Amount", Range(0, 1)) = 1.0
        
        [Header(Subsurface Scattering)]
        _SSSColor ("SSS Color", Color) = (1, 0.8, 0.6, 1)
        _SSSIntensity ("SSS Intensity", Range(0, 2)) = 0.8  // Values > 1.0 create dramatic glow for stylized look
        _SSSPower ("SSS Power", Range(1, 8)) = 3.0

        [Header(Oil and Glaze)]
        _OilColor ("Oil Color", Color) = (1, 0.95, 0.85, 1)
        _OilRoughness ("Oil Roughness", Range(0, 1)) = 0.05
        _OilFresnel ("Oil Fresnel Power", Range(0.1, 10)) = 3.5
        _GlazeIntensity ("Glaze Intensity", Range(0, 2)) = 1.2

        [Header(Displacement)]
        _NoiseTex ("Displacement Noise", 2D) = "gray" {}
        _DisplacementStrength ("Displacement Strength", Range(0, 0.2)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        // Physically based Standard lighting model, with vertex displacement
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
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
            INTERNAL_DATA
        };

        half _CookLevel;
        half _BatterAmount; 
        half _OilRoughness;
        half _OilFresnel;
        half _GlazeIntensity;
        half _SSSPower;
        fixed4 _OilColor;
        fixed4 _SSSColor;
        half _SSSIntensity;
        half _DisplacementStrength;

        // Vertex Modifier
        void vert (inout appdata_full v) {
            float noise = tex2Dlod(_NoiseTex, float4(v.texcoord.xy, 0, 0)).r;
            float puff = smoothstep(0.0, 1.0, _CookLevel) * 0.5 + 0.5;
            v.vertex.xyz += v.normal * noise * _DisplacementStrength * puff;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. Textures & Blending
            fixed4 c_raw = tex2D (_MainTex, IN.uv_MainTex);
            fixed4 c_cooked = tex2D (_CookedTex, IN.uv_MainTex);
            fixed4 c_burnt = tex2D (_BurntTex, IN.uv_MainTex);
            
            float cookNoise = tex2D(_NoiseTex, IN.uv_NoiseTex).r;
            float localCook = _CookLevel * (0.8 + cookNoise * 0.4);

            float toCooked = smoothstep(0.2, 0.8, localCook);
            float toBurnt = smoothstep(1.2, 1.8, localCook);
            
            fixed4 albedo = lerp(c_raw, c_cooked, toCooked);
            albedo = lerp(albedo, c_burnt, toBurnt);
            
            o.Albedo = albedo.rgb;
            o.Alpha = albedo.a;
            o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_MainTex));

            // 2. Oil / Specular with Enhanced Glaze
            float baseRoughness = lerp(0.3, 0.9, toCooked); 
            
            // Fix: Use WorldNormalVector when writing to o.Normal
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            float fresnel = pow(1.0 - saturate(dot(worldNormal, normalize(IN.viewDir))), _OilFresnel);
            
            // Enhanced oil/glaze effect - more prominent on cooked takoyaki
            float oilFactor = lerp(0.3, 0.8, toCooked) * _GlazeIntensity; 
            float glazeSmoothness = lerp(0.1, 0.95, toCooked * fresnel);
            
            o.Smoothness = (1.0 - baseRoughness) + (fresnel * oilFactor) + glazeSmoothness;
            o.Smoothness = saturate(o.Smoothness);
            o.Metallic = 0.0;

            // 3. Enhanced SSS (Emission) with Rim Lighting
            float sssMask = 1.0 - toCooked; 
            float backlight = pow(1.0 - saturate(dot(IN.viewDir, -worldNormal)), _SSSPower);
            
            // Add rim lighting for cooked state
            float rimLight = pow(fresnel, 2.5) * toCooked * 0.5;
            
            // Golden glow on perfectly cooked takoyaki
            float perfectGlow = smoothstep(0.7, 0.9, toCooked) * (1.0 - toBurnt);
            float3 glowColor = float3(1.0, 0.85, 0.5) * perfectGlow * 0.3;
            
            o.Emission = (_SSSColor * _SSSIntensity * sssMask * backlight) + 
                         (_OilColor.rgb * rimLight) + 
                         glowColor; 
        }
        ENDCG
    }
    
    // Simpler SubShader for lower-end devices (LOD 200)
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        
        sampler2D _MainTex;
        sampler2D _CookedTex;
        sampler2D _BurntTex;
        
        struct Input
        {
            float2 uv_MainTex;
        };
        
        half _CookLevel;
        half _BatterAmount;
        
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Simplified version without displacement, SSS, or advanced effects
            fixed4 c_raw = tex2D (_MainTex, IN.uv_MainTex);
            fixed4 c_cooked = tex2D (_CookedTex, IN.uv_MainTex);
            fixed4 c_burnt = tex2D (_BurntTex, IN.uv_MainTex);
            
            float toCooked = smoothstep(0.2, 0.8, _CookLevel);
            float toBurnt = smoothstep(1.2, 1.8, _CookLevel);
            
            fixed4 albedo = lerp(c_raw, c_cooked, toCooked);
            albedo = lerp(albedo, c_burnt, toBurnt);
            
            o.Albedo = albedo.rgb;
            o.Smoothness = lerp(0.3, 0.7, toCooked);
            o.Metallic = 0.0;
        }
        ENDCG
    }
    
    FallBack "Diffuse"
}
