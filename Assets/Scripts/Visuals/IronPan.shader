Shader "Takoyaki/IronPan"
{
    Properties
    {
        _Color ("Main Color", Color) = (0.1, 0.1, 0.1, 1) // Black iron
        _Glossiness ("Smoothness", Range(0,1)) = 0.4
        _Metallic ("Metallic", Range(0,1)) = 0.8
        _NoiseScale ("Noise Scale", Float) = 50.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        fixed4 _Color;
        half _Glossiness;
        half _Metallic;
        float _NoiseScale;

        // Simple pseudo-random noise
        float rand(float3 co) {
            return frac(sin(dot(co.xyz ,float3(12.9898,78.233,45.5432))) * 43758.5453);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Procedural Grain
            float noise = rand(IN.worldPos * _NoiseScale);
            
            // Iron is dark, slightly noisy/rough
            fixed4 c = _Color + (noise * 0.05 - 0.025);
            
            o.Albedo = c.rgb;
            o.Metallic = _Metallic + noise * 0.1;
            o.Smoothness = _Glossiness - noise * 0.1; // Rough spots
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
