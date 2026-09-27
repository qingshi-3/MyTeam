shader_type canvas_item;
render_mode unshaded;

// Layout framing only: samples the untouched authored illustration and clips its window.
uniform vec2 focal_point = vec2(0.5, 0.32);
uniform float window_aspect = 1.3;
uniform vec2 window_size = vec2(320.0, 494.0);
uniform float portrait_zoom = 1.0;
uniform bool full_illustration = false;
uniform bool stone_frame = false;
uniform float bottom_occlusion = 0.0;
varying vec4 canvas_tint;

void vertex() {
    canvas_tint = COLOR;
}

void fragment() {
    float image_aspect = TEXTURE_PIXEL_SIZE.y / TEXTURE_PIXEL_SIZE.x;
    vec2 coverage = vec2(min(1.0, window_aspect / image_aspect),
                         min(1.0, image_aspect / window_aspect)) / portrait_zoom;
    vec2 center = clamp(focal_point, coverage * 0.5, vec2(1.0) - coverage * 0.5);
    vec2 window_uv = stone_frame ? (UV - vec2(0.5)) / 0.92 + vec2(0.5) : UV;
    vec4 paint = texture(TEXTURE, center + (window_uv - vec2(0.5)) * coverage);
    float radius = length((UV - vec2(0.5)) * 2.0);
    float edge = max(fwidth(radius), 0.002);
    if (stone_frame) {
        paint.a *= 1.0 - smoothstep(0.92 - edge, 0.92, radius);
    } else if (!full_illustration) {
        paint.a *= 1.0 - smoothstep(1.0 - edge, 1.0, radius);
    } else {
        // The illustration sits below a cut stone opening. Its narrow contact
        // shadow follows the opening; it does not recolor the authored figure.
        vec2 half_size = window_size * 0.5;
        vec2 local = (UV - vec2(0.5)) * window_size;
        vec2 side = abs(local) - half_size;
        float cut = min(window_size.x * 0.075, 24.0);
        float corner = (side.x + side.y + cut) * 0.70710678;
        float distance_to_edge = max(max(side.x, side.y), corner);
        float antialias = max(fwidth(distance_to_edge), 0.7);
        float inside = max(-distance_to_edge, 0.0);
        float recess = exp(-inside / 5.0);
        // A soft top-left contact shade establishes depth without a bright rim.
        float lighting = mix(0.18, 0.34, clamp(0.75 - UV.y * 0.45 - UV.x * 0.3, 0.0, 1.0));
        paint.rgb *= 1.0 - lighting * recess;
        paint.a *= 1.0 - smoothstep(-antialias, 0.0, distance_to_edge);
        // The lower edge disappears behind the authored name ribbon, so boots or
        // a rectangular image edge cannot reappear below the foreground paper.
        if (bottom_occlusion > 0.0)
            paint.a *= 1.0 - smoothstep(window_size.y - bottom_occlusion - 1.0,
                window_size.y - bottom_occlusion, UV.y * window_size.y);
    }
    COLOR = paint * canvas_tint;
}
