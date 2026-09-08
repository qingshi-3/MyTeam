shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float motion_amount = 1.0;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    float r = length((UV - 0.5) * 2.0);
    float alpha = exp(-r * r * 5.0) * 0.6;
    COLOR = vec4(1.0, 0.18, 0.025, alpha) * layer_color;
}
