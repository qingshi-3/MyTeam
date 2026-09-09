shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }

float hash(float n) { return fract(sin(n*127.1)*43758.5453); }
float zig(float x, float seed) {
    float u=x*18.0;
    float i=floor(u);
    return mix(hash(i+seed),hash(i+1.0+seed),fract(u))-.5;
}
void fragment() {
    float shift = sin(age*24.0)*motion_amount*.35;
    float path=.5+zig(UV.x,2.0+shift)*.26;
    float d=abs(UV.y-path);
    float main=exp(-d*d*8000.0);
    float glow=exp(-d*d*420.0)*.65;
    float branch1=abs(UV.y-(path+max(0.0,UV.x-.37)*.55+zig(UV.x,42.0)*.08));
    float branch2=abs(UV.y-(path-max(0.0,UV.x-.61)*.85+zig(UV.x,71.0)*.08));
    float branches=(exp(-branch1*branch1*35000.0)*smoothstep(.37,.43,UV.x)
        +exp(-branch2*branch2*35000.0)*smoothstep(.61,.66,UV.x))*.6;
    float ink=texture(TEXTURE,vec2(UV.x,.5+d)).a;
    float flash=1.5*exp(-age*6.0)+.38*exp(-pow((age-.16)/.06,2.0));
    float envelope=smoothstep(0.0,.01,age)*(1.0-smoothstep(.6,1.0,progress));
    COLOR=vec4(mix(vec3(.26,.38,1.0),vec3(.92,.96,1.0),main+branches),
        (main+glow+branches)*(.8+ink*.2)*flash*envelope*(1.0-release)*mix(.65,1.0,motion_amount))*layer_color;
}
