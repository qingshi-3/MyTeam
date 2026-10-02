shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform float progress=0.0;
uniform float release=0.0;
uniform float motion_amount=1.0;
uniform int shape=0;
uniform vec4 tint : source_color = vec4(0.4,0.85,1.0,1.0);
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    float x=UV.x;
    float y=abs(UV.y-0.5);
    if(shape==1){
        float saw=abs(fract(x*9.0)-0.5)*2.0;
        y=abs(UV.y-0.5-(saw-0.5)*0.40);
    }
    float core=exp(-y*y*1300.0);
    float halo=exp(-y*y*90.0)*0.35;
    float envelope=pow(max(0.0,1.0-progress),0.75)*(1.0-release);
    float ends=smoothstep(0.0,0.018,x)*(1.0-smoothstep(0.98,1.0,x));
    COLOR=vec4(mix(tint.rgb,vec3(1.0),core*0.65),(core+halo)*envelope*ends)*layer_color;
}
