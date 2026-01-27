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
        _SSSIntensity ("SSS Intensity", Range(0, 1)) = 0.5

        [Header(Oil and Glaze)]
        _OilColor ("Oil Color", Color) = (1, 1, 1, 1)
        _OilRoughness ("Oil Roughness", Range(0, 1)) = 0.1
        _OilFresnel ("Oil Fresnel Power", Range(0.1, 10)) = 5.0

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

            // 2. Oil / Specular
            float baseRoughness = lerp(0.3, 0.9, toCooked); 
            
            // Fix: Use WorldNormalVector when writing to o.Normal
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            float fresnel = pow(1.0 - saturate(dot(worldNormal, normalize(IN.viewDir))), _OilFresnel);
            float oilFactor = 0.5; 
            
            o.Smoothness = (1.0 - baseRoughness) + (fresnel * oilFactor);
            o.Metallic = 0.0;

            // 3. Fake SSS (Emission)
            float sssMask = 1.0 - toCooked; 
            float backlight = pow(1.0 - saturate(dot(IN.viewDir, -worldNormal)), 2.0);
            o.Emission = _SSSColor * _SSSIntensity * sssMask * backlight; 
        }
        ENDCG
    }
    FallBack "Diffuse"
}
