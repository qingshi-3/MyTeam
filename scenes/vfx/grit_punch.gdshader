shader_type canvas_item;
render_mode unshaded;
uniform float progress = 0.0;
uniform float motion_amount = 1.0;
uniform bool released = false;
uniform bool body_layer = false;
void fragment() {
    vec2 p = UV;
    float y = abs(p.y - 0.5) * 2.0;
    float border = smoothstep(0.93, 0.96, y) * (1.0 - smoothstep(0.975, 1.0, y));
    float cap = smoothstep(0.977, 0.985, p.x);
    vec3 gold = vec3(1.0, 0.56, 0.15);
    float a = 0.0;
    if (!released) {
        a = (border + cap * 0.6) * (0.3 + progress * 0.5);
        if (body_layer) a = exp(-p.x * 22.0) * pow(1.0-y, 4.0) * progress * 0.85;
    } else {
        float front = min(1.1, progress * 3.8);
        float wave = exp(-pow((p.x - front + y * y * 0.10) * 14.0, 2.0));
        float wake = (1.0 - smoothstep(front - 0.40, front, p.x)) * smoothstep(front - 0.58, front - 0.22, p.x);
        float streak = pow(max(0.0, sin(p.y * 49.0 + p.x * 4.0)), 14.0);
        float fade = pow(1.0 - progress, 1.2);
        if (body_layer) {
            a = (wave * (1.0-y*y) + wake * streak * 0.38) * fade;
            gold = mix(gold, vec3(1.0,0.95,0.68), wave * 0.8);
        } else {
            float dust = pow(max(0.0, sin(p.x*77.0+p.y*29.0+progress*motion_amount*4.0)), 5.0);
            a = (wake * 0.14 + wave * dust * y * 0.65) * fade;
            gold = vec3(0.68,0.46,0.24);
        }
    }
    COLOR = vec4(gold, clamp(a, 0.0, 1.0)) * COLOR;
}
