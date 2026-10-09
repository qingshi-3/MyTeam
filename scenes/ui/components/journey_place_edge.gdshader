shader_type canvas_item;
render_mode unshaded, blend_add;

uniform vec2 extent = vec2(600.0, 300.0);
uniform int edge = 0;
uniform float progress = 0.0;

void fragment() {
    vec2 p = UV * extent;
    bool vertical = edge < 2;
    float across = edge == 0 ? p.x : edge == 1 ? extent.x - p.x
        : edge == 2 ? p.y : extent.y - p.y;
    float along = vertical ? UV.y : UV.x;
    // The rect extends 14 px beyond the surface. Light rests on its bevel,
    // with one traveling highlight and a broad, weaker halo outside it.
    float distance_to_edge = abs(across - 14.0);
    float core = exp(-distance_to_edge * distance_to_edge / 6.0);
    float halo = exp(-distance_to_edge * distance_to_edge / 85.0);
    float center = mix(0.22, 0.78, progress);
    float streak = exp(-pow((along - center) / 0.19, 2.0));
    float envelope = sin(clamp(progress, 0.0, 1.0) * 3.14159265);
    float ends = smoothstep(0.06, 0.19, along) * (1.0 - smoothstep(0.81, 0.94, along));
    float energy = (core * 0.58 + halo * 0.23) * streak * envelope * ends;
    COLOR = vec4(mix(vec3(1.0, 0.65, 0.27), vec3(1.0, 0.92, 0.68), core), energy);
}
