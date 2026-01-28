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
    
    // Procedural Waving Animation
    // We want the edges (high UV values) to flap more than the center
    float flap = sin(uTime * 10.0 + aPosition.x * 5.0 + aPosition.y * 5.0) * aTexCoord.y * 0.05;
    flap += cos(uTime * 15.0 + aPosition.z * 10.0) * aTexCoord.x * 0.03;
    
    vec3 animatedPos = aPosition + aNormal * flap;

    vec4 worldPos = uModelMatrix * vec4(animatedPos, 1.0);
    vFragPos = worldPos.xyz;
    vNormal = normalize(mat3(uModelMatrix) * aNormal);
    
    gl_Position = uVPMatrix * worldPos;
}
