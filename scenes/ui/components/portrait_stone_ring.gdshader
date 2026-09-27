shader_type canvas_item;
render_mode unshaded;

// Remap the supplied annulus to a narrow inner frame; the original pixels and
// hand-painted stone joints remain the source, independent of the card aspect.
void fragment() {
    vec2 p = (UV - vec2(0.5)) * 2.0;
    float r = length(p);
    float source_r = mix(0.695, 0.942, (r - 0.91) / 0.09);
    vec2 source_uv = vec2(0.5) + p / max(r, 0.001) * source_r * 0.5;
    vec4 stone = texture(TEXTURE, source_uv);
    float aa = max(fwidth(r), 0.001);
    stone.a *= smoothstep(0.905, 0.91, r) * (1.0 - smoothstep(1.0 - aa, 1.0, r));
    COLOR = stone;
}
