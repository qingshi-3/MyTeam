shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform vec4 tint : source_color = vec4(0.28, 0.95, 0.67, 0.9);

void fragment() {
    vec2 q = (UV - 0.5) * 2.0;
    float p = clamp(age / 0.65, 0.0, 1.0);
    float envelope = smoothstep(0.0, 0.08, age) * (1.0 - smoothstep(0.23, 0.65, age));
    float rise = mix(0.0, smoothstep(0.0, 0.55, age), motion_amount);
    float center = 0.56 - rise * 1.12;
    // Two soft, tapered ribbons rise through the body at different heights.
    // Their concave ends express a lifting motion without a shield bubble.
    float curve = center + q.x * q.x * 0.9;
    float width = 0.065 + abs(q.x) * 0.11;
    float main = exp(-pow((q.y - curve) / width, 2.0));
    float tail = exp(-pow((q.y - curve - 0.16) / 0.15, 2.0)) * 0.27;
    float span = 1.0 - smoothstep(0.2, 0.77 - p * 0.12, abs(q.x));
    float fine = 0.72 + 0.28 * sin(q.x * 9.0 + age * 4.0 * motion_amount);
    float vertical = 1.0 - smoothstep(0.65, 0.95, abs(q.y));
    float alpha = (main + tail) * span * fine * vertical * envelope;
    vec3 color = mix(tint.rgb, vec3(0.96, 1.0, 0.8), pow(main, 2.0) * 0.7);
    COLOR = vec4(color, alpha * tint.a * (1.0 - release));
}
