shader_type canvas_item;
render_mode unshaded;

void fragment() {
    float radius = length((UV - vec2(0.5)) * 2.0);
    float aa = max(fwidth(radius), 0.001);
    float edge_shade = smoothstep(0.63, 0.935, radius);
    vec3 base = mix(vec3(0.205, 0.187, 0.158), vec3(0.095, 0.086, 0.073), edge_shade);
    COLOR = vec4(base, 1.0 - smoothstep(0.93 - aa, 0.93, radius));
}
