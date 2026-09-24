shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age=0.0;
uniform float travel_duration=.045;
uniform float tail_duration=.22;
uniform float release=0.0;
uniform float motion_amount=1.0;
uniform float strength=1.0;
uniform float root_fade=0.0;
varying float branch_strength;
void vertex(){branch_strength=COLOR.g;}
float hash(float n){return fract(sin(n*127.1)*43758.5453);}
float noise(float x){float i=floor(x);float f=fract(x);return mix(hash(i),hash(i+1.0),smoothstep(0.,1.,f));}
void fragment(){
    float arrival=UV.x*travel_duration*motion_amount;
    float t=age-arrival;
    float flow=noise(UV.x*24.0-age*19.0*motion_amount);
    float d=abs(UV.y-.5)*2.0*(.78+flow*.40);
    float core=exp(-pow(d/.10,2.0));
    float corona=exp(-pow(d/.38,2.0));
    float glow=exp(-pow(d/.74,2.0));
    float life=smoothstep(0.0,.008,t)*(1.0-smoothstep(.02,tail_duration,t));
    float current=.65+.35*flow;
    vec3 color=mix(vec3(.17,.30,1.0),vec3(.91,.98,1.0),clamp(core+corona*.27,0.,1.));
    float start_mask=root_fade>0.0?smoothstep(0.0,root_fade,UV.x):1.0;
    COLOR=vec4(color,(core+corona*.4+glow*.18)*life*current*branch_strength*strength*(1.0-release)*mix(.30,1.,motion_amount)*start_mask);
}
