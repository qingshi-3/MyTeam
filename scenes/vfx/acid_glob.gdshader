shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform sampler2D flow: repeat_enable, filter_linear;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 p=UV;float noise=texture(flow,UV*.7+vec2(-age*.6,age*.12)).r;
    float y=.5+sin(p.x*9.-age*17.)*.035*(1.-p.x);
    float head=length((p-vec2(.66,y))/vec2(.27,.33));
    float tail=abs(p.y-y)/(.05+.18*p.x)+(1.-smoothstep(.05,.52,p.x))*.7;
    float shape=min(head,tail+smoothstep(.60,.82,p.x)*4.);
    float alpha=(1.-smoothstep(.83,.97,shape+noise*.12))*smoothstep(.01,.14,p.x);
    float light=exp(-dot((p-vec2(.66,.36))*vec2(5.,7.),(p-vec2(.66,.36))*vec2(5.,7.)));
    vec3 body=mix(vec3(.10,.24,.02),vec3(.46,.77,.06),clamp(1.-shape*.7+noise*.22,0.,1.));
    body=mix(body,vec3(.87,1.,.43),light*.92);
    COLOR=vec4(body,alpha*.96)*layer_color;
}
