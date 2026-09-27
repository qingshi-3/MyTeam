shader_type canvas_item;
render_mode unshaded;
varying vec4 tint;
void vertex(){tint=COLOR;}
void fragment(){
    vec4 ink=texture(TEXTURE,UV);
    float plate=dot(ink.rgb,vec3(.25,.6,.15));
    float groove=pow(.5+.5*sin(UV.y*34.0+UV.x*9.0),12.0);
    vec3 col=mix(vec3(.075,.10,.125),vec3(.37,.43,.43),smoothstep(.08,.8,plate));
    col*=1.0-groove*.16;
    COLOR=vec4(col,ink.a)*tint;
}
