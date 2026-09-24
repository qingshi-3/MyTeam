shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float flow_duration = 0.68;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool local_gather = false;
uniform float strand_phase = 0.0;
uniform vec4 tint : source_color = vec4(0.7,0.1,0.3,1.0);
void fragment() {
    float clock = age * motion_amount;
    float p = age / max(flow_duration,0.05);
    float grain = texture(TEXTURE,vec2(fract(UV.x*1.7-clock*0.65+strand_phase),0.24+UV.y*0.52)).a;
    float y = UV.y-0.5 + sin(UV.x*17.0-clock*2.0+strand_phase*9.0)*0.09*motion_amount;
    float cross_section = exp(-y*y*72.0);
    float pulses = pow(0.5+0.5*cos((UV.x-clock/max(flow_duration,0.05)+strand_phase)*6.283185*3.0),5.0);
    float band = local_gather ? smoothstep(p-0.38,p-0.10,UV.x)*(1.0-smoothstep(p-0.05,p+0.08,UV.x)) : 0.32+pulses*0.55;
    if(local_gather && motion_amount<0.5) band = (1.0-smoothstep(0.15,0.7,age))*0.4;
    float end_fade = smoothstep(0.0,0.05,UV.x)*(1.0-smoothstep(0.94,1.0,UV.x));
    float cutoff = smoothstep(release-0.05,release+0.05,UV.x);
    float alpha = cross_section*(0.4+grain*0.6)*band*end_fade*cutoff*smoothstep(0.0,0.1,age)*(1.0-release);
    COLOR = vec4(mix(tint.rgb,tint.rgb*0.55+vec3(0.45),cross_section*pulses*0.24),alpha*0.72*tint.a);
}
