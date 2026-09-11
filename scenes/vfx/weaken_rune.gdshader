shader_type canvas_item;
render_mode unshaded;
uniform float age=0.0;
uniform float particle_progress=0.0;
uniform float motion_amount=1.0;
varying vec4 particle_color;
void vertex() { particle_color=COLOR; }
float hash(vec2 p) { return fract(sin(dot(p,vec2(127.1,311.7)))*43758.5453); }
float noise(vec2 p) {
    vec2 i=floor(p),f=fract(p);f=f*f*(3.-2.*f);
    return mix(mix(hash(i),hash(i+vec2(1,0)),f.x),mix(hash(i+vec2(0,1)),hash(i+vec2(1,1)),f.x),f.y);
}
void fragment() {
    float dissolve=motion_amount<.5 ? 0. : smoothstep(.60,1.,particle_progress);
    vec2 uv=UV;
    // The rune stays rigid while falling; only its fading fragments separate.
    uv.x+=sin(UV.y*19.+age)*.012*dissolve;
    vec4 ink=texture(TEXTURE,uv);
    float n=noise(UV*vec2(13.,16.));
    float breakup=1.-smoothstep(n-.14,n+.24,dissolve*1.35);
    float sweep=motion_amount<.5 ? .45 : fract(age*.65);
    float sheen=exp(-pow((UV.y-sweep)*8.,2.));
    vec3 color=ink.rgb*(.92+sheen*.4);
    COLOR=vec4(color,ink.a*breakup)*particle_color;
}
