shader_type canvas_item;
render_mode unshaded, blend_add;

uniform vec4 tint : source_color = vec4(0.45, 1.0, 0.82, 1.0);
uniform vec4 frame_region = vec4(0.0, 0.0, 100.0, 100.0);
uniform float age = 0.0;
uniform float duration = 0.58;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform float strength = 0.7;

void fragment() {
    vec4 body = texture(TEXTURE, UV);
    vec2 local_uv = (UV / TEXTURE_PIXEL_SIZE - frame_region.xy) / max(frame_region.zw, vec2(1.0));
    float p = clamp(age / duration, 0.0, 1.0);
    float envelope = smoothstep(0.0, 0.065, age) * (1.0 - smoothstep(0.22, duration, age));
    // Broad, upward sweep inside the authored silhouette, not an enlarged outline.
    float center = mix(0.56, 0.79 - p * 0.72, motion_amount);
    float band = exp(-pow((local_uv.y - center) / 0.105, 2.0));
    float trailing = exp(-pow((local_uv.y - center - 0.12) / 0.19, 2.0)) * 0.25;
    float detail = mix(0.68, 1.0, dot(body.rgb, vec3(0.3, 0.55, 0.15)));
    vec3 color = mix(tint.rgb, vec3(1.0, 0.97, 0.86), band * 0.48);
    COLOR = vec4(color, body.a * detail * (0.14 + band + trailing) * envelope * strength * (1.0 - release));
}
