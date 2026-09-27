shader_type canvas_item;
render_mode unshaded;
uniform vec4 tint:source_color=vec4(.5,.8,.1,1.);
uniform float progress=0.0;
uniform float age=0.0;
uniform float seed=0.0;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 p=UV*2.-1.;float r=length(p*vec2(1.,1.15));
    float body=1.-smoothstep(.66,.87,r+.07*sin(p.y*8.+seed));
    float shine=exp(-length((p-vec2(-.15,-.20))*vec2(7.,8.)));
    vec3 color=mix(tint.rgb*.5,tint.rgb,1.-r*.4)+vec3(.65,.8,.3)*shine;
    COLOR=vec4(color,body*tint.a*(1.-smoothstep(.55,1.,progress)))*layer_color;
}
