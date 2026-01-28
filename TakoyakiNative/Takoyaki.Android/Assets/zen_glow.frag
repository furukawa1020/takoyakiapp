#version 300 es
precision mediump float;

in vec2 vTexCoord;
out vec4 FragColor;

uniform vec4 uColor;
uniform float uIntensity;

void main() {
    // Screen-space coordinates from 0..1 (vTexCoord)
    // Distance from edges
    float vignette = smoothstep(0.0, 0.4, vTexCoord.x) * 
                     smoothstep(1.0, 0.6, vTexCoord.x) * 
                     smoothstep(0.0, 0.4, vTexCoord.y) * 
                     smoothstep(1.0, 0.6, vTexCoord.y);
    
    // Invert the vignette to get a border glow
    float borderGlow = 1.0 - vignette;
    
    // Master's Golden Glow
    FragColor = uColor * (borderGlow * uIntensity);
}
