shader_type canvas_item;
render_mode unshaded;

// One shared edge veil; no per-entry panels and no wash over the central portrait.
void fragment() {
    vec4 tint = COLOR;
    vec2 edge = min(UV, vec2(1.0) - UV);
    float side = 1.0 - smoothstep(0.07, 0.38, edge.x);
    float top = 1.0 - smoothstep(0.025, 0.23, UV.y);
    float bottom = 1.0 - smoothstep(0.015, 0.12, 1.0 - UV.y);
    float vertical_fade = smoothstep(0.0, 0.08, UV.y) * smoothstep(0.0, 0.10, 1.0 - UV.y);
    float feather = smoothstep(0.0, 0.035, edge.x) * smoothstep(0.0, 0.025, edge.y);
    float alpha = max(side * vertical_fade * 0.78, max(top * 0.66, bottom * 0.48));
    COLOR = vec4(vec3(0.0), alpha * feather * tint.a);
}
