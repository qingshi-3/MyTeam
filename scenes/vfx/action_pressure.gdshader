shader_type canvas_item;
render_mode unshaded;
uniform sampler2D wind : repeat_enable, filter_linear;
uniform float age=0.0;
uniform float strength=1.0;
uniform vec4 tint : source_color = vec4(1.0,.87,.57,1.0);
void fragment(){
    vec2 p=vec2((UV.x+.16)*.86,(UV.y-.5)*1.9);
    float r=length(p);
    float angle=atan(p.y,p.x);
    float band=(r-.75)*3.4+.5;
    float shape=texture(wind,vec2(angle*.65-age*.13,band)).a;
    float trim=smoothstep(.08,.25,UV.x)*(1.-smoothstep(1.05,1.3,abs(angle)));
    float core=exp(-pow((r-.77)*62.,2.));
    float alpha=(shape*.8+core*.6)*trim*(1.-smoothstep(.16,.26,abs(r-.75)));
    vec3 col=mix(tint.rgb,vec3(1.,.99,.9),core);
    COLOR=vec4(col,alpha*strength*tint.a);
}
