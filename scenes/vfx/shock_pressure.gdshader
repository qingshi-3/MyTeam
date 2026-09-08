shader_type canvas_item;
render_mode unshaded, blend_add;
#include "res://scenes/vfx/shock_motion.gdshaderinc"
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
void fragment() {
    float radius = shock_radius(progress, motion_amount);
    vec2 p = UV - 0.5;
    float distance = length(p);
    // The ring art contributes only material breakup behind the front. Its
    // angular orientation stays fixed throughout expansion and dissipation.
    vec2 source_uv = p * (0.38 / max(radius, 0.025)) + 0.5;
    vec4 ink = texture(TEXTURE, clamp(source_uv, vec2(0.0), vec2(1.0)));
    float band = smoothstep(radius - 0.10, radius - 0.03, distance)
        * (1.0 - smoothstep(radius - 0.005, radius + 0.012, distance));
    float fade = 1.0 - smoothstep(0.12, 0.95, progress);
    float alpha = ink.a * band * fade * smoothstep(0.0, 0.035, age) * (1.0 - release);
    COLOR = vec4(mix(vec3(0.12, 0.48, 1.0), vec3(0.7, 0.95, 1.0), ink.b), alpha * 0.35);
}
