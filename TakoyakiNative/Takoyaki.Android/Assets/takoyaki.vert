#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

uniform mat4 uMVPMatrix;
uniform mat4 uModelMatrix;
uniform float uDisplacementStrength;
uniform sampler2D uNoiseMapFix;
uniform float uTime;

out vec3 vFragPos;
out vec3 vNormal;
out vec2 vTexCoord;
out float vVertexHeight;

void main() {
    vTexCoord = aTexCoord;
    
    // Vertex Displacement (Puffiness logic)
    float noiseVal = texture(uNoiseMapFix, aTexCoord).r;
    vec3 displacedPos = aPosition + aNormal * (noiseVal * uDisplacementStrength);

    // Soft Body Animation (Jiggle/Wobble)
    // "Nentai" effect: varying displacement over time
    float wobble = sin(uTime * 6.0 + aPosition.y * 3.0) * 0.025; 
    wobble += cos(uTime * 4.5 + aPosition.x * 3.0 + aPosition.z * 1.5) * 0.02;
    
    vec3 animatedPos = displacedPos + aNormal * wobble;

    // World Space Position
    vFragPos = vec3(uModelMatrix * vec4(animatedPos, 1.0));
    vVertexHeight = aPosition.y; // Pass original Y for blend logic
    
    // Simple Normal Recalculation (Approximate)
    vNormal = mat3(transpose(inverse(uModelMatrix))) * aNormal;

    gl_Position = uMVPMatrix * vec4(animatedPos, 1.0);
}
