shader_type canvas_item;
render_mode unshaded, blend_add;
uniform vec4 tint : source_color = vec4(1.0, 0.6, 0.2, 1.0);
uniform bool beam = false;
uniform float progress = 0.0;
uniform float age = 0.0;
uniform float length_pixels = 600.0;
uniform float motion_amount = 1.0;
void fragment() {
    float y = abs(UV.y - 0.5) * 2.0;
    float ends = smoothstep(0.0, 0.02, UV.x) * (1.0 - smoothstep(0.985, 1.0, UV.x));
    vec3 color = tint.rgb;
    float alpha;
    if (beam) {
        float width = mix(0.8, 0.06, pow(progress, 0.6));
        float core = 1.0 - smoothstep(width * 0.23, width * 0.48, y);
        float halo = exp(-y * y / max(0.002, width * width * 0.55));
        float flow = 0.9 + 0.1 * sin(UV.x * 90.0 - age * 65.0 * motion_amount + y * 12.0);
        alpha = (core + halo * 0.72) * pow(1.0 - progress, 0.65) * flow;
        color = mix(tint.rgb, vec3(1.0, 0.99, 0.91), core);
    } else {
        float center = exp(-y * y * 230.0);
        float border = exp(-pow((y - 0.57) * 36.0, 2.0));
        // The tip points toward increasing U (the target), including leftward casts.
        float dash = (1.0 - smoothstep(0.02, 0.06, abs(fract(UV.x * length_pixels / 72.0 + y * 0.25) - 0.5)))
            * (1.0 - smoothstep(0.5, 0.65, y));
        float fill = (1.0 - smoothstep(0.5, 0.6, y)) * 0.08;
        alpha = (center * 0.48 + border * 0.34 + dash * 0.18 + fill) * (0.5 + progress * 0.5);
    }
    COLOR = vec4(color, alpha * ends * tint.a);
}
