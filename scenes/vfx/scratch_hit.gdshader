shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float progress = 0.0;
uniform float release = 0.0;

void fragment() {
    vec4 tint = COLOR;
    float x = UV.x;
    float y = abs(UV.y - 0.5);
    float width = 0.018 * pow(max(0.0, sin(clamp((x - 0.1) / 0.8, 0.0, 1.0) * 3.141593)), 0.65);
    float slit = exp(-pow(y / max(width, 0.001), 2.0) * 2.0);
    float soft = exp(-pow(y / (width + 0.025), 2.0) * 2.0) * 0.3;
    float tip = mix(0.25, 0.92, clamp(progress / 0.2, 0.0, 1.0));
    float length_mask = smoothstep(0.08, 0.18, x) * (1.0 - smoothstep(tip - 0.035, tip, x));
    float fade = pow(1.0 - smoothstep(0.22, 1.0, progress), 1.5) * (1.0 - release);
    vec3 light = mix(vec3(1.0, 0.55, 0.23), vec3(1.0, 0.97, 0.86), slit);
    COLOR = vec4(light * tint.rgb, (slit + soft) * length_mask * fade * tint.a);
}
