shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }

uniform vec4 tint : source_color=vec4(.45,.55,1.0,1.0);
// Authored sprite dimensions, before the shared effect root scales it.
uniform vec2 canvas_size = vec2(166.4,300.0);
uniform float ground_y = 232.0;
uniform float footprint_ratio = 0.6;
uniform vec3 layer_radii = vec3(66.56,47.0,29.0);
uniform vec3 layer_bases = vec3(0.0,35.0,72.0);
uniform vec3 layer_heights = vec3(40.0,42.0,36.0);

float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p) {
    vec2 cell=floor(p), f=fract(p);
    f=f*f*(3.-2.*f);
    return mix(mix(hash(cell),hash(cell+vec2(1,0)),f.x),
        mix(hash(cell+vec2(0,1)),hash(cell+vec2(1,1)),f.x),f.y);
}

vec4 column_layer(vec2 point,float radius,float base,float height,float index,float fade) {
    float t=age*motion_amount;
    // Local patches peel upward at an accelerating rate, instead of a wiping plane.
    float drift=fade*motion_amount;
    point.y-=drift*(17.+20.*noise(vec2(point.x*.055,index+4.)));
    point.x-=drift*7.*sin(point.y*.045+index*2.);
    float x=point.x/radius;
    float h=point.y-base;
    float chord=sqrt(max(0.,1.-x*x));
    float side=1.-smoothstep(.94,1.04,abs(x));
    float lift=motion_amount<.5 ? 1. : smoothstep(index*.045,.29+index*.045,age);
    float roof=height*lift;
    float cap_depth=radius*footprint_ratio;
    float body=smoothstep(-cap_depth*chord-3.6,-cap_depth*chord+6.,h)
        *(1.-smoothstep(roof+cap_depth*chord-12.,roof+cap_depth*chord+5.,h));
    float azimuth=asin(clamp(x,-1.,1.));
    float ribbon=pow(.5+.5*sin(azimuth*8.+h*.008+sin(h*.037-t*5.+index)*.32),4.);
    float flow=.5+.5*sin(h*.10-t*8.+azimuth*1.7+index);
    float rim=exp(-pow((abs(x)-.93)/.055,2.));
    float volume=.23+.16*chord+.10*ribbon*(.4+.6*flow)+.035*flow*chord;
    float base_ring=exp(-pow((length(vec2(x,h/cap_depth))-1.)/.11,2.));
    float top_ring=exp(-pow((length(vec2(x,(h-roof)/cap_depth))-1.)/.17,2.));
    float core=exp(-x*x*5.)*exp(-max(h,0.)*.0125);
    float patches=noise(point*vec2(.085,.065)+vec2(index*9.,0.));
    // Soft patches thin at different rates; avoid hard holes that read as melted wax.
    float erosion=motion_amount<.5 ? 1. : pow(max(1.-fade,0.),.45+patches*2.6);
    float envelope=smoothstep(index*.045,index*.045+.07,age)*(1.-fade);
    float alpha=side*(body*(volume+rim*.10+core*.10)+base_ring*mix(.21,.14,min(index,1.))+top_ring*.065)
        *erosion*envelope*mix(1.,.76,index*.5);
    vec3 color=mix(tint.rgb,vec3(.65,.9,1.),clamp(chord*.48+ribbon*.15+core*.22+index*.10,0.,1.));
    return vec4(color,alpha);
}

void fragment() {
    vec2 point=vec2((UV.x-.5)*canvas_size.x,ground_y-UV.y*canvas_size.y);
    float departure=clamp((progress-.42)/.58,0.,1.);
    float fade=pow(departure,2.2);
    vec4 combined=vec4(0.);
    for (int i=0;i<3;i++) {
        vec4 layer=column_layer(point,layer_radii[i],layer_bases[i],layer_heights[i],float(i),fade);
        combined.rgb=layer.rgb*layer.a+combined.rgb*(1.-layer.a);
        combined.a=layer.a+combined.a*(1.-layer.a);
    }
    COLOR=vec4(combined.rgb/max(combined.a,.0001),combined.a*(1.-release)*tint.a)*layer_color;
}
