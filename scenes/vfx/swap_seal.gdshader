shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
varying vec4 layer_color;
void vertex(){layer_color=COLOR;}
void fragment(){
    vec4 tex=texture(TEXTURE,UV);vec2 p=UV-.5;
    float a=atan(p.y,p.x),r=length(p);
    float sweep=pow(max(0.,cos(a-age*2.7)),5.);
    vec3 color=mix(vec3(.43,.34,.73),vec3(.8,.91,1.),sweep);
    COLOR=vec4(color,tex.a*(.65+sweep*.35)*smoothstep(0.,.16,age))*layer_color;
}
