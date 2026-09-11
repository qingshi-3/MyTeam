shader_type canvas_item;
// The authored plume carries a colored body and transparent light edges.
// Alpha blending preserves its gold on light stages; FootLight supplies the
// separate additive flash instead of clipping every overlapping plume white.
render_mode unshaded, blend_mix;
uniform float age = 0.0;
uniform float progress = 0.0;
uniform float motion_amount = 1.0;
varying vec4 tint;
void vertex() { tint = COLOR; }
void fragment() {
    // The broad plume carries the impulse. A few tail openings soften its
    // release without slicing the texture into narrow electrical filaments.
    vec2 uv = UV;
    uv.x += sin(UV.y * 9.0 + age * 5.0) * .004 * motion_amount;
    vec4 ink = texture(TEXTURE, uv);
    float folds = .5 + .5 * sin(uv.x * 19.0 + sin(uv.y * 7.0 - age * 3.0 * motion_amount));
    float dissolve = smoothstep(progress * .9, progress * .9 + .2, (1.0 - uv.y) * .76 + folds * .24);
    float life = smoothstep(0.0, .075, progress) * (1.0 - smoothstep(.50, 1.0, progress));
    COLOR = vec4(ink.rgb, ink.a * dissolve * life) * tint;
}
