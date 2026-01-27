#version 300 es
precision mediump float;

in vec3 vFragPos;
in vec3 vNormal;
in vec2 vTexCoord;

uniform sampler2D uBatterTex;
uniform sampler2D uCookedTex;
uniform sampler2D uBurntTex;

uniform vec3 uLightPos;
uniform vec3 uViewPos;

uniform float uCookLevel; // 0.0 to 2.0
uniform float uOilFresnel;

out vec4 FragColor;

void main() {
    // 1. Texture Blending (Multi-layer cooking)
    vec3 colRaw = texture(uBatterTex, vTexCoord).rgb;
    vec3 colCooked = texture(uCookedTex, vTexCoord).rgb;
    vec3 colBurnt = texture(uBurntTex, vTexCoord).rgb;

    vec3 albedo = colRaw;
    
    // Blend Raw -> Cooked
    float t1 = smoothstep(0.0, 1.0, uCookLevel);
    albedo = mix(albedo, colCooked, t1);
    
    // Blend Cooked -> Burnt
    float t2 = smoothstep(1.0, 2.0, uCookLevel);
    albedo = mix(albedo, colBurnt, t2);

    // 2. Lighting (PBR-ish)
    vec3 norm = normalize(vNormal);
    vec3 lightDir = normalize(uLightPos - vFragPos);
    vec3 viewDir = normalize(uViewPos - vFragPos);
    vec3 halfDir = normalize(lightDir + viewDir);

    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diff * albedo;

    // 3. Specular / Oil (Fresnel)
    float specStr = 0.5;
    // Oil effect: High gloss, sharper highlight
    float roughness = 0.3; 
    
    float NdotH = max(dot(norm, halfDir), 0.0);
    float spec = pow(NdotH, 16.0); // Shininess

    // Fresnel (Schlick)
    float F0 = 0.04; // Dielectric
    float cosTheta = max(dot(halfDir, viewDir), 0.0);
    float fresnel = F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
    
    vec3 specular = vec3(1.0) * spec * (specStr + fresnel * uOilFresnel);

    // 4. Subsurface Scattering (Fake SSS)
    // Wrap lighting
    float wrap = 0.5;
    float sssDiff = max(0.0, (dot(norm, lightDir) + wrap) / (1.0 + wrap));
    vec3 sssColor = vec3(1.0, 0.4, 0.2) * 0.4; // Internal glow color
    vec3 lighting = diffuse + sssColor * (1.0 - t2) * 0.5; // SSS decreases as it burns

    vec3 finalColor = lighting + specular;

    // Gamma correct
    finalColor = pow(finalColor, vec3(1.0/2.2));

    FragColor = vec4(finalColor, 1.0);
}
