#version 300 es
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;

uniform mat4 uOrthoMatrix;

out vec2 vTexCoord;

void main() {
    vTexCoord = aTexCoord;
    gl_Position = uOrthoMatrix * vec4(aPosition, 0.0, 1.0);
}
