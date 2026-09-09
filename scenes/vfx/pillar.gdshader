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
void fragment() {
    float x=(UV.x-.5)*2.;
    float h=1.-UV.y;
    float t=age*motion_amount;
    float azimuth=asin(clamp(x,-1.,1.));
    float wave=sin(azimuth*7.+sin(h*7.-t*4.)*.7);
    float fine=sin(azimuth*19.-h*4.+t*1.3);
    float ribs=pow(.5+.5*wave,5.)*(.65+.35*fine);
    float flow=.65+.35*sin(h*17.-t*9.+azimuth*2.);
    float edge=1.-smoothstep(.76,1.,abs(x));
    // Rise from the actual ground edge. Shutdown also travels upward;
    // it must not leave a stationary curtain floating above the seal.
    float front=motion_amount<.5 ? 1.1 : smoothstep(0.,.32,age)*1.1;
    float reveal=1.-smoothstep(front-.06,front+.04,h);
    float cutoff=smoothstep(.52,.95,progress)*1.1;
    float clear=motion_amount<.5 ? 1.-smoothstep(.52,1.,progress)
        : smoothstep(cutoff-.07,cutoff+.03,h);
    float top=1.-smoothstep(.78,1.,h);
    float base=exp(-h*15.)*.22;
    float alpha=edge*top*reveal*clear*(.055+ribs*.42*flow+base);
    vec3 color=mix(tint.rgb,vec3(.78,.95,1.),ribs*.7+base);
    COLOR=vec4(color,alpha*(1.-release)*tint.a)*layer_color;
}
