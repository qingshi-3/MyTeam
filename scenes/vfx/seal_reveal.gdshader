shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }

uniform vec4 tint : source_color=vec4(.5,.45,1.0,1.0);
uniform float spin_speed=.22;
void fragment() {
    vec2 p=UV-.5;
    float r=length(p);
    float spin=age*spin_speed*motion_amount*mix(-.7,1.0,smoothstep(.30,.38,r));
    vec2 q=mat2(vec2(cos(spin),sin(spin)),vec2(-sin(spin),cos(spin)))*p;
    // Slightly shrink source so the authored tips have room to rotate.
    vec2 uv=q*1.10+.5;
    vec4 ink=texture(TEXTURE,clamp(uv,vec2(0.0),vec2(1.0)));
    float valid=step(0.0,uv.x)*step(uv.x,1.0)*step(0.0,uv.y)*step(uv.y,1.0);
    // The whole outline exists at once. Radial bands energize independently;
    // never draw a pie wedge from a fixed zero-angle seam.
    float band=mix(.04,.16,smoothstep(.18,.34,r));
    float reveal=smoothstep(band,band+.16,age);
    float angle=atan(p.y,p.x);
    float sweep=.88+.12*sin(angle*6.0-age*2.8*motion_amount);
    float fade=sustained?1.0:1.0-smoothstep(.62,1.0,progress);
    float l=dot(ink.rgb,vec3(.22,.55,.23));
    // Select the luminous engraving from the source; the darker metal backing
    // does not become another opaque layer beneath the summoned light.
    float engraving=smoothstep(.30,.72,l);
    COLOR=vec4(tint.rgb*(.52+l*.85),ink.a*engraving*valid*reveal*sweep*fade*(1.0-release)*tint.a)*layer_color;
}
