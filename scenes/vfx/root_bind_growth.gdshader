shader_type canvas_item;
render_mode unshaded, blend_mix;
uniform float growth = 0.0;
uniform float age = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform vec4 tint : source_color = vec4(1.0);
uniform vec2 atlas_x = vec2(0.0, 1.0);
uniform vec2 atlas_y = vec2(0.0, 1.0);
uniform float alpha_power = 1.0;

void fragment() {
    float u = 1.0 - UV.y;
    float tip = 1.0 - smoothstep(growth - 0.055, growth + 0.005, u);
    // The un-grown cutoff narrows before fading; the path itself never rotates.
    float tip_width = mix(1.0, 0.18, smoothstep(growth - 0.07, growth, u));
    vec2 sample_uv = vec2((UV.x - 0.5) / max(tip_width, 0.08) + 0.5, UV.y);
    vec2 asset_uv = vec2(mix(atlas_x.x, atlas_x.y, sample_uv.x), mix(atlas_y.x, atlas_y.y, sample_uv.y));
    vec4 tex = texture(TEXTURE, asset_uv);
    tex.a = pow(tex.a, alpha_power);
    float valid = step(0.0, sample_uv.x) * step(sample_uv.x, 1.0);
    float born = smoothstep(0.0, 0.025, u);
    float highlight = exp(-pow((u - growth + 0.018) / 0.055, 2.0)) * (1.0 - release);
    float flow = 0.5 + 0.5 * sin(u * 22.0 - age * 2.0 * motion_amount);
    vec3 color = tex.rgb * tint.rgb * (0.93 + flow * 0.1) + vec3(0.27, 0.42, 0.09) * highlight * tex.a;
    COLOR = vec4(color, tex.a * tip * valid * born * (1.0 - release) * tint.a);
}
