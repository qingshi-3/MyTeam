shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
void fragment() {
    float r = length(UV - 0.5);
    float core = exp(-pow(r / mix(0.055, 0.17, progress), 2.0));
    float skirt = exp(-pow(r / 0.24, 2.0)) * 0.22;
    float fade = pow(1.0 - progress, 2.0);
    COLOR = vec4(mix(vec3(0.10, 0.50, 1.0), vec3(0.85, 0.98, 1.0), core),
        (core + skirt) * fade * (1.0 - release) * mix(0.3, 1.0, motion_amount));
}
