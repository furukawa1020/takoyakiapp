#version 300 es
precision mediump float;

in float vAlpha;
out vec4 FragColor;

void main() {
    // Circle shape from Point Coord (0..1)
    vec2 circCoord = 2.0 * gl_PointCoord - 1.0;
    if (dot(circCoord, circCoord) > 1.0) {
        discard;
    }
    
    // Soft gradient
    float dist = length(circCoord);
    float alpha = (1.0 - dist) * vAlpha * 0.5; // 0.5 max opacity
    
    FragColor = vec4(1.0, 1.0, 1.0, alpha);
}
