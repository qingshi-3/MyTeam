shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform sampler2D grain: repeat_enable, filter_linear;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 p=UV*2.0-1.0;
    float r=length(p),a=atan(p.y,p.x);
    float phase=fract(a/6.283185-age*1.7);
    float sweep=pow(max(0.0,sin(phase*18.84955)),5.0);
    float irregular=texture(grain,vec2(phase, r*.6+age*.06)).r;
    float edge=exp(-pow((r-.76)/.036,2.0))*sweep;
    float inner=exp(-pow((r-.65)/.11,2.0))*sweep*.18;
    COLOR=vec4(mix(vec3(.46,.59,.65),vec3(.96,.93,.76),edge),
        (edge+inner)*(.5+irregular*.5)*(1.0-smoothstep(.9,1.,r)))*layer_color;
}
