shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform float release=0.0;
uniform float motion_amount=1.0;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 p=(UV-.5)*2.;float radius=length(p);float a=atan(p.y,p.x);
    float angular=abs(sin(a*6.0+sin(radius*21.0)*.4));
    float split=exp(-angular*60.0)*smoothstep(.1,.2,radius)*(1.-smoothstep(.65,.88,radius));
    float first=smoothstep(0.,.06,age)*(1.-smoothstep(.18,.65,age));
    float expansion=.15+min(age/.36,1.)*.65;
    float pressure=exp(-pow((radius-expansion)*34.0,2.0))*exp(-age*5.0);
    vec3 color=mix(vec3(.34,.16,.055),vec3(1.,.63,.21),exp(-age*8.));
    COLOR=vec4(color,(split*.75+pressure*.18)*first*(1.-release))*layer_color;
}
