#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

uniform mat4 uMVPMatrix;
uniform mat4 uModelMatrix;
uniform float uDisplacementStrength;
uniform sampler2D uNoiseMapFix;

out vec3 vFragPos;
out vec3 vNormal;
out vec2 vTexCoord;
out vec3 vLocalPos;

void main() {
    vTexCoord = aTexCoord;
    
    // Vertex Displacement (Puffiness logic)
    float noiseVal = texture(uNoiseMapFix, aTexCoord).r;
    vec3 displacedPos = aPosition + aNormal * (noiseVal * uDisplacementStrength);

    // World Space Position
    vFragPos = vec3(uModelMatrix * vec4(displacedPos, 1.0));
    vLocalPos = displacedPos; // Pass local pos for slicing
    
    // Simple Normal (Ideally recalculate bitangent for mapped normal, keeping simple for now)
    vNormal = mat3(transpose(inverse(uModelMatrix))) * aNormal;

    gl_Position = uMVPMatrix * vec4(displacedPos, 1.0);
}
