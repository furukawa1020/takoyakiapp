#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;

uniform mat4 uVPMatrix;
uniform mat4 uModelMatrix;
uniform float uScale;

out vec2 vTexCoord;

void main() {
    vTexCoord = aTexCoord;
    
    // Extract camera basis from View matrix (assuming it's in VP)
    // For a simple billboard, we just use the world position of the object 
    // and add offset in view space.
    
    vec4 worldPos = uModelMatrix * vec4(0.0, 0.0, 0.0, 1.0);
    vec4 viewPos = uVPMatrix * worldPos;
    
    // Offset in screen/view space
    vec2 offset = aPosition.xy * uScale;
    gl_Position = viewPos + vec4(offset, 0.0, 0.0);
}
