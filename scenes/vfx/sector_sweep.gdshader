shader_type canvas_item;
render_mode unshaded, blend_mix;
uniform float age = 0.0;
uniform float sweep_duration = .3;
uniform float tail_duration = .24;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
void fragment() {
    float arrival=(1.0-sqrt(max(0.0,1.0-UV.x)))*sweep_duration;
    float passed=mix(age,age-arrival,motion_amount);
    float born=smoothstep(0.0,.018,passed);
    float fade=1.0-smoothstep(.05,1.0,clamp(passed/tail_duration,0.0,1.0));
    float head=1.0-smoothstep(.015,.08,passed);
    // The source image contributes brush detail only. Polar geometry and the
    // radial mask supply the broad blade silhouette, not the old slash outline.
    vec4 ink=texture(TEXTURE,vec2(fract(UV.x*1.8-age*.16*motion_amount),mix(.31,.69,UV.y)));
    float grain=dot(ink.rgb,vec3(.3,.5,.2))*.65+ink.a*.35;
    float radial=UV.y;
    float outer=exp(-pow((radial-.88)/.025,2.0));
    float inner=smoothstep(.0,.40,radial)*(1.0-smoothstep(.78,1.0,radial));
    float shape=(outer*.98+inner*(.10+grain*grain*.35));
    float tips=smoothstep(0.0,.075,UV.x)*(1.0-smoothstep(.96,1.0,UV.x));
    vec3 color=mix(vec3(.72,.52,.29),vec3(1.0,.88,.63),radial);
    color=mix(color,vec3(1.0,.98,.90),outer*.90+head*.08);
    float alpha=shape*born*fade*tips*(1.0-release);
    COLOR=vec4(color,alpha);
}
