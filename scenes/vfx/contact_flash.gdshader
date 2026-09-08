shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
void fragment() {
    vec2 p = (UV - 0.5) * 2.0;
    float radial = length(p);
    float core = exp(-radial * radial * 65.0);
    float halo = exp(-radial * radial * 8.0) * 0.45;
    float fade = pow(max(0.0, 1.0 - progress), 2.2);
    vec3 color = mix(vec3(1.0, 0.22, 0.16), vec3(1.0, 0.92, 0.72), core);
    COLOR = vec4(color, (core + halo) * fade * (1.0 - release) * mix(0.45, 1.0, motion_amount));
}
