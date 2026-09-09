shader_type canvas_item;
render_mode unshaded;
void fragment() {
    vec2 p=UV-.5;
    float r=length(p);
    float shell=exp(-pow((r-.32)/.038,2.0));
    float lens=exp(-dot(p,p)*22.0)*.12;
    float highlight=exp(-dot(p-vec2(-.11,-.19),p-vec2(-.11,-.19))*380.0);
    vec4 color=COLOR;
    COLOR=vec4(mix(vec3(.12,.65,.27),vec3(.8,1.0,.5),highlight),
        (shell*.5+lens+highlight*.65)*color.a);
}
