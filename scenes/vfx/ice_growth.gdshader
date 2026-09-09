shader_type canvas_item;
render_mode unshaded;
uniform float growth=0.0;
uniform float fracture=0.0;
uniform float release=0.0;
uniform float motion_amount=1.0;
varying vec4 tint;
void vertex(){tint=COLOR;}
float hash(vec2 p){return fract(sin(dot(p,vec2(12.98,78.23)))*43758.54);}
float noise(vec2 p){
    vec2 i=floor(p);vec2 f=smoothstep(vec2(0.),vec2(1.),fract(p));
    return mix(mix(hash(i),hash(i+vec2(1.,0.)),f.x),mix(hash(i+vec2(0.,1.)),hash(i+vec2(1.)),f.x),f.y);
}
void fragment(){
    vec4 ice=texture(TEXTURE,UV);
    float chips=noise(UV*vec2(8.,15.))*.7+noise(UV*vec2(19.,31.))*.3;
    float dissolve=1.0-smoothstep(chips*.55,chips*.55+.42,fracture);
    float rim=pow(max(ice.r,max(ice.g,ice.b)),3.0);
    float flash=exp(-pow((fracture-.12)/.10,2.0))*.85*motion_amount;
    vec3 color=ice.rgb+vec3(.32,.62,.75)*rim*flash;
    float life=smoothstep(0.,.16,growth)*(1.-release);
    COLOR=vec4(color,ice.a*dissolve*life)*tint;
}
