#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

uniform mat4 uVPMatrix;
uniform mat4 uModelMatrix;
uniform float uTime;
uniform float uRoughness; // Used here as a proxy for animation intensity for katsuobushi

out vec3 vFragPos;
out vec3 vNormal;
out vec2 vTexCoord;

void main() {
    vTexCoord = aTexCoord;
    
    // Procedural Waving & Curling Animation
    // Katsuobushi should curl and dance on the heat
    float curl = sin(uTime * 8.0 + aPosition.x * 12.0) * aTexCoord.y * 0.12;
    curl += cos(uTime * 12.0 + aPosition.z * 15.0) * aTexCoord.x * 0.08;
    
    // Add a randomized twist based on UV
    float twist = sin(uTime * 5.0 + (aTexCoord.x + aTexCoord.y) * 10.0) * 0.05;
    
    vec3 animatedPos = aPosition + aNormal * (curl + twist);

    vec4 worldPos = uModelMatrix * vec4(animatedPos, 1.0);
    vFragPos = worldPos.xyz;
    vNormal = normalize(mat3(uModelMatrix) * aNormal);
    
    gl_Position = uVPMatrix * worldPos;
}
