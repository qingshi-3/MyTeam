shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform bool active = true;
varying vec4 tint;
void vertex(){tint=COLOR;}
void fragment(){
    vec4 ink=texture(TEXTURE,UV);
    // Each tip arrives before its darker wake; the cut is drawn, then erodes.
    float along=clamp(UV.x*.62+UV.y*.38,0.0,1.0);
    float front=age*6.5;
    float reveal=1.0-smoothstep(front-.055,front,along);
    float local=max(0.0,age-along/6.5);
    float ridge=exp(-pow((front-along)*16.0,2.0));
    float grain=sin(UV.y*167.0+UV.x*63.0)*sin(UV.x*103.0-UV.y*51.0);
    float erosion=1.0-smoothstep(.13+grain*.025,.38,local);
    vec3 col=mix(vec3(.5,.20,.07),vec3(1.0,.88,.61),max(ridge,exp(-local*10.0)*.85));
    float tail=1.0-smoothstep(.17,.44,local);
    COLOR=vec4(col,ink.a*reveal*erosion*tail*(active?1.0:0.0))*tint;
}
