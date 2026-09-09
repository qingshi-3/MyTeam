shader_type canvas_item;
render_mode unshaded;
varying vec4 layer_color;
void vertex() { layer_color=COLOR; }
void fragment() {
    vec2 p=(UV-.5)*2.0;
    float r=length(p);
    float a=mod(atan(p.y,p.x)+1.570796,6.283185);
    float sector=floor(a/.6283185);
    float angle0=sector*.6283185-1.570796;
    float angle1=angle0+.6283185;
    float r0=mod(sector,2.)<.5 ? .72 : .34;
    float r1=mod(sector,2.)<.5 ? .34 : .72;
    vec2 v0=vec2(cos(angle0),sin(angle0))*r0;
    vec2 v1=vec2(cos(angle1),sin(angle1))*r1;
    vec2 direction=p/max(r,.0001);
    vec2 segment=v1-v0;
    float denominator=direction.x*segment.y-direction.y*segment.x;
    float edge=(v0.x*v1.y-v0.y*v1.x)/max(.0001,denominator);
    float shape=1.-smoothstep(edge-.015,edge+.015,r);
    float inset=1.-smoothstep(edge-.09,edge-.025,r);
    vec3 gold=mix(vec3(.78,.31,.025),vec3(1.,.82,.23),inset);
    gold=mix(gold,vec3(1.,.98,.68),exp(-dot(p-vec2(-.08,-.10),p-vec2(-.08,-.10))*18.)*.8);
    float halo=exp(-r*r*6.)*.20;
    COLOR=vec4(gold,shape+halo)*layer_color;
}
