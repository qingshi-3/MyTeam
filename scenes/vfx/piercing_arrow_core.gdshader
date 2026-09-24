shader_type canvas_item;
render_mode unshaded, blend_add;

// A luminous arrow silhouette, independent of the ordinary metal-arrow atlas.
// Local +X is forward. The scene places its tip near the collision position.
void fragment() {
    vec2 p = UV;
    float y = abs(p.y - 0.5);
    float head_width = max(0.0, (0.94 - p.x) * 0.24);
    float head = max(max(0.42 + y * 2.0 - p.x, p.x - 0.94), y - head_width);
    float shaft_width = 0.015 * smoothstep(0.06, 0.35, p.x);
    float shaft = max(max(0.06 - p.x, p.x - 0.67), y - shaft_width);
    float shape = min(head, shaft);
    float edge = 1.0 - smoothstep(-0.004, 0.012, shape);
    float heart = 1.0 - smoothstep(-0.022, -0.005, shape);
    float glow = exp(-max(shape, 0.0) * 27.0) * 0.4;
    float axis = exp(-y * y * 3400.0) * smoothstep(0.05, 0.5, p.x)
        * (1.0 - smoothstep(0.9, 0.95, p.x));
    vec3 light = vec3(1.0, 0.34, 0.035) * glow
        + vec3(1.0, 0.63, 0.12) * edge * 0.45
        + vec3(1.0, 0.98, 0.83) * (heart * 0.25 + axis * 1.25);
    float boundary = smoothstep(0.0, 0.035, p.x) * (1.0 - smoothstep(0.97, 1.0, p.x));
    COLOR = vec4(light, clamp(glow + edge + axis, 0.0, 1.0) * boundary);
}
