shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
varying vec4 tint;
void vertex() { tint = COLOR; }
float wave(float radius, float phase) {
    float envelope = pow(sin(phase * 3.14159265), 2.0);
    return exp(-pow((radius - mix(0.20, 0.94, phase)) * 19.0, 2.0)) * envelope;
}
void fragment() {
    vec2 p = (UV - 0.5) * 2.0;
    float r = length(p);
    float t = motion_amount < 0.5 ? 0.5 : age * 0.23;
    float edge = exp(-pow((r - 0.93) * 40.0, 2.0));
    float soft_edge = exp(-pow((r - 0.91) * 12.0, 2.0)) * 0.20;
    float breathing = 0.85 + 0.15 * sin(t * 6.2831853);
    float ripples = wave(r, fract(t)) + wave(r, fract(t + 0.5)) * 0.62;
    float fill = (1.0 - smoothstep(0.50, 0.95, r)) * 0.045;
    float alpha = (edge * 0.42 + soft_edge + ripples * 0.18 + fill) * breathing;
    alpha *= smoothstep(0.0, 0.22, age) * (1.0 - smoothstep(0.97, 1.0, r)) * (1.0 - release);
    COLOR = vec4(mix(vec3(0.12, 0.67, 0.34), vec3(0.47, 1.0, 0.67), edge), alpha) * tint;
}
