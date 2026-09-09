shader_type canvas_item;
render_mode unshaded;
uniform float phase = 0.0;
uniform float motion_amount = 1.0;
varying vec4 tint;
void vertex() { tint=COLOR; }
void fragment() {
    vec2 p=(UV-.5)*2.0;
    float grow=smoothstep(.03,.53,phase);
    float pop=smoothstep(.53,.63,phase);
    float radius=mix(.06,.27,grow);
    vec2 q=(p-vec2(0.0,-radius*.40))/radius;
    float r=length(q);
    float dome=(1.0-smoothstep(.94,1.04,r))*(1.0-pop);
    float z=sqrt(max(0.0,1.0-dot(q,q)));
    vec3 normal=normalize(vec3(q,z+.001));
    float diffuse=max(dot(normal,normalize(vec3(-.4,-.7,1.0))),0.0);
    float spec=pow(max(dot(normal,normalize(vec3(-.36,-.5,1.2))),0.0),22.0);
    vec3 color=mix(vec3(.025,.20,.07),vec3(.40,.78,.12),diffuse);
    color+=vec3(.75,1.0,.42)*spec*.8;
    float alpha=dome*(.48+.26*diffuse);
    // The rim opens into separated crown lobes, then droplets fall back.
    float u=clamp((phase-.55)/.35,0.0,1.0);
    float burst=smoothstep(.54,.58,phase)*(1.0-smoothstep(.75,.91,phase))*motion_amount;
    float ringRadius=.23+u*.40;
    vec2 ringP=vec2(p.x,p.y*2.4);
    float ripple=exp(-pow((length(ringP)-ringRadius)/.035,2.0))*burst*.42;
    color=mix(color,vec3(.32,.64,.10),clamp(ripple,0.0,1.0));alpha=max(alpha,ripple);
    float droplets=0.0;
    for(int i=0;i<5;i++) {
        float angle=float(i)*1.256637+.35;
        vec2 location=vec2(cos(angle),sin(angle)*.40)*(.16+u*.38);
        location.y-=sin(u*3.141593)*(.25+.05*sin(float(i)*2.0));
        float drop=1.0-smoothstep(.034,.065,length(p-location));
        droplets=max(droplets,drop*burst);
    }
    color=mix(color,vec3(.49,.85,.15),droplets);alpha=max(alpha,droplets*.8);
    float life=smoothstep(.0,.08,phase)*(1.0-smoothstep(.88,1.0,phase));
    COLOR=vec4(color,alpha*life)*tint;
}
