shader_type canvas_item;
render_mode unshaded;
uniform vec4 tint : source_color = vec4(1.0);
uniform sampler2D flow : repeat_enable, filter_linear;
uniform float progress=0.0;
uniform float age=0.0;
uniform float seed=0.0;
uniform int cell=0;
uniform int kind=0;
void fragment(){
    vec2 uv=UV;
    vec2 curl=texture(flow,uv*.55+vec2(seed,age*.13)).rg-.5;
    uv+=curl*(kind==1?.12:.055)*sin(progress*3.14159);
    vec2 tile=vec2(float(cell%2),float(cell/2));
    float density=texture(TEXTURE,(clamp(uv,vec2(.01),vec2(.99))+tile)*.5).a;
    float dissolve=smoothstep(.0,.12,progress)*(1.0-smoothstep(.48,1.0,progress));
    float a=clamp(density*(kind==1?3.2:1.8),0.,1.);
    // Fire retains the torn bright tongues. Dust keeps its soft low-alpha fringe.
    a*=(kind==1?smoothstep(progress*.25,progress*.25+.18,density):1.0)*dissolve;
    vec3 rgb=tint.rgb;
    if(kind==1){
        float heat=clamp(density*2.6+(1.-progress)*.4-.27,0.,1.);
        rgb=mix(vec3(.68,.035,.005),vec3(1.,.37,.025),smoothstep(0.,.38,heat));
        rgb=mix(rgb,vec3(1.,.88,.38),smoothstep(.35,.78,heat));
        rgb=mix(rgb,vec3(1.,.99,.82),smoothstep(.83,1.,heat));
    }
    COLOR=vec4(rgb,a*tint.a);
}
