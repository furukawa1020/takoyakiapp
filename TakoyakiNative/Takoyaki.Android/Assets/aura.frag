#version 300 es
precision mediump float;

in vec2 vTexCoord;
out vec4 FragColor;

uniform vec4 uColor;
uniform float uTime;
uniform float uIntensity;

void main() {
    float dist = distance(vTexCoord, vec2(0.5));
    if (dist > 0.5) discard;
    
    // Soft radial gradient
    float glow = smoothstep(0.5, 0.0, dist);
    
    // Rhythmic pulse
    float pulse = 0.8 + 0.2 * sin(uTime * 10.0);
    
    FragColor = uColor * (glow * uIntensity * pulse);
    // Additive glow feel
    FragColor.a *= glow;
}
