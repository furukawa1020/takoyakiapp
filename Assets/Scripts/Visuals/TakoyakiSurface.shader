Shader "Takoyaki/TakoyakiSurface"
{
    Properties
    {
        _MainTex ("Raw Batter Texture", 2D) = "white" {}
        _CookedTex ("Cooked Texture", 2D) = "white" {}
        _BurntTex ("Burnt Texture", 2D) = "black" {}
        
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        _CookLevel ("Cook Level", Range(0, 2)) = 0.0
        _BatterAmount ("Batter Amount", Range(0, 1)) = 1.0
        
        _NoiseTex ("Noise Map (for uneven cooking)", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _CookedTex;
        sampler2D _BurntTex;
        sampler2D _NoiseTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
        };

        half _Glossiness;
        half _Metallic;
        half _CookLevel;
        half _BatterAmount;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Noise for uneven cooking
            fixed noise = tex2D (_NoiseTex, IN.uv_NoiseTex).r;
            
            // Adjust cook level based on noise (some parts cook faster)
            half localCook = _CookLevel * (0.8 + noise * 0.4);

            // 1. Blend Raw -> Cooked (0.0 to 1.0)
            fixed4 c_raw = tex2D (_MainTex, IN.uv_MainTex);
            fixed4 c_cooked = tex2D (_CookedTex, IN.uv_MainTex);
            
            half blend1 = smoothstep(0.2, 0.8, localCook);
            fixed4 baseColor = lerp(c_raw, c_cooked, blend1);

            // 2. Blend Cooked -> Burnt (1.0 to 1.5+)
            fixed4 c_burnt = tex2D (_BurntTex, IN.uv_MainTex);
            half blend2 = smoothstep(1.0, 1.5, localCook);
            
            baseColor = lerp(baseColor, c_burnt, blend2);

            // 3. Batter Amount blending (Transparency or masking if needed, but for opaque takoyaki we assume full)
            // If batter is low, maybe darken or show 'pan' color? (Simulated by generic darkening for now)
            if (_BatterAmount < 0.9) {
                baseColor *= (_BatterAmount + 0.1); 
            }

            o.Albedo = baseColor.rgb;
            
            // Oiliness increases with cooking
            o.Smoothness = _Glossiness * (1.0 + blend1 * 0.5); 
            o.Metallic = _Metallic;
            o.Alpha = baseColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
