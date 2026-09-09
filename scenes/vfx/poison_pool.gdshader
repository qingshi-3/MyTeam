shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }

void fragment() {
    float t=age*motion_amount;
    vec2 drift=vec2(sin(UV.y*17.0+t*.65),cos(UV.x*14.0-t*.5))*.008;
    vec4 ink=texture(TEXTURE,UV+drift);
    float detail=max(ink.g,ink.b);
    // A translucent liquid film; no opaque smoke volume in the center.
    float alpha=ink.a*(.09+smoothstep(.15,.65,detail)*.46);
    float caustic=.86+.14*sin(UV.x*25.0+UV.y*19.0-t*1.6);
    float life=smoothstep(0.0,.26,age)*(1.0-release);
    COLOR=vec4(ink.rgb*vec3(.75,1.12,.82)*caustic,alpha*life)*layer_color;
}
