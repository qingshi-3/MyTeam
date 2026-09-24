shader_type canvas_item;
render_mode unshaded, blend_add;
void fragment() {
    float width = 0.02 + UV.x * 0.19;
    float side = exp(-pow((UV.y - 0.5) / width, 2.0) * 3.0);
    float core = exp(-pow((UV.y - 0.5) / (0.008 + UV.x * 0.025), 2.0));
    float tail = pow(UV.x, 1.4) * (1.0 - smoothstep(0.82, 1.0, UV.x));
    COLOR = vec4(mix(vec3(1.0, 0.37, 0.04), vec3(1.0, 0.96, 0.76), core), (side * 0.52 + core) * tail);
}
