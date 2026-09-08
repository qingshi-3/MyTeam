shader_type canvas_item;
render_mode unshaded, blend_add;
#include "res://scenes/vfx/fire_palette.gdshaderinc"
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    vec4 ink = texture(TEXTURE, UV);
    float heat = dot(ink.rgb, vec3(0.25, 0.45, 0.3));
    COLOR = vec4(fire_color(heat * 0.95), ink.a) * layer_color;
}
