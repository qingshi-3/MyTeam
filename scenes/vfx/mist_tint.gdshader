shader_type canvas_item;
render_mode unshaded;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    vec4 ink=texture(TEXTURE,UV);
    float light=dot(ink.rgb,vec3(.3,.5,.2));
    COLOR=vec4(vec3(.35+light*.9),ink.a)*layer_color;
}
