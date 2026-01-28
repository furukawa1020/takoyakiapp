#version 300 es
precision mediump float;

in vec2 vTexCoord;
out vec4 FragColor;

uniform vec4 uColor;
uniform float uProgress; // 0..1 for bar
uniform int uType; // 0: Solid, 1: Progress Bar

void main() {
    if (uType == 1) {
        if (vTexCoord.x > uProgress) {
            FragColor = vec4(0.2, 0.2, 0.2, 0.5); // Empty part
        } else {
            FragColor = uColor; // Filled part
        }
    } else {
        FragColor = uColor;
    }
}
