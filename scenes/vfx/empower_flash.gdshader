shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
varying vec4 tint;
void vertex() { tint = COLOR; }
void fragment() {
    vec2 p = (UV - .5) * 2.0;
    float glow = exp(-dot(p,p) * 7.0);
    float life = smoothstep(.0,.045,age) * (1.0-smoothstep(.10,.45,age));
    COLOR = vec4(1.0,.56,.10,glow*life*.5*(1.0-release)*mix(.4,1.0,motion_amount))*tint;
}
