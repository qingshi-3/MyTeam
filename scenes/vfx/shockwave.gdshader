shader_type canvas_item;
render_mode unshaded;
#include "res://scenes/vfx/shock_motion.gdshaderinc"

uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform float impact = 0.0;
uniform bool sustained = false;
uniform vec4 wave_color : source_color = vec4(0.42, 0.82, 1.0, 1.0);
varying vec4 layer_color;

void vertex() { layer_color = COLOR; }
void fragment() {
    float t = clamp(progress, 0.0, 1.0);
    float distance_from_center = length(UV - 0.5);
    // Fast initial impulse followed by continued outward travel, including
    // throughout the fade. No angular mask, rotation, or fragment erosion.
    float radius = shock_radius(t, motion_amount);
    float width = mix(0.021, 0.009, t);
    float offset = distance_from_center - radius;
    float crest = exp(-pow(offset / width, 2.0));
    float glow = exp(-pow(offset / (width * 2.5), 2.0)) * 0.22;
    float wake = exp(-pow((offset + width * 1.6) / (width * 2.0), 2.0)) * 0.12;
    float appear = smoothstep(0.0, 0.035, age);
    float fade = 1.0 - smoothstep(0.12, 1.0, t);
    float alpha = (crest * 0.72 + glow + wake) * appear * fade * (1.0 - release);
    // Keep the soft outer tail inside the sprite even at the final radius.
    alpha *= 1.0 - smoothstep(0.485, 0.5, distance_from_center);
    vec3 color = mix(wave_color.rgb, vec3(0.85, 0.97, 1.0), crest * 0.4);
    COLOR = vec4(color, alpha * wave_color.a) * layer_color;
}
