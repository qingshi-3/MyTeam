shader_type canvas_item;
render_mode unshaded;
uniform bool rush = false;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float length_ratio = 3.0;

void fragment() {
    vec2 p = UV;
    float y = abs(p.y - 0.5) * 2.0;
    float alpha;
    vec3 color;
    if (!rush) {
        float edge = smoothstep(0.90, 0.95, y) * (1.0 - smoothstep(0.97, 1.0, y));
        float chevron = abs(fract(p.x * length_ratio * 2.0 + y * 0.35) - 0.5);
        float arrow = (1.0 - smoothstep(0.035, 0.09, chevron)) * (1.0 - smoothstep(0.65, 0.9, y));
        float caps = smoothstep(0.0, 0.02, p.x) * (1.0 - smoothstep(0.98, 1.0, p.x));
        alpha = caps * (0.055 + edge * 0.75 + arrow * (0.15 + 0.3 * progress));
        color = mix(vec3(0.85, 0.22, 0.06), vec3(1.0, 0.57, 0.16), progress);
    } else {
        float front = exp(-pow((p.x - 0.76) * 13.0 + y * y * 1.6, 2.0));
        float shoulder = exp(-pow((y - 0.65) * 7.0, 2.0));
        float tail = smoothstep(0.0, 0.6, p.x) * (1.0 - smoothstep(0.64, 0.85, p.x));
        float streak = pow(max(0.0, sin(y * 24.0 + p.x * 5.0 - age * 14.0)), 8.0);
        float dust = 0.0;
        for (int i = 0; i < 6; i++) {
            float f = float(i);
            float t = fract(age * 1.8 + f * 0.163);
            vec2 center = vec2(0.72 - t * 0.62, 0.5 + (mod(f, 2.0) * 2.0 - 1.0) * (0.25 + 0.2 * t));
            vec2 d = (p - center) / vec2(0.045 + 0.03 * t, 0.055 + 0.05 * t);
            dust += exp(-dot(d, d) * 2.0) * (1.0 - t) * 0.45;
        }
        alpha = front * 0.55 * (1.0 - smoothstep(0.8, 1.0, y)) + tail * shoulder * (0.25 + 0.3 * streak) + dust;
        color = mix(vec3(0.55, 0.22, 0.09), vec3(1.0, 0.56, 0.17), clamp(front + streak * 0.3, 0.0, 1.0));
    }
    COLOR = vec4(color, clamp(alpha, 0.0, 0.85));
}
