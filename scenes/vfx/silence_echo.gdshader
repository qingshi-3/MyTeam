shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
void fragment() {
    vec2 p = (UV - 0.5) * 2.0;
    float r = length(p * vec2(1.0,1.6));
    float phase = fract(age * 0.6 * motion_amount);
    float radius = 0.5 + phase * 0.33;
    float arc = exp(-pow((r - radius) / 0.065, 2.0));
    float side = smoothstep(0.48,0.74,abs(p.x)) * (1.0 - smoothstep(0.22,0.62,abs(p.y)));
    float gate = motion_amount < 0.5 ? 0.25 : sin(phase * 3.14159);
    float alpha = arc * side * gate * 0.36 * smoothstep(0.0,0.2,age) * (1.0-release);
    COLOR = vec4(0.62,0.51,0.92,alpha);
}
