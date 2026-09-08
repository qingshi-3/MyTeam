shader_type canvas_item;
render_mode unshaded;
#include "res://scenes/vfx/shock_motion.gdshaderinc"
#include "res://scenes/vfx/fire_palette.gdshaderinc"
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    vec2 p = UV - 0.5;
    float r = length(p);
    vec2 direction = p / max(r, 0.0001);
    float front = shock_radius(progress, motion_amount);
    // Reuse only the painted flame material in a radial band. The front is
    // driven independently; texture flow stays radial and never becomes a spin.
    float across = clamp((r - front + 0.095) / 0.14, 0.0, 1.0);
    float source_radius = mix(0.24, 0.49, across) + age * 0.022 * motion_amount;
    vec4 ink = texture(TEXTURE, clamp(direction * source_radius + 0.5, vec2(0.0), vec2(1.0)));
    float band = smoothstep(front - 0.095, front - 0.06, r)
        * (1.0 - smoothstep(front + 0.005, front + 0.035, r));
    float crest = exp(-pow((r - front + 0.014) / 0.012, 2.0));
    float heat = dot(ink.rgb, vec3(0.18, 0.40, 0.42));
    heat = clamp(heat * 0.85 + crest * 0.15, 0.0, 1.0);
    float fade = 1.0 - smoothstep(0.08, 1.0, progress);
    float alpha = (pow(ink.a, 0.8) * band * 0.95 + crest * 0.18)
        * smoothstep(0.0, 0.025, age) * fade * (1.0 - release);
    alpha *= 1.0 - smoothstep(0.48, 0.5, r);
    COLOR = vec4(fire_color(heat), alpha) * layer_color;
}
