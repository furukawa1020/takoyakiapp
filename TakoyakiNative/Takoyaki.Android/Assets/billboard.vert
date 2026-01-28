#version 300 es
precision mediump float;

uniform mat4 uMVPMatrix;
uniform vec3 uBillboardPos; // Center position in world space
uniform vec2 uBillboardSize; // Width, Height

layout(location = 0) in vec2 aCorner; // (-1,-1) to (1,1)
layout(location = 1) in vec2 aTexCoord;

out vec2 vTexCoord;

void main() {
    // Simple billboard: expand quad in screen space
    // For true billboarding, we'd extract camera right/up from view matrix
    // For MVP, we just offset in XY plane
    vec3 worldPos = uBillboardPos + vec3(aCorner * uBillboardSize * 0.5, 0.0);
    gl_Position = uMVPMatrix * vec4(worldPos, 1.0);
    vTexCoord = aTexCoord;
}
