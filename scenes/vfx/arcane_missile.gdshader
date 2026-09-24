shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float release = 0.0;
uniform float motion_amount = 1.0;
uniform bool sustained = false;
varying vec4 tint;
void vertex() { tint = COLOR; }
void fragment() {
    float clock = max(age, 0.0) * motion_amount;
    vec2 uv = UV;
    // The head remains on the trajectory; only the thin, trailing material sways.
    float tail = 1.0 - smoothstep(0.30, 0.77, UV.x);
    uv.y += tail * sin(UV.x * 11.0 - clock * 9.0) * 0.018 * motion_amount;
    vec4 ink = texture(TEXTURE, clamp(uv, vec2(0.001), vec2(0.999)));
    float value = dot(ink.rgb, vec3(0.22, 0.52, 0.26));
    vec3 color = mix(vec3(0.10, 0.34, 0.85), vec3(0.46, 0.83, 1.0), smoothstep(0.25, 0.67, value));
    color = mix(color, vec3(0.91, 0.98, 1.0), smoothstep(0.73, 0.97, value));
    float flow = 0.90 + 0.10 * sin(UV.x * 19.0 - clock * 12.0);
    COLOR = vec4(color * flow, ink.a * (1.0 - release)) * tint;
}
