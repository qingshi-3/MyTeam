shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform float release=0.0;
uniform float entry=0.0;
uniform float motion_amount=1.0;
uniform sampler2D flow_texture : repeat_enable, filter_linear_mipmap;
void fragment(){
    vec2 p=(UV-.5)*2.;float r=length(p);float a=atan(p.y,p.x);
    float texture_flow=texture(flow_texture,vec2(a/6.2831853-age*.38*motion_amount,mix(.28,.69,r))).a;
    float skirt=exp(-pow((r-.53)*6.5,2.))*texture_flow;
    float edge=exp(-pow((r-.76)*35.,2.))*texture_flow*.24;
    float alpha=(skirt*.26+edge)*entry*(1.-release);
    COLOR=vec4(.35,.72,.76,alpha);
}
