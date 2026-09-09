shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age=0.0;
uniform float release=0.0;
uniform float motion_amount=1.0;
void fragment(){
    vec2 p=(UV-.5)*2.;
    float core=exp(-dot(p,p)*95.);
    float halo=exp(-dot(p,p)*8.0);
    float flash=smoothstep(0.,.014,age)*(1.-smoothstep(.035,.19,age));
    COLOR=vec4(mix(vec3(.16,.32,1.),vec3(.88,.98,1.),core),
        (core+halo*.20)*flash*(1.-release)*mix(.22,1.,motion_amount));
}
