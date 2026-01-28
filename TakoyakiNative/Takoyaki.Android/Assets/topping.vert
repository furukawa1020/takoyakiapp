#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

uniform mat4 uModelMatrix;
uniform mat4 uVPMatrix;
uniform float uTime;

out vec3 vFragPos;
out vec3 vNormal;
out vec2 vTexCoord;

void main() {
    vTexCoord = aTexCoord;
    
    // 1. Calculate World Position (Base)
    vec4 worldPosRaw = uModelMatrix * vec4(aPosition, 1.0);
    
    // 2. Calculate World Normal
    vec3 worldNormal = normalize(mat3(transpose(inverse(uModelMatrix))) * aNormal);

    // 3. Soft Body Animation (Synchronized with Takoyaki)
    // Synchronized based on absolute world position
    float wobble = sin(uTime * 6.0 + worldPosRaw.y * 3.0) * 0.025; 
    wobble += cos(uTime * 4.5 + worldPosRaw.x * 3.0 + worldPosRaw.z * 1.5) * 0.02;
    
    vec3 animatedWorldPos = worldPosRaw.xyz + worldNormal * wobble;

    vFragPos = animatedWorldPos;
    vNormal = worldNormal;

    // Final Position: VP * Animated World
    gl_Position = uVPMatrix * vec4(animatedWorldPos, 1.0);
}
