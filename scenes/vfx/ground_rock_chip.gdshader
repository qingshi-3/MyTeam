shader_type canvas_item;
render_mode unshaded;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec2 p=UV*2.-1.;
    float edge=max(abs(p.x)*.86+abs(p.y)*.40,max(p.y*.95-p.x*.26,-p.y*.85-p.x*.36));
    float alpha=1.-smoothstep(.68,.78,edge);
    vec4 stone=texture(TEXTURE,vec2(.36+UV.x*.25,.43+UV.y*.30));
    float facet=.75+step(p.x+p.y*.42,.04)*.4;
    COLOR=vec4(stone.rgb*facet*1.2,alpha)*layer_color;
}
