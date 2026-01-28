#version 300 es
precision mediump float;

in vec3 vFragPos;
in vec3 vNormal;
in vec2 vTexCoord;

uniform vec4 uToppingColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;

out vec4 FragColor;

// Simple hash for noise
float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

// Simple noise function for edge clipping
float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    float a = hash(i);
    float b = hash(i + vec2(1.0, 0.0));
    float c = hash(i + vec2(0.0, 1.0));
    float d = hash(i + vec2(1.0, 1.0));
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(a, b, u.x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
}

void main() {
    // 1. NOISE-BASED EDGE CLIPPING (The "Torn" look)
    float edgeMask = min(min(vTexCoord.x, 1.0 - vTexCoord.x), min(vTexCoord.y, 1.0 - vTexCoord.y));
    float n = noise(vTexCoord * 20.0);
    
    // Higher discard threshold at the center of the flake for a more "torn" look
    if (n > edgeMask * 6.0 + 0.1) {
        discard;
    }

    // 2. SHADING
    vec3 N = normalize(vNormal);
    if (!gl_FrontFacing) N = -N; // Two-sided lighting

    vec3 L = normalize(uLightPos - vFragPos);
    vec3 V = normalize(uViewPos - vFragPos);
    
    // Half-Lambert for softer, more food-like lighting
    float diff = pow(dot(N, L) * 0.5 + 0.5, 1.5);
    
    // Subsurface translucency (Fake)
    float translucency = pow(1.0 - max(dot(N, V), 0.0), 4.0) * 0.4;
    
    // Specular Rim Highlight (Makes it look thin and crisp)
    float rim = pow(1.0 - max(dot(N, V), 0.0), 3.0) * max(dot(N, L), 0.0);
    
    vec3 color = uToppingColor.rgb * (0.3 + 0.7 * diff + translucency);
    color += vec3(1.0, 0.9, 0.8) * rim * 0.3; // Golden rim highlight
    
    // Add subtle color variation based on UV
    color *= (0.85 + 0.3 * noise(vTexCoord * 8.0));

    FragColor = vec4(color, uToppingColor.a);
}
