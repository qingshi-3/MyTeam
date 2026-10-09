shader_type canvas_item;

uniform float reveal : hint_range(0.0, 1.0) = 0.0;
uniform vec2 reveal_origin = vec2(0.5);
uniform float reveal_reach = 2.4;
varying vec4 canvas_tint;

void vertex() { canvas_tint = COLOR; }

void fragment() {
    vec4 fog = texture(TEXTURE, UV);
    float front = mix(-0.15, reveal_reach, reveal);
    vec2 relative = UV - reveal_origin;
    float distance_from_party = length(relative);
    // Coordinates are relative to the supplied party origin: the front crosses
    // the newly revealed ground, rather than lighting every cell's perimeter.
    float fragments = sin(relative.x * 33.0 + relative.y * 17.0) *
        sin(relative.y * 29.0 - relative.x * 11.0);
    float rough_front = front + fragments * 0.018 * sin(reveal * 3.14159);
    float coverage = smoothstep(rough_front - 0.16, rough_front + 0.05, distance_from_party);
    float edge = exp(-pow((distance_from_party - rough_front + 0.035) / 0.048, 2.0));
    float broken_edge = smoothstep(-0.6, 0.55, fragments);
    float light = edge * (0.12 + 0.23 * broken_edge) * sin(reveal * 3.14159);
    // Sparse detached flecks dissolve just behind the moving front. Their
    // support is the front itself, so unrevealed cells never sparkle in place.
    float grains = sin(relative.x * 79.0 - reveal * 4.0) * sin(relative.y * 71.0 + reveal * 2.5);
    float flecks = smoothstep(0.86, 0.98, grains) *
        exp(-pow((distance_from_party - rough_front + 0.10) / 0.085, 2.0)) *
        sin(reveal * 3.14159) * 0.22;
    light += flecks;
    vec3 tint = mix(fog.rgb, vec3(0.91, 0.90, 0.72), min(light * 1.5, 0.62));
    COLOR = vec4(tint, clamp(fog.a * coverage + light * (1.0 - coverage), 0.0, 1.0)) * canvas_tint;
}
