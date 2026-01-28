#version 300 es
precision mediump float;

in vec3 vFragPos;
in vec3 vNormal;
in vec2 vTexCoord;

uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform vec4 uColor;
uniform float uRoughness; // 0.0 (mirror) to 1.0 (matte)

out vec4 FragColor;

void main() {
    vec3 N = normalize(vNormal);
    vec3 V = normalize(uViewPos - vFragPos);
    vec3 L = normalize(uLightPos - vFragPos);
    vec3 H = normalize(V + L);

    // 1. Diffuse (Oren-Nayar simplified or Lambert)
    float diff = max(dot(N, L), 0.0);
    vec3 diffuse = uColor.rgb * diff;

    // 2. Specular (Glossy Highlights for Sauce/Mayo)
    // Sharper highlights for lower roughness
    float shininess = 128.0 * (1.0 - uRoughness);
    float spec = pow(max(dot(N, H), 0.0), max(1.0, shininess));
    vec3 specular = vec3(1.0) * spec * (1.0 - uRoughness); // Specular color is white

    // 3. Ambient
    vec3 ambient = uColor.rgb * 0.25;

    // 4. Rim Lighting (Fresnel fake)
    float rim = 1.0 - max(dot(V, N), 0.0);
    rim = pow(rim, 3.0) * 0.2;
    vec3 rimColor = vec3(rim);

    vec3 finalColor = ambient + diffuse + specular + rimColor;

    // Gamma correction
    finalColor = pow(finalColor, vec3(1.0/2.2));

    FragColor = vec4(finalColor, uColor.a);
}
