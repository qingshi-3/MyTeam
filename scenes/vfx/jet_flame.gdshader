shader_type canvas_item;
render_mode unshaded;
uniform float age = 0.0;
uniform float particle_progress = 0.0;
uniform float motion_amount = 1.0;
varying vec4 layer_color;
void vertex() { layer_color = COLOR; }
void fragment() {
    // Authored head faces +X; the tail alone curls, without reversing travel.
    float tail = 1.0 - smoothstep(.18, .86, UV.x);
    vec2 uv = UV;
    uv.y += sin(UV.x * 11.0 - age * 8.0) * .026 * tail * motion_amount;
    uv.x += sin(UV.y * 9.0 + age * 5.0) * .009 * tail * motion_amount;
    vec4 ink = texture(TEXTURE, clamp(uv, vec2(.001), vec2(.999)));
    float end = 1.0 - smoothstep(.58, .99, particle_progress);
    COLOR = vec4(ink.rgb, pow(ink.a, 1.35) * end) * layer_color;
}
