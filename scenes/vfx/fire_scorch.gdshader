shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
void fragment() {
    vec2 p = UV - 0.5;
    float r = length(p);
    float grain = sin(p.x * 63.0 + sin(p.y * 41.0)) * sin(p.y * 55.0 + p.x * 17.0) * 0.5 + 0.5;
    float edge = 1.0 - smoothstep(0.25 + grain * 0.025, 0.40, r);
    float reveal = smoothstep(0.0, 0.10, age);
    vec3 ember = mix(vec3(0.055, 0.012, 0.004), vec3(0.50, 0.065, 0.008), grain * (1.0 - progress));
    COLOR = vec4(ember, edge * reveal * (1.0 - progress) * 0.45 * (1.0 - release));
}
