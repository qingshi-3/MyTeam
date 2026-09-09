shader_type canvas_item;
render_mode unshaded, blend_mix;
#include "res://scenes/vfx/ribbon_motion.gdshaderinc"
uniform vec4 fresh_tint : source_color = vec4(1.0, 0.38, 0.36, 1.0);
uniform vec4 faded_tint : source_color = vec4(0.72, 0.08, 0.14, 1.0);
void fragment() {
    float reduced = 1.0 - motion_amount;
    float passed = passed_time(UV.x);
    float visible = smoothstep(0.0, 0.012 + reduced * 0.06, passed);
    float tail = clamp(passed / tail_duration, 0.0, 1.0);
    float fade = 1.0 - smoothstep(0.10, 1.0, tail);
    float tip_width = mix(smoothstep(0.0, 0.025, passed), 1.0, reduced);
    float tip = 1.0 - smoothstep(tip_width, tip_width + 0.1, abs(UV.y - 0.5) * 2.0);
    vec2 tex_uv = vec2(UV.x, mix(0.28, 0.72, UV.y));
    tex_uv.x = clamp(tex_uv.x - passed * 0.07 * motion_amount, 0.0, 1.0);
    vec4 ink = texture(TEXTURE, tex_uv);
    float edge = (1.0 - smoothstep(0.006, 0.042, passed)) * motion_amount;
    vec3 tint = mix(fresh_tint.rgb, faded_tint.rgb, tail);
    vec3 color = mix(ink.rgb * tint, vec3(1.0, 0.96, 0.85), edge * 0.55);
    // Denser pigment preserves the red body without increasing its glow.
    COLOR = vec4(color, pow(ink.a, 0.72) * visible * tip * fade * (1.0 - release));
}
