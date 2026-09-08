shader_type canvas_item;
render_mode unshaded;
#include "res://scenes/vfx/fire_palette.gdshaderinc"
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float motion_amount = 1.0;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    vec2 uv = UV;
    float tail = 1.0 - smoothstep(0.45, 0.85, UV.x);
    uv.y += (sin(UV.x * 22.0 - age * 23.0) * 0.035 + sin(UV.x * 43.0 - age * 34.0) * 0.012) * tail * motion_amount;
    vec4 ink = texture(TEXTURE, uv);
    float heat = dot(ink.rgb, vec3(0.25, 0.45, 0.30)) * 0.68;
    float head_mask = 1.0 - smoothstep(0.72, 0.91, UV.x);
    COLOR = vec4(fire_color(heat), pow(ink.a, 0.85) * head_mask) * layer_color;
}
