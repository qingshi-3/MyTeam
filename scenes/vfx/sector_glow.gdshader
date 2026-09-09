shader_type canvas_item;
render_mode unshaded, blend_add;
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
    float head=1.0-smoothstep(.015,.065,passed);
    float band=exp(-pow((UV.y-.90)/.09,2.0));
    float tips=smoothstep(0.0,.075,UV.x)*(1.0-smoothstep(.96,1.0,UV.x));
    COLOR=vec4(1.0,.70,.32,band*(.38+head*.65)*born*fade*tips*(1.0-release)*mix(.3,1.0,motion_amount));
}
