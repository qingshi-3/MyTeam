shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
void fragment() {
    vec2 p = (UV - 0.5) * 2.0;
    float t = clamp(age / 0.38, 0.0, 1.0);
    float front = mix(0.82,-0.82,1.0-pow(1.0-t,2.0));
    float height = p.y + 0.20*p.x*p.x;
    float scan = exp(-pow((height-front)/0.11,2.0));
    float mist = exp(-pow((height-front-0.17)/0.3,2.0))*0.18;
    float width = 1.0 - smoothstep(0.42,0.86,abs(p.x));
    float grain = 0.93+0.07*sin(p.x*8.0+p.y*5.0-age*7.0*motion_amount);
    float alpha = (scan+mist)*width*grain*smoothstep(0.0,0.065,age)*(1.0-smoothstep(0.30,0.50,age));
    if(motion_amount<0.5) alpha = exp(-dot(p*vec2(1.4,0.75),p*vec2(1.4,0.75))*3.0)*0.4*(1.0-progress);
    alpha *= (1.0-smoothstep(0.84,1.0,abs(p.y)))*(1.0-release);
    COLOR = vec4(mix(vec3(0.22,0.77,0.71),vec3(0.88,1.0,0.92),scan),alpha*0.85);
}
