shader_type canvas_item;
render_mode unshaded;

#include "res://scenes/environments/courtyard_lighting.gdshaderinc"

uniform vec4 frame_uv = vec4(0.0, 0.0, 1.0, 1.0);
uniform vec2 rect_center = vec2(0.0);
uniform float outline_width : hint_range(0.0, 2.0) = 1.0;
uniform vec4 outline_color : source_color = vec4(0.10, 0.09, 0.14, 0.65);
uniform float hit_amount : hint_range(0.0, 1.0) = 0.0;
uniform bool environment_enabled = false;
uniform vec2 environment_uv = vec2(0.5);
varying vec4 tint;

void vertex() {
    tint = COLOR;
    VERTEX += sign(VERTEX - rect_center) * outline_width;
    UV += sign(UV - (frame_uv.xy + frame_uv.zw) * 0.5) * TEXTURE_PIXEL_SIZE * outline_width;
}

bool inside_frame(vec2 point, vec2 half_texel) {
    return !any(lessThan(point, frame_uv.xy + half_texel)) &&
        !any(greaterThan(point, frame_uv.zw - half_texel));
}

void fragment() {
    vec4 body = inside_frame(UV, TEXTURE_PIXEL_SIZE * 0.5) ? texture(TEXTURE, UV) : vec4(0.0);
    vec2 step_uv = TEXTURE_PIXEL_SIZE * outline_width;
    float edge = 0.0;
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            vec2 point = UV + vec2(float(x), float(y)) * step_uv;
            float sample_alpha = inside_frame(point, TEXTURE_PIXEL_SIZE * 0.5) ? texture(TEXTURE, point).a : 0.0;
            edge = max(edge, sample_alpha);
        }
    }
    float border_alpha = edge * (1.0 - body.a) * outline_color.a * step(0.01, outline_width);
    float alpha = body.a + border_alpha;
    vec3 illumination = vec3(1.0);
    if (environment_enabled) {
        // Same spatial field and clock as the paving, with a gentler contrast on actors.
        illumination = mix(vec3(0.78, 0.84, 0.83), vec3(1.14, 1.08, 0.94), courtyard_sunlight(environment_uv));
    }
    vec3 body_rgb = mix(body.rgb * tint.rgb * illumination, vec3(1.0, 0.91, 0.77), hit_amount);
    vec3 rgb = (body_rgb * body.a + outline_color.rgb * tint.rgb * border_alpha) / max(alpha, 0.0001);
    COLOR = vec4(rgb, alpha * tint.a);
}
