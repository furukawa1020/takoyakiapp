#version 300 es
precision mediump float;

in float vAlpha;
out vec4 FragColor;

void main() {
    // Circle mask for point sprite
    float d = distance(gl_PointCoord, vec2(0.5));
    if (d > 0.5) discard;
    
    float ring = smoothstep(0.5, 0.4, d);
    
    // Golden color
    vec3 gold = vec3(1.0, 0.84, 0.0);
    vec3 glow = vec3(1.0, 0.95, 0.6);
    
    vec3 color = mix(gold, glow, smoothstep(0.4, 0.0, d));
    
    FragColor = vec4(color, vAlpha * ring);
}
