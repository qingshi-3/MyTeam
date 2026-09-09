shader_type canvas_item;
render_mode unshaded, blend_add;
#include "res://scenes/vfx/ribbon_motion.gdshaderinc"
uniform float strength : hint_range(0.0, 2.0) = 0.85;
uniform vec4 inner_tint : source_color = vec4(1.0, 0.28, 0.20, 1.0);
uniform vec4 outer_tint : source_color = vec4(0.90, 0.04, 0.09, 1.0);

void fragment() {
    float passed = passed_time(UV.x);
    float visible = smoothstep(0.0, mix(0.06, 0.012, motion_amount), passed);
    float tail = clamp(passed / tail_duration, 0.0, 1.0);
    float fade = pow(1.0 - smoothstep(0.08, 0.85, tail), 1.5);
    // Integrate the painted cross-section, then spread its energy smoothly.
    // A Gaussian profile avoids visibly duplicated streaks from sparse blur taps.
    // This local additive layer needs no full-screen bloom in Compatibility.
    vec2 sample_uv = vec2(clamp(UV.x - passed * 0.07 * motion_amount, 0.0, 1.0),
        0.5);
    float light = 0.0;
    float weights = 0.0;
    for (int i = -3; i <= 3; i++) {
        float offset = float(i);
        float weight = exp(-offset * offset / 4.5);
        vec4 ink = texture(TEXTURE, sample_uv + vec2(0.0, offset * 0.065));
        light += ink.a * dot(ink.rgb, vec3(0.3, 0.5, 0.2)) * weight;
        weights += weight;
    }
    light /= weights;
    float edge = 1.0 - smoothstep(0.015, 0.055, passed);
    float across = abs(UV.y - 0.5) * 2.0;
    float boundary = exp(-across * across * 5.0) * (1.0 - smoothstep(0.65, 1.0, across));
    vec3 tint = mix(inner_tint.rgb, outer_tint.rgb, across);
    float gain = strength * (1.0 + edge * 1.4 * motion_amount);
    COLOR = vec4(tint, light * gain * visible * fade * boundary * (1.0 - release)
        * mix(0.35, 1.0, motion_amount));
}
