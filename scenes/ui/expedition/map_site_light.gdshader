shader_type canvas_item;
varying vec4 canvas_tint;
void vertex() { canvas_tint = COLOR; }

uniform vec2 cell_size = vec2(56.0);
uniform float focus_strength : hint_range(0.0, 1.0) = 0.0;
uniform float warm_strength : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    vec2 edge_px = min(UV, vec2(1.0) - UV) * cell_size;
    float inset = min(edge_px.x, edge_px.y);
    float rim = exp(-pow((inset - 2.2) / 1.0, 2.0));
    float shoulder = exp(-pow((inset - 2.2) / 3.0, 2.0));
    float focus = (rim * 0.55 + shoulder * 0.14) * focus_strength;
    vec2 icon_space = (UV - vec2(0.5, 0.36)) / vec2(0.30, 0.27);
    float warm = exp(-dot(icon_space, icon_space) * 2.4) * warm_strength * 0.22;
    float alpha = focus + warm;
    vec3 rgb = (vec3(0.93, 0.97, 1.0) * focus + vec3(1.0, 0.77, 0.40) * warm) / max(alpha, 0.0001);
    COLOR = vec4(rgb, alpha) * canvas_tint;
}
