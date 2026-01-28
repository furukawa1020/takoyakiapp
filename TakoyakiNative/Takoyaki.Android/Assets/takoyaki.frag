#version 300 es
precision mediump float;

in vec3 vFragPos;
in vec3 vNormal;
in vec2 vTexCoord;
in float vVertexHeight;

uniform sampler2D uBatterTex;
uniform sampler2D uCookedTex;
uniform sampler2D uBurntTex;
uniform sampler2D uNoiseMapFix;

uniform vec3 uLightPos;
uniform vec3 uViewPos;

uniform float uCookLevel; // 0.0 to 2.0
uniform float uBatterLevel; // 0.0 to 1.0
uniform float uOilFresnel;

out vec4 FragColor;

void main() {
    // 0. Pouring/Filling Visualization
    // Sphere is radius ~1.0 (Y: -1 to 1)
    // Map BatterLevel 0..1 to Height -1..1
    float fillHeight = -1.0 + (uBatterLevel * 2.1); // 2.1 to ensure full coverage
    // TEMP: Disable discard to test rendering
    // if (uBatterLevel < 0.99 && vVertexHeight > fillHeight) {
    //     discard;
    // }

    // 1. Texture Blending (Multi-layer cooking)
    vec3 colRaw = texture(uBatterTex, vTexCoord).rgb;
    vec3 colCooked = texture(uCookedTex, vTexCoord).rgb;
    vec3 colBurnt = texture(uBurntTex, vTexCoord).rgb;

    vec3 albedo = colRaw;
    
    // Get Height/Displacement value for realistic blending
    // "Crispy" parts (high values) cook first
    float height = texture(uNoiseMapFix, vTexCoord).r;

    // Height-Based Blend: Raw -> Cooked
    // Instead of global fade, we use the height map to control the transition frontier.
    float cookedEdge = uCookLevel * 1.2; // Move the edge
    float cookedMask = smoothstep(cookedEdge - 0.2, cookedEdge + 0.2, height + uCookLevel * 0.5);
    albedo = mix(albedo, colCooked, cookedMask);
    
    // Height-Based Blend: Cooked -> Burnt
    // Burnt also starts at high points
    float burntMask = smoothstep(1.5, 2.5, uCookLevel + height);
    albedo = mix(albedo, colBurnt, burntMask);

    // PBR CONSTANTS
    float roughness = 0.3; // Oil is smooth
    float metallic = 0.0; // Food is dielectric
    float ao = 1.0; 

    // Adjust Roughness based on Cooking (Raw is matte, Cooked is Oily, Burnt is matte/dry)
    // Raw (Wet batter) -> 0.4
    // Cooked (Oily) -> 0.2
    // Burnt (Charred) -> 0.8
    if (uCookLevel < 1.0) roughness = mix(0.4, 0.2, uCookLevel);
    else roughness = mix(0.2, 0.8, uCookLevel - 1.0);

    // Height affects roughness (Crevices are wetter/oilier)
    roughness = max(0.1, roughness - height * 0.2);

    vec3 N = normalize(vNormal);
    vec3 V = normalize(uViewPos - vFragPos);
    vec3 L = normalize(uLightPos - vFragPos);
    vec3 H = normalize(V + L);

    // F0 for dielectric (food/oil) is usually 0.04
    vec3 F0 = vec3(0.04); 
    
    // COOK-TORRANCE BRDF
    
    // 1. Normal Distribution (GGX)
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;
    float nom = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = 3.141592 * denom * denom;
    float NDF = nom / denom;

    // 2. Geometry (Smith)
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float k = (roughness + 1.0) * (roughness + 1.0) / 8.0;
    
    float ggx1 = NdotV / (NdotV * (1.0 - k) + k);
    float ggx2 = NdotL / (NdotL * (1.0 - k) + k);
    float G = ggx1 * ggx2;

    // 3. Fresnel (Schlick)
    vec3 F = F0 + (1.0 - F0) * pow(clamp(1.0 - max(dot(H, V), 0.0), 0.0, 1.0), 5.0);

    // Specular contribution
    vec3 numerator = NDF * G * F;
    float denominator = 4.0 * NdotV * NdotL + 0.0001;
    vec3 specular = numerator / denominator;

    // Diffuse contribution (Conservation of Energy)
    vec3 kS = F;
    vec3 kD = vec3(1.0) - kS;
    kD *= 1.0 - metallic;

    // Light Radiance (Sun)
    vec3 radiance = vec3(2.5); // High intensity light
    
    // Final Lo
    float theta = NdotL;
    vec3 Lo = (kD * albedo / 3.141592 + specular) * radiance * theta;

    // Ambient (IBL fake)
    vec3 ambient = vec3(0.05) * albedo * ao;
    
    // SUBSURFACE SCATTERING (Approximated)
    // Add a reddish transmission for thin parts
    float sssMask = 1.0 - height; // Thin parts
    vec3 sssColor = vec3(1.0, 0.3, 0.1);
    vec3 sss = sssColor * sssMask * 0.2 * (1.0 - burntMask); // Disappears when burnt
    
    vec3 color = ambient + Lo + sss;

    // Tone Mapping (Reinhard)
    color = color / (color + vec3(1.0));
    
    // Gamma
    color = pow(color, vec3(1.0/2.2));

    // DEBUG: TEST IF GEOMETRY IS RENDERING
    FragColor = vec4(1.0, 0.0, 0.0, 1.0); // SOLID RED
}
