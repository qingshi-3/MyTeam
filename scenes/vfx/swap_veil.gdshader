shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform bool active=false;
uniform sampler2D flow:repeat_enable,filter_linear;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 p=UV;float n=texture(flow,vec2(p.x*1.4,p.y*.8-age*.6)).r;
    float v=sin(p.y*3.14159);
    float width=(.07+v*.29)*(active?1.:.65);
    float dist=abs(p.x-.5+(n-.5)*.09);
    float rim=exp(-pow((dist-width)/.025,2.))*v;
    float core=exp(-pow(dist/(active?.11:.035),2.))*v;
    float body=(1.-smoothstep(width*.4,width,dist))*v*(.16+n*.22);
    float shards=pow(max(0.,n-.38)*1.5,3.)*rim;
    COLOR=vec4(mix(vec3(.30,.23,.74),vec3(.72,.9,1.),core*.8+rim*.3),
        clamp(body+rim*.65+core*(active?.65:.2)+shards,0.,1.))*layer_color;
}
