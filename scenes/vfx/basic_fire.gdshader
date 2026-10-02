shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float motion_amount = 1.0;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    float clock=age*motion_amount;
    vec2 uv=UV;
    uv.y+=sin(UV.x*13.0-clock*16.0)*0.028*motion_amount;
    uv.x+=sin(UV.y*9.0+clock*8.0)*0.016*motion_amount;
    vec4 ink=texture(TEXTURE,clamp(uv,vec2(0.001),vec2(0.999)));
    float head=length((UV-vec2(0.62,0.5))/vec2(0.34,0.43));
    float tail=abs(UV.y-0.5+sin(UV.x*14.0-clock*17.0)*0.04*motion_amount)/(0.04+UV.x*0.33);
    float mask=(1.0-smoothstep(0.8,1.0,min(head,tail+smoothstep(0.45,0.75,UV.x)*3.0)))*smoothstep(0.0,0.12,UV.x);
    COLOR=vec4(ink.rgb*1.2,ink.a*mask)*layer_color;
}
