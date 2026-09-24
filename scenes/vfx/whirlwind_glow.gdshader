shader_type canvas_item;
render_mode unshaded, blend_add;
#include "res://scenes/vfx/whirlwind_motion.gdshaderinc"
void fragment(){
    vec2 life=whirl_life(UV.x);
    float glow=exp(-pow((UV.y-.91)/.13,2.0));
    COLOR=vec4(.48,.76,.88,glow*life.x*(.15+life.y*.36)*(1.0-release)*mix(.25,1.0,motion_amount));
}
