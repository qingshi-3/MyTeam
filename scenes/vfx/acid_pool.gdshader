shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 p=(UV-.5)*2.;
    float a=atan(p.y,p.x),r=length(p);
    float edge=.70+sin(a*5.+.8)*.075+sin(a*9.-1.4)*.035+sin(a*13.)*.024;
    float body=1.-smoothstep(edge-.018,edge+.018,r);
    float rim=smoothstep(edge-.065,edge-.03,r)*body;
    float inner=1.-smoothstep(.1,.63,length(p-vec2(-.14,-.13)));
    float fleck=0.;
    for(int i=0;i<5;i++){
        float k=float(i),angle=k*2.4+.3;
        vec2 center=vec2(cos(angle),sin(angle))*(.82+sin(k*4.)*.055);
        fleck=max(fleck,1.-smoothstep(.022,.058,length(p-center)));
    }
    float ripple=pow(max(0.,1.-abs(r-(age*1.6+.13))/.035),2.)*body*.14;
    vec3 color=mix(vec3(.13,.25,.025),vec3(.37,.59,.065),inner);
    color+=vec3(.25,.34,.04)*rim+vec3(.35,.46,.07)*ripple;
    COLOR=vec4(mix(color,vec3(.39,.6,.08),fleck),max(body*.83,fleck*.7))*layer_color;
}
