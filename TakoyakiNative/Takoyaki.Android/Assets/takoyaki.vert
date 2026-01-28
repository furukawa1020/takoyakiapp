#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

uniform mat4 uMVPMatrix;
uniform mat4 uModelMatrix;
uniform float uDisplacementStrength;
uniform sampler2D uNoiseMapFix;
uniform float uTime;
uniform mat4 uVPMatrix;

out vec3 vFragPos;
out vec3 vNormal;
out vec2 vTexCoord;
out float vVertexHeight;

void main() {
    vTexCoord = aTexCoord;
    
    // Vertex Displacement (Puffiness logic)
    float noiseVal = texture(uNoiseMapFix, aTexCoord).r;
    vec3 displacedPos = aPosition + aNormal * (noiseVal * uDisplacementStrength);

    // 1. Calculate World Position (Base)
    vec4 worldPosRaw = uModelMatrix * vec4(displacedPos, 1.0);
    
    // 2. Calculate World Normal
    vec3 worldNormal = normalize(mat3(transpose(inverse(uModelMatrix))) * aNormal);

    // 3. Soft Body Animation (World Space)
    // "Nentai" effect: synchronized based on absolute position
    float wobble = sin(uTime * 6.0 + worldPosRaw.y * 3.0) * 0.025; 
    wobble += cos(uTime * 4.5 + worldPosRaw.x * 3.0 + worldPosRaw.z * 1.5) * 0.02;
    
    vec3 animatedWorldPos = worldPosRaw.xyz + worldNormal * wobble;

    // Pass using World Space
    vFragPos = animatedWorldPos;
    vNormal = worldNormal;
    vVertexHeight = aPosition.y; // Keep local Y for gradient logic (batter level)

    // Final Position: VP * World
    gl_Position = uVPMatrix * vec4(animatedWorldPos, 1.0);
}
