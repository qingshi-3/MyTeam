shader_type canvas_item;
render_mode unshaded;
#include "res://scenes/vfx/piercing_thrust_motion.gdshaderinc"
void fragment(){
    vec3 shape=thrust_shape(UV);
    vec4 brush=texture(TEXTURE,vec2(UV.x,mix(.28,.72,UV.y)));
    float ink=brush.a*.5+dot(brush.rgb,vec3(.3,.5,.2))*.5;
    float body=shape.x*(.55+.45*ink);
    vec3 color=mix(vec3(.82,.41,.12),vec3(1.0,.88,.56),ink);
    color=mix(color,vec3(1.,.98,.86),shape.z*.8);
    COLOR=vec4(color,(body+shape.y*.36)*(1.0-release));
}
