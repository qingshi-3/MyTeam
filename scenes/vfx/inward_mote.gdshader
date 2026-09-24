shader_type canvas_item;
render_mode unshaded, blend_add;
varying vec4 mote_color;
void vertex() { mote_color = COLOR; }
void fragment() {
    vec4 tex = texture(TEXTURE,UV);
    float light = dot(tex.rgb,vec3(0.2126,0.7152,0.0722));
    COLOR = vec4(mote_color.rgb*(0.55+light*0.6),pow(tex.a,1.6)*mote_color.a);
}
