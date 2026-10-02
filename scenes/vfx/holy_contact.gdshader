shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age=0.0;
uniform float progress=0.0;
uniform float release=0.0;
uniform float motion_amount=1.0;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 p=(UV-0.5)*2.0;
    float glow=exp(-dot(p,p)*7.0);
    float gleam=exp(-p.x*p.x*11.0-p.y*p.y*85.0);
    float fade=1.0-smoothstep(0.045,0.22,age);
    COLOR=vec4(1.0,0.92,0.65,(glow*0.17+gleam*0.36)*fade*(1.0-release))*layer_color;
}
