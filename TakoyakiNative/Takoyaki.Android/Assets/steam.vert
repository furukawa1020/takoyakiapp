#version 300 es
layout(location = 0) in vec3 aPosition;
layout(location = 1) in float aAlpha; // Life/Alpha

uniform mat4 uMVPMatrix;
uniform float uPointSize;

out float vAlpha;

void main() {
    gl_Position = uMVPMatrix * vec4(aPosition, 1.0);
    gl_PointSize = uPointSize; // Simple point sprite
    vAlpha = aAlpha;
}
