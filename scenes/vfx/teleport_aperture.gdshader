shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
uniform bool departure = false;
void fragment() {
    vec2 p = (UV - 0.5)*2.0;
    float r = length(p);
    float angle = atan(p.y,p.x);
    float t = clamp(age/(departure ? 0.42 : 0.5),0.0,1.0);
    float radius = departure ? mix(0.84,0.19,t*t) : mix(0.22,0.84,1.0-pow(1.0-t,3.0));
    if(motion_amount<0.5) radius = 0.68;
    float edge = exp(-pow((r-radius)/0.052,2.0));
    float halo = exp(-pow((r-radius)/0.15,2.0))*0.25;
    float detail = 0.76+0.24*sin(angle*11.0 + age*4.0*motion_amount + r*25.0);
    float opening = smoothstep(0.0,0.09,age);
    float fade = departure ? 1.0 : 1.0-smoothstep(0.38,0.78,age);
    float spark = pow(max(0.0,sin(angle*7.0-age*2.7*motion_amount)),18.0)*edge*0.3;
    float alpha = ((edge+halo)*detail+spark)*opening*fade*(1.0-release);
    COLOR = vec4(mix(vec3(0.26,0.48,0.93),vec3(0.75,0.9,1.0),edge),alpha*0.85);
}
