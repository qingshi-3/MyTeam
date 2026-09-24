shader_type canvas_item;
render_mode unshaded, blend_add;
#include "res://scenes/vfx/piercing_thrust_motion.gdshaderinc"
void fragment(){
    vec3 shape=thrust_shape(UV);
    COLOR=vec4(1.0,.61,.24,(shape.x*.095+shape.y*.055)*(1.0-release)*mix(.25,1.0,motion_amount));
}
