shader_type canvas_item;
render_mode unshaded, blend_add;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float motion_amount = 1.0;
varying vec4 tint;
void vertex() { tint = COLOR; }
void fragment() {
    // Only the fine filaments move; particle motion owns the main upward impulse.
    vec2 uv = UV;
    uv.x += sin(UV.y * 19.0 + age * 8.0) * .009 * motion_amount;
    vec4 ink = texture(TEXTURE, uv);
    float threads = .5 + .5 * sin(uv.x * 69.0 + sin(uv.y * 21.0 - age * 5.0 * motion_amount));
    float dissolve = smoothstep(progress * .94, progress * .94 + .16, (1.0 - uv.y) * .64 + threads * .36);
    float life = smoothstep(0.0, .075, progress) * (1.0 - smoothstep(.50, 1.0, progress));
    COLOR = vec4(ink.rgb, ink.a * dissolve * life) * tint;
}
