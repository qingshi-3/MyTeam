shader_type canvas_item;

uniform float discovery : hint_range(0.0, 1.0) = 0.0;
varying vec4 canvas_tint;
void vertex() { canvas_tint = COLOR; }

void fragment() {
    vec4 icon = texture(TEXTURE, UV);
    // One narrow sweep over the opaque icon, never over the name beneath it.
    float front = mix(-0.35, 1.35, discovery);
    float sweep = exp(-pow((UV.x + UV.y * 0.22 - front) / 0.11, 2.0)) *
        sin(discovery * 3.14159);
    icon.rgb += vec3(0.40, 0.36, 0.22) * sweep;
    COLOR = icon * canvas_tint;
}
