shader_type canvas_item;
render_mode unshaded;
#include "res://scenes/vfx/whirlwind_motion.gdshaderinc"
void fragment(){
    vec2 life=whirl_life(UV.x);
    vec4 brush=texture(TEXTURE,vec2(fract(UV.x*2.0-age*.2*motion_amount),mix(.30,.72,UV.y)));
    float ink=brush.a*.55+dot(brush.rgb,vec3(.3,.5,.2))*.45;
    float sharp=exp(-pow((UV.y-.88)/.024,2.0));
    float body=smoothstep(.05,.42,UV.y)*(1.0-smoothstep(.78,.96,UV.y));
    float grain=.34+.66*pow(ink,.75);
    float alpha=life.x*(sharp*.93+body*grain*.42);
    vec3 color=mix(vec3(.30,.45,.53),vec3(.86,.94,.96),UV.y);
    color=mix(color,vec3(1.,.98,.88),sharp*.68+life.y*.16);
    COLOR=vec4(color,alpha*(1.0-release));
}
