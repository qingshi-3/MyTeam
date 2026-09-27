shader_type canvas_item;
render_mode unshaded;
uniform sampler2D flame : repeat_enable, filter_linear;
uniform sampler2D flow : repeat_enable, filter_linear;
uniform float age=0.0;
uniform float supply=1.0;
uniform float stop_age=-1.0;
void fragment(){
    float x=UV.x;
    float v=(UV.y-.5)/max(.06,x*.92);
    vec2 curl=texture(flow,vec2(x*.7-age*.65,v*.4+age*.12)).rg-.5;
    float f=texture(flame,vec2(v*.8+curl.x*.28,-x*1.3+age*1.9+curl.y*.2)).r;
    float edge=1.-smoothstep(.32,.53,abs(v)+curl.x*.07);
    float tip=1.-smoothstep(.70,.99,x+curl.y*.15);
    float body=pow(f,.65)*edge*tip*smoothstep(0.,.05,x);
    vec3 col=mix(vec3(.92,.07,.003),vec3(1.,.65,.07),smoothstep(.05,.5,body));
    col=mix(col,vec3(1.,.96,.63),smoothstep(.55,.88,body));
    float start=smoothstep(x*.09,x*.09+.045,age);
    float feed=stop_age<0.0?1.0:1.0-smoothstep(x*.18,x*.18+.09,stop_age);
    COLOR=vec4(col,clamp(body*1.4,0.,1.)*supply*start*feed);
}
